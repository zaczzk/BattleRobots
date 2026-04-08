using UnityEngine;

namespace BattleRobots.UI
{
    /// <summary>
    /// Standalone Update-driven scene controller that owns replay playback for scenes
    /// that contain both a <see cref="ReplayUI"/> and a <see cref="SpectatorCameraController"/>.
    ///
    /// ── Problem it solves ────────────────────────────────────────────────────────
    ///   <see cref="SpectatorCameraController.Tick"/> already calls
    ///   <see cref="ReplayUI.AdvancePlayback"/> when <see cref="ReplayUI.IsPlaying"/>.
    ///   In scenes without a dedicated controller that is fine — one owner, no duplication.
    ///   When a ReplayController is added, it becomes the <em>sole</em> owner of
    ///   <c>AdvancePlayback</c>. To prevent SpectatorCameraController from advancing the
    ///   playhead a second time in the same frame, ReplayController calls
    ///   <see cref="SpectatorCameraController.DisableInternalPlaybackAdvance"/> in Awake.
    ///
    /// ── Execution order ──────────────────────────────────────────────────────────
    ///   Each frame:
    ///     1. <see cref="Update"/> advances the playhead via <c>_replayUI.AdvancePlayback</c>.
    ///     2. <see cref="SpectatorCameraController.Tick"/> is called so the camera moves
    ///        to the position encoded in the now-updated <see cref="ReplayUI.CurrentSnapshot"/>.
    ///        Step 1 is not repeated inside Tick because
    ///        <see cref="SpectatorCameraController.DisableInternalPlaybackAdvance"/> was
    ///        called in Awake.
    ///
    /// ── Architecture notes ───────────────────────────────────────────────────────
    ///   • <c>BattleRobots.UI</c> namespace — no BattleRobots.Physics references.
    ///   • No heap allocations in Update: <c>AdvancePlayback</c> and <c>Tick</c> are
    ///     both zero-alloc (value-type arithmetic only).
    ///   • <c>_spectatorCamera</c> is optional — ReplayController can drive playback
    ///     without a camera (useful for unit tests and non-visual playback scenarios).
    ///
    /// ── Scene wiring ─────────────────────────────────────────────────────────────
    ///   1. Add to a scene manager GameObject alongside (or instead of) a
    ///      SpectatorCameraController-only setup.
    ///   2. Assign <c>_replayUI</c> (required).
    ///   3. Optionally assign <c>_spectatorCamera</c> — if assigned, set its
    ///      <c>_advancePlayback</c> flag to false in the Inspector OR rely on the
    ///      Awake call to <see cref="SpectatorCameraController.DisableInternalPlaybackAdvance"/>.
    ///   4. Connect <c>ReplayUI</c> and <c>SpectatorCameraController</c> as usual:
    ///        ReplaySO._onReplayReady → ReplayUI.OnReplayReady
    ///        P1/P2/Midpoint buttons → SpectatorCameraController.SwitchToPlayer1/2/Midpoint
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("BattleRobots/UI/Replay Controller")]
    public sealed class ReplayController : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────

        [Header("Playback")]
        [Tooltip("The ReplayUI that drives playback state. Required.")]
        [SerializeField] private ReplayUI _replayUI;

        [Header("Camera (optional)")]
        [Tooltip("SpectatorCameraController that follows the replay snapshot positions. " +
                 "When assigned, DisableInternalPlaybackAdvance() is called in Awake so " +
                 "this controller becomes the sole owner of AdvancePlayback.")]
        [SerializeField] private SpectatorCameraController _spectatorCamera;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Awake()
        {
            // Disable playback advancement inside SpectatorCameraController so that
            // only this controller calls AdvancePlayback — prevents double-advance.
            if (_spectatorCamera != null)
                _spectatorCamera.DisableInternalPlaybackAdvance();
        }

        private void Update()
        {
            if (_replayUI == null) return;

            // Step 1: advance playhead — the sole call to AdvancePlayback this frame.
            // Zero alloc: AdvancePlayback does only float arithmetic + struct reads.
            if (_replayUI.IsPlaying)
                _replayUI.AdvancePlayback(Time.deltaTime);

            // Step 2: update camera position from the now-current snapshot.
            // Tick will NOT call AdvancePlayback again (disabled in Awake).
            if (_spectatorCamera != null)
                _spectatorCamera.Tick(Time.deltaTime);
        }

        // ── Editor validation ──────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_replayUI == null)
                Debug.LogWarning("[ReplayController] _replayUI not assigned — controller is inert.");
        }
#endif
    }
}
