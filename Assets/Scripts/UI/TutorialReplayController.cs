using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// Allows a player who has already completed the tutorial to re-watch it from the
    /// career menu (or any other screen that hosts this component).
    ///
    /// ── When is the Replay button enabled? ────────────────────────────────────────
    ///   • The replay button is interactable only when <c>_progress.IsComplete</c> is
    ///     true, i.e. the player has previously finished or skipped the tutorial.
    ///   • While the tutorial is replaying (<c>_progress.IsComplete</c> is false after
    ///     the reset), the button is disabled so the player cannot interrupt an in-
    ///     progress replay.
    ///   • When the replayed tutorial finishes and <c>_onTutorialCompleted</c> fires,
    ///     the button is re-enabled so the player can replay again.
    ///
    /// ── Replay flow ───────────────────────────────────────────────────────────────
    ///   1. Player clicks the Replay button.
    ///   2. <see cref="Replay"/> resets <c>_progress</c> (clears IsComplete + step ids,
    ///      no events) and calls <see cref="TutorialController.BeginTutorial"/> to jump
    ///      directly to step 0, showing the first tutorial step.
    ///   3. <see cref="RefreshButton"/> is called; because IsComplete is now false, the
    ///      button becomes non-interactable for the duration of the replay.
    ///   4. When the tutorial finishes, <c>_onTutorialCompleted</c> fires and the button
    ///      is re-enabled (IsComplete is true again after <c>Complete()</c>).
    ///
    /// ── Inspector wiring ─────────────────────────────────────────────────────────
    ///   Data (optional):
    ///     _progress           → TutorialProgressSO tracking completion state.
    ///   Tutorial Controller (optional):
    ///     _tutorialController → the TutorialController that drives the step overlay.
    ///   Event Channel — In (optional):
    ///     _onTutorialCompleted → VoidGameEvent raised on tutorial completion; re-enables
    ///                            the replay button. Leave null — button stays disabled
    ///                            until the panel is re-enabled.
    ///   UI Refs (optional — all null-safe):
    ///     _replayButton → "Re-watch Tutorial" button wired to Replay in Awake.
    ///                     Interactability reflects <c>_progress.IsComplete</c>.
    ///
    /// ── Architecture notes ───────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no BattleRobots.Physics references.
    ///   - No allocations in Update / FixedUpdate — no Update loop.
    ///   - All inspector fields optional; fully null-safe throughout.
    ///   - No new SaveData fields — replay uses the existing TutorialProgressSO.Reset()
    ///     / Complete() cycle; persistence is handled by TutorialController.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TutorialReplayController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("Tracks whether the tutorial has been completed. " +
                 "The replay button is enabled only when IsComplete is true. " +
                 "Leave null — the button will always be non-interactable.")]
        [SerializeField] private TutorialProgressSO _progress;

        [Header("Tutorial Controller (optional)")]
        [Tooltip("The TutorialController that drives the tutorial step overlay. " +
                 "Replay() calls BeginTutorial() on this controller after resetting progress. " +
                 "Leave null — Replay still resets progress but no overlay will be shown.")]
        [SerializeField] private TutorialController _tutorialController;

        [Header("Event Channel — In (optional)")]
        [Tooltip("VoidGameEvent raised when the tutorial completes or is skipped. " +
                 "Subscribe to re-enable the replay button once the replay finishes. " +
                 "Leave null — button stays non-interactable until next OnEnable.")]
        [SerializeField] private VoidGameEvent _onTutorialCompleted;

        [Header("UI Refs (optional)")]
        [Tooltip("Re-watch Tutorial button. Wired to Replay() in Awake. " +
                 "Interactable only when _progress.IsComplete is true.")]
        [SerializeField] private Button _replayButton;

        // ── Runtime ───────────────────────────────────────────────────────────

        private Action _onTutorialCompletedDelegate;
        private Action _replayDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _onTutorialCompletedDelegate = OnTutorialCompletedCallback;
            _replayDelegate = Replay;

            if (_replayButton != null)
                _replayButton.onClick.AddListener(_replayDelegate);
        }

        private void OnEnable()
        {
            _onTutorialCompleted?.RegisterCallback(_onTutorialCompletedDelegate);
            RefreshButton();
        }

        private void OnDisable()
        {
            _onTutorialCompleted?.UnregisterCallback(_onTutorialCompletedDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Resets tutorial progress and starts the tutorial from step 0 via the linked
        /// <see cref="TutorialController"/>.
        ///
        /// <para>
        /// Sequence: reset <c>_progress</c> (silently clears IsComplete + step ids,
        /// no events fired) → call <c>_tutorialController.BeginTutorial()</c> → refresh
        /// the replay button (interactable = false while tutorial is in progress).
        /// </para>
        ///
        /// Null <c>_progress</c> and null <c>_tutorialController</c> are safe no-ops for
        /// their respective steps.  Wired to <c>_replayButton.onClick</c> in <c>Awake</c>.
        /// </summary>
        public void Replay()
        {
            _progress?.Reset();
            _tutorialController?.BeginTutorial();
            RefreshButton();
        }

        // ── Internal ──────────────────────────────────────────────────────────

        /// <summary>
        /// Updates <c>_replayButton.interactable</c> based on <c>_progress.IsComplete</c>.
        /// Button is interactable only when a completed tutorial can be re-watched.
        /// </summary>
        private void RefreshButton()
        {
            if (_replayButton == null) return;
            _replayButton.interactable = _progress != null && _progress.IsComplete;
        }

        /// <summary>
        /// Invoked when <c>_onTutorialCompleted</c> fires (tutorial finished or skipped).
        /// Re-enables the replay button so the player can watch the tutorial again.
        /// </summary>
        private void OnTutorialCompletedCallback()
        {
            RefreshButton();
        }
    }
}
