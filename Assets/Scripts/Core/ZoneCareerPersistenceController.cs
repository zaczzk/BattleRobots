using System;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// MonoBehaviour that persists career zone-control totals to <see cref="SaveData"/>
    /// at the end of each match.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────
    ///   _onMatchEnded fires → HandleMatchEnded():
    ///     • Null-guard _tracker.
    ///     • Call _tracker.AccumulateToCareer() to add current match scores to career totals.
    ///     • Load → mutate → save round-trip:
    ///         save.careerPlayerZoneScore = _tracker.CareerPlayerScore
    ///         save.careerEnemyZoneScore  = _tracker.CareerEnemyScore
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace.
    ///   - DisallowMultipleComponent — one controller per scene.
    ///   - All refs optional; null refs produce a silent no-op.
    ///   - Delegate cached in Awake; zero heap allocations after initialisation.
    ///
    /// Scene wiring:
    ///   _tracker     → ZoneScoreTrackerSO (source of per-match and career scores).
    ///   _onMatchEnded → VoidGameEvent raised by MatchManager at match end.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneCareerPersistenceController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("Zone score tracker SO. AccumulateToCareer called at match end; " +
                 "career totals then persisted to SaveData.")]
        [SerializeField] private ZoneScoreTrackerSO _tracker;

        [Header("Event Channels — In (optional)")]
        [Tooltip("Raised by MatchManager when the match ends. Triggers HandleMatchEnded().")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handleMatchEndedDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleMatchEndedDelegate = HandleMatchEnded;
        }

        private void OnEnable()
        {
            _onMatchEnded?.RegisterCallback(_handleMatchEndedDelegate);
        }

        private void OnDisable()
        {
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
        }

        // ── Logic ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Accumulates the current match's zone scores into career totals and persists
        /// them to <see cref="SaveData"/> using the standard load → mutate → save pattern.
        /// No-op when <see cref="_tracker"/> is null.
        /// </summary>
        public void HandleMatchEnded()
        {
            if (_tracker == null) return;

            _tracker.AccumulateToCareer();

            SaveData save = SaveSystem.Load();
            save.careerPlayerZoneScore = _tracker.CareerPlayerScore;
            save.careerEnemyZoneScore  = _tracker.CareerEnemyScore;
            SaveSystem.Save(save);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="ZoneScoreTrackerSO"/>. May be null.</summary>
        public ZoneScoreTrackerSO Tracker => _tracker;
    }
}
