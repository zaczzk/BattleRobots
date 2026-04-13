using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// Per-step floating tooltip label that follows the
    /// <see cref="TutorialHighlightOverlay"/>'s highlight frame.
    ///
    /// For each tutorial step the overlay:
    ///   1. Resolves the current step ID from <c>_sequence</c> + <c>_progress</c>
    ///      (first step not yet completed).
    ///   2. Looks up the spotlight element's Unity scene tag via <c>_highlightConfig</c>.
    ///   3. Fetches the tooltip hint text for that tag from <c>_tooltipConfig</c>.
    ///   4. Populates <c>_tooltipLabel</c>, positions <c>_tooltipPanel</c> near
    ///      <c>_highlightFrame</c>, and shows the panel.
    ///
    /// The panel is hidden automatically when any of the following are true:
    ///   • <c>_progress</c>, <c>_sequence</c>, <c>_highlightConfig</c>, or
    ///     <c>_tooltipConfig</c> is null.
    ///   • <c>_progress.IsComplete</c> is true.
    ///   • The current step has no tag configured in <c>_highlightConfig</c>.
    ///   • The resolved tag has no tooltip text in <c>_tooltipConfig</c>
    ///     (empty tooltipText entries produce no tooltip).
    ///
    /// ── Inspector wiring ─────────────────────────────────────────────────────────
    ///   Data (all optional):
    ///     _tooltipConfig   → <see cref="TutorialTooltipConfig"/> mapping panel tags to
    ///                        hint texts.  Shared with <see cref="TutorialTooltipController"/>.
    ///     _highlightConfig → <see cref="TutorialHighlightConfig"/> mapping step IDs to
    ///                        panel tags.  Shared with <see cref="TutorialHighlightOverlay"/>.
    ///     _sequence        → Shared <see cref="TutorialSequenceSO"/> used by
    ///                        <see cref="TutorialController"/>.
    ///     _progress        → Shared <see cref="TutorialProgressSO"/> used by
    ///                        <see cref="TutorialController"/>.
    ///   Event Channels — In (optional):
    ///     _onStepCompleted     → Raised by <see cref="TutorialProgressSO"/> after each step
    ///                            is acknowledged.  Triggers <see cref="Refresh"/>.
    ///     _onTutorialCompleted → Raised when the tutorial ends or is skipped.
    ///                            Triggers <see cref="Hide"/>.
    ///   UI Refs (optional):
    ///     _tooltipPanel   → Root panel for the floating label.
    ///                       Shown when a tooltip is available for the current step.
    ///     _tooltipLabel   → <see cref="Text"/> populated with the resolved tooltip hint.
    ///     _highlightFrame → <see cref="RectTransform"/> of the spotlight frame from
    ///                       <see cref="TutorialHighlightOverlay"/>.  The tooltip panel is
    ///                       repositioned to match this frame's world position on each refresh.
    ///
    /// ── Architecture notes ───────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no BattleRobots.Physics references.
    ///   - No Update / FixedUpdate loop — driven entirely by event channels.
    ///   - All inspector fields are optional; fully null-safe throughout.
    ///   - No heap allocations per-frame (delegates cached in Awake).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TutorialTooltipOverlay : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("Maps UI panel tags to tooltip hint texts. " +
                 "Shared with TutorialTooltipController. " +
                 "Steps whose resolved tag has no entry produce no tooltip — the panel hides.")]
        [SerializeField] private TutorialTooltipConfig _tooltipConfig;

        [Tooltip("Maps tutorial step IDs to Unity scene tags. " +
                 "Shared with TutorialHighlightOverlay. " +
                 "Steps with no entry have no tag, so no tooltip text can be resolved.")]
        [SerializeField] private TutorialHighlightConfig _highlightConfig;

        [Tooltip("Shared sequence SO — must be the same asset used by TutorialController.")]
        [SerializeField] private TutorialSequenceSO _sequence;

        [Tooltip("Shared progress SO — must be the same asset used by TutorialController.")]
        [SerializeField] private TutorialProgressSO _progress;

        [Header("Event Channels — In (optional)")]
        [Tooltip("Raised by TutorialProgressSO._onStepCompleted after each step is acknowledged. " +
                 "TutorialTooltipOverlay subscribes here to refresh the floating label.")]
        [SerializeField] private VoidGameEvent _onStepCompleted;

        [Tooltip("Raised when the tutorial completes or is skipped. " +
                 "TutorialTooltipOverlay subscribes here to hide the floating label.")]
        [SerializeField] private VoidGameEvent _onTutorialCompleted;

        [Header("UI Refs (optional)")]
        [Tooltip("Root panel of the floating tooltip label. " +
                 "Shown when tooltip text is available for the current step; hidden otherwise.")]
        [SerializeField] private GameObject _tooltipPanel;

        [Tooltip("Text component populated with the resolved tooltip hint for the current step. " +
                 "Unchanged when _tooltipConfig is null or no entry matches the current tag.")]
        [SerializeField] private Text _tooltipLabel;

        [Tooltip("RectTransform of the TutorialHighlightOverlay spotlight frame. " +
                 "The tooltip panel's world position is set to match this frame on each refresh. " +
                 "Leave null — the panel still shows and hides, just without repositioning.")]
        [SerializeField] private RectTransform _highlightFrame;

        // ── Runtime ───────────────────────────────────────────────────────────

        private Action _refreshDelegate;
        private Action _hideDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;
            _hideDelegate    = Hide;
        }

        private void OnEnable()
        {
            _onStepCompleted?.RegisterCallback(_refreshDelegate);
            _onTutorialCompleted?.RegisterCallback(_hideDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onStepCompleted?.UnregisterCallback(_refreshDelegate);
            _onTutorialCompleted?.UnregisterCallback(_hideDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Resolves the current tutorial step, looks up its panel tag and tooltip text,
        /// optionally repositions <c>_tooltipPanel</c> near <c>_highlightFrame</c>, then
        /// shows the panel.
        ///
        /// Hides the panel when any required data is missing or unresolvable.
        /// </summary>
        public void Refresh()
        {
            if (_progress == null || _progress.IsComplete ||
                _sequence == null || _highlightConfig == null || _tooltipConfig == null)
            {
                Hide();
                return;
            }

            string stepId = GetCurrentStepId();
            if (string.IsNullOrEmpty(stepId))
            {
                Hide();
                return;
            }

            string tag = _highlightConfig.GetTagForStep(stepId);
            if (string.IsNullOrEmpty(tag))
            {
                Hide();
                return;
            }

            string text = _tooltipConfig.GetTooltipForTag(tag);
            if (string.IsNullOrEmpty(text))
            {
                Hide();
                return;
            }

            if (_tooltipLabel != null)
                _tooltipLabel.text = text;

            // Reposition next to the spotlight frame when both refs are wired.
            if (_tooltipPanel != null && _highlightFrame != null)
                _tooltipPanel.transform.position = _highlightFrame.position;

            _tooltipPanel?.SetActive(true);
        }

        /// <summary>
        /// Hides the tooltip panel immediately.
        /// Called when the tutorial completes, is skipped, or no tooltip is available
        /// for the current step.
        /// </summary>
        public void Hide()
        {
            _tooltipPanel?.SetActive(false);
        }

        // ── Internal ──────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the step ID of the first step in <c>_sequence</c> that has not yet
        /// been completed in <c>_progress</c>.
        ///
        /// Returns <see cref="string.Empty"/> when the sequence is empty or all steps
        /// are already marked complete. Null step entries are skipped silently.
        /// </summary>
        private string GetCurrentStepId()
        {
            for (int i = 0; i < _sequence.Count; i++)
            {
                TutorialStepSO step = _sequence.Steps[i];
                if (step == null) continue;
                if (!_progress.HasCompletedStep(step.StepId))
                    return step.StepId;
            }

            return string.Empty;
        }
    }
}
