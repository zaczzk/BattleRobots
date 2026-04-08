using BattleRobots.Core;
using UnityEngine;

namespace BattleRobots.UI
{
    /// <summary>
    /// Drives post-match replay playback and smoothly follows a chosen robot's position
    /// by reading <see cref="MatchStateSnapshot"/> data from <see cref="ReplayUI"/>.
    ///
    /// ── Responsibilities ──────────────────────────────────────────────────────
    ///   1. Calls <see cref="ReplayUI.AdvancePlayback"/> each frame while the replay
    ///      is playing — keeping playback logic out of ReplayUI's Update (zero-alloc).
    ///   2. Computes a desired camera position from the current snapshot + an offset,
    ///      then lerps the assigned <see cref="Camera"/> transform toward it.
    ///   3. Exposes <see cref="SwitchTarget"/> so UI buttons can hot-swap between
    ///      Player 1, Player 2, or the midpoint between both robots.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   • <c>BattleRobots.UI</c> namespace — no BattleRobots.Physics references.
    ///   • <see cref="Tick"/> is called from Unity's Update and can also be invoked
    ///     directly in EditMode tests, removing the need for a player loop.
    ///   • <see cref="DesiredPosition"/> and <see cref="ComputeDesiredPosition"/> are
    ///     public to support unit-test assertions without reflection.
    ///   • No heap allocation in Tick: only value-type reads and Vector3 arithmetic.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Add to any GameObject in the replay/spectator scene.
    ///   2. Assign <c>_replayUI</c> to the ReplayUI component.
    ///   3. Assign <c>_camera</c> to the Camera whose transform should be moved.
    ///   4. Tune <c>_cameraOffset</c> for the desired vantage point.
    ///   5. Wire P1/P2/Midpoint buttons to <see cref="SwitchToPlayer1"/>,
    ///      <see cref="SwitchToPlayer2"/>, <see cref="SwitchToMidpoint"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("BattleRobots/UI/Spectator Camera Controller")]
    public sealed class SpectatorCameraController : MonoBehaviour
    {
        // ── Follow target ──────────────────────────────────────────────────────

        /// <summary>Which robot (or midpoint) the spectator camera tracks.</summary>
        public enum FollowTarget
        {
            /// <summary>Player 1 robot (index 0 in MatchStateSnapshot).</summary>
            Player1,

            /// <summary>Player 2 / opponent robot (index 1 in MatchStateSnapshot).</summary>
            Player2,

            /// <summary>Halfway point between both robots — good for wide-shots.</summary>
            Midpoint,
        }

        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("References")]
        [Tooltip("The ReplayUI that drives playback state and provides CurrentSnapshot.")]
        [SerializeField] private ReplayUI _replayUI;

        [Tooltip("The Camera whose transform is repositioned each frame. " +
                 "Can be left null to test position computation without a camera.")]
        [SerializeField] private Camera _camera;

        [Header("Follow Settings")]
        [Tooltip("Which robot the camera tracks by default.")]
        [SerializeField] private FollowTarget _followTarget = FollowTarget.Midpoint;

        [Tooltip("World-space offset applied on top of the tracked position. " +
                 "Positive Y = above the robot; negative Z = behind it.")]
        [SerializeField] private Vector3 _cameraOffset = new Vector3(0f, 5f, -8f);

        [Tooltip("Lerp speed (units per second). Higher values = snappier tracking.")]
        [SerializeField, Min(0f)] private float _followSpeed = 5f;

        // ── Runtime state ─────────────────────────────────────────────────────

        /// <summary>Which robot the camera is currently following.</summary>
        public FollowTarget CurrentTarget => _followTarget;

        /// <summary>
        /// The world-space position the camera is trying to reach this frame.
        /// Updated each <see cref="Tick"/> call. Exposed for unit-test assertions.
        /// </summary>
        public Vector3 DesiredPosition { get; private set; }

        // When true (default), Tick calls ReplayUI.AdvancePlayback — the controller manages its own
        // playback. Set to false via DisableInternalPlaybackAdvance() when a ReplayController MB
        // is present in the scene and will call AdvancePlayback itself, preventing double-advance.
        private bool _advancePlayback = true;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Update() => Tick(Time.deltaTime);

