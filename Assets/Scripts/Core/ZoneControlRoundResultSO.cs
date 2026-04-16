using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Stores the result of a single zone-control round (win/loss and score delta).
    /// </summary>
    [Serializable]
    public sealed class ZoneControlRoundResultEntry
    {
        [SerializeField] private bool _playerWon;
        [SerializeField] private int  _scoreDelta;

        public ZoneControlRoundResultEntry(bool playerWon, int scoreDelta)
        {
            _playerWon  = playerWon;
            _scoreDelta = scoreDelta;
        }

        /// <summary>True when the player won this round.</summary>
        public bool PlayerWon  => _playerWon;

        /// <summary>Player score minus opponent score for this round.</summary>
        public int  ScoreDelta => _scoreDelta;
    }

    /// <summary>
    /// Runtime ScriptableObject that records per-round win/loss results for a
    /// zone-control session, capped at a configurable capacity (ring buffer).
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   Call <see cref="RecordResult"/> at the end of each round.
    ///   Oldest entries are evicted when the buffer is full.
    ///   <see cref="WinRate"/> returns the fraction of recorded rounds won.
    ///   Call <see cref="Reset"/> to clear all results.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised — call Reset at session start.
    ///   - Zero heap allocation on the read path.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlRoundResult.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlRoundResult", order = 52)]
    public sealed class ZoneControlRoundResultSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Settings")]
        [Tooltip("Maximum number of round results retained (ring buffer).")]
        [Min(1)]
        [SerializeField] private int _capacity = 5;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised after every RecordResult call.")]
        [SerializeField] private VoidGameEvent _onResultRecorded;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private readonly List<ZoneControlRoundResultEntry> _results =
            new List<ZoneControlRoundResultEntry>();

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Maximum number of results stored.</summary>
        public int Capacity => _capacity;

        /// <summary>Number of results currently stored.</summary>
        public int EntryCount => _results.Count;

        /// <summary>
        /// Fraction of stored rounds won by the player in [0, 1].
        /// Returns 0 when no results have been recorded.
        /// </summary>
        public float WinRate
        {
            get
            {
                if (_results.Count == 0) return 0f;
                int wins = 0;
                foreach (var r in _results)
                    if (r.PlayerWon) wins++;
                return (float)wins / _results.Count;
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Appends a new round result, evicting the oldest entry when capacity is reached.
        /// Fires <see cref="_onResultRecorded"/> after each call.
        /// </summary>
        public void RecordResult(bool playerWon, int scoreDelta)
        {
            _results.Add(new ZoneControlRoundResultEntry(playerWon, scoreDelta));
            while (_results.Count > _capacity)
                _results.RemoveAt(0);
            _onResultRecorded?.Raise();
        }

        /// <summary>Returns a read-only view of the recorded results (oldest first).</summary>
        public IReadOnlyList<ZoneControlRoundResultEntry> GetResults() => _results;

        /// <summary>
        /// Clears all results silently (no events fired).
        /// Called automatically by <c>OnEnable</c>.
        /// </summary>
        public void Reset()
        {
            _results.Clear();
        }

        private void OnValidate()
        {
            _capacity = Mathf.Max(1, _capacity);
        }
    }
}
