using System;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Core MonoBehaviour that connects <see cref="ZoneControlMatchPressureSO"/>
    /// pressure-change events to <see cref="ZoneControlAdaptiveSpawnSO"/>.
    ///
    /// ── Wiring ─────────────────────────────────────────────────────────────────
    ///   _onHighPressure    → ZoneControlMatchPressureSO._onHighPressure.
    ///   _onPressureRelieved→ ZoneControlMatchPressureSO._onPressureRelieved.
    ///   _onMatchStarted    → shared MatchStarted VoidGameEvent.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlAdaptiveSpawnController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlAdaptiveSpawnSO _spawnSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onHighPressure;
        [SerializeField] private VoidGameEvent _onPressureRelieved;
        [SerializeField] private VoidGameEvent _onMatchStarted;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handleHighPressureDelegate;
        private Action _handlePressureRelievedDelegate;
        private Action _handleMatchStartedDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleHighPressureDelegate     = HandleHighPressure;
            _handlePressureRelievedDelegate = HandlePressureRelieved;
            _handleMatchStartedDelegate     = HandleMatchStarted;
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

        /// <summary>Activates high-pressure spawn rate.</summary>
        public void HandleHighPressure() => _spawnSO?.SetHighPressure(true);

        /// <summary>Deactivates high-pressure spawn rate (returns to low-pressure rate).</summary>
        public void HandlePressureRelieved() => _spawnSO?.SetHighPressure(false);

        /// <summary>Resets the spawn SO to baseline at match start.</summary>
        public void HandleMatchStarted() => _spawnSO?.Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound adaptive spawn SO (may be null).</summary>
        public ZoneControlAdaptiveSpawnSO SpawnSO => _spawnSO;

        /// <summary>True when the spawn SO is in high-pressure mode.</summary>
        public bool IsHighPressure => _spawnSO != null && _spawnSO.IsHighPressure;
    }
}
