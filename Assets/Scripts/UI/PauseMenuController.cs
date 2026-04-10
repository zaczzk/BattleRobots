using System;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Shows and hides the pause menu panel in response to
    /// <see cref="PauseManager"/> SO event channels, and wires the
    /// Resume and Quit buttons.
    ///
    /// ── Scene wiring instructions ─────────────────────────────────────────────
    ///   1. Add this component to the Canvas that owns the pause panel.
    ///   2. Assign _pausePanel — the root GameObject of the pause overlay.
    ///   3. Assign _pauseManager — the PauseManager component in the scene.
    ///   4. Assign _onPaused / _onResumed — the same VoidGameEvent SOs used by PauseManager.
    ///   5. Assign _sceneRegistry — the shared SceneRegistry SO asset.
    ///   6. Wire the Resume button onClick → PauseMenuController.OnResumePressed().
    ///   7. Wire the Quit button onClick → PauseMenuController.OnQuitToMenuPressed().
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace. References BattleRobots.Core (PauseManager,
    ///     VoidGameEvent, SceneLoader, SceneRegistry) — allowed by architecture rules.
    ///   - Must NOT reference BattleRobots.Physics.
    ///   - No Update / FixedUpdate — purely event-driven.
    ///   - Delegates cached in Awake — zero alloc on Subscribe/Unsubscribe.
    /// </summary>
    public sealed class PauseMenuController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("UI")]
        [Tooltip("Root GameObject of the pause overlay panel. Hidden by default.")]
        [SerializeField] private GameObject _pausePanel;

        [Header("References")]
        [Tooltip("PauseManager in the Arena scene — used by Resume and Quit buttons.")]
        [SerializeField] private PauseManager _pauseManager;

        [Header("Event Channels — In")]
        [Tooltip("VoidGameEvent raised by PauseManager when the game is paused.")]
        [SerializeField] private VoidGameEvent _onPaused;

        [Tooltip("VoidGameEvent raised by PauseManager when the game is resumed.")]
        [SerializeField] private VoidGameEvent _onResumed;

        [Header("Scene Names")]
        [Tooltip("Single SO holding all scene names. " +
                 "Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ SceneRegistry.")]
        [SerializeField] private SceneRegistry _sceneRegistry;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _pauseCallback;
        private Action _resumeCallback;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _pauseCallback  = ShowPauseMenu;
            _resumeCallback = HidePauseMenu;

            // Panel starts hidden.
            if (_pausePanel != null) _pausePanel.SetActive(false);
        }

        private void OnEnable()
        {
            _onPaused?.RegisterCallback(_pauseCallback);
            _onResumed?.RegisterCallback(_resumeCallback);
        }

        private void OnDisable()
        {
            _onPaused?.UnregisterCallback(_pauseCallback);
            _onResumed?.UnregisterCallback(_resumeCallback);
        }

        // ── Private event handlers ────────────────────────────────────────────

        private void ShowPauseMenu() => _pausePanel?.SetActive(true);
        private void HidePauseMenu() => _pausePanel?.SetActive(false);

        // ── Public API (button callbacks) ─────────────────────────────────────

        /// <summary>
        /// Called by the Resume button.
        /// Delegates to <see cref="PauseManager.Resume()"/> which raises _onResumed,
        /// causing HidePauseMenu() to run automatically.
        /// </summary>
        public void OnResumePressed()
        {
            _pauseManager?.Resume();
        }

        /// <summary>
        /// Called by the Quit to Main Menu button.
        /// Restores <c>Time.timeScale</c> via PauseManager before transitioning so
        /// the loading screen update loop runs at normal speed.
        /// </summary>
        public void OnQuitToMenuPressed()
        {
            // Resume first to restore Time.timeScale = 1 before async load.
            _pauseManager?.Resume();
            string sceneName = _sceneRegistry != null ? _sceneRegistry.MainMenuSceneName : "MainMenu";
            SceneLoader.LoadScene(sceneName);
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_pausePanel == null)
                Debug.LogWarning("[PauseMenuController] _pausePanel not assigned.", this);
            if (_pauseManager == null)
                Debug.LogWarning("[PauseMenuController] _pauseManager not assigned.", this);
            if (_onPaused == null)
                Debug.LogWarning("[PauseMenuController] _onPaused not assigned.", this);
            if (_onResumed == null)
                Debug.LogWarning("[PauseMenuController] _onResumed not assigned.", this);
            if (_sceneRegistry == null)
                Debug.LogWarning("[PauseMenuController] _sceneRegistry not assigned — " +
                                 "QuitToMenu will fall back to hard-coded scene name.", this);
        }
#endif
    }
}
