namespace DpsCounterMod
{
    /// <summary>
    /// Persistent settings for the mod. The game writes these to
    /// DpsCounter.GlobalSettings.json so values can be tuned without rebuilding.
    /// </summary>
    public class DpsCounterSettings
    {
        /// <summary>Whether the HUD is shown at all.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Key used to toggle the HUD while playing. Set to "None" to disable.
        /// Any UnityEngine.KeyCode name is accepted in the JSON file.
        /// </summary>
        public string ToggleKey { get; set; } = "F6";

        /// <summary>
        /// Key pressed during gameplay to reset the peak DPS value shown under
        /// the main reading. Set to "None" to disable.
        /// </summary>
        public string ResetMaxKey { get; set; } = "F7";

        /// <summary>
        /// Length of the rolling window (in seconds) used to compute DPS.
        /// </summary>
        public float WindowSeconds { get; set; } = 3f;

        /// <summary>
        /// Whether to also count the "Generic" attack type. Nail, Spell, NailBeam
        /// and SharpShadow damage is always counted. Some charm/companion damage
        /// is delivered as Generic, but Generic also covers some non-player kills,
        /// so it is opt-in.
        /// </summary>
        public bool CountGenericDamage { get; set; } = false;

        /// <summary>Font size of the HUD text.</summary>
        public int FontSize { get; set; } = 26;

        /// <summary>Horizontal anchor of the HUD (0 = left, 1 = right).</summary>
        public float HudAnchorX { get; set; } = 1f;

        /// <summary>Vertical anchor of the HUD (0 = bottom, 1 = top).</summary>
        public float HudAnchorY { get; set; } = 1f;

        /// <summary>Pixel offset from the anchor (negative x keeps it inside the screen on the right).</summary>
        public float HudOffsetX { get; set; } = -20f;

        /// <summary>Pixel offset from the anchor (negative y keeps it inside the screen at the top).</summary>
        public float HudOffsetY { get; set; } = -20f;
    }
}
