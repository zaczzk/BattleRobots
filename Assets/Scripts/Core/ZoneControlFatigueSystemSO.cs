using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that models bot fatigue after consecutive zone captures.
    /// Once the bot has captured <c>_fatigueThreshold</c> zones in a row
    /// without recovery, it enters a fatigued state which consumers (e.g.
    /// the bot AI interval controller) can read to slow down captures.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   <see cref="RecordBotCapture"/> increments the consecutive capture
    ///   counter; fires <c>_onFatigueTriggered</c> the first time the count
    ///   reaches <c>_fatigueThreshold</c>.
    ///   <see cref="RecoverFromFatigue"/> resets the counter and clears the
    ///   fatigued flag; fires <c>_onFatigueRecovered</c>.
    ///   <see cref="Reset"/> clears all runtime state silently; called from
    ///   <c>OnEnable</c>.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlFatigueSystem.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlFatigueSystem", order = 76)]
    public sealed class ZoneControlFatigueSystemSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Fatigue Settings")]
        [Tooltip("Number of consecutive bot captures before fatigue triggers.")]
        [Min(1)]
        [SerializeField] private int _fatigueThreshold = 3;

        [Tooltip("Delay in seconds added to bot capture intervals while fatigued.")]
        [Min(0.1f)]
        [SerializeField] private float _fatigueDelay = 5f;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onFatigueTriggered;
        [SerializeField] private VoidGameEvent _onFatigueRecovered;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private int  _consecutiveCaptures;
        private bool _isFatigued;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        public int   ConsecutiveCaptures => _consecutiveCaptures;
        public bool  IsFatigued          => _isFatigued;
        public int   FatigueThreshold    => _fatigueThreshold;
        public float FatigueDelay        => _fatigueDelay;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Records one bot zone capture.  When consecutive captures reach
        /// <c>_fatigueThreshold</c> for the first time, fires
        /// <c>_onFatigueTriggered</c>.
        /// </summary>
        public void RecordBotCapture()
        {
            _consecutiveCaptures++;
            if (!_isFatigued && _consecutiveCaptures >= _fatigueThreshold)
            {
                _isFatigued = true;
                _onFatigueTriggered?.Raise();
            }
        }

        /// <summary>
        /// Clears the fatigue state and resets the consecutive-capture counter.
        /// Fires <c>_onFatigueRecovered</c> when the system was actually fatigued.
        /// </summary>
        public void RecoverFromFatigue()
        {
            bool wasFatigued = _isFatigued;
            _consecutiveCaptures = 0;
            _isFatigued          = false;
            if (wasFatigued)
                _onFatigueRecovered?.Raise();
        }

        /// <summary>Clears all runtime state silently.  Called from <c>OnEnable</c>.</summary>
        public void Reset()
        {
            _consecutiveCaptures = 0;
            _isFatigued          = false;
        }
    }
}
