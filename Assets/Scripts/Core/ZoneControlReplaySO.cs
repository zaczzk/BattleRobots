using System;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// One zone-ownership snapshot captured during a match.
    /// </summary>
    [Serializable]
    public struct ZoneControlSnapshot
    {
        /// <summary>Match-elapsed time when the snapshot was taken.</summary>
        public float timestamp;

        /// <summary>
        /// Per-zone capture state at the time of the snapshot.
        /// Index N corresponds to the N-th zone in the catalog.
        /// May be null or empty when the catalog has no zones.
        /// </summary>
        public bool[] captureState;

        public ZoneControlSnapshot(float timestamp, bool[] captureState)
        {
            this.timestamp    = timestamp;
            // Deep-copy so the ring buffer owns its data.
            if (captureState == null || captureState.Length == 0)
            {
                this.captureState = Array.Empty<bool>();
            }
            else
            {
                this.captureState = new bool[captureState.Length];
                Array.Copy(captureState, this.captureState, captureState.Length);
            }
        }
    }

    /// <summary>
    /// Runtime ScriptableObject that stores a ring buffer of
    /// <see cref="ZoneControlSnapshot"/> frames recorded during a match and exposes
    /// a stepped playback cursor for post-match replay.
    ///
    /// ── Recording ──────────────────────────────────────────────────────────────
    ///   • Call <see cref="AddSnapshot"/> each time a snapshot is taken (e.g. on a
    ///     fixed timer from <see cref="ZoneControlReplayController"/>).
    ///   • The buffer overwrites the oldest snapshot once <see cref="MaxSnapshots"/>
    ///     frames have been stored.
    ///
    /// ── Playback ───────────────────────────────────────────────────────────────
    ///   • Use <see cref="StepForward"/> / <see cref="StepBackward"/> to move the
    ///     <see cref="CurrentStep"/> cursor.
    ///   • Read <see cref="CurrentSnapshot"/> to get the frame at the cursor.
    ///   • <see cref="IsAtStart"/> / <see cref="IsAtEnd"/> report cursor bounds.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised — resets on domain reload.
    ///   - <see cref="AddSnapshot"/> allocates a new bool[] per call; all other
    ///     hot-path methods are zero-allocation.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlReplay.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlReplay", order = 23)]
    public sealed class ZoneControlReplaySO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Settings")]
        [Tooltip("Maximum number of snapshots retained in the ring buffer.")]
        [SerializeField, Min(1)] private int _maxSnapshots = 60;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised after AddSnapshot and after Reset. " +
                 "Wire to ZoneControlReplayHUDController.Refresh.")]
        [SerializeField] private VoidGameEvent _onReplayUpdated;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private ZoneControlSnapshot[] _buffer;
        private int _head;
        private int _count;
        private int _currentStep;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Maximum frames stored in the ring buffer.</summary>
        public int MaxSnapshots => _maxSnapshots;

        /// <summary>Number of frames currently stored (≤ MaxSnapshots).</summary>
        public int Count => _count;

        /// <summary>Index of the currently displayed frame (0 = oldest stored).</summary>
        public int CurrentStep => _currentStep;

        /// <summary>True when <see cref="CurrentStep"/> is at the first frame.</summary>
        public bool IsAtStart => _currentStep <= 0;

        /// <summary>True when <see cref="CurrentStep"/> is at the last stored frame.</summary>
        public bool IsAtEnd => _count == 0 || _currentStep >= _count - 1;

        /// <summary>
        /// The snapshot at the current playback position.
        /// Returns a default snapshot when the buffer is empty.
        /// </summary>
        public ZoneControlSnapshot CurrentSnapshot =>
            _count > 0 ? GetSnapshot(_currentStep) : default;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Adds a snapshot of the current zone ownership state to the ring buffer.
        /// Overwrites the oldest frame once the buffer is full.
        /// Fires <see cref="_onReplayUpdated"/>.
        /// </summary>
        public void AddSnapshot(float timestamp, bool[] captureState)
        {
            EnsureBuffer();
            _buffer[_head] = new ZoneControlSnapshot(timestamp, captureState);
            _head          = (_head + 1) % _maxSnapshots;
            _count         = Mathf.Min(_count + 1, _maxSnapshots);
            // Keep current step clamped to the valid range after buffer wraps.
            _currentStep   = Mathf.Clamp(_currentStep, 0, Mathf.Max(0, _count - 1));
            _onReplayUpdated?.Raise();
        }

        /// <summary>
        /// Returns the snapshot at <paramref name="indexFromOldest"/> (0 = oldest
        /// stored frame). Returns a default snapshot when out of range.
        /// Zero allocation.
        /// </summary>
        public ZoneControlSnapshot GetSnapshot(int indexFromOldest)
        {
            if (_count == 0 || indexFromOldest < 0 || indexFromOldest >= _count)
                return default;

            // With a ring buffer where _head points to the NEXT write slot:
            // oldest slot = (_head - _count + maxSnapshots * k) % maxSnapshots
            int oldest = (_head - _count + _maxSnapshots * 2) % _maxSnapshots;
            int rawIdx = (oldest + indexFromOldest) % _maxSnapshots;
            return _buffer[rawIdx];
        }

        /// <summary>
        /// Advances the playback cursor by one frame (clamped to the last frame).
        /// Zero allocation.
        /// </summary>
        public void StepForward()
        {
            if (_count > 0)
                _currentStep = Mathf.Min(_currentStep + 1, _count - 1);
        }

        /// <summary>
        /// Moves the playback cursor back by one frame (clamped to 0).
        /// Zero allocation.
        /// </summary>
        public void StepBackward()
        {
            _currentStep = Mathf.Max(0, _currentStep - 1);
        }

        /// <summary>
        /// Clears the ring buffer and resets the playback cursor.
        /// Fires <see cref="_onReplayUpdated"/>.
        /// </summary>
        public void Reset()
        {
            _head        = 0;
            _count       = 0;
            _currentStep = 0;
            _onReplayUpdated?.Raise();
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void EnsureBuffer()
        {
            if (_buffer == null || _buffer.Length != _maxSnapshots)
                _buffer = new ZoneControlSnapshot[_maxSnapshots];
        }
    }
}
