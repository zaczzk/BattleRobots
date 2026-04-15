using System;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Core MonoBehaviour that records per-frame zone ownership snapshots into a
    /// <see cref="ZoneControlReplaySO"/> during a match and exposes
    /// <see cref="StepForward"/> / <see cref="StepBackward"/> for post-match playback.
    ///
    /// ── Recording lifecycle ───────────────────────────────────────────────────
    ///   1. On <c>_onMatchStarted</c>: resets the replay SO and starts recording.
    ///   2. Call <see cref="RecordSnapshot"/> externally (e.g. on a fixed timer or
    ///      zone-event channel) to capture the current catalog state.
    ///   3. On <c>_onMatchEnded</c>: stops recording (snapshots are preserved).
    ///
    /// ── Playback ──────────────────────────────────────────────────────────────
    ///   • <see cref="StepForward"/> / <see cref="StepBackward"/> delegate directly
    ///     to the replay SO.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no UI references.
    ///   - All refs are optional; null-safe throughout.
    ///   - Delegates cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - <see cref="RecordSnapshot"/> allocates a bool[] per call (unavoidable
    ///     for snapshot capture); all other paths are zero-allocation.
    ///   - DisallowMultipleComponent — one recorder per arena.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _replaySO      → ZoneControlReplaySO asset.
    ///   2. Assign _catalog       → ControlZoneCatalogSO asset.
    ///   3. Assign _onMatchStarted / _onMatchEnded → match event channels.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlReplayController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlReplaySO     _replaySO;
        [SerializeField] private ControlZoneCatalogSO    _catalog;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMatchEnded;

        // ── Runtime state ─────────────────────────────────────────────────────

        private bool _isRecording;

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

        // ── Event handlers ────────────────────────────────────────────────────

        private void HandleMatchStarted()
        {
            _replaySO?.Reset();
            _isRecording = true;
        }

        private void HandleMatchEnded()
        {
            _isRecording = false;
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Captures the current <see cref="ControlZoneCatalogSO"/> zone states
        /// and stores a snapshot in the replay SO.
        /// No-op when not recording, or when <c>_replaySO</c> or <c>_catalog</c>
        /// is null.
        /// </summary>
        /// <param name="timestamp">Match-elapsed time in seconds.</param>
        public void RecordSnapshot(float timestamp)
        {
            if (!_isRecording || _replaySO == null || _catalog == null) return;

            int count = _catalog.EntryCount;
            var state = new bool[count];
            for (int i = 0; i < count; i++)
            {
                var zone = _catalog.GetZone(i);
                state[i] = zone != null && zone.IsCaptured;
            }
            _replaySO.AddSnapshot(timestamp, state);
        }

        /// <summary>
        /// Advances the replay SO's playback cursor by one step.
        /// Null-safe; no-op when <c>_replaySO</c> is null.
        /// </summary>
        public void StepForward() => _replaySO?.StepForward();

        /// <summary>
        /// Moves the replay SO's playback cursor back by one step.
        /// Null-safe; no-op when <c>_replaySO</c> is null.
        /// </summary>
        public void StepBackward() => _replaySO?.StepBackward();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>True while recording is active (between match start and end).</summary>
        public bool IsRecording => _isRecording;

        /// <summary>The bound replay SO (may be null).</summary>
        public ZoneControlReplaySO ReplaySO => _replaySO;

        /// <summary>The bound catalog SO (may be null).</summary>
        public ControlZoneCatalogSO Catalog => _catalog;
    }
}
