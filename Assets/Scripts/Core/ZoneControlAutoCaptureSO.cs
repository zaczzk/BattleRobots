using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that models bot auto-capture configuration.
    /// When the player holds zero zones for <see cref="AutoCaptureDuration"/> seconds,
    /// the controller fires <see cref="FireAutoCapture"/> which raises
    /// <see cref="_onAutoCapture"/>.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   The SO is stateless: it holds config and owns the event channel.
    ///   Timing accumulation is handled by <see cref="ZoneControlAutoCaptureController"/>.
    ///   Call <see cref="FireAutoCapture"/> when the duration is met.
    ///   Call <see cref="Reset"/> to clear any SO-level state (bootstrapper-safe).
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Zero heap allocation on all hot-path methods.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlAutoCapture.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlAutoCapture", order = 57)]
    public sealed class ZoneControlAutoCaptureSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Settings")]
        [Tooltip("Seconds with zero player zones before auto-capture fires.")]
        [Min(0.1f)]
        [SerializeField] private float _autoCaptureDuration = 5f;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised when the auto-capture condition is met.")]
        [SerializeField] private VoidGameEvent _onAutoCapture;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Seconds with zero player zones required to trigger auto-capture.</summary>
        public float AutoCaptureDuration => _autoCaptureDuration;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Raises the <see cref="_onAutoCapture"/> event channel.
        /// Called by the controller once the accumulation threshold is met.
        /// </summary>
        public void FireAutoCapture()
        {
            _onAutoCapture?.Raise();
        }

        /// <summary>
        /// Bootstrapper-safe reset.  The SO itself is stateless; this method
        /// exists so the controller can reset SO state if future fields are added.
        /// </summary>
        public void Reset() { }

        private void OnValidate()
        {
            _autoCaptureDuration = Mathf.Max(0.1f, _autoCaptureDuration);
        }
    }
}
