using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Populates a scrollable UI panel with the local match leaderboard, reading
    /// entries from a <see cref="MatchLeaderboardSO"/> and rebuilding the row list
    /// whenever <c>_onLeaderboardUpdated</c> fires.
    ///
    /// ── Row layout convention ────────────────────────────────────────────────
    ///   Each instantiated row prefab is expected to have at least five
    ///   <see cref="UnityEngine.UI.Text"/> components (found via
    ///   <c>GetComponentsInChildren&lt;Text&gt;(true)</c> in index order):
    ///     [0] Rank         — "1", "2", … "N"
    ///     [1] Score        — the numeric match score
    ///     [2] Result       — "WIN" or "LOSS"
    ///     [3] Opponent     — opponent display name (empty string when none selected)
    ///     [4] Duration     — match length formatted as "Xm Ys" or "Ys"
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - Delegates cached in Awake; no closures or allocs after Awake.
    ///   - No Update; refresh is entirely event-driven via VoidGameEvent subscription.
    ///   - All inspector fields are optional; any unassigned field is silently skipped.
    ///   - <see cref="FormatDuration"/> is internal static for testability via reflection.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Add this component to the leaderboard panel root GameObject.
    ///   2. Assign <c>_leaderboard</c> — the same <see cref="MatchLeaderboardSO"/> SO
    ///      used by <see cref="MatchManager"/> and <see cref="GameBootstrapper"/>.
    ///   3. Assign <c>_onLeaderboardUpdated</c> — the same VoidGameEvent SO wired
    ///      to <c>MatchLeaderboardSO._onLeaderboardUpdated</c>.
    ///   4. Create a row prefab with five child Text components and assign it to
    ///      <c>_rowPrefab</c>. Assign the scroll content rect as <c>_listContainer</c>.
    ///   5. Optionally assign <c>_emptyLabel</c> — a GameObject shown when the board
    ///      has no entries (e.g. a "No scores yet!" label).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MatchLeaderboardController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data Source")]
        [Tooltip("Runtime SO that holds the sorted leaderboard entries. " +
                 "Leave null to suppress all display.")]
        [SerializeField] private MatchLeaderboardSO _leaderboard;

        [Header("Event Channel — In")]
        [Tooltip("VoidGameEvent raised by MatchLeaderboardSO after Submit(). " +
                 "Wire the same asset assigned to MatchLeaderboardSO._onLeaderboardUpdated. " +
                 "Leave null if the panel only needs to display data on open (no live refresh).")]
        [SerializeField] private VoidGameEvent _onLeaderboardUpdated;

        [Header("UI Refs (optional)")]
        [Tooltip("Parent transform under which row prefabs are instantiated " +
                 "(typically a VerticalLayoutGroup inside a ScrollView content rect).")]
        [SerializeField] private Transform _listContainer;

        [Tooltip("Prefab with at least 5 child Text components: " +
                 "[0] Rank, [1] Score, [2] Result, [3] Opponent, [4] Duration.")]
        [SerializeField] private GameObject _rowPrefab;

        [Tooltip("GameObject shown when the leaderboard has no entries " +
                 "(e.g. a 'No scores yet!' label). Hidden when entries exist.")]
        [SerializeField] private GameObject _emptyLabel;

        // ── Cached delegate ───────────────────────────────────────────────────

        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onLeaderboardUpdated?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onLeaderboardUpdated?.UnregisterCallback(_refreshDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Destroys all existing row children of <c>_listContainer</c> and rebuilds
        /// one row per <see cref="MatchLeaderboardSO.Entries"/> entry.
        ///
        /// Toggles <c>_emptyLabel</c> based on whether the board has any entries.
        /// Safe to call when any optional field is null (silently skipped).
        /// </summary>
        public void Refresh()
        {
            bool hasEntries = _leaderboard != null && _leaderboard.Entries.Count > 0;

            _emptyLabel?.SetActive(!hasEntries);

            if (_listContainer == null) return;

            // Destroy all existing row children.
            for (int i = _listContainer.childCount - 1; i >= 0; i--)
                Destroy(_listContainer.GetChild(i).gameObject);

            if (_leaderboard == null || _rowPrefab == null) return;

            var entries = _leaderboard.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                LeaderboardEntry entry = entries[i];

                GameObject row = Instantiate(_rowPrefab, _listContainer);
                Text[] texts  = row.GetComponentsInChildren<Text>(true);

                // [0] Rank
                if (texts.Length > 0) texts[0].text = (i + 1).ToString();
                // [1] Score
                if (texts.Length > 1) texts[1].text = entry.score.ToString();
                // [2] Result
                if (texts.Length > 2) texts[2].text = entry.playerWon ? "WIN" : "LOSS";
                // [3] Opponent
                if (texts.Length > 3) texts[3].text = entry.opponentName;
                // [4] Duration
                if (texts.Length > 4) texts[4].text = FormatDuration(entry.durationSeconds);
            }
        }

        // ── Internal helpers ──────────────────────────────────────────────────

        /// <summary>
        /// Converts a duration in seconds to a human-readable string.
        /// Negative input is clamped to zero.
        /// Examples: 0s→"0s", 30s→"30s", 65s→"1m 5s", 120s→"2m 0s".
        /// Testable via reflection from <see cref="MatchLeaderboardControllerTests"/>.
        /// </summary>
        internal static string FormatDuration(float seconds)
        {
            if (seconds < 0f) seconds = 0f;
            int total = Mathf.RoundToInt(seconds);
            int mins  = total / 60;
            int secs  = total % 60;
            return mins > 0 ? $"{mins}m {secs}s" : $"{secs}s";
        }
    }
}
