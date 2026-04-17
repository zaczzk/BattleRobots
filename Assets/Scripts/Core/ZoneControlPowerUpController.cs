using System;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Core MonoBehaviour that drives the <see cref="ZoneControlPowerUpSO"/> tick
    /// loop and resets it at match boundaries.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   <c>Update</c> calls <see cref="ZoneControlPowerUpSO.Tick"/> each frame.
    ///   On <c>_onMatchStarted</c>: calls <see cref="ZoneControlPowerUpSO.Reset"/>
    ///   to begin a fresh spawn cycle.
    ///   On <c>_onMatchEnded</c>: calls <see cref="ZoneControlPowerUpSO.Reset"/>
    ///   to stop accumulation after the match ends.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no UI references.
    ///   - All inspector fields optional and null-safe.
    ///   - Delegates cached in Awake; zero alloc after init.
    ///   - DisallowMultipleComponent — one power-up controller per scene.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlPowerUpController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlPowerUpSO _powerUpSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMatchEnded;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handleMatchStartedDelegate;
        private Action _handleMatchEndedDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleMatchEndedDelegate   = HandleMatchEnded;
        }

        private void OnEnable()
        {
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onMatchEnded?.RegisterCallback(_handleMatchEndedDelegate);
        }

        private void OnDisable()
        {
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
        }

        private void Update()
        {
            _powerUpSO?.Tick(Time.deltaTime);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        /// <summary>Resets the power-up SO to begin a fresh spawn cycle.</summary>
        public void HandleMatchStarted() => _powerUpSO?.Reset();

        /// <summary>Resets the power-up SO to halt accumulation after match end.</summary>
        public void HandleMatchEnded() => _powerUpSO?.Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound power-up SO (may be null).</summary>
        public ZoneControlPowerUpSO PowerUpSO => _powerUpSO;
    }
}
