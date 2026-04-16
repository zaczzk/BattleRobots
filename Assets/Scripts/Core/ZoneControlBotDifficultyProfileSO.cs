using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Data ScriptableObject that defines how quickly simulated bots capture
    /// zones at each wave of a zone-control match.
    ///
    /// The formula reduces the inter-capture interval each wave:
    ///   <c>GetCaptureInterval(wave) = Max(MinimumInterval, BaseInterval - wave × ReductionPerWave)</c>
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Immutable at runtime (inspector data only).
    ///   - OnValidate clamps MinimumInterval to [0.1, BaseInterval].
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlBotDifficultyProfile.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlBotDifficultyProfile", order = 41)]
    public sealed class ZoneControlBotDifficultyProfileSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Bot Capture Timing")]
        [Tooltip("Starting interval (seconds) between bot zone captures at wave 0.")]
        [Min(0.1f)]
        [SerializeField] private float _baseCaptureInterval = 10f;

        [Tooltip("Seconds subtracted from the interval per wave number.")]
        [Min(0f)]
        [SerializeField] private float _intervalReductionPerWave = 0.5f;

        [Tooltip("Floor: the capture interval will never drop below this value.")]
        [Min(0.1f)]
        [SerializeField] private float _minimumInterval = 2f;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Starting capture interval before any wave reduction is applied.</summary>
        public float BaseCaptureInterval => _baseCaptureInterval;

        /// <summary>Interval reduction applied per wave number.</summary>
        public float IntervalReductionPerWave => _intervalReductionPerWave;

        /// <summary>Absolute minimum capture interval regardless of wave count.</summary>
        public float MinimumInterval => _minimumInterval;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the bot capture interval (seconds) for the given <paramref name="wave"/>
        /// number using the formula:
        ///   <c>Max(MinimumInterval, BaseInterval - wave × ReductionPerWave)</c>
        /// Wave values below 0 are treated as 0.
        /// </summary>
        public float GetCaptureInterval(int wave)
        {
            float w = Mathf.Max(0, wave);
            return Mathf.Max(_minimumInterval, _baseCaptureInterval - w * _intervalReductionPerWave);
        }

        // ── Validation ────────────────────────────────────────────────────────

        private void OnValidate()
        {
            _baseCaptureInterval       = Mathf.Max(0.1f, _baseCaptureInterval);
            _intervalReductionPerWave  = Mathf.Max(0f,   _intervalReductionPerWave);
            _minimumInterval           = Mathf.Clamp(_minimumInterval, 0.1f, _baseCaptureInterval);
        }
    }
}
