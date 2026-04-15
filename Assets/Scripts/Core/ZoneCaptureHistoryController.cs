using System;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Core MonoBehaviour that feeds zone capture and loss events into a
    /// <see cref="ZoneCaptureHistorySO"/> ring buffer, recording the zone ID and
    /// match-elapsed time for each event.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────
    ///   _onMatchStarted → HandleMatchStarted(): clears history, starts clock.
    ///   _onMatchEnded   → HandleMatchEnded(): stops clock.
    ///   Per-zone ControlZoneSO._onCaptured → RecordCapture(i).
    ///   Per-zone ControlZoneSO._onLost     → RecordLoss(i).
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Per-zone capture and loss delegates are pre-allocated in Awake
    ///     (one pair per entry in _zones). Zero heap alloc after initialisation.
    ///   - All refs optional; null-safe throughout.
    ///   - DisallowMultipleComponent — one history controller per match.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   _historySO      → ZoneCaptureHistorySO asset.
    ///   _zones          → Array of ControlZoneSO assets (one per arena zone).
    ///   _onMatchStarted → shared match-start VoidGameEvent.
    ///   _onMatchEnded   → shared match-end VoidGameEvent.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneCaptureHistoryController : MonoBehaviour
    {
        // ── Inspector — Data ──────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("Ring-buffer SO that stores capture/loss events.")]
        [SerializeField] private ZoneCaptureHistorySO _historySO;

        [Tooltip("Array of ControlZoneSOs to monitor. " +
                 "Each SO's OnCaptured / OnLost events are subscribed individually.")]
        [SerializeField] private ControlZoneSO[] _zones;

        // ── Inspector — Event Channels ────────────────────────────────────────

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMatchEnded;

        // ── Runtime state ─────────────────────────────────────────────────────

        private bool  _matchRunning;
        private float _matchElapsed;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _startDelegate;
        private Action _endDelegate;

        // Per-zone delegates; allocated once in Awake.
        private Action[] _captureDelegate;
        private Action[] _lossDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _startDelegate = HandleMatchStarted;
            _endDelegate   = HandleMatchEnded;

            int zoneCount = _zones != null ? _zones.Length : 0;
            _captureDelegate = new Action[zoneCount];
            _lossDelegate    = new Action[zoneCount];

            for (int i = 0; i < zoneCount; i++)
            {
                int captured = i; // closure capture
                _captureDelegate[i] = () => RecordCapture(captured);
                _lossDelegate[i]    = () => RecordLoss(captured);
            }
        }

        private void OnEnable()
        {
            _onMatchStarted?.RegisterCallback(_startDelegate);
            _onMatchEnded?.RegisterCallback(_endDelegate);

            if (_zones == null) return;
            for (int i = 0; i < _zones.Length; i++)
            {
                _zones[i]?.OnCaptured?.RegisterCallback(_captureDelegate[i]);
                _zones[i]?.OnLost?.RegisterCallback(_lossDelegate[i]);
            }
        }

        private void OnDisable()
        {
            _onMatchStarted?.UnregisterCallback(_startDelegate);
            _onMatchEnded?.UnregisterCallback(_endDelegate);

            if (_zones == null) return;
            for (int i = 0; i < _zones.Length; i++)
            {
                _zones[i]?.OnCaptured?.UnregisterCallback(_captureDelegate[i]);
                _zones[i]?.OnLost?.UnregisterCallback(_lossDelegate[i]);
            }
        }

        private void Update()
        {
            if (_matchRunning)
                _matchElapsed += Time.deltaTime;
        }

        // ── Logic ─────────────────────────────────────────────────────────────

        /// <summary>Clears history and starts the match elapsed timer.</summary>
        public void HandleMatchStarted()
        {
            _matchRunning = true;
            _matchElapsed = 0f;
            _historySO?.Clear();
        }

        /// <summary>Stops the match elapsed timer.</summary>
        public void HandleMatchEnded()
        {
            _matchRunning = false;
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void RecordCapture(int zoneIndex)
        {
            if (_historySO == null || _zones == null || zoneIndex >= _zones.Length) return;
            string id = _zones[zoneIndex] != null ? _zones[zoneIndex].ZoneId : string.Empty;
            _historySO.AddEntry(id, _matchElapsed, true);
        }

        private void RecordLoss(int zoneIndex)
        {
            if (_historySO == null || _zones == null || zoneIndex >= _zones.Length) return;
            string id = _zones[zoneIndex] != null ? _zones[zoneIndex].ZoneId : string.Empty;
            _historySO.AddEntry(id, _matchElapsed, false);
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="ZoneCaptureHistorySO"/>. May be null.</summary>
        public ZoneCaptureHistorySO HistorySO => _historySO;

        /// <summary>True while a match is in progress.</summary>
        public bool IsMatchRunning => _matchRunning;

        /// <summary>Elapsed seconds since the current match started.</summary>
        public float MatchElapsed => _matchElapsed;
    }
}
