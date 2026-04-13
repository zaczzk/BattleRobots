using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Data SO that maps each <see cref="DamageType"/> to a display color and label
    /// used by <see cref="BattleRobots.UI.DamageTypeHUDController"/> to show visual
    /// feedback for the last damage type received.
    ///
    /// ── Visual rules ────────────────────────────────────────────────────────────
    ///   Each damage type has:
    ///     • A <see cref="Color"/> for coloring HUD elements (icon tint, flash color).
    ///     • A short display <see cref="string"/> label (e.g. "PHYSICAL", "ENERGY").
    ///   The controller reads these values reactively when a DamageGameEvent fires.
    ///
    /// ── Integration ─────────────────────────────────────────────────────────────
    ///   Assign to <c>DamageTypeHUDController._iconConfig</c>.
    ///   Call <see cref="GetColor"/> / <see cref="GetLabel"/> with the
    ///   <see cref="DamageType"/> from the received <see cref="DamageInfo"/>.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - All properties are read-only; asset is immutable at runtime.
    ///   - Zero allocation on the hot path: switch + struct / string return.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ UI ▶ DamageTypeIconConfig.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/UI/DamageTypeIconConfig")]
    public sealed class DamageTypeIconConfig : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Physical")]
        [Tooltip("Tint color for Physical-type damage indicators.")]
        [SerializeField] private Color _physicalColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        [Tooltip("Short label shown in the HUD for Physical damage.")]
        [SerializeField] private string _physicalLabel = "PHYSICAL";

        [Header("Energy")]
        [Tooltip("Tint color for Energy-type damage indicators.")]
        [SerializeField] private Color _energyColor = Color.cyan;
        [Tooltip("Short label shown in the HUD for Energy damage.")]
        [SerializeField] private string _energyLabel = "ENERGY";

        [Header("Thermal")]
        [Tooltip("Tint color for Thermal-type damage indicators.")]
        [SerializeField] private Color _thermalColor = new Color(1f, 0.45f, 0f, 1f);
        [Tooltip("Short label shown in the HUD for Thermal damage.")]
        [SerializeField] private string _thermalLabel = "THERMAL";

        [Header("Shock")]
        [Tooltip("Tint color for Shock-type damage indicators.")]
        [SerializeField] private Color _shockColor = Color.yellow;
        [Tooltip("Short label shown in the HUD for Shock damage.")]
        [SerializeField] private string _shockLabel = "SHOCK";

        [Header("Display Duration")]
        [Tooltip("Seconds the damage-type indicator stays visible after receiving a hit.")]
        [SerializeField, Min(0.1f)] private float _displayDuration = 1.5f;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Color for Physical-type damage display.</summary>
        public Color PhysicalColor => _physicalColor;

        /// <summary>Color for Energy-type damage display.</summary>
        public Color EnergyColor => _energyColor;

        /// <summary>Color for Thermal-type damage display.</summary>
        public Color ThermalColor => _thermalColor;

        /// <summary>Color for Shock-type damage display.</summary>
        public Color ShockColor => _shockColor;

        /// <summary>Seconds the type indicator remains visible after the last hit.</summary>
        public float DisplayDuration => _displayDuration;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the tint <see cref="Color"/> for the given <paramref name="type"/>.
        /// Returns white for unknown / out-of-range types.
        /// Zero allocation — switch on value-type enum, returns struct.
        /// </summary>
        public Color GetColor(DamageType type)
        {
            switch (type)
            {
                case DamageType.Physical: return _physicalColor;
                case DamageType.Energy:   return _energyColor;
                case DamageType.Thermal:  return _thermalColor;
                case DamageType.Shock:    return _shockColor;
                default:                  return Color.white;
            }
        }

        /// <summary>
        /// Returns the short display label for the given <paramref name="type"/>.
        /// Returns an empty string for unknown / out-of-range types.
        /// Note: returns the serialized string reference — no allocation on repeated calls.
        /// </summary>
        public string GetLabel(DamageType type)
        {
            switch (type)
            {
                case DamageType.Physical: return _physicalLabel;
                case DamageType.Energy:   return _energyLabel;
                case DamageType.Thermal:  return _thermalLabel;
                case DamageType.Shock:    return _shockLabel;
                default:                  return string.Empty;
            }
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            _displayDuration = Mathf.Max(0.1f, _displayDuration);

            if (string.IsNullOrEmpty(_physicalLabel)) _physicalLabel = "PHYSICAL";
            if (string.IsNullOrEmpty(_energyLabel))   _energyLabel   = "ENERGY";
            if (string.IsNullOrEmpty(_thermalLabel))  _thermalLabel  = "THERMAL";
            if (string.IsNullOrEmpty(_shockLabel))    _shockLabel    = "SHOCK";
        }
#endif
    }
}
