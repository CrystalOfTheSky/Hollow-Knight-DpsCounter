using System;
using System.Collections.Generic;
using System.Reflection;
using Modding;
using UnityEngine;
using UnityEngine.UI;

namespace DpsCounterMod
{
    /// <summary>
    /// Counts damage dealt by the player to enemies and shows rolling DPS on screen.
    /// </summary>
    public class DpsCounter : Mod, IGlobalSettings<DpsCounterSettings>, ITogglableMod, IMenuMod
    {
        /// <summary>The active mod instance.</summary>
        public static DpsCounter Instance { get; private set; }

        /// <summary>Live settings, editable from the generated JSON file.</summary>
        public DpsCounterSettings Settings { get; private set; } = new DpsCounterSettings();

        private static readonly string[] OnOffValues = { "Off", "On" };
        private static readonly string[] WindowValues = { "1s", "2s", "3s", "5s", "10s" };
        private static readonly float[] WindowSecondsValues = { 1f, 2f, 3f, 5f, 10f };
        private static readonly string[] KeyOptions =
        {
            "None", "F5", "F6", "F7", "F8", "F9",
            "Keypad0", "Keypad1", "Keypad2", "Keypad3", "Keypad4",
            "Keypad5", "Keypad6", "Keypad7", "Keypad8", "Keypad9"
        };
        private static readonly string[] HudCornerOptions =
        {
            "Top-Right", "Top-Left", "Bottom-Right", "Bottom-Left"
        };
        private static readonly string[] MultiTargetOptions = { "Single target", "All targets" };
        private static FieldInfo _flukeDamageField;

        private readonly List<DamageSample> _samples = new List<DamageSample>();
        private readonly Dictionary<GameObject, AttackGroup> _attackGroups =
            new Dictionary<GameObject, AttackGroup>();
        private readonly List<GameObject> _staleAttackGroups = new List<GameObject>();
        private long _totalDamage;
        private float _maxDps;
        private int _lastExtraDamageFrame = -1;

        private GameObject _canvas;
        private Text _dpsText;
        private Text _maxText;
        private bool _hooked;
        private bool _hasToggleKey;
        private KeyCode _toggleKey;
        private bool _hasResetMaxKey;
        private KeyCode _resetMaxKey;
        private bool _inSpellFlukeHook;

        public DpsCounter() : base("DPS Counter")
        {
        }

        public override string GetVersion() => Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public void OnLoadGlobal(DpsCounterSettings settings)
        {
            if (settings != null)
            {
                Settings = settings;
            }

            RefreshHotkeys();
        }

        public DpsCounterSettings OnSaveGlobal() => Settings;

        public bool ToggleButtonInsideMenu => true;

        public override void Initialize()
        {
            if (Instance != null)
            {
                return;
            }

            Instance = this;
            Hook();
            RefreshHotkeys();
            Log($"DPS Counter v{GetVersion()} initialized. Rolling window: {Settings.WindowSeconds:0.#}s.");
        }

