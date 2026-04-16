using System;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Core MonoBehaviour that drives <see cref="ZoneControlZoneLockSO"/> —
    /// locking a zone on capture and ticking the unlock countdown each frame.
    ///
    /// ── Flow ────────────────────────────────────────────────────────────────────
    ///   • <c>_onZoneCaptured</c> → calls <see cref="ZoneControlZoneLockSO.LockZone"/>.
    ///   • <c>Update</c>          → calls <see cref="ZoneControlZoneLockSO.Tick"/> each frame.
    ///   • <c>_onMatchStarted</c> → resets lock state via <see cref="ZoneControlZoneLockSO.Reset"/>.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no UI references.
    ///   - All inspector fields optional and null-safe.
    ///   - Delegates cached in Awake; zero alloc after init.
    ///   - DisallowMultipleComponent — one zone-lock driver per scene.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _zoneLockSO     → ZoneControlZoneLockSO asset.
    ///   2. Assign _onZoneCaptured → VoidGameEvent raised when any zone changes hands.
    ///   3. Assign _onMatchStarted → VoidGameEvent raised at match start.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlZoneLockController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlZoneLockSO _zoneLockSO;

        [Header("Event Channels — In (optional)")]
        [Tooltip("Raised when a zone changes hands; triggers LockZone.")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;

        [Tooltip("Raised at match start; resets lock state.")]
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
            _zoneLockSO?.Tick(Time.deltaTime);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        /// <summary>Locks the zone when a capture event fires.</summary>
        public void HandleZoneCaptured()
        {
            _zoneLockSO?.LockZone();
        }

        /// <summary>Resets the lock state at match start.</summary>
        public void HandleMatchStarted()
        {
            _zoneLockSO?.Reset();
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound zone-lock SO (may be null).</summary>
        public ZoneControlZoneLockSO ZoneLockSO => _zoneLockSO;
    }
}
