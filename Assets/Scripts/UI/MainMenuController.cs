using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Drives the main menu: Play, Shop, and Quit actions.
    ///
    /// Each public method is intended to be wired directly to a UI Button's
    /// onClick UnityEvent in the Inspector — no code coupling required.
    ///
    /// ── Scene wiring instructions ─────────────────────────────────────────────
    ///   • Assign _sceneRegistry (SceneRegistry SO) in the Inspector.
    ///   • Assign _onMenuAction (VoidGameEvent SO) if audio/analytics must react.
    ///   • Wire Button onClick events:
    ///       Play Button  → MainMenuController.PlayGame()
    ///       Shop Button  → MainMenuController.OpenShop()
    ///       Quit Button  → MainMenuController.QuitGame()
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - No Update / FixedUpdate — purely button-driven.
    ///   - SceneLoader (static helper) centralises async loading.
    ///   - Scene names sourced from SceneRegistry SO — single source of truth.
    ///   - _onMenuAction SO channel decouples audio/analytics from this MB.
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Scene Names")]
        [Tooltip("Single SO that holds all scene names. " +
                 "Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ SceneRegistry.")]
        [SerializeField] private SceneRegistry _sceneRegistry;

        [Header("Event Channels — Out")]
        [Tooltip("Raised on any menu action (play, shop, quit). Optional — for audio/analytics.")]
        [SerializeField] private VoidGameEvent _onMenuAction;

        // ── Public API (wired to Button.onClick in Inspector) ─────────────────

        /// <summary>
        /// Begins async load of the arena scene and raises the menu-action event.
        /// Wire to Play button onClick.
        /// </summary>
        public void PlayGame()
        {
            _onMenuAction?.Raise();
            string sceneName = _sceneRegistry != null ? _sceneRegistry.ArenaSceneName : "Arena";
            SceneLoader.LoadScene(sceneName);
            Debug.Log("[MainMenuController] PlayGame — loading arena scene.");
        }

        /// <summary>
        /// Begins async load of the shop scene and raises the menu-action event.
        /// Wire to Shop button onClick.
        /// </summary>
        public void OpenShop()
        {
            _onMenuAction?.Raise();
            string sceneName = _sceneRegistry != null ? _sceneRegistry.ShopSceneName : "Shop";
            SceneLoader.LoadScene(sceneName);
            Debug.Log("[MainMenuController] OpenShop — loading shop scene.");
        }

        /// <summary>
        /// Quits the application (or stops Play mode in the Editor).
        /// Wire to Quit button onClick.
        /// </summary>
        public void QuitGame()
        {
            _onMenuAction?.Raise();
            Debug.Log("[MainMenuController] QuitGame requested.");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_sceneRegistry == null)
                Debug.LogWarning("[MainMenuController] _sceneRegistry not assigned — " +
                                 "PlayGame/OpenShop will fall back to hard-coded scene names.", this);
        }
#endif
    }
}
