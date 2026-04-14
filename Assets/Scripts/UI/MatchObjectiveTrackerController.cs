using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// Post-match summary panel that reads <see cref="MatchObjectiveTrackerSO"/> and
    /// shows how many objectives were completed versus total resolved.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────
    ///   <c>_onMatchStarted</c>       → HandleMatchStarted (resets tracker, hides panel).
    ///   <c>_onMatchEnded</c>         → Refresh (shows final results).
    ///   <c>_onObjectiveCompleted</c> → Refresh (live count update during match).
    ///   Refresh reads <see cref="MatchObjectiveTrackerSO"/> and sets:
    ///     • <c>_summaryLabel</c>              — "N/M objectives completed"
    ///     • <c>_completionRatioSlider</c>.value — CompletionRatio [0, 1]
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   - All delegates cached in Awake; zero heap allocations after initialisation.
    ///   - DisallowMultipleComponent — one tracker HUD per canvas.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign <c>_tracker</c> — MatchObjectiveTrackerSO asset.
    ///   2. Assign <c>_onMatchStarted</c>, <c>_onMatchEnded</c>, and optionally
    ///      <c>_onObjectiveCompleted</c> event channels.
    ///   3. Assign <c>_panel</c>, <c>_summaryLabel</c>, and optionally
    ///      <c>_completionRatioSlider</c>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MatchObjectiveTrackerController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("MatchObjectiveTrackerSO populated during the match.")]
        [SerializeField] private MatchObjectiveTrackerSO _tracker;

        [Header("Event Channels — In (optional)")]
        [Tooltip("VoidGameEvent raised by MatchManager at match start. " +
                 "Resets tracker and hides the panel.")]
        [SerializeField] private VoidGameEvent _onMatchStarted;

        [Tooltip("VoidGameEvent raised by MatchManager at match end. Triggers Refresh.")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        [Tooltip("VoidGameEvent raised by MatchObjectiveTrackerSO when an objective is " +
                 "completed. Triggers an in-match live Refresh.")]
        [SerializeField] private VoidGameEvent _onObjectiveCompleted;

        [Header("UI References (optional)")]
        [Tooltip("Root panel; shown when a tracker is assigned.")]
        [SerializeField] private GameObject _panel;

        [Tooltip("Label showing 'N/M objectives completed'.")]
        [SerializeField] private Text _summaryLabel;

        [Tooltip("Slider driven by CompletionRatio [0, 1].")]
        [SerializeField] private Slider _completionRatioSlider;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _matchStartDelegate;
        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _matchStartDelegate = HandleMatchStarted;
            _refreshDelegate    = Refresh;
        }

        private void OnEnable()
        {
            _onMatchStarted?.RegisterCallback(_matchStartDelegate);
            _onMatchEnded?.RegisterCallback(_refreshDelegate);
            _onObjectiveCompleted?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMatchStarted?.UnregisterCallback(_matchStartDelegate);
            _onMatchEnded?.UnregisterCallback(_refreshDelegate);
            _onObjectiveCompleted?.UnregisterCallback(_refreshDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="MatchObjectiveTrackerSO"/>. May be null.</summary>
        public MatchObjectiveTrackerSO Tracker => _tracker;

        /// <summary>
        /// Resets the tracker at match start and hides the summary panel.
        /// </summary>
        public void HandleMatchStarted()
        {
            _tracker?.Reset();
            _panel?.SetActive(false);
        }

        /// <summary>
        /// Reads <see cref="MatchObjectiveTrackerSO"/> and updates all UI elements.
        /// Hides the panel when <c>_tracker</c> is null. Fully null-safe.
        /// </summary>
        public void Refresh()
        {
            if (_tracker == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_summaryLabel != null)
                _summaryLabel.text = string.Format(
                    "{0}/{1} objectives completed",
                    _tracker.CompletedCount,
                    _tracker.TotalTracked);

            if (_completionRatioSlider != null)
                _completionRatioSlider.value = _tracker.CompletionRatio;
        }
    }
}
