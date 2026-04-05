using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Main menu screen controller.
    ///
    /// Buttons:
    ///   Play     → loads the Arena scene via <see cref="SceneTransitionController"/>
    ///   Shop     → loads the Shop scene
    ///   Settings → loads the Settings scene
    ///   Quit     → exits the application
    ///
    /// Architecture constraints:
    ///   • <c>BattleRobots.UI</c> namespace — no reference to BattleRobots.Physics.
    ///   • No Update override — all logic is event-driven (button clicks).
    ///   • No heap allocations during gameplay.
    ///
    /// Inspector wiring:
    ///   □ _transitionController → SceneTransitionController (persistent GO)
    ///   □ _playButton / _shopButton / _settingsButton / _quitButton → Button
    ///   □ Scene name fields → exact names matching Build Settings entries
    /// </summary>
    public sealed class MainMenuUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Scene Transition")]
        [Tooltip("The persistent SceneTransitionController that handles async loading.")]
        [SerializeField] private SceneTransitionController _transitionController;

        [Header("Scene Names (must match Build Settings)")]
        [SerializeField] private string _arenaSceneName    = "Arena";
        [SerializeField] private string _shopSceneName     = "Shop";
        [SerializeField] private string _settingsSceneName = "Settings";

        [Header("Buttons")]
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _shopButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _quitButton;

        // ── Unity Lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            if (_playButton     != null) _playButton.onClick.AddListener(OnPlayClicked);
            if (_shopButton     != null) _shopButton.onClick.AddListener(OnShopClicked);
            if (_settingsButton != null) _settingsButton.onClick.AddListener(OnSettingsClicked);
            if (_quitButton     != null) _quitButton.onClick.AddListener(OnQuitClicked);

            ValidateDependencies();
        }

        private void OnDestroy()
        {
            if (_playButton     != null) _playButton.onClick.RemoveListener(OnPlayClicked);
            if (_shopButton     != null) _shopButton.onClick.RemoveListener(OnShopClicked);
            if (_settingsButton != null) _settingsButton.onClick.RemoveListener(OnSettingsClicked);
            if (_quitButton     != null) _quitButton.onClick.RemoveListener(OnQuitClicked);
        }

        // ── Button Handlers ───────────────────────────────────────────────────

        private void OnPlayClicked()
        {
            if (_transitionController == null)
            {
                Debug.LogError("[MainMenuUI] SceneTransitionController is not assigned.", this);
                return;
            }
            _transitionController.LoadScene(_arenaSceneName);
        }

        private void OnShopClicked()
        {
            if (_transitionController == null)
            {
                Debug.LogError("[MainMenuUI] SceneTransitionController is not assigned.", this);
                return;
            }
            _transitionController.LoadScene(_shopSceneName);
        }

        private void OnSettingsClicked()
        {
            if (_transitionController == null)
            {
                Debug.LogError("[MainMenuUI] SceneTransitionController is not assigned.", this);
                return;
            }
            _transitionController.LoadScene(_settingsSceneName);
        }

        private void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ── Validation ────────────────────────────────────────────────────────

        private void ValidateDependencies()
        {
            if (_transitionController == null)
                Debug.LogWarning("[MainMenuUI] SceneTransitionController is not assigned — buttons will log errors on click.", this);
            if (_playButton == null)
                Debug.LogWarning("[MainMenuUI] Play button not assigned.", this);
        }
    }
}
