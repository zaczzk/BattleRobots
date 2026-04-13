using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// Drives the in-game tutorial overlay, presenting one <see cref="TutorialStepSO"/>
    /// at a time from a <see cref="TutorialSequenceSO"/> and persisting completion state
    /// via <see cref="TutorialProgressSO"/>.
    ///
    /// ── When does it show? ────────────────────────────────────────────────────────
    ///   • On <c>OnEnable</c>: begins from step 0 if <c>_progress.IsComplete</c> is false;
    ///     otherwise hides immediately (tutorial already finished on a previous session).
    ///
    /// ── Player actions ────────────────────────────────────────────────────────────
    ///   • Next / Got It button (→ AdvanceStep): marks the current step complete,
    ///     increments index, and shows the next step (or completes when exhausted).
    ///   • Skip button (→ SkipAll): marks the tutorial complete immediately and hides panel.
    ///
    /// ── Inspector wiring ─────────────────────────────────────────────────────────
    ///   Data (optional):
    ///     _sequence        → the TutorialSequenceSO asset.
    ///     _progress        → the TutorialProgressSO asset.
    ///   Event Channel — In (optional):
    ///     _onTutorialCompleted → VoidGameEvent that hides the panel when raised externally.
    ///   UI Refs (optional — all null-safe):
    ///     _tutorialPanel   → root panel GameObject to show/hide.
    ///     _headerText      → Text for the step header.
    ///     _bodyText        → Text for the step body.
    ///     _nextButton      → "Next" / "Got It" button (wired to AdvanceStep).
    ///     _skipButton      → "Skip Tutorial" button (wired to SkipAll).
    ///
    /// ── Architecture notes ───────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no BattleRobots.Physics references.
    ///   - No allocations in Update/FixedUpdate — no Update loop.
    ///   - All inspector fields optional; null-safe throughout.
    ///   - Persists completion via the standard load→mutate→Save pattern.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TutorialController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("Ordered sequence of tutorial steps. Leave null to skip tutorial entirely.")]
        [SerializeField] private TutorialSequenceSO _sequence;

        [Tooltip("Tracks which steps the player has completed and whether the tutorial " +
                 "is fully done. Leave null to disable persistence.")]
        [SerializeField] private TutorialProgressSO _progress;

        [Header("Event Channel — In (optional)")]
        [Tooltip("Raised when the tutorial completes (by SkipAll or exhausting all steps). " +
                 "Subscribe to hide the panel or unlock post-tutorial features. " +
                 "Leave null — HideTutorial is called directly regardless.")]
        [SerializeField] private VoidGameEvent _onTutorialCompleted;

        [Header("UI Refs (all optional)")]
        [Tooltip("Root panel shown while the tutorial is active. Hidden on completion or skip.")]
        [SerializeField] private GameObject _tutorialPanel;

        [Tooltip("Text component for the current step's header.")]
        [SerializeField] private Text _headerText;

        [Tooltip("Text component for the current step's instructional body.")]
        [SerializeField] private Text _bodyText;

        [Tooltip("'Next' / 'Got It' button. Wired to AdvanceStep in Awake.")]
        [SerializeField] private Button _nextButton;

        [Tooltip("'Skip Tutorial' button. Wired to SkipAll in Awake.")]
        [SerializeField] private Button _skipButton;

        // ── Runtime ───────────────────────────────────────────────────────────

        private Action _hideTutorialDelegate;
        private int    _currentIndex;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _hideTutorialDelegate = HideTutorial;

            if (_nextButton != null)
                _nextButton.onClick.AddListener(AdvanceStep);

            if (_skipButton != null)
                _skipButton.onClick.AddListener(SkipAll);
        }

        private void OnEnable()
        {
            _onTutorialCompleted?.RegisterCallback(_hideTutorialDelegate);

            // Show tutorial only if not yet complete.
            if (_progress != null && !_progress.IsComplete)
                BeginTutorial();
            else
                HideTutorial();
        }

        private void OnDisable()
        {
            _onTutorialCompleted?.UnregisterCallback(_hideTutorialDelegate);
        }

        // ── Tutorial flow ─────────────────────────────────────────────────────

        /// <summary>
        /// Resets to step 0 and shows the first step.
        /// Called automatically on <c>OnEnable</c> when the tutorial is not yet complete.
        /// </summary>
        public void BeginTutorial()
        {
            _currentIndex = 0;
            ShowStep(0);
        }

        /// <summary>
        /// Displays the step at <paramref name="index"/>.
        /// When <paramref name="index"/> equals or exceeds <see cref="TutorialSequenceSO.Count"/>
        /// (or <c>_sequence</c> is null), the tutorial is considered exhausted and
        /// <see cref="FinishTutorial"/> is called instead.
        /// </summary>
        private void ShowStep(int index)
        {
            if (_sequence == null || index >= _sequence.Count)
            {
                FinishTutorial();
                return;
            }

            TutorialStepSO step = _sequence.Steps[index];

            if (_headerText != null)
                _headerText.text = step.HeaderText;

            if (_bodyText != null)
                _bodyText.text = step.BodyText;

            _tutorialPanel?.SetActive(true);
        }

        /// <summary>
        /// Marks the current step complete, advances the index, and shows the next step.
        /// No-op when <c>_progress</c> or <c>_sequence</c> is null.
        /// Wired to <c>_nextButton.onClick</c> in <c>Awake</c>.
        /// </summary>
        public void AdvanceStep()
        {
            if (_progress == null || _sequence == null) return;
            if (_currentIndex >= _sequence.Count) return;

            _progress.MarkStepComplete(_sequence.Steps[_currentIndex].StepId);
            _currentIndex++;
            ShowStep(_currentIndex);
        }

        /// <summary>
        /// Marks the tutorial complete immediately, persists the flag via
        /// the load→mutate→Save pattern, and hides the panel.
        /// Wired to <c>_skipButton.onClick</c> in <c>Awake</c>.
        /// </summary>
        public void SkipAll()
        {
            if (_progress != null)
            {
                _progress.Complete();

                SaveData save = SaveSystem.Load();
                save.tutorialComplete = true;
                SaveSystem.Save(save);
            }

            HideTutorial();
        }

        /// <summary>Hides the tutorial panel. Called when all steps are done or on skip.</summary>
        public void HideTutorial()
        {
            _tutorialPanel?.SetActive(false);
        }

        // ── Internal ──────────────────────────────────────────────────────────

        /// <summary>
        /// Called when the step sequence is exhausted naturally (all steps shown).
        /// Marks progress complete, persists, and hides panel.
        /// </summary>
        private void FinishTutorial()
        {
            if (_progress != null)
            {
                _progress.Complete();

                SaveData save = SaveSystem.Load();
                save.tutorialComplete = true;
                SaveSystem.Save(save);
            }

            HideTutorial();
        }
    }
}
