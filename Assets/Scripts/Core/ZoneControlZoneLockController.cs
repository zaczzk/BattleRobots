using System;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Core MonoBehaviour that ticks <see cref="ZoneControlZoneLockSO"/> each frame
    /// and wires zone-capture and match-boundary events to the SO's lock lifecycle.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   • <c>Update</c> calls <c>_lockSO.Tick(Time.deltaTime)</c> so the SO's
    ///     unlock timer advances automatically.
    ///   • Subscribes to <c>_onZoneCaptured</c> → calls <see cref="HandleZoneCaptured"/>
    ///     which locks the zone via the SO.
    ///   • Subscribes to <c>_onMatchStarted</c> → resets the lock SO.
    ///   • All refs are optional; the component is safe with no wiring.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Delegate cached in Awake; zero alloc after init.
    ///   - DisallowMultipleComponent — one lock controller per zone.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _lockSO         → ZoneControlZoneLockSO asset (one per zone).
    ///   2. Assign _onZoneCaptured → VoidGameEvent raised when this specific zone
    ///      is captured (wire from ControlZoneController._onCaptured or similar).
    ///   3. Assign _onMatchStarted → shared MatchStarted VoidGameEvent.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlZoneLockController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlZoneLockSO _lockSO;

        [Header("Event Channels — In (optional)")]
        [Tooltip("Raised when this zone is captured; triggers a lock.")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;

        [Tooltip("Raised at match start; resets the lock state.")]
        [SerializeField] private VoidGameEvent _onMatchStarted;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handleZoneCapturedDelegate;
        private Action _handleMatchStartedDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleZoneCapturedDelegate = HandleZoneCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
        }

        private void OnEnable()
        {
            _onZoneCaptured?.RegisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
        }

        private void Update()
        {
            _lockSO?.Tick(Time.deltaTime);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Locks the zone via the SO.
        /// No-op when <c>_lockSO</c> is null.
        /// </summary>
        public void HandleZoneCaptured()
        {
            _lockSO?.LockZone();
        }

        /// <summary>
        /// Resets the lock SO at match start.
        /// No-op when <c>_lockSO</c> is null.
        /// </summary>
        public void HandleMatchStarted()
        {
            _lockSO?.Reset();
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound zone-lock SO (may be null).</summary>
        public ZoneControlZoneLockSO LockSO => _lockSO;
    }
}