        public List<IMenuMod.MenuEntry> GetMenuData(IMenuMod.MenuEntry? toggleButtonEntry)
        {
            var entries = new List<IMenuMod.MenuEntry>();

            if (toggleButtonEntry.HasValue)
            {
                entries.Add(toggleButtonEntry.Value);
            }

            entries.Add(
                new IMenuMod.MenuEntry(
                    "Show HUD",
                    OnOffValues,
                    "Show or hide the DPS HUD. This is what the toggle hotkey changes.",
                    index => Settings.Enabled = index == 1,
                    () => Settings.Enabled ? 1 : 0
                )
            );

            entries.Add(
                new IMenuMod.MenuEntry(
                    "Toggle Key",
                    KeyOptions,
                    "Key pressed during gameplay to show/hide the DPS HUD.",
                    index =>
                    {
                        Settings.ToggleKey = KeyOptions[index];
                        RefreshHotkeys();
                    },
                    () =>
                    {
                        int index = Array.IndexOf(KeyOptions, Settings.ToggleKey);
                        return index < 0 ? 1 : index;
                    }
                )
            );

            entries.Add(
                new IMenuMod.MenuEntry(
                    "Reset Max Key",
                    KeyOptions,
                    "Key pressed during gameplay to reset the peak DPS value.",
                    index =>
                    {
                        Settings.ResetMaxKey = KeyOptions[index];
                        RefreshHotkeys();
                    },
                    () =>
                    {
                        int index = Array.IndexOf(KeyOptions, Settings.ResetMaxKey);
                        return index < 0 ? 2 : index;
                    }
                )
            );

            entries.Add(
                new IMenuMod.MenuEntry(
                    "Counting Window",
                    WindowValues,
                    "Length of the rolling average window used to compute DPS.",
                    index => Settings.WindowSeconds = WindowSecondsValues[index],
                    () =>
                    {
                        int index = Array.IndexOf(WindowSecondsValues, Settings.WindowSeconds);
                        return index < 0 ? 2 : index;
                    }
                )
            );

            entries.Add(
                new IMenuMod.MenuEntry(
                    "Generic Damage",
                    OnOffValues,
                    "Also count Generic damage that has no identifiable player source.",
                    index => Settings.CountGenericDamage = index == 1,
                    () => Settings.CountGenericDamage ? 1 : 0
                )
            );

            entries.Add(
                new IMenuMod.MenuEntry(
                    "Multi-Target Damage",
                    MultiTargetOptions,
                    "Single target counts one enemy's damage per attack; All targets sums damage across every enemy hit.",
                    index => Settings.CountDamageToAllTargets = index == 1,
                    () => Settings.CountDamageToAllTargets ? 1 : 0
                )
            );

            entries.Add(
                new IMenuMod.MenuEntry(
                    "HUD Corner",
                    HudCornerOptions,
                    "Which screen corner the DPS counter is attached to.",
                    index =>
                    {
                        ApplyHudCorner(index);
                        DestroyHud();
                    },
                    GetHudCornerIndex
                )
            );

            return entries;
        }

        public void Unload()
        {
            Unhook();
            DestroyHud();
            _samples.Clear();
            _attackGroups.Clear();
            _lastExtraDamageFrame = -1;
            _totalDamage = 0;
            _maxDps = 0f;
            Instance = null;
        }

        private void Hook()
        {
            if (_hooked)
            {
                return;
            }

            On.HealthManager.TakeDamage += OnTakeDamage;
            On.ExtraDamageable.ApplyExtraDamageToHealthManager += OnExtraDamageApplied;
            On.SpellFluke.DoDamage += OnSpellFlukeDamage;
            ModHooks.HeroUpdateHook += OnHeroUpdate;
            ModHooks.SceneChanged += OnSceneChanged;
            _hooked = true;
        }

        private void Unhook()
        {
            if (!_hooked)
            {
                return;
            }

            On.HealthManager.TakeDamage -= OnTakeDamage;
            On.ExtraDamageable.ApplyExtraDamageToHealthManager -= OnExtraDamageApplied;
            On.SpellFluke.DoDamage -= OnSpellFlukeDamage;
            ModHooks.HeroUpdateHook -= OnHeroUpdate;
            ModHooks.SceneChanged -= OnSceneChanged;
            _hooked = false;
        }

        private void RefreshHotkeys()
        {
            _hasToggleKey =
                !string.IsNullOrEmpty(Settings.ToggleKey) &&
                Settings.ToggleKey != "None" &&
                Enum.TryParse(Settings.ToggleKey, out _toggleKey);

            _hasResetMaxKey =
                !string.IsNullOrEmpty(Settings.ResetMaxKey) &&
                Settings.ResetMaxKey != "None" &&
                Enum.TryParse(Settings.ResetMaxKey, out _resetMaxKey);
        }

        private void HandleToggleKey()
        {
            if (!_hasToggleKey)
            {
                return;
            }

            if (Input.GetKeyDown(_toggleKey))
            {
                Settings.Enabled = !Settings.Enabled;
                Log($"DPS HUD toggled {(Settings.Enabled ? "on" : "off")} via {_toggleKey}.");

                if (!Settings.Enabled && _canvas != null && _canvas.activeSelf)
                {
                    _canvas.SetActive(false);
                }
            }
        }

