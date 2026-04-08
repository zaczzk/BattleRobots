using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// ScriptableObject ring-buffer that records and plays back <see cref="MatchStateSnapshot"/>
    /// frames for post-match replay viewing.
    ///
    /// RECORDING:
    ///   Call <see cref="StartRecording"/> at match start, then <see cref="Record"/> once per
    ///   FixedUpdate tick. Call <see cref="StopRecording"/> at match end — this fires
    ///   <see cref="_onReplayReady"/> so the UI can unlock playback controls.
    ///
    /// PLAYBACK (read path):
    ///   Use <see cref="Seek"/> to retrieve the nearest recorded snapshot to a given elapsed
    ///   time, or <see cref="GetSnapshot"/> for direct ordered-index access.
    ///
    /// RING BUFFER:
    ///   Fixed-size array of <see cref="_capacity"/> entries (default 600 = 60 fps × 10 s).
    ///   When the buffer is full, the oldest entry is silently overwritten. Zero heap
    ///   allocation after <see cref="OnEnable"/> — <see cref="Record"/> performs only
    ///   a struct copy and integer arithmetic.
    ///
    /// ORDERING:
    ///   <see cref="GetSnapshot(int)"/> with index 0 always returns the oldest recorded
    ///   snapshot; index <see cref="SnapshotCount"/>−1 returns the newest.
    ///
    /// Create via Assets ▶ BattleRobots ▶ Replay ▶ ReplaySO.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Replay/ReplaySO", order = 0)]
    public sealed class ReplaySO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Buffer")]
        [Tooltip("Maximum number of snapshots to store. " +
                 "600 = ~10 s at 60 fps. Allocated once at OnEnable.")]
        [SerializeField, Min(1)] private int _capacity = 600;

        [Header("Event Channels")]
        [Tooltip("Fired once when StopRecording() is called and at least one snapshot exists.")]
        [SerializeField] private VoidGameEvent _onReplayReady;

        // ── Internal state ────────────────────────────────────────────────────

        private MatchStateSnapshot[] _buffer;
        private int _head;   // index where the NEXT Record() write will land
        private int _count;  // number of valid entries in the buffer (≤ _capacity)

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>True while <see cref="StartRecording"/> has been called and
        /// <see cref="StopRecording"/> has not yet been called.</summary>
        public bool IsRecording { get; private set; }

        /// <summary>Number of snapshots currently stored (never exceeds capacity).</summary>
        public int SnapshotCount => _count;

        /// <summary>True if no snapshots have been recorded since the last <see cref="Clear"/>.</summary>
        public bool IsEmpty => _count == 0;

        /// <summary>
        /// Elapsed time of the newest recorded snapshot, or 0 if the buffer is empty.
        /// Use as <c>Slider.maxValue</c> in the replay UI.
        /// </summary>
        public float TotalDuration => IsEmpty ? 0f : GetSnapshot(_count - 1).elapsedTime;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void OnEnable()
        {
            EnsureBuffer();
        }

        // ── Recording API ─────────────────────────────────────────────────────

        /// <summary>
        /// Clears the buffer and begins recording. Any previous replay data is discarded.
        /// </summary>
        public void StartRecording()
        {
            EnsureBuffer();
            Clear();
            IsRecording = true;
        }

        /// <summary>
        /// Stops recording and fires <see cref="_onReplayReady"/> if at least one
        /// snapshot was recorded. No-op if not currently recording.
        /// </summary>
        public void StopRecording()
        {
            if (!IsRecording) return;
            IsRecording = false;

            if (!IsEmpty)
                _onReplayReady?.Raise();
        }

        /// <summary>
        /// Appends a snapshot to the ring buffer. When the buffer is full, the oldest
        /// entry is overwritten. No-op when not recording. Zero heap allocation.
        /// </summary>
        /// <param name="snapshot">The snapshot to record (copied by value).</param>
        public void Record(MatchStateSnapshot snapshot)
        {
            if (!IsRecording) return;
            EnsureBuffer();

            _buffer[_head] = snapshot;           // struct copy — zero alloc
            _head          = (_head + 1) % _capacity;
            if (_count < _capacity) _count++;
        }

        // ── Playback / query API ──────────────────────────────────────────────

        /// <summary>
        /// Returns the snapshot whose <c>elapsedTime</c> is nearest to <paramref name="time"/>.
        /// Uses a linear scan (O(n), acceptable for ≤ 600 entries).
        /// Returns <c>default</c> if the buffer is empty.
        /// </summary>
        public MatchStateSnapshot Seek(float time)
        {
            if (IsEmpty) return default;

            int   bestIdx  = 0;
            float bestDist = float.MaxValue;

            for (int i = 0; i < _count; i++)
            {
                float dist = Mathf.Abs(GetSnapshot(i).elapsedTime - time);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestIdx  = i;
                }
            }

            return GetSnapshot(bestIdx);
        }

        /// <summary>
        /// Returns the snapshot at the given ordered index (0 = oldest, <see cref="SnapshotCount"/>−1 = newest).
        /// </summary>
        /// <param name="orderedIndex">Must be in [0, SnapshotCount).</param>
        public MatchStateSnapshot GetSnapshot(int orderedIndex)
        {
            bool isFull   = _count == _capacity;
            int  rawIndex = isFull
                ? (_head + orderedIndex) % _capacity
                : orderedIndex;
            return _buffer[rawIndex];
        }

        /// <summary>
        /// Resets the ring buffer without deallocating. After this call,
        /// <see cref="IsEmpty"/> is true and <see cref="IsRecording"/> is false.
        /// </summary>
        public void Clear()
        {
            EnsureBuffer();
            _head       = 0;
            _count      = 0;
            IsRecording = false;
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void EnsureBuffer()
        {
            if (_buffer == null || _buffer.Length != _capacity)
                _buffer = new MatchStateSnapshot[_capacity];
        }
    }
}