        // ── External driver API ───────────────────────────────────────────────

        /// <summary>
        /// Called by <see cref="ReplayController"/> during Awake to signal that an external
        /// owner will drive <see cref="ReplayUI.AdvancePlayback"/> each frame.
        /// After this call, <see cref="Tick"/> skips Step 1 (playback advancement) and only
        /// computes the desired camera position — preventing double-advance.
        ///
        /// Existing scenes that have no <see cref="ReplayController"/> do not call this method
        /// and retain the default single-owner behaviour (backwards-compatible).
        /// </summary>
        public void DisableInternalPlaybackAdvance()
        {
            _advancePlayback = false;
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Core logic tick. Called from <see cref="Update"/> at runtime and directly from
        /// unit tests so no player-loop dependency is needed in tests.
        ///
        /// Steps:
        ///   1. Advance the replay playhead when <see cref="ReplayUI.IsPlaying"/> is true.
        ///   2. Compute <see cref="DesiredPosition"/> from the current snapshot.
        ///   3. Lerp the camera toward <see cref="DesiredPosition"/>.
        ///
        /// Zero heap allocation: only value-type reads and Vector3 arithmetic.
        /// </summary>
        /// <param name="deltaTime">Seconds since the last tick (use Time.deltaTime at runtime).</param>
        public void Tick(float deltaTime)
        {
            if (_replayUI == null) return;

            // Step 1: drive playback — skipped when an external ReplayController owns this.
            if (_advancePlayback && _replayUI.IsPlaying)
                _replayUI.AdvancePlayback(deltaTime);

            // Step 2: compute desired camera position from the snapshot
            DesiredPosition = ComputeDesiredPosition(_replayUI.CurrentSnapshot);

            // Step 3: move camera (null-safe — tests may omit the camera reference)
            if (_camera != null)
            {
                _camera.transform.position = Vector3.Lerp(
                    _camera.transform.position,
                    DesiredPosition,
                    _followSpeed * deltaTime);
            }
        }

        /// <summary>
        /// Computes the world-space camera target from <paramref name="snapshot"/> and the
        /// current <see cref="_followTarget"/> + <see cref="_cameraOffset"/>.
        ///
        /// Public so unit tests can call it with arbitrary snapshots without triggering
        /// the full Tick pipeline.
        /// </summary>
        public Vector3 ComputeDesiredPosition(MatchStateSnapshot snapshot)
        {
            Vector3 trackedPos;

            switch (_followTarget)
            {
                case FollowTarget.Player1:
                    trackedPos = snapshot.p1Pos;
                    break;

                case FollowTarget.Player2:
                    trackedPos = snapshot.p2Pos;
                    break;

                case FollowTarget.Midpoint:
                default:
                    trackedPos = (snapshot.p1Pos + snapshot.p2Pos) * 0.5f;
                    break;
            }

            return trackedPos + _cameraOffset;
        }

        // ── Target switch API (wire to UI buttons) ────────────────────────────

        /// <summary>Switch camera to follow Player 1.</summary>
        public void SwitchToPlayer1() => SwitchTarget(FollowTarget.Player1);

        /// <summary>Switch camera to follow Player 2.</summary>
        public void SwitchToPlayer2() => SwitchTarget(FollowTarget.Player2);

        /// <summary>Switch camera to the midpoint between both robots.</summary>
        public void SwitchToMidpoint() => SwitchTarget(FollowTarget.Midpoint);

        /// <summary>
        /// Changes the follow target. Can be called from UI buttons or at runtime.
        /// Takes effect on the next <see cref="Tick"/> call.
        /// </summary>
        public void SwitchTarget(FollowTarget target)
        {
            _followTarget = target;
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_replayUI == null)
                Debug.LogWarning($"[SpectatorCameraController] '{name}': _replayUI not assigned.");
            if (_camera == null)
                Debug.LogWarning($"[SpectatorCameraController] '{name}': _camera not assigned — " +
                                 "DesiredPosition will be computed but no camera will be moved.");
        }
#endif
    }
}