        private void HandleResetMaxKey()
        {
            if (!_hasResetMaxKey)
            {
                return;
            }

            if (Input.GetKeyDown(_resetMaxKey))
            {
                _maxDps = 0f;
                Log($"DPS peak reset via {_resetMaxKey}.");
            }
        }

        private void OnTakeDamage(On.HealthManager.orig_TakeDamage orig, HealthManager self, HitInstance hitInstance)
        {
            orig(self, hitInstance);

            if (!Settings.Enabled || hitInstance.DamageDealt <= 0 || !IsPlayerDamage(hitInstance))
            {
                return;
            }

            int damage = Mathf.RoundToInt(hitInstance.DamageDealt * Mathf.Max(hitInstance.Multiplier, 0f));
            if (damage <= 0)
            {
                return;
            }

            RecordDamage(damage, hitInstance.Source);
        }

        private void OnExtraDamageApplied(
            On.ExtraDamageable.orig_ApplyExtraDamageToHealthManager orig,
            ExtraDamageable self,
            int damageAmount)
        {
            orig(self, damageAmount);

            if (!Settings.Enabled || damageAmount <= 0)
            {
                return;
            }

            RecordExtraDamage(damageAmount);
        }

        private void OnSpellFlukeDamage(
            On.SpellFluke.orig_DoDamage orig,
            SpellFluke self,
            GameObject target,
            int upwardRecursionAmount,
            bool burst)
        {
            // SpellFluke.DoDamage subtracts health directly instead of going
            // through HealthManager.TakeDamage, and recursively walks up the
            // object's parent chain. Only the outermost call should count, so
            // nested invocations are passed straight through.
            if (_inSpellFlukeHook)
            {
                orig(self, target, upwardRecursionAmount, burst);
                return;
            }

            // Mirror the vanilla checks ahead of time: each level in the chain
            // loses `damage` HP unless it is dead, or invincible and not marked
            // Spell Vulnerable (which makes the whole call stop early).
            int damage = GetFlukeDamage(self);
            int totalDamage = 0;
            GameObject current = target;
            int depth = Mathf.Max(1, upwardRecursionAmount);

            for (int i = 0; i < depth && current != null; i++)
            {
                HealthManager health = current.GetComponent<HealthManager>();

                if (health != null && !health.isDead)
                {
                    bool spellVulnerable = current.CompareTag("Spell Vulnerable");
                    if (!health.IsInvincible || spellVulnerable)
                    {
                        totalDamage += damage;
                    }
                    else
                    {
                        // Vanilla returns from the whole damage call here.
                        break;
                    }
                }

                current = current.transform.parent != null
                    ? current.transform.parent.gameObject
                    : null;
            }

            _inSpellFlukeHook = true;
            try
            {
                orig(self, target, upwardRecursionAmount, burst);
            }
            finally
            {
                _inSpellFlukeHook = false;
            }

            if (totalDamage > 0 && Settings.Enabled)
            {
                RecordDamage(totalDamage, self.gameObject);
            }
        }

        private static int GetFlukeDamage(SpellFluke fluke)
        {
            if (_flukeDamageField == null)
            {
                _flukeDamageField = typeof(SpellFluke).GetField(
                    "damage",
                    BindingFlags.Instance | BindingFlags.NonPublic
                );
            }

            return _flukeDamageField == null ? 0 : (int)_flukeDamageField.GetValue(fluke);
        }

        private bool IsPlayerDamage(HitInstance hit)
        {
            switch (hit.AttackType)
            {
                case AttackTypes.Nail:
                case AttackTypes.NailBeam:
                case AttackTypes.Spell:
                case AttackTypes.SharpShadow:
                    return true;

                case AttackTypes.Generic:
                    return Settings.CountGenericDamage || IsPlayerGenericSource(hit.Source);

                default:
                    return false;
            }
        }

        private static bool IsPlayerGenericSource(GameObject source)
        {
            if (source == null)
            {
                // Environment/cutscene kills that fabricate a huge Generic hit
                // (e.g. EnemyKillEventListener) carry no source. Excluding them
                // keeps a single fake kill from distorting the DPS.
                return false;
            }

            // Generic damage that carries a real source object is player charm
            // or companion damage in practice (Grimmchild, Weaversong,
            // Dreamshield, Spore/Dung clouds, etc.).
            return true;
        }

