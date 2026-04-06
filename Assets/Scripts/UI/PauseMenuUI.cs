using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// In-game pause overlay.
    ///
    /// Shows when <see cref="PauseManager.IsPaused"/> becomes true (driven by
    /// VoidGameEvent SO channels); hides on resume.  Provides Resume and Quit buttons.
    ///
    /// Architecture constraints
    ///   • <c>BattleRobots.UI</c> namespace — no reference to BattleRobots.Physics.
    ///   • No Update override — all logic is event/button driven.
    ///   • No heap allocations during gameplay.
    ///
    /// Inspector wire-up
    ///   □ _pauseManager  → scene PauseManager reference
    ///   □ _pausePanel    → root CanvasGroup (or GameObject) for the overlay
    ///   □ _resumeButton  → Button
    ///   □ _quitButton    → Button
    ///   □ Add a VoidGameEventListener on this GO for the _onPause channel → ShowPanel()
    ///   □ Add a VoidGameEventListener on this GO for the _onResume channel → HidePanel()
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PauseMenuUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("PauseManager in the scene. Resume() is called on the Resume button click.")]
        [SerializeField] private PauseManager _pauseManager;

        [Tooltip("Root GameObject of the pause overlay (shown/hidden to display the menu).")]
        [SerializeField] private GameObject _pausePanel;

        [Tooltip("'Resume' button — calls PauseManager.Resume().")]
        [SerializeField] private Button _resumeButton;

        [Tooltip("'Quit to Main Menu' button — exits to main menu (or application in build).")]
        [SerializeField] private Button _quitButton;

        [Header("Scene Names (must match Build Settings)")]
        [Tooltip("Scene to load on Quit. Usually 'MainMenu'.")]
        [SerializeField] private string _mainMenuSceneName = "MainMenu";

        [Tooltip("(Optional) SceneTransitionController for async loading. If null, uses synchronous load.")]
        [SerializeField] private SceneTransitionController _transitionController;

        // ── Unity Lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            if (_resumeButton != null) _resumeButton.onClick.AddListener(OnResumeClicked);
            if (_quitButton   != null) _quitButton.onClick.AddListener(OnQuitClicked);

            // Start hidden — the overlay should not show before the first pause.
            HidePanel();
        }

        private void OnDestroy()
        {
            if (_resumeButton != null) _resumeButton.onClick.RemoveListener(OnResumeClicked);
            if (_quitButton   != null) _quitButton.onClick.RemoveListener(OnQuitClicked);
        }

        // ── Public API (wired via VoidGameEventListener in the Inspector) ─────

        /// <summary>
        /// Shows the pause overlay.
        /// Wire this to the PauseManager's _onPause VoidGameEvent via a listener.
        /// </summary>
        public void ShowPanel()
        {
            if (_pausePanel != null)
                _pausePanel.SetActive(true);
        }

        /// <summary>
        /// Hides the pause overlay.
        /// Wire this to the PauseManager's _onResume VoidGameEvent via a listener.
        /// </summary>
        public void HidePanel()
        {
            if (_pausePanel != null)
                _pausePanel.SetActive(false);
        }

        // ── Button Handlers ───────────────────────────────────────────────────

        private void OnResumeClicked()
        {
            if (_pauseManager == null)
            {
                Debug.LogError("[PauseMenuUI] PauseManager is not assigned.", this);
                return;
            }
            _pauseManager.Resume();
        }

        private void OnQuitClicked()
        {
            // Always resume time before leaving the scene — avoids a frozen next scene.
            if (_pauseManager != null && _pauseManager.IsPaused)
                _pauseManager.Resume();

            if (_transitionController != null)
            {
                _transitionController.LoadScene(_mainMenuSceneName);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(_mainMenuSceneName);
            }
        }
    }
}
