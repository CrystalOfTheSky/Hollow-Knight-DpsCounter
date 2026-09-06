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
    public class DpsCounter : Mod, IGlobalSettings<DpsCounterSettings>, ITogglableMod
    {
        /// <summary>The active mod instance.</summary>
        public static DpsCounter Instance { get; private set; }

        /// <summary>Live settings, editable from the generated JSON file.</summary>
        public DpsCounterSettings Settings { get; private set; } = new DpsCounterSettings();

        private readonly List<DamageSample> _samples = new List<DamageSample>();
        private long _totalDamage;

        private GameObject _canvas;
        private Text _dpsText;
        private bool _hooked;

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
        }

        public DpsCounterSettings OnSaveGlobal() => Settings;

        public override void Initialize()
        {
            if (Instance != null)
            {
                return;
            }

            Instance = this;
            Hook();
            Log($"DPS Counter v{GetVersion()} initialized. Rolling window: {Settings.WindowSeconds:0.#}s.");
        }

        public void Unload()
        {
            Unhook();
            DestroyHud();
            _samples.Clear();
            _totalDamage = 0;
            Instance = null;
        }

        private void Hook()
        {
            if (_hooked)
            {
                return;
            }

            On.HealthManager.TakeDamage += OnTakeDamage;
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
            ModHooks.HeroUpdateHook -= OnHeroUpdate;
            ModHooks.SceneChanged -= OnSceneChanged;
            _hooked = false;
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

            RecordDamage(damage);
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
                    return Settings.CountGenericDamage;

                default:
                    return false;
            }
        }

        private void RecordDamage(int damage)
        {
            _samples.Add(new DamageSample(Time.time, damage));
            _totalDamage += damage;
        }

        private void OnHeroUpdate()
        {
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

        private void OnSceneChanged(string targetScene)
        {
            if (targetScene == Constants.MENU_SCENE)
            {
                _samples.Clear();
                _totalDamage = 0;
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
                Vector2 size = new Vector2(420f, 48f);
                Vector2 offset = new Vector2(Settings.HudOffsetX, Settings.HudOffsetY);

                var rectData = new CanvasUtil.RectData(
                    size,
                    offset,
                    anchor,
                    anchor,
                    new Vector2(1f, 1f));

                GameObject textObject = CanvasUtil.CreateTextPanel(
                    _canvas,
                    string.Empty,
                    Settings.FontSize,
                    TextAnchor.MiddleRight,
                    rectData,
                    true);

                _dpsText = textObject.GetComponent<Text>();
                _dpsText.color = Color.white;
                _dpsText.supportRichText = true;

                Outline outline = textObject.AddComponent<Outline>();
                outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
                outline.effectDistance = new Vector2(1.5f, -1.5f);
            }
            catch (Exception ex)
            {
                LogError($"Failed to create DPS HUD: {ex}");
                DestroyHud();
            }
        }

        private void UpdateText()
        {
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
            _dpsText.text = $"DPS {dps:0.#}";
        }

        private void DestroyHud()
        {
            if (_canvas != null)
            {
                UnityEngine.Object.Destroy(_canvas);
            }

            _canvas = null;
            _dpsText = null;
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
    }
}
