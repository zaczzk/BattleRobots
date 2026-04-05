using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BattleRobots.Core
{
    /// <summary>
    /// Handles async scene loading and broadcasts progress via SO event channels.
    /// Persists across scene loads (DontDestroyOnLoad).
    ///
    /// Event channels (wire in Inspector):
    ///   _onLoadStart    → VoidGameEvent  — raised when loading begins (show loading screen)
    ///   _onLoadProgress → FloatGameEvent — raised every frame with [0..1] progress
    ///   _onLoadComplete → VoidGameEvent  — raised when scene is fully loaded and active
    ///
    /// No Update; no heap allocations in hot path.
    /// </summary>
    public sealed class SceneTransitionController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Event Channels")]
        [Tooltip("Raised when a scene load begins. Wire to: show loading screen.")]
        [SerializeField] private VoidGameEvent _onLoadStart;

        [Tooltip("Raised each frame during loading. Payload = normalised progress [0..1].")]
        [SerializeField] private FloatGameEvent _onLoadProgress;

        [Tooltip("Raised when the scene is active and ready. Wire to: hide loading screen.")]
        [SerializeField] private VoidGameEvent _onLoadComplete;

        [Header("Settings")]
        [Tooltip("Minimum display time for the loading screen (seconds). " +
                 "Prevents a flash when loading a lightweight scene.")]
        [SerializeField, Min(0f)] private float _minimumLoadSeconds = 0.5f;

        // ── State ─────────────────────────────────────────────────────────────

        /// <summary>True while an async scene load is in progress.</summary>
        public bool IsLoading { get; private set; }

        // ── Unity Lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Begins an async load of <paramref name="sceneName"/>.
        /// Ignored if a load is already in progress.
        /// </summary>
        public void LoadScene(string sceneName)
        {
            if (IsLoading)
            {
                Debug.LogWarning($"[SceneTransitionController] LoadScene('{sceneName}') ignored — already loading.", this);
                return;
            }

            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("[SceneTransitionController] LoadScene called with null or empty scene name.", this);
                return;
            }

            StartCoroutine(LoadSceneRoutine(sceneName));
        }

        /// <summary>
        /// Begins an async load by build index.
        /// Ignored if a load is already in progress.
        /// </summary>
        public void LoadSceneByIndex(int buildIndex)
        {
            if (IsLoading)
            {
                Debug.LogWarning($"[SceneTransitionController] LoadSceneByIndex({buildIndex}) ignored — already loading.", this);
                return;
            }

            StartCoroutine(LoadSceneByIndexRoutine(buildIndex));
        }

        // ── Coroutines ────────────────────────────────────────────────────────

        private IEnumerator LoadSceneRoutine(string sceneName)
        {
            IsLoading = true;
            _onLoadStart?.Raise();
            _onLoadProgress?.Raise(0f);

            float elapsed = 0f;
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);

            // Prevent scene activation until we're ready (respects minimum display time).
            op.allowSceneActivation = false;

            while (!op.isDone)
            {
                elapsed += Time.unscaledDeltaTime;

                // AsyncOperation progress goes 0→0.9 while loading, then jumps to 1.0 on activation.
                float progress = Mathf.Clamp01(op.progress / 0.9f);
                _onLoadProgress?.Raise(progress);

                bool loadComplete    = op.progress >= 0.9f;
                bool minimumMet      = elapsed >= _minimumLoadSeconds;

                if (loadComplete && minimumMet)
                {
                    _onLoadProgress?.Raise(1f);
                    op.allowSceneActivation = true;
                }

                yield return null;
            }

            IsLoading = false;
            _onLoadComplete?.Raise();
        }

        private IEnumerator LoadSceneByIndexRoutine(int buildIndex)
        {
            IsLoading = true;
            _onLoadStart?.Raise();
            _onLoadProgress?.Raise(0f);

            float elapsed = 0f;
            AsyncOperation op = SceneManager.LoadSceneAsync(buildIndex);
            op.allowSceneActivation = false;

            while (!op.isDone)
            {
                elapsed += Time.unscaledDeltaTime;

                float progress  = Mathf.Clamp01(op.progress / 0.9f);
                _onLoadProgress?.Raise(progress);

                bool loadComplete = op.progress >= 0.9f;
                bool minimumMet   = elapsed >= _minimumLoadSeconds;

                if (loadComplete && minimumMet)
                {
                    _onLoadProgress?.Raise(1f);
                    op.allowSceneActivation = true;
                }

                yield return null;
            }

            IsLoading = false;
            _onLoadComplete?.Raise();
        }
    }
}
