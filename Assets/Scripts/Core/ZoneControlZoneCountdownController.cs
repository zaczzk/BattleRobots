using System;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Core MonoBehaviour that ticks <see cref="ZoneControlZoneCountdownSO"/> each frame
    /// and starts the countdown when a match begins.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   • <c>Update</c> calls <c>_countdownSO.Tick(Time.deltaTime)</c> so the
    ///     SO's timer advances automatically.
    ///   • Subscribes <c>_onMatchStarted</c> → <see cref="HandleMatchStarted"/>
    ///     which calls <c>StartCountdown()</c> on the SO.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - All inspector fields optional and null-safe.
    ///   - Delegate cached in Awake; zero alloc after init.
    ///   - DisallowMultipleComponent — one countdown controller per zone.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _countdownSO  → ZoneControlZoneCountdownSO asset (one per zone).
    ///   2. Assign _onMatchStarted → shared MatchStarted VoidGameEvent.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlZoneCountdownController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlZoneCountdownSO _countdownSO;

        [Header("Event Channels — In (optional)")]
        [Tooltip("Raised at match start; triggers StartCountdown on the SO.")]
        [SerializeField] private VoidGameEvent _onMatchStarted;

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

        private void Update()
        {
            _countdownSO?.Tick(Time.deltaTime);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Starts the countdown on the bound SO at match start.
        /// No-op when <c>_countdownSO</c> is null.
        /// </summary>
        public void HandleMatchStarted()
        {
            _countdownSO?.StartCountdown();
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound countdown SO (may be null).</summary>
        public ZoneControlZoneCountdownSO CountdownSO => _countdownSO;
    }
}
