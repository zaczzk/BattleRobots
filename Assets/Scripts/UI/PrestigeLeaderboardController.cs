using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Variant of <see cref="MatchLeaderboardController"/> that augments each row
    /// with the player's current prestige rank label and activates a "Legend" badge
    /// when <see cref="PrestigeSystemSO.IsMaxPrestige"/> is true.
    ///
    /// ── Row layout convention ─────────────────────────────────────────────────
    ///   Each instantiated row prefab must have at least four
    ///   <see cref="UnityEngine.UI.Text"/> components (found via
    ///   <c>GetComponentsInChildren&lt;Text&gt;(true)</c>) in index order:
    ///     [0] Rank         — "1", "2", … "N"
    ///     [1] Score        — numeric match score
    ///     [2] Result       — "WIN" or "LOSS"
    ///     [3] Prestige     — prestige rank label (e.g. "Gold II") or empty string
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; must NOT reference BattleRobots.Physics.
    ///   - DisallowMultipleComponent — one prestige leaderboard per canvas.
    ///   - Subscribes to both _onLeaderboardUpdated and _onPrestige to rebuild on
    ///     either score submission or prestige-rank change.
    ///   - Delegate cached in Awake; zero heap allocations after initialisation.
    ///   - All inspector fields are optional; unassigned refs are silently skipped.
    ///
    /// Scene wiring:
    ///   _leaderboard           → MatchLeaderboardSO.
    ///   _prestigeSystem        → PrestigeSystemSO.
    ///   _onLeaderboardUpdated  → VoidGameEvent raised by MatchLeaderboardSO.Submit().
    ///   _onPrestige            → VoidGameEvent raised by PrestigeSystemSO.Prestige().
    ///   _listContainer         → Scroll content rect for row prefabs.
    ///   _rowPrefab             → Prefab with ≥4 child Text components.
    ///   _prestigeRankLabel     → Text showing current prestige rank label.
    ///   _legendBadge           → GameObject shown when IsMaxPrestige is true.
    ///   _emptyLabel            → GameObject shown when the leaderboard is empty.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PrestigeLeaderboardController : MonoBehaviour
    {
        // ── Inspector — Data ──────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("Runtime leaderboard SO. Leave null to suppress all display.")]
        [SerializeField] private MatchLeaderboardSO _leaderboard;

        [Tooltip("Runtime prestige SO. Provides rank label and max-prestige flag.")]
        [SerializeField] private PrestigeSystemSO _prestigeSystem;

        // ── Inspector — Event Channels ────────────────────────────────────────

        [Header("Event Channels — In")]
        [Tooltip("Raised by MatchLeaderboardSO.Submit(). Triggers Refresh().")]
        [SerializeField] private VoidGameEvent _onLeaderboardUpdated;

        [Tooltip("Raised by PrestigeSystemSO.Prestige(). Triggers Refresh() so that " +
                 "rank labels and the legend badge update immediately.")]
        [SerializeField] private VoidGameEvent _onPrestige;

        // ── Inspector — UI Refs (optional) ────────────────────────────────────

        [Header("UI Refs (optional)")]
        [Tooltip("Parent transform for row prefab instances.")]
        [SerializeField] private Transform _listContainer;

        [Tooltip("Prefab with ≥4 child Texts: [0] Rank, [1] Score, [2] Result, [3] Prestige.")]
        [SerializeField] private GameObject _rowPrefab;

        [Tooltip("Text showing the player's current prestige rank label.")]
        [SerializeField] private Text _prestigeRankLabel;

        [Tooltip("GameObject shown when the player has reached maximum prestige.")]
        [SerializeField] private GameObject _legendBadge;

        [Tooltip("GameObject shown when the leaderboard has no entries.")]
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
            _onPrestige?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onLeaderboardUpdated?.UnregisterCallback(_refreshDelegate);
            _onPrestige?.UnregisterCallback(_refreshDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Refreshes the prestige rank label, legend badge, and the sorted leaderboard
        /// row list from <see cref="_leaderboard"/> and <see cref="_prestigeSystem"/>.
        /// Fully null-safe on all optional refs.
        /// </summary>
        public void Refresh()
        {
            // Prestige header.
            if (_prestigeRankLabel != null)
                _prestigeRankLabel.text = _prestigeSystem != null
                    ? _prestigeSystem.GetRankLabel()
                    : "—";

            _legendBadge?.SetActive(_prestigeSystem != null && _prestigeSystem.IsMaxPrestige);

            bool hasEntries = _leaderboard != null && _leaderboard.Entries.Count > 0;
            _emptyLabel?.SetActive(!hasEntries);

            if (_listContainer == null) return;

            // Destroy stale rows.
            for (int i = _listContainer.childCount - 1; i >= 0; i--)
                Destroy(_listContainer.GetChild(i).gameObject);

            if (_leaderboard == null || _rowPrefab == null) return;

            string rankLabel = _prestigeSystem != null
                ? _prestigeSystem.GetRankLabel()
                : string.Empty;

            var entries = _leaderboard.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                LeaderboardEntry entry = entries[i];

                GameObject row = Instantiate(_rowPrefab, _listContainer);
                Text[]     texts = row.GetComponentsInChildren<Text>(true);

                if (texts.Length > 0) texts[0].text = (i + 1).ToString();
                if (texts.Length > 1) texts[1].text = entry.score.ToString();
                if (texts.Length > 2) texts[2].text = entry.playerWon ? "WIN" : "LOSS";
                if (texts.Length > 3) texts[3].text = rankLabel;
            }
        }

        /// <summary>The assigned <see cref="MatchLeaderboardSO"/>. May be null.</summary>
        public MatchLeaderboardSO Leaderboard => _leaderboard;

        /// <summary>The assigned <see cref="PrestigeSystemSO"/>. May be null.</summary>
        public PrestigeSystemSO PrestigeSystem => _prestigeSystem;
    }
}
