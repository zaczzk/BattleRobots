using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that tracks progress towards a long-term career goal
    /// driven by <see cref="ZoneControlCareerGoalSO"/>.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   On <c>_onMatchEnded</c>: reads the relevant metric from the bound SOs
    ///   according to <see cref="ZoneControlCareerGoalSO.GoalType"/>, calls
    ///   <see cref="ZoneControlCareerGoalSO.AddProgress"/>, then refreshes the UI.
    ///   On <c>_onGoalAchieved</c>: refreshes the UI.
    ///   <see cref="Refresh"/> updates the progress bar, label, and completion
    ///   badge.  The panel is hidden when <c>_goalSO</c> is null.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - All inspector fields optional and null-safe.
    ///   - Delegates cached in Awake; zero alloc after init.
    ///   - DisallowMultipleComponent — one career goal controller per scene.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlCareerGoalController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCareerGoalSO     _goalSO;
        [SerializeField] private ZoneControlProfileSO         _profileSO;
        [SerializeField] private ZoneControlSessionSummarySO  _summarySO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onMatchEnded;
        [SerializeField] private VoidGameEvent _onGoalAchieved;

        [Header("UI References (optional)")]
        [SerializeField] private Slider     _progressBar;
        [SerializeField] private Text       _progressLabel;
        [SerializeField] private GameObject _completeBadge;
        [SerializeField] private GameObject _panel;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handleMatchEndedDelegate;
        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleMatchEndedDelegate = HandleMatchEnded;
            _refreshDelegate          = Refresh;
        }

        private void OnEnable()
        {
            _onMatchEnded?.RegisterCallback(_handleMatchEndedDelegate);
            _onGoalAchieved?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
            _onGoalAchieved?.UnregisterCallback(_refreshDelegate);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Reads the match metric matching <see cref="ZoneControlCareerGoalSO.GoalType"/>,
        /// adds progress to the goal SO, then refreshes the display.
        /// </summary>
        public void HandleMatchEnded()
        {
            if (_goalSO == null)
            {
                Refresh();
                return;
            }

            int amount = 0;
            switch (_goalSO.GoalType)
            {
                case ZoneControlCareerGoalType.TotalCaptures:
                    amount = _summarySO?.TotalZonesCaptured ?? 0;
                    break;
                case ZoneControlCareerGoalType.TotalWins:
                    amount = (_profileSO != null && _profileSO.TotalWins > 0) ? 1 : 0;
                    break;
                case ZoneControlCareerGoalType.TotalMatches:
                    amount = 1;
                    break;
            }

            _goalSO.AddProgress(amount);
            Refresh();
        }

        // ── Display ───────────────────────────────────────────────────────────

        /// <summary>
        /// Updates the progress bar, label, and completion badge.
        /// Hides the panel when <c>_goalSO</c> is null.
        /// </summary>
        public void Refresh()
        {
            if (_goalSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_progressBar != null)
                _progressBar.value = _goalSO.Progress;

            if (_progressLabel != null)
            {
                _progressLabel.text = _goalSO.IsAchieved
                    ? "Goal Achieved!"
                    : $"{Mathf.RoundToInt(_goalSO.Progress * 100f)}% Complete";
            }

            _completeBadge?.SetActive(_goalSO.IsAchieved);
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound goal SO (may be null).</summary>
        public ZoneControlCareerGoalSO GoalSO => _goalSO;

        /// <summary>The bound profile SO (may be null).</summary>
        public ZoneControlProfileSO ProfileSO => _profileSO;

        /// <summary>The bound session summary SO (may be null).</summary>
        public ZoneControlSessionSummarySO SummarySO => _summarySO;
    }
}
