using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Configuration ScriptableObject that maps each <see cref="ZoneControlVictoryType"/>
    /// to a celebration banner string and defines the celebration display duration.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   Call <see cref="GetBannerText"/> with a <see cref="ZoneControlVictoryType"/>
    ///   to retrieve the localisation-ready banner string.
    ///   <see cref="Duration"/> returns the seconds to show the celebration panel.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Immutable at runtime — all fields are inspector-only.
    ///   - Zero heap allocation on all hot-path methods.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlVictoryCelebrationConfig.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlVictoryCelebrationConfig", order = 67)]
    public sealed class ZoneControlVictoryCelebrationConfig : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Banner Text")]
        [Tooltip("Text displayed when victory is achieved by FirstToCaptures.")]
        [SerializeField] private string _firstToCapturesBanner = "Victory \u2014 First to Captures!";

        [Tooltip("Text displayed when victory is achieved by MostZonesHeld.")]
        [SerializeField] private string _mostZonesHeldBanner = "Victory \u2014 Most Zones Held!";

        [Header("Timing")]
        [Tooltip("Duration in seconds to display the celebration panel.")]
        [Min(0.1f)]
        [SerializeField] private float _celebrationDuration = 3f;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Seconds the celebration panel is displayed.</summary>
        public float Duration => _celebrationDuration;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the banner text for the given <paramref name="victoryType"/>.
        /// Zero allocation.
        /// </summary>
        public string GetBannerText(ZoneControlVictoryType victoryType) =>
            victoryType == ZoneControlVictoryType.MostZonesHeld
                ? _mostZonesHeldBanner
                : _firstToCapturesBanner;

        private void OnValidate()
        {
            _celebrationDuration = Mathf.Max(0.1f, _celebrationDuration);
        }
    }
}
