using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Single-source-of-truth SO for all scene names used in async scene transitions.
    ///
    /// Centralises the string literals that were previously duplicated across
    /// <c>MainMenuController</c>, <c>PostMatchController</c>, and <c>PauseMenuController</c>.
    /// Renaming a scene in Build Settings only requires updating this one asset.
    ///
    /// ── Usage ────────────────────────────────────────────────────────────────
    ///   Inject a single SceneRegistry SO asset into any MonoBehaviour that needs
    ///   to trigger a scene transition via <see cref="BattleRobots.UI.SceneLoader"/>.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no UI or Physics references.
    ///   - All properties are read-only at runtime (immutable SO pattern).
    ///   - OnValidate warns on empty scene names at edit time.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ SceneRegistry.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Core/SceneRegistry", order = 0)]
    public sealed class SceneRegistry : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Scene Names (must match Build Settings exactly)")]
        [Tooltip("Exact name of the Main Menu scene in Build Settings.")]
        [SerializeField] private string _mainMenuSceneName = "MainMenu";

        [Tooltip("Exact name of the Arena / Battle scene in Build Settings.")]
        [SerializeField] private string _arenaSceneName = "Arena";

        [Tooltip("Exact name of the Shop scene in Build Settings.")]
        [SerializeField] private string _shopSceneName = "Shop";

        // ── Read-only properties ──────────────────────────────────────────────

        /// <summary>Exact build-settings name for the main menu scene.</summary>
        public string MainMenuSceneName => _mainMenuSceneName;

        /// <summary>Exact build-settings name for the arena / battle scene.</summary>
        public string ArenaSceneName => _arenaSceneName;

        /// <summary>Exact build-settings name for the shop scene.</summary>
        public string ShopSceneName => _shopSceneName;

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_mainMenuSceneName))
                Debug.LogWarning("[SceneRegistry] _mainMenuSceneName is empty — " +
                                 "QuitToMenu transitions will fail at runtime.", this);

            if (string.IsNullOrWhiteSpace(_arenaSceneName))
                Debug.LogWarning("[SceneRegistry] _arenaSceneName is empty — " +
                                 "Play and PlayAgain transitions will fail at runtime.", this);

            if (string.IsNullOrWhiteSpace(_shopSceneName))
                Debug.LogWarning("[SceneRegistry] _shopSceneName is empty — " +
                                 "Shop transitions will fail at runtime.", this);
        }
#endif
    }
}
