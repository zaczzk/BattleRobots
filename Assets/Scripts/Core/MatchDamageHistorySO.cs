using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Snapshot of per-damage-type totals for a single match.
    ///
    /// Populated by <see cref="BattleRobots.UI.PostMatchDamageHistoryController"/>
    /// from <see cref="MatchStatisticsSO"/> at match end.
    /// </summary>
    [System.Serializable]
    public struct DamageTypeSnapshot
    {
        /// <summary>Total Physical damage dealt this match.</summary>
        public float physical;

        /// <summary>Total Energy damage dealt this match.</summary>
        public float energy;

        /// <summary>Total Thermal damage dealt this match.</summary>
        public float thermal;

        /// <summary>Total Shock damage dealt this match.</summary>
        public float shock;
    }

    /// <summary>
    /// Runtime SO that maintains a fixed-size ring buffer of
    /// <see cref="DamageTypeSnapshot"/> records (one per completed match).
    ///
    /// ── Responsibilities ──────────────────────────────────────────────────────
    ///   • Stores per-type damage totals for the last <see cref="MaxHistory"/>
    ///     matches in a ring buffer (oldest entry overwritten first).
    ///   • Exposes <see cref="GetRollingAverage"/> to compute the mean damage per
    ///     type across all stored entries.
    ///   • <see cref="Clear"/> wipes the buffer without reallocating.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Ring buffer is initialised once in <c>OnEnable</c> and reused.
    ///     <see cref="AddEntry"/> re-initialises lazily if the buffer is null or
    ///     its size no longer matches <see cref="MaxHistory"/> (e.g. after
    ///     <see cref="MaxHistory"/> is changed via Inspector or tests).
    ///   - Zero allocation on the hot path: struct writes + integer arithmetic.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ MatchDamageHistory.
    /// Assign to <see cref="BattleRobots.UI.PostMatchDamageHistoryController"/>.
    /// </summary>
    [CreateAssetMenu(
        menuName = "BattleRobots/Core/MatchDamageHistory",
        fileName = "MatchDamageHistory")]
    public sealed class MatchDamageHistorySO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Settings")]
        [Tooltip("Maximum number of past-match snapshots to retain. " +
                 "Oldest entry is overwritten first once the buffer is full.")]
        [SerializeField, Min(1)] private int _maxHistory = 10;

        // ── Runtime state ─────────────────────────────────────────────────────

        private DamageTypeSnapshot[] _entries;
        private int                  _head;    // index of the NEXT write position
        private int                  _count;   // number of valid entries (≤ MaxHistory)

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Maximum number of match snapshots retained in the ring buffer.</summary>
        public int MaxHistory => _maxHistory;

        /// <summary>
        /// Number of snapshots currently stored.
        /// Always in [0, <see cref="MaxHistory"/>].
        /// </summary>
        public int Count => _count;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable()
        {
            InitBuffer();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Adds a new per-type damage snapshot to the ring buffer.
        ///
        /// <para>When the buffer is full the oldest entry is overwritten.
        /// The internal buffer is re-initialised automatically if its size no longer
        /// matches <see cref="MaxHistory"/>.</para>
        /// </summary>
        public void AddEntry(DamageTypeSnapshot snapshot)
        {
            if (_entries == null || _entries.Length != _maxHistory)
                InitBuffer();

            _entries[_head] = snapshot;
            _head            = (_head + 1) % _maxHistory;
            if (_count < _maxHistory) _count++;
        }

        /// <summary>
        /// Returns the rolling average of the given damage type across all stored snapshots.
        ///
        /// <para>Returns 0 when the buffer is empty or <paramref name="type"/> is unknown.</para>
        /// </summary>
        public float GetRollingAverage(DamageType type)
        {
            if (_entries == null || _count == 0) return 0f;

            float sum = 0f;
            for (int i = 0; i < _count; i++)
            {
                DamageTypeSnapshot s = _entries[i];
                switch (type)
                {
                    case DamageType.Physical: sum += s.physical; break;
                    case DamageType.Energy:   sum += s.energy;   break;
                    case DamageType.Thermal:  sum += s.thermal;  break;
                    case DamageType.Shock:    sum += s.shock;    break;
                }
            }
            return sum / _count;
        }

        /// <summary>
        /// Resets the ring buffer, discarding all stored snapshots.
        /// Does not reallocate the underlying array.
        /// </summary>
        public void Clear()
        {
            _head  = 0;
            _count = 0;

            if (_entries != null)
            {
                for (int i = 0; i < _entries.Length; i++)
                    _entries[i] = default;
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void InitBuffer()
        {
            _entries = new DamageTypeSnapshot[_maxHistory];
            _head    = 0;
            _count   = 0;
        }
    }
}
