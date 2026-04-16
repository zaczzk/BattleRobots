using System;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Core MonoBehaviour that bridges <see cref="ZoneControlMatchPressureSO"/>
    /// pressure events to <see cref="ZoneControlAdaptiveSpawnSO"/>, updating the
    /// active spawn interval whenever the pressure state changes.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   • Subscribes <c>_onHighPressure</c>   → <see cref="HandleHighPressure"/>
    ///     → <c>SetHighPressure(true)</c>.
    ///   • Subscribes <c>_onPressureRelieved</c> → <see cref="HandlePressureRelieved"/>
    ///     → <c>SetHighPressure(false)</c>.
    ///   • Subscribes <c>_onMatchStarted</c>   → <see cref="HandleMatchStarted"/>
    ///     → <c>Reset</c> the spawn SO.
    ///   • No Update loop — purely event-driven.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - All inspector fields optional and null-safe.
    ///   - Delegates cached in Awake; zero alloc after init.
    ///   - DisallowMultipleComponent — one adaptive-spawn controller per object.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlAdaptiveSpawnController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlAdaptiveSpawnSO _spawnSO;

        [Header("Event Channels — In (optional)")]
        [Tooltip("Wire to ZoneControlMatchPressureSO._onHighPressure.")]
        [SerializeField] private VoidGameEvent _onHighPressure;

        [Tooltip("Wire to ZoneControlMatchPressureSO._onPressureRelieved.")]
        [SerializeField] private VoidGameEvent _onPressureRelieved;

        [Tooltip("Raised at match start; resets the spawn SO.")]
        [SerializeField] private VoidGameEvent _onMatchStarted;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handleHighPressureDelegate;
        private Action _handlePressureRelievedDelegate;
        private Action _handleMatchStartedDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleHighPressureDelegate      = HandleHighPressure;
            _handlePressureRelievedDelegate  = HandlePressureRelieved;
            _handleMatchStartedDelegate      = HandleMatchStarted;
        }

        private void OnEnable()
        {
            _onHighPressure?.RegisterCallback(_handleHighPressureDelegate);
            _onPressureRelieved?.RegisterCallback(_handlePressureRelievedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
        }

        private void OnDisable()
        {
            _onHighPressure?.UnregisterCallback(_handleHighPressureDelegate);
            _onPressureRelieved?.UnregisterCallback(_handlePressureRelievedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        /// <summary>Transitions spawn SO to high-pressure mode.</summary>
        public void HandleHighPressure()
        {
            _spawnSO?.SetHighPressure(true);
        }

        /// <summary>Transitions spawn SO back to normal pressure mode.</summary>
        public void HandlePressureRelieved()
        {
            _spawnSO?.SetHighPressure(false);
        }

        /// <summary>Resets the spawn SO at match start.</summary>
        public void HandleMatchStarted()
        {
            _spawnSO?.Reset();
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound adaptive spawn SO (may be null).</summary>
        public ZoneControlAdaptiveSpawnSO SpawnSO => _spawnSO;
    }
}
