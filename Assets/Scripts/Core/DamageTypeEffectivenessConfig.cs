using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Outcome category for a damage-type matchup, combining resistance and vulnerability.
    ///
    /// Effective — the combined multiplier exceeds the effective threshold (net amplification).
    /// Resisted  — the combined multiplier falls below the resisted threshold (net reduction).
    /// Neutral   — the combined multiplier falls between the two thresholds (near-normal damage).
    /// </summary>
    public enum EffectivenessOutcome
    {
        /// <summary>Net damage is amplified beyond the effective threshold.</summary>
        Effective = 0,

        /// <summary>Net damage is reduced below the resisted threshold.</summary>
        Resisted = 1,

        /// <summary>Net damage is near-normal (between the two thresholds).</summary>
        Neutral = 2,
    }

    /// <summary>
    /// Immutable data SO that configures the thresholds, labels, and colors used by
    /// <see cref="BattleRobots.UI.DamageTypeEffectivenessHUDController"/> to display
    /// "EFFECTIVE / RESISTED / NEUTRAL" feedback banners during combat.
    ///
    /// ── How the ratio is computed ────────────────────────────────────────────────
    ///   combinedRatio = (1 − resistance) × vulnerabilityMultiplier
    ///   A ratio of 1 means normal damage passes through unmodified.
    ///   ratios > effectiveThreshold → Effective banner.
    ///   ratios < resistedThreshold  → Resisted banner.
    ///   Otherwise                   → Neutral (no banner shown, or neutral label).
    ///
    /// ── Integration ─────────────────────────────────────────────────────────────
    ///   Assign to <c>DamageTypeEffectivenessHUDController._effectivenessConfig</c>.
    ///   Also assign (optionally) the same DamageResistanceConfig and
    ///   DamageVulnerabilityConfig that the attacked robot's DamageReceiver uses —
    ///   the controller reads them to compute the combined ratio without needing
    ///   the actual damage amount.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - All properties are read-only; asset is immutable at runtime.
    ///   - Zero allocation on the hot path: switch + float comparison only.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ UI ▶ DamageTypeEffectivenessConfig.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/UI/DamageTypeEffectivenessConfig")]
    public sealed class DamageTypeEffectivenessConfig : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Thresholds")]
        [Tooltip("Combined ratio above this value shows the Effective banner. Must be > 1.")]
        [SerializeField, Range(1.01f, 3f)] private float _effectiveThreshold = 1.1f;

        [Tooltip("Combined ratio below this value shows the Resisted banner. Must be < 1.")]
        [SerializeField, Range(0f, 0.99f)] private float _resistedThreshold = 0.9f;

        [Header("Effective")]
        [Tooltip("Banner text shown when damage is amplified above the effective threshold.")]
        [SerializeField] private string _effectiveLabel = "EFFECTIVE!";
        [Tooltip("Color of the effective banner text.")]
        [SerializeField] private Color  _effectiveColor = Color.green;

        [Header("Resisted")]
        [Tooltip("Banner text shown when damage is reduced below the resisted threshold.")]
        [SerializeField] private string _resistedLabel = "RESISTED!";
        [Tooltip("Color of the resisted banner text.")]
        [SerializeField] private Color  _resistedColor = new Color(0.6f, 0.3f, 0f, 1f);

        [Header("Neutral")]
        [Tooltip("Banner text shown when damage is near-normal.")]
        [SerializeField] private string _neutralLabel = "NEUTRAL";
        [Tooltip("Color of the neutral banner text.")]
        [SerializeField] private Color  _neutralColor = Color.white;

        [Header("Display")]
        [Tooltip("Seconds the effectiveness banner stays visible after a hit.")]
        [SerializeField, Min(0.1f)] private float _displayDuration = 1.5f;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Ratio above which a hit is considered Effective.</summary>
        public float EffectiveThreshold => _effectiveThreshold;

        /// <summary>Ratio below which a hit is considered Resisted.</summary>
        public float ResistedThreshold => _resistedThreshold;

        /// <summary>Banner label for the Effective outcome.</summary>
        public string EffectiveLabel => _effectiveLabel;

        /// <summary>Banner label for the Resisted outcome.</summary>
        public string ResistedLabel => _resistedLabel;

        /// <summary>Banner label for the Neutral outcome.</summary>
        public string NeutralLabel => _neutralLabel;

        /// <summary>Seconds the effectiveness banner stays visible.</summary>
        public float DisplayDuration => _displayDuration;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Classifies a combined damage ratio as Effective, Resisted, or Neutral
        /// based on the configured thresholds.
        /// Zero allocation — float comparisons only.
        /// </summary>
        public EffectivenessOutcome GetOutcome(float combinedRatio)
        {
            if (combinedRatio > _effectiveThreshold) return EffectivenessOutcome.Effective;
            if (combinedRatio < _resistedThreshold)  return EffectivenessOutcome.Resisted;
            return EffectivenessOutcome.Neutral;
        }

        /// <summary>
        /// Returns the display label string for the given <paramref name="outcome"/>.
        /// Returns an empty string for unrecognised values.
        /// Note: returns the serialized string reference — no allocation on repeated calls.
        /// </summary>
        public string GetLabel(EffectivenessOutcome outcome)
        {
            switch (outcome)
            {
                case EffectivenessOutcome.Effective: return _effectiveLabel;
                case EffectivenessOutcome.Resisted:  return _resistedLabel;
                case EffectivenessOutcome.Neutral:   return _neutralLabel;
                default:                             return string.Empty;
            }
        }

        /// <summary>
        /// Returns the display <see cref="Color"/> for the given <paramref name="outcome"/>.
        /// Returns white for unrecognised values.
        /// Zero allocation — switch on value-type enum, returns struct.
        /// </summary>
        public Color GetColor(EffectivenessOutcome outcome)
        {
            switch (outcome)
            {
                case EffectivenessOutcome.Effective: return _effectiveColor;
                case EffectivenessOutcome.Resisted:  return _resistedColor;
                case EffectivenessOutcome.Neutral:   return _neutralColor;
                default:                             return Color.white;
            }
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            _effectiveThreshold = Mathf.Clamp(_effectiveThreshold, 1.01f, 3f);
            _resistedThreshold  = Mathf.Clamp(_resistedThreshold,  0f, 0.99f);
            _displayDuration    = Mathf.Max(0.1f, _displayDuration);

            if (string.IsNullOrEmpty(_effectiveLabel)) _effectiveLabel = "EFFECTIVE!";
            if (string.IsNullOrEmpty(_resistedLabel))  _resistedLabel  = "RESISTED!";
            if (string.IsNullOrEmpty(_neutralLabel))   _neutralLabel   = "NEUTRAL";
        }
#endif
    }
}
