using System;
using BattleRobots.Core;
using UnityEngine;

namespace BattleRobots.UI
{
    /// <summary>
    /// Semi-transparent overlay that spotlights a tagged UI element during each
    /// tutorial step.  Consults <see cref="TutorialHighlightConfig"/> to find the
    /// scene tag for the current step, locates the tagged <c>GameObject</c>, and
    /// positions <c>_highlightFrame</c> around it.
    ///
    /// ── How it determines the current step ────────────────────────────────────────
    ///   Walks the shared <see cref="TutorialSequenceSO"/> in order and returns the
    ///   first step not yet completed in the shared <see cref="TutorialProgressSO"/>.
    ///   This keeps TutorialController and TutorialHighlightOverlay automatically in
    ///   sync — they share the same SO assets and communicate via the
    ///   <c>_onStepCompleted</c> event channel.
    ///
    /// ── Inspector wiring ─────────────────────────────────────────────────────────
    ///   Data (all optional):
    ///     _config      → TutorialHighlightConfig SO mapping stepIds to scene tags.
    ///     _sequence    → TutorialSequenceSO shared with TutorialController.
    ///     _progress    → TutorialProgressSO shared with TutorialController.
    ///   Event Channels — In (optional):
    ///     _onStepCompleted     → VoidGameEvent raised after each step is acknowledged.
    ///     _onTutorialCompleted → VoidGameEvent raised when the tutorial ends/is skipped.
    ///   UI Refs (optional):
    ///     _overlayPanel    → Root panel (semi-transparent dark backdrop + frame).
    ///                        Shown when a tag target exists; hidden otherwise.
    ///     _highlightFrame  → RectTransform repositioned to frame the tagged element.
    ///
    /// ── Architecture notes ───────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no BattleRobots.Physics references.
    ///   - No allocations in Update / FixedUpdate — no Update loop at all.
    ///   - All inspector fields are optional; fully null-safe throughout.
    ///   - <c>GameObject.FindWithTag</c> is called once per step advance (not per frame).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TutorialHighlightOverlay : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("Maps tutorial step IDs to Unity scene tags. " +
                 "Steps with no entry produce no highlight — the overlay hides itself.")]
        [SerializeField] private TutorialHighlightConfig _config;

        [Tooltip("Shared sequence SO — must be the same asset used by TutorialController.")]
        [SerializeField] private TutorialSequenceSO _sequence;

        [Tooltip("Shared progress SO — must be the same asset used by TutorialController.")]
        [SerializeField] private TutorialProgressSO _progress;

        [Header("Event Channels — In (optional)")]
        [Tooltip("Raised by TutorialProgressSO._onStepCompleted after each step is acknowledged. " +
                 "TutorialHighlightOverlay subscribes here to reposition the highlight frame.")]
        [SerializeField] private VoidGameEvent _onStepCompleted;

        [Tooltip("Raised when the tutorial completes or is skipped. " +
                 "TutorialHighlightOverlay subscribes here to hide the overlay entirely.")]
        [SerializeField] private VoidGameEvent _onTutorialCompleted;

        [Header("UI Refs (optional)")]
        [Tooltip("Root panel containing the semi-transparent backdrop and highlight frame. " +
                 "Shown when a tagged target exists for the current step, hidden otherwise.")]
        [SerializeField] private GameObject _overlayPanel;

        [Tooltip("RectTransform repositioned and resized to frame the spotlighted UI element. " +
                 "Leave null to skip frame positioning (overlay still shows/hides).")]
        [SerializeField] private RectTransform _highlightFrame;

        // ── Runtime ───────────────────────────────────────────────────────────

        private Action _refreshDelegate;
        private Action _hideDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = RefreshHighlight;
            _hideDelegate    = Hide;
        }

        private void OnEnable()
        {
            _onStepCompleted?.RegisterCallback(_refreshDelegate);
            _onTutorialCompleted?.RegisterCallback(_hideDelegate);
            RefreshHighlight();
        }

        private void OnDisable()
        {
            _onStepCompleted?.UnregisterCallback(_refreshDelegate);
            _onTutorialCompleted?.UnregisterCallback(_hideDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Determines the current tutorial step, looks up its highlight tag in
        /// <c>_config</c>, finds the tagged <c>GameObject</c> in the active scene,
        /// positions <c>_highlightFrame</c> to frame it, and shows <c>_overlayPanel</c>.
        ///
        /// The overlay is hidden when any of the following are true:
        ///   • <c>_progress</c>, <c>_sequence</c>, or <c>_config</c> is null.
        ///   • <c>_progress.IsComplete</c> is true.
        ///   • No entry exists in <c>_config</c> for the current step.
        ///   • No scene <c>GameObject</c> carries the configured tag.
        /// </summary>
        public void RefreshHighlight()
        {
            if (_progress == null || _progress.IsComplete || _sequence == null || _config == null)
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

            string tag = _config.GetTagForStep(stepId);
            if (string.IsNullOrEmpty(tag))
            {
                Hide();
                return;
            }

            GameObject target = GameObject.FindWithTag(tag);
            if (target == null)
            {
                Hide();
                return;
            }

            // Position the highlight frame to match the target's RectTransform.
            if (_highlightFrame != null)
            {
                RectTransform targetRect = target.GetComponent<RectTransform>();
                if (targetRect != null)
                {
                    _highlightFrame.position  = targetRect.position;
                    _highlightFrame.sizeDelta = targetRect.rect.size;
                }
            }

            _overlayPanel?.SetActive(true);
        }

        /// <summary>
        /// Hides the overlay panel immediately.
        /// Called when the tutorial completes, is skipped, or no highlight target exists
        /// for the current step.
        /// </summary>
        public void Hide()
        {
            _overlayPanel?.SetActive(false);
        }

        // ── Internal ──────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the step ID of the first step in <c>_sequence</c> that has not yet
        /// been completed in <c>_progress</c>.
        ///
        /// Returns <see cref="string.Empty"/> when the sequence is empty or all steps
        /// are already marked complete.  Null step entries in the sequence are skipped.
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
