using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Live HUD panel that shows progress toward the nearest incomplete achievement.
    ///
    /// ── Which achievement is shown ────────────────────────────────────────────
    ///   Scans <see cref="AchievementCatalogSO.Achievements"/> in order and picks
    ///   the first entry that has NOT yet been unlocked in
    ///   <see cref="PlayerAchievementsSO"/>. The matching entry's name and progress
    ///   ratio are displayed. If all achievements are unlocked the panel is hidden
    ///   and <c>_achievementNameLabel</c> shows "All Complete!".
    ///
    /// ── Progress mapping ─────────────────────────────────────────────────────
    ///   <see cref="AchievementTrigger.MatchWon"/>    → TotalMatchesWon  / TargetCount
    ///   <see cref="AchievementTrigger.WinStreak"/>   → BestStreak       / TargetCount
    ///   <see cref="AchievementTrigger.ReachLevel"/>  → CurrentLevel     / TargetCount
    ///   <see cref="AchievementTrigger.TotalMatches"/>→ TotalMatchesPlayed / TargetCount
    ///   <see cref="AchievementTrigger.PartUpgraded"/> → 0 (not tracked here)
    ///   Unknown trigger                              → 0
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────
    ///   Subscribe <c>_onMatchEnded</c> to refresh after each match; also refreshes
    ///   on <c>OnEnable</c>.
    ///
    /// ── Architecture notes ───────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   - DisallowMultipleComponent — one achievement HUD per canvas.
    ///   - All inspector fields optional — safe with no refs assigned.
    ///   - Delegate cached in Awake; zero heap allocations after initialisation.
    ///
    /// Scene wiring:
    ///   _catalog               → shared AchievementCatalogSO asset.
    ///   _playerAchievements    → shared PlayerAchievementsSO runtime SO.
    ///   _winStreak             → shared WinStreakSO (for WinStreak trigger).
    ///   _progression           → shared PlayerProgressionSO (for ReachLevel trigger).
    ///   _onMatchEnded          → same VoidGameEvent as MatchManager raises.
    ///   _panel                 → root HUD panel.
    ///   _achievementNameLabel  → Text showing the achievement display name.
    ///   _progressLabel         → Text showing "N / target".
    ///   _progressBar           → Slider showing progress ratio [0,1].
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AchievementProgressHUDController : MonoBehaviour
    {
        // ── Inspector — Data ─────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("Catalog listing all achievement definitions.")]
        [SerializeField] private AchievementCatalogSO _catalog;

        [Tooltip("Runtime SO tracking unlocked achievements and match counters.")]
        [SerializeField] private PlayerAchievementsSO _playerAchievements;

        [Tooltip("WinStreakSO — used for WinStreak trigger progress.")]
        [SerializeField] private WinStreakSO _winStreak;

        [Tooltip("PlayerProgressionSO — used for ReachLevel trigger progress.")]
        [SerializeField] private PlayerProgressionSO _progression;

        // ── Inspector — Event ────────────────────────────────────────────────

        [Header("Event Channel — In (optional)")]
        [Tooltip("Raised by MatchManager at the end of each match.")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        // ── Inspector — UI ───────────────────────────────────────────────────

        [Header("UI (all optional)")]
        [Tooltip("Root HUD panel.")]
        [SerializeField] private GameObject _panel;

        [Tooltip("Text receiving the achievement display name or 'All Complete!'.")]
        [SerializeField] private Text _achievementNameLabel;

        [Tooltip("Text receiving 'N / target'.")]
        [SerializeField] private Text _progressLabel;

        [Tooltip("Slider fill bar showing progress toward the achievement [0, 1].")]
        [SerializeField] private Slider _progressBar;

        // ── Cached delegate ──────────────────────────────────────────────────

        private Action _refreshDelegate;

        // ── Lifecycle ────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onMatchEnded?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMatchEnded?.UnregisterCallback(_refreshDelegate);
        }

        // ── Logic ────────────────────────────────────────────────────────────

        /// <summary>
        /// Scans the catalog for the first incomplete achievement and updates the HUD.
        /// Safe to call with any combination of null references.
        /// </summary>
        public void Refresh()
        {
            if (_catalog == null || _playerAchievements == null)
            {
                _panel?.SetActive(false);
                return;
            }

            AchievementDefinitionSO next = FindNextIncomplete();

            if (next == null)
            {
                // All achievements unlocked.
                if (_achievementNameLabel != null)
                    _achievementNameLabel.text = "All Complete!";
                if (_progressLabel != null)
                    _progressLabel.text = string.Empty;
                if (_progressBar != null)
                    _progressBar.value = 1f;
                _panel?.SetActive(false);
                return;
            }

            int current  = GetCurrentProgress(next);
            int target   = next.TargetCount;
            float ratio  = target > 0 ? Mathf.Clamp01((float)current / target) : 0f;

            if (_achievementNameLabel != null)
                _achievementNameLabel.text = next.DisplayName;

            if (_progressLabel != null)
                _progressLabel.text = string.Format("{0} / {1}", current, target);

            if (_progressBar != null)
                _progressBar.value = ratio;

            _panel?.SetActive(true);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private AchievementDefinitionSO FindNextIncomplete()
        {
            var achievements = _catalog.Achievements;
            for (int i = 0; i < achievements.Count; i++)
            {
                var def = achievements[i];
                if (def == null) continue;
                if (!_playerAchievements.HasUnlocked(def.Id))
                    return def;
            }
            return null;
        }

        /// <summary>
        /// Returns the player's current progress value for the given achievement
        /// based on its <see cref="AchievementTrigger"/> type.
        /// </summary>
        internal int GetCurrentProgress(AchievementDefinitionSO def)
        {
            if (def == null) return 0;

            switch (def.TriggerType)
            {
                case AchievementTrigger.MatchWon:
                    return _playerAchievements?.TotalMatchesWon ?? 0;

                case AchievementTrigger.WinStreak:
                    return _winStreak?.BestStreak ?? 0;

                case AchievementTrigger.ReachLevel:
                    return _progression?.CurrentLevel ?? 0;

                case AchievementTrigger.TotalMatches:
                    return _playerAchievements?.TotalMatchesPlayed ?? 0;

                default:
                    return 0;
            }
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="AchievementCatalogSO"/>. May be null.</summary>
        public AchievementCatalogSO Catalog => _catalog;

        /// <summary>The assigned <see cref="PlayerAchievementsSO"/>. May be null.</summary>
        public PlayerAchievementsSO PlayerAchievements => _playerAchievements;

        /// <summary>The assigned <see cref="WinStreakSO"/>. May be null.</summary>
        public WinStreakSO WinStreak => _winStreak;

        /// <summary>The assigned <see cref="PlayerProgressionSO"/>. May be null.</summary>
        public PlayerProgressionSO Progression => _progression;
    }
}
