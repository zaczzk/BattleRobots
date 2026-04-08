using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// Drives a loading-screen progress bar while an async scene load is in flight.
    ///
    /// Reads <see cref="SceneLoader.Progress"/> each frame (a cached float read —
    /// zero heap allocation) and applies it to either a <see cref="Slider"/> or
    /// an <see cref="Image"/> set to Filled mode.
    ///
    /// ── Scene wiring instructions ─────────────────────────────────────────────
    ///   • Place this MB on the LoadingScreen Canvas root.
    ///   • Assign either _progressSlider OR _progressImage (or both) in Inspector.
    ///   • The loading screen GameObject should be activated by whatever triggers
    ///     the scene transition (e.g. a VoidGameEventListener responding to
    ///     _onMenuAction), then deactivated automatically when IsDone is true.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - Update reads one float from SceneLoader — no allocations, no strings.
    ///   - Deactivates its own GameObject when loading completes so the canvas
    ///     disappears without a separate controller.
    /// </summary>
    public sealed class LoadingScreenController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Progress Widgets (assign at least one)")]
        [Tooltip("Optional Slider driven by load progress [0,1].")]
        [SerializeField] private Slider _progressSlider;

        [Tooltip("Optional Image in Filled mode driven by load progress [0,1].")]
        [SerializeField] private Image _progressImage;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Update()
        {
            // Pure float read — zero allocation
            float progress = SceneLoader.Progress;

            if (_progressSlider != null)
                _progressSlider.value = progress;

            if (_progressImage != null)
                _progressImage.fillAmount = progress;

            // Deactivate self when loading is complete to reveal the new scene
            if (SceneLoader.IsDone)
                gameObject.SetActive(false);
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_progressSlider == null && _progressImage == null)
                Debug.LogWarning("[LoadingScreenController] Neither _progressSlider nor " +
                                 "_progressImage is assigned — progress bar will not render.");
        }
#endif
    }
}
