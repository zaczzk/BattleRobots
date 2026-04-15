using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that renders a ranked score leaderboard panel showing the
    /// player and enemy sorted by their current zone-control score from
    /// <see cref="ZoneScoreTrackerSO"/>.
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _panel          → Root container; hidden when <c>_trackerSO</c> is null.
    ///   _listContainer  → Parent Transform where per-participant rows are spawned.
    ///   _rowPrefab      → Prefab with ≥ 3 Text children per row:
    ///                       [0] = Rank label   (e.g. "#1").
    ///                       [1] = Name label   (e.g. "Player" / "Enemy").
    ///                       [2] = Score label  (rounded to nearest integer).
    ///   _emptyLabel     → Shown when <c>_trackerSO</c> is null.
    ///
    /// ── Notes ─────────────────────────────────────────────────────────────────
    ///   Rows are sorted descending by score; equal scores place the player first.
    ///   The participant names are configurable via <c>_playerName</c> /
    ///   <c>_enemyName</c> inspector fields.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - Subscribes to <c>_onLeaderboardUpdated</c> for reactive refresh.
    ///   - All refs optional; null-safe throughout.
    ///   - Delegate cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - DisallowMultipleComponent — one leaderboard panel per HUD.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _trackerSO           → ZoneScoreTrackerSO asset.
    ///   2. Assign _onLeaderboardUpdated → ZoneScoreTrackerSO._onScoreUpdated channel.
    ///   3. Assign _listContainer, _rowPrefab, _emptyLabel, and _panel.
    ///   4. Optionally set _playerName / _enemyName display labels.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlLeaderboardPresenterController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneScoreTrackerSO _trackerSO;

        [Header("Display Names")]
        [SerializeField] private string _playerName = "Player";
        [SerializeField] private string _enemyName  = "Enemy";

        [Header("Event Channels — In (optional)")]
        [Tooltip("Wire to ZoneScoreTrackerSO._onScoreUpdated for reactive refresh.")]
        [SerializeField] private VoidGameEvent _onLeaderboardUpdated;

        [Header("UI Refs (optional)")]
        [SerializeField] private Transform  _listContainer;
        [SerializeField] private GameObject _rowPrefab;
        [SerializeField] private Text       _emptyLabel;
        [SerializeField] private GameObject _panel;

        // ── Cached delegates ──────────────────────────────────────────────────

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
        /// Destroys existing rows and rebuilds the ranked leaderboard from the
        /// current <see cref="ZoneScoreTrackerSO"/> state.
        /// Shows <c>_emptyLabel</c> and hides the panel when
        /// <c>_trackerSO</c> is null.
        /// No-op when <c>_listContainer</c> or <c>_rowPrefab</c> is unassigned.
        /// </summary>
        public void Refresh()
        {
            if (_listContainer == null || _rowPrefab == null) return;

            // Destroy stale rows.
            for (int i = _listContainer.childCount - 1; i >= 0; i--)
                Object.Destroy(_listContainer.GetChild(i).gameObject);

            if (_trackerSO == null)
            {
                _emptyLabel?.gameObject.SetActive(true);
                _panel?.SetActive(false);
                return;
            }

            _emptyLabel?.gameObject.SetActive(false);
            _panel?.SetActive(true);

            // Sort player vs enemy descending by score; ties go to player first.
            float playerScore = _trackerSO.PlayerScore;
            float enemyScore  = _trackerSO.EnemyScore;
            bool  playerFirst = playerScore >= enemyScore;

            SpawnRow(1,
                     playerFirst ? _playerName : _enemyName,
                     playerFirst ? playerScore : enemyScore);

            SpawnRow(2,
                     playerFirst ? _enemyName : _playerName,
                     playerFirst ? enemyScore : playerScore);
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void SpawnRow(int rank, string name, float score)
        {
            GameObject row   = Object.Instantiate(_rowPrefab, _listContainer);
            Text[]     texts = row.GetComponentsInChildren<Text>();

            if (texts.Length > 0) texts[0].text = $"#{rank}";
            if (texts.Length > 1) texts[1].text = name;
            if (texts.Length > 2) texts[2].text = Mathf.RoundToInt(score).ToString();
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound <see cref="ZoneScoreTrackerSO"/>. May be null.</summary>
        public ZoneScoreTrackerSO TrackerSO => _trackerSO;

        /// <summary>Display name used for the player participant.</summary>
        public string PlayerName => _playerName;

        /// <summary>Display name used for the enemy participant.</summary>
        public string EnemyName => _enemyName;
    }
}
