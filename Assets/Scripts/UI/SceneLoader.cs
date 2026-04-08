using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BattleRobots.UI
{
    /// <summary>
    /// Thin static wrapper around Unity's <see cref="SceneManager.LoadSceneAsync"/>.
    ///
    /// Centralises scene transitions so every caller (MainMenuController,
    /// post-match flow, etc.) goes through one place, and so LoadingScreen
    /// can observe progress without coupling to the initiating system.
    ///
    /// ── Usage ────────────────────────────────────────────────────────────────
    ///   SceneLoader.LoadScene("Arena");          // begin async load
    ///   float p = SceneLoader.Progress;          // 0–0.9 while loading, 1.0 when done
    ///   bool  d = SceneLoader.IsDone;            // true when fully loaded
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - Static class — no MonoBehaviour, no heap allocation per call.
    ///   - allowSceneActivation is set to true (Unity's default); the scene
    ///     activates automatically when progress reaches 0.9.
    ///   - <see cref="Progress"/> clamps to [0, 1] and maps Unity's 0–0.9 range
    ///     to 0–1 for clean progress-bar display.
    ///   - BattleRobots.UI namespace; no Physics references.
    /// </summary>
    public static class SceneLoader
    {
        private static AsyncOperation _asyncOp;

        /// <summary>
        /// Begins an asynchronous scene load.
        /// Logs an error and no-ops if <paramref name="sceneName"/> is null/empty.
        /// </summary>
        /// <param name="sceneName">The exact scene name as registered in Build Settings.</param>
        public static void LoadScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("[SceneLoader] LoadScene called with null or empty sceneName.");
                return;
            }

            _asyncOp = SceneManager.LoadSceneAsync(sceneName);
            Debug.Log($"[SceneLoader] Beginning async load of scene '{sceneName}'.");
        }

        /// <summary>
        /// Normalised load progress in [0, 1].
        /// Unity reports 0–0.9 during load then jumps to 1.0 on activation;
        /// this property maps that range cleanly to [0, 1].
        /// Returns 0 when no load is in progress.
        /// </summary>
        public static float Progress
        {
            get
            {
                if (_asyncOp == null) return 0f;
                // Unity clamps progress at 0.9 until scene activates; remap to [0,1]
                return Mathf.Clamp01(_asyncOp.progress / 0.9f);
            }
        }

        /// <summary>True once the loaded scene is fully active.</summary>
        public static bool IsDone => _asyncOp != null && _asyncOp.isDone;

        /// <summary>True while an async load is in flight (started but not yet done).</summary>
        public static bool IsLoading => _asyncOp != null && !_asyncOp.isDone;
    }
}