        private void RecordDamage(int damage, GameObject source)
        {
            if (!Settings.CountDamageToAllTargets && source != null)
            {
                AddGroupedDamageSample(source, damage);
                return;
            }

            AddDamageSample(damage);
        }

        private void RecordExtraDamage(int damage)
        {
            if (!Settings.CountDamageToAllTargets)
            {
                int frame = Time.frameCount;
                if (_lastExtraDamageFrame == frame)
                {
                    return;
                }

                _lastExtraDamageFrame = frame;
            }

            AddDamageSample(damage);
        }

        private void AddGroupedDamageSample(GameObject source, int damage)
        {
            int frame = Time.frameCount;

            if (_attackGroups.TryGetValue(source, out AttackGroup group))
            {
                if (group.Frame == frame)
                {
                    // The same attack already added a sample this frame; in
                    // Single-target mode additional enemies are not counted.
                    return;
                }

                _attackGroups[source] = new AttackGroup(frame);
            }
            else
            {
                _attackGroups.Add(source, new AttackGroup(frame));
            }

            AddDamageSample(damage);
        }

        private void AddDamageSample(int damage)
        {
            _samples.Add(new DamageSample(Time.time, damage));
            _totalDamage += damage;
        }

        private void OnHeroUpdate()
        {
            HandleToggleKey();
            HandleResetMaxKey();

            if (!Settings.Enabled)
            {
                if (_canvas != null && _canvas.activeSelf)
                {
                    _canvas.SetActive(false);
                }

                return;
            }

            if (_canvas == null)
            {
                CreateHud();
            }

            if (_canvas == null)
            {
                return; // HUD resources not available yet; retry next frame.
            }

            _canvas.SetActive(true);
            UpdateText();
        }

        private void ApplyHudCorner(int index)
        {
            switch (index)
            {
                case 1:
                    Settings.HudAnchorX = 0f;
                    Settings.HudAnchorY = 1f;
                    Settings.HudOffsetX = 20f;
                    Settings.HudOffsetY = -20f;
                    break;

                case 2:
                    Settings.HudAnchorX = 1f;
                    Settings.HudAnchorY = 0f;
                    Settings.HudOffsetX = -20f;
                    Settings.HudOffsetY = 20f;
                    break;

                case 3:
                    Settings.HudAnchorX = 0f;
                    Settings.HudAnchorY = 0f;
                    Settings.HudOffsetX = 20f;
                    Settings.HudOffsetY = 20f;
                    break;

                default:
                    Settings.HudAnchorX = 1f;
                    Settings.HudAnchorY = 1f;
                    Settings.HudOffsetX = -20f;
                    Settings.HudOffsetY = -20f;
                    break;
            }
        }

        private int GetHudCornerIndex()
        {
            if (Settings.HudAnchorX < 0.5f && Settings.HudAnchorY > 0.5f)
            {
                return 1;
            }

            if (Settings.HudAnchorX > 0.5f && Settings.HudAnchorY < 0.5f)
            {
                return 2;
            }

            if (Settings.HudAnchorX < 0.5f && Settings.HudAnchorY < 0.5f)
            {
                return 3;
            }

            return 0;
        }

        private void OnSceneChanged(string targetScene)
        {
            if (targetScene == Constants.MENU_SCENE)
            {
                _samples.Clear();
                _attackGroups.Clear();
                _lastExtraDamageFrame = -1;
                _totalDamage = 0;
                _maxDps = 0f;
                DestroyHud();
            }
        }

