using System;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Core MonoBehaviour that accumulates per-zone player occupation time into a
    /// <see cref="ZonePresenceTimerSO"/> each frame a zone is captured.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────
    ///   Update: while match running → for each non-null zone whose
    ///           <see cref="ControlZoneSO.IsCaptured"/> is true →
    ///           <see cref="ZonePresenceTimerSO.AddPresenceTime"/>(i, dt).
    ///   _onMatchStarted → HandleMatchStarted(): _matchRunning = true; Reset SO.
    ///   _onMatchEnded   → HandleMatchEnded():   _matchRunning = false.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace — no Physics / UI dependencies.
    ///   - All refs optional; null-safe throughout.
    ///   - Delegates cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - DisallowMultipleComponent — one timer controller per scene.
    ///   - Update loop performs no heap allocation (array iteration + float add).
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign <c>_timerSO</c>       → a ZonePresenceTimerSO asset.
    ///   2. Assign <c>_zones</c>         → array of ControlZoneSO assets (one per zone).
    ///   3. Assign <c>_onMatchStarted</c> → shared match-start VoidGameEvent.
    ///   4. Assign <c>_onMatchEnded</c>   → shared match-end VoidGameEvent.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZonePresenceTimerController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("The ZonePresenceTimerSO that stores per-zone occupation totals.")]
        [SerializeField] private ZonePresenceTimerSO _timerSO;

        [Tooltip("Array of ControlZoneSOs to monitor. Index matches the zone index " +
                 "used in ZonePresenceTimerSO.AddPresenceTime.")]
        [SerializeField] private ControlZoneSO[] _zones;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMatchEnded;

        // ── Runtime state ─────────────────────────────────────────────────────

        private bool _matchRunning;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _startDelegate;
        private Action _endDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _startDelegate = HandleMatchStarted;
            _endDelegate   = HandleMatchEnded;
        }

        private void OnEnable()
        {
            _onMatchStarted?.RegisterCallback(_startDelegate);
            _onMatchEnded?.RegisterCallback(_endDelegate);
        }

        private void OnDisable()
        {
            _onMatchStarted?.UnregisterCallback(_startDelegate);
            _onMatchEnded?.UnregisterCallback(_endDelegate);
        }

        private void Update()
        {
            if (!_matchRunning || _zones == null) return;

            float dt = Time.deltaTime;
            for (int i = 0; i < _zones.Length; i++)
            {
                if (_zones[i] != null && _zones[i].IsCaptured)
                    _timerSO?.AddPresenceTime(i, dt);
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Starts accumulation and resets the SO. Wired to <c>_onMatchStarted</c>.</summary>
        public void HandleMatchStarted()
        {
            _matchRunning = true;
            _timerSO?.Reset();
        }

        /// <summary>Stops accumulation. Wired to <c>_onMatchEnded</c>.</summary>
        public void HandleMatchEnded()
        {
            _matchRunning = false;
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="ZonePresenceTimerSO"/>. May be null.</summary>
        public ZonePresenceTimerSO TimerSO => _timerSO;

        /// <summary>True while a match is in progress.</summary>
        public bool IsMatchRunning => _matchRunning;
    }
}
