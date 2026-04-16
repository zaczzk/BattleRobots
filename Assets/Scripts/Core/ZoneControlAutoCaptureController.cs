using System;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Core MonoBehaviour that detects when the player holds zero zones for
    /// <see cref="ZoneControlAutoCaptureSO.AutoCaptureDuration"/> seconds and fires
    /// <c>_onAutoCapture</c> via the SO.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   • Each frame <see cref="Tick(float)"/> checks
    ///     <c>_catalogSO.PlayerOwnedCount == 0</c>.
    ///   • When true, accumulates time.  When the threshold is met, calls
    ///     <see cref="ZoneControlAutoCaptureSO.FireAutoCapture"/> once, then resets.
    ///   • When the player recaptures any zone the accumulator resets immediately.
    ///   • Subscribes <c>_onMatchStarted</c> → <see cref="HandleMatchStarted"/>
    ///     which resets the accumulator and the SO.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - All inspector fields optional and null-safe.
    ///   - Cached bool flag; zero heap allocation in Tick.
    ///   - DisallowMultipleComponent — one auto-capture controller per scene.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _autoCaptureSO  → ZoneControlAutoCaptureSO asset.
    ///   2. Assign _catalogSO      → ZoneControlZoneControllerCatalogSO asset.
    ///   3. Assign _onMatchStarted → shared MatchStarted VoidGameEvent.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlAutoCaptureController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlAutoCaptureSO           _autoCaptureSO;
        [SerializeField] private ZoneControlZoneControllerCatalogSO _catalogSO;

        [Header("Event Channels — In (optional)")]
        [Tooltip("Raised at match start; resets the accumulator.")]
        [SerializeField] private VoidGameEvent _onMatchStarted;

        // ── Runtime state ─────────────────────────────────────────────────────

        private bool  _isAccumulating;
        private float _accumulatedTime;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handleMatchStartedDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake() => _handleMatchStartedDelegate = HandleMatchStarted;

        private void OnEnable()
        {
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
        }

        private void OnDisable()
        {
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
        }

        private void Update() => Tick(Time.deltaTime);

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Advances the zero-player-zone accumulator by <paramref name="deltaTime"/>.
        /// Fires auto-capture once the configured duration is met.
        /// No-op when either SO ref is null.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (_autoCaptureSO == null || _catalogSO == null) return;

            bool noPlayerZones = _catalogSO.PlayerOwnedCount == 0;

            if (!noPlayerZones)
            {
                _isAccumulating  = false;
                _accumulatedTime = 0f;
                return;
            }

            if (!_isAccumulating)
            {
                _isAccumulating  = true;
                _accumulatedTime = 0f;
            }

            _accumulatedTime += deltaTime;

            if (_accumulatedTime >= _autoCaptureSO.AutoCaptureDuration)
            {
                _autoCaptureSO.FireAutoCapture();
                _isAccumulating  = false;
                _accumulatedTime = 0f;
            }
        }

        /// <summary>
        /// Resets the accumulator and the SO at match start.
        /// </summary>
        public void HandleMatchStarted()
        {
            _isAccumulating  = false;
            _accumulatedTime = 0f;
            _autoCaptureSO?.Reset();
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound auto-capture SO (may be null).</summary>
        public ZoneControlAutoCaptureSO AutoCaptureSO => _autoCaptureSO;

        /// <summary>True while the accumulator is counting up (zero-player-zone state).</summary>
        public bool IsAccumulating => _isAccumulating;

        /// <summary>Time in seconds accumulated during the current zero-player-zone period.</summary>
        public float AccumulatedTime => _accumulatedTime;
    }
}