        private void CreateHud()
        {
            if (CanvasUtil.TrajanBold == null)
            {
                return;
            }

            try
            {
                _canvas = CanvasUtil.CreateCanvas(RenderMode.ScreenSpaceOverlay, 100);

                // The canvas is display-only; remove the raycaster so it never
                // intercepts mouse clicks aimed at the real game UI.
                GraphicRaycaster raycaster = _canvas.GetComponent<GraphicRaycaster>();
                if (raycaster != null)
                {
                    UnityEngine.Object.Destroy(raycaster);
                }

                Vector2 anchor = new Vector2(Settings.HudAnchorX, Settings.HudAnchorY);
                Vector2 pivot = new Vector2(
                    Settings.HudAnchorX >= 0.5f ? 1f : 0f,
                    Settings.HudAnchorY >= 0.5f ? 1f : 0f
                );
                TextAnchor alignment = Settings.HudAnchorX >= 0.5f
                    ? TextAnchor.MiddleRight
                    : TextAnchor.MiddleLeft;

                // Container attached to the chosen screen corner. Both text
                // rows live inside it, so "Max" always sits below the DPS line
                // regardless of which corner is selected.
                GameObject container = CanvasUtil.CreateBasePanel(
                    _canvas,
                    new CanvasUtil.RectData(
                        new Vector2(470f, 92f),
                        new Vector2(Settings.HudOffsetX, Settings.HudOffsetY),
                        anchor,
                        anchor,
                        pivot)
                );

                int maxFontSize = Mathf.Max(13, Mathf.RoundToInt(Settings.FontSize * 0.65f));

                GameObject dpsObject = CanvasUtil.CreateTextPanel(
                    container,
                    string.Empty,
                    Settings.FontSize,
                    alignment,
                    new CanvasUtil.RectData(
                        Vector2.zero,
                        Vector2.zero,
                        new Vector2(0f, 0.5f),
                        new Vector2(1f, 1f),
                        new Vector2(0.5f, 0.5f)),
                    true
                );

                _dpsText = dpsObject.GetComponent<Text>();
                _dpsText.color = Color.white;
                _dpsText.supportRichText = true;
                _dpsText.raycastTarget = false;
                AddOutline(dpsObject);

                GameObject maxObject = CanvasUtil.CreateTextPanel(
                    container,
                    string.Empty,
                    maxFontSize,
                    alignment,
                    new CanvasUtil.RectData(
                        Vector2.zero,
                        Vector2.zero,
                        new Vector2(0f, 0f),
                        new Vector2(1f, 0.5f),
                        new Vector2(0.5f, 0.5f)),
                    true
                );

                _maxText = maxObject.GetComponent<Text>();
                _maxText.color = new Color(0.92f, 0.92f, 0.92f, 1f);
                _maxText.supportRichText = true;
                _maxText.raycastTarget = false;
                AddOutline(maxObject);
            }
            catch (Exception ex)
            {
                LogError($"Failed to create DPS HUD: {ex}");
                DestroyHud();
            }
        }

        private static void AddOutline(GameObject textObject)
        {
            Outline outline = textObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
        }

        private void UpdateText()
        {
            PruneAttackGroups();

            float now = Time.time;
            float window = Mathf.Max(Settings.WindowSeconds, 0.25f);
            float cutoff = now - window;

            _samples.RemoveAll(sample => sample.Time < cutoff);

            int damageInWindow = 0;
            for (int i = 0; i < _samples.Count; i++)
            {
                damageInWindow += _samples[i].Damage;
            }

            float dps = damageInWindow / window;
            if (dps > _maxDps)
            {
                _maxDps = dps;
            }

            _dpsText.text = $"DPS {dps:0.00}";
            _maxText.text = $"Max {_maxDps:0.00}";
        }

        private void PruneAttackGroups()
        {
            int cutoff = Time.frameCount - 2;
            _staleAttackGroups.Clear();

            foreach (KeyValuePair<GameObject, AttackGroup> pair in _attackGroups)
            {
                if (pair.Key == null || pair.Value.Frame < cutoff)
                {
                    _staleAttackGroups.Add(pair.Key);
                }
            }

            for (int i = 0; i < _staleAttackGroups.Count; i++)
            {
                _attackGroups.Remove(_staleAttackGroups[i]);
            }
        }

        private void DestroyHud()
        {
            if (_canvas != null)
            {
                UnityEngine.Object.Destroy(_canvas);
            }

            _canvas = null;
            _dpsText = null;
            _maxText = null;
        }

        private struct DamageSample
        {
            public readonly float Time;

            public readonly int Damage;

            public DamageSample(float time, int damage)
            {
                Time = time;
                Damage = damage;
            }
        }

        private struct AttackGroup
        {
            public readonly int Frame;

            public AttackGroup(int frame)
            {
                Frame = frame;
            }
        }
    }
}
