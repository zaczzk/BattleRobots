using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode integration tests for T073 — SpectatorCameraController.
    ///
    /// Coverage (12 cases):
    ///
    /// Null guard
    ///   [01] Tick_NullReplayUI_DoesNotThrow
    ///
    /// Playback driving
    ///   [02] Tick_WhenNotPlaying_PlayheadDoesNotAdvance
    ///   [03] Tick_WhenPlaying_PlayheadAdvancesByDeltaTime
    ///   [04] Tick_WhenPlayingReachesEnd_IsPlayingBecomesFalse
    ///
    /// ComputeDesiredPosition — target variants
    ///   [05] ComputeDesiredPosition_Player1_ReturnsP1PlusCameraOffset
    ///   [06] ComputeDesiredPosition_Player2_ReturnsP2PlusCameraOffset
    ///   [07] ComputeDesiredPosition_Midpoint_ReturnsMidpointPlusCameraOffset
    ///
    /// Target switching
    ///   [08] DefaultFollowTarget_IsMidpoint
    ///   [09] SwitchToPlayer1_ChangesCurrentTargetToPlayer1
    ///   [10] SwitchToPlayer2_ChangesCurrentTargetToPlayer2
    ///   [11] SwitchToMidpoint_ChangesCurrentTargetToMidpoint
    ///
    /// DesiredPosition integration
    ///   [12] Tick_WhenPlaying_DesiredPositionReflectsCurrentSnapshot
    /// </summary>
    [TestFixture]
    public sealed class SpectatorCameraControllerTests
    {
        // ── Reflection field names (match SpectatorCameraController inspector fields) ──

        private const string FieldReplayUI     = "_replayUI";
        private const string FieldFollowTarget = "_followTarget";
        private const string FieldCameraOffset = "_cameraOffset";
        private const string FieldReplayInUI   = "_replay";    // ReplayUI's ReplaySO field

        private const BindingFlags NonPublicInstance =
            BindingFlags.NonPublic | BindingFlags.Instance;

        // ── Fixtures ──────────────────────────────────────────────────────────

        private GameObject                _controllerGO;
        private SpectatorCameraController _controller;
        private GameObject                _replayUIGO;
        private ReplayUI                  _replayUI;
        private ReplaySO                  _replaySO;

        // A known camera offset injected into _controller for deterministic position assertions.
        private static readonly Vector3 TestOffset = new Vector3(0f, 4f, -6f);

        [SetUp]
        public void SetUp()
        {
            // ── Build ReplaySO with a handful of recorded snapshots ────────────

            _replaySO = ScriptableObject.CreateInstance<ReplaySO>();
            _replaySO.StartRecording();
            _replaySO.Record(new MatchStateSnapshot(0.0f, 100f, 100f, Vector3.zero,    Vector3.right * 5f));
            _replaySO.Record(new MatchStateSnapshot(0.1f,  90f, 100f, Vector3.forward, Vector3.right * 5f));
            _replaySO.Record(new MatchStateSnapshot(0.2f,  80f,  90f, Vector3.up,      Vector3.right * 6f));
            _replaySO.StopRecording();

            // ── Build ReplayUI and inject _replay SO ──────────────────────────

            _replayUIGO = new GameObject("ReplayUI");
            _replayUI   = _replayUIGO.AddComponent<ReplayUI>();

            // Awake() deactivates the GO; re-activate before injecting
            _replayUIGO.SetActive(true);

            typeof(ReplayUI)
                .GetField(FieldReplayInUI, NonPublicInstance)
                ?.SetValue(_replayUI, _replaySO);

            // ── Build SpectatorCameraController ────────────────────────────────

            _controllerGO = new GameObject("SpectatorCamera");
            _controller   = _controllerGO.AddComponent<SpectatorCameraController>();

            typeof(SpectatorCameraController)
                .GetField(FieldReplayUI, NonPublicInstance)
                ?.SetValue(_controller, _replayUI);

            typeof(SpectatorCameraController)
                .GetField(FieldCameraOffset, NonPublicInstance)
                ?.SetValue(_controller, TestOffset);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_controllerGO);
            Object.DestroyImmediate(_replayUIGO);
            Object.DestroyImmediate(_replaySO);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void SetFollowTarget(SpectatorCameraController.FollowTarget target)
        {
            typeof(SpectatorCameraController)
                .GetField(FieldFollowTarget, NonPublicInstance)
                ?.SetValue(_controller, target);
        }

        /// <summary>
        /// Opens the ReplayUI panel and starts playback.
        /// Mirrors the scene-level flow: OnReplayReady → OnPlayClicked.
        /// </summary>
        private void StartPlayback()
        {
            _replayUI.OnReplayReady();   // activates panel + initialises scrubber
            _replayUI.OnPlayClicked();   // sets IsPlaying = true
        }

        // ─────────────────────────────────────────────────────────────────────
        // [01] Tick_NullReplayUI_DoesNotThrow
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void Tick_NullReplayUI_DoesNotThrow()
        {
            typeof(SpectatorCameraController)
                .GetField(FieldReplayUI, NonPublicInstance)
                ?.SetValue(_controller, null);

            Assert.DoesNotThrow(
                () => _controller.Tick(0.1f),
                "Tick must be a no-op when _replayUI is null.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // [02] Tick_WhenNotPlaying_PlayheadDoesNotAdvance
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void Tick_WhenNotPlaying_PlayheadDoesNotAdvance()
        {
            float before = _replayUI.PlayheadTime;
            _controller.Tick(0.1f);
            float after = _replayUI.PlayheadTime;

            Assert.AreEqual(before, after,
                "Tick should not advance the playhead when ReplayUI.IsPlaying is false.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // [03] Tick_WhenPlaying_PlayheadAdvancesByDeltaTime
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void Tick_WhenPlaying_PlayheadAdvancesByDeltaTime()
        {
            StartPlayback();

            float before = _replayUI.PlayheadTime;
            _controller.Tick(0.05f);
            float after = _replayUI.PlayheadTime;

            Assert.Greater(after, before,
                "Tick should advance PlayheadTime when IsPlaying is true.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // [04] Tick_WhenPlayingReachesEnd_IsPlayingBecomesFalse
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void Tick_WhenPlayingReachesEnd_IsPlayingBecomesFalse()
        {
            StartPlayback();

            // Advance past TotalDuration in one tick so auto-stop fires inside AdvancePlayback.
            _controller.Tick(_replaySO.TotalDuration + 1f);

            Assert.IsFalse(_replayUI.IsPlaying,
                "ReplayUI.IsPlaying must become false once the replay reaches its end.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // [05] ComputeDesiredPosition_Player1_ReturnsP1PlusCameraOffset
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void ComputeDesiredPosition_Player1_ReturnsP1PlusCameraOffset()
        {
            SetFollowTarget(SpectatorCameraController.FollowTarget.Player1);

            var snap     = new MatchStateSnapshot(0f, 100f, 100f, new Vector3(3f, 0f, 2f), new Vector3(-3f, 0f, 2f));
            Vector3 result   = _controller.ComputeDesiredPosition(snap);
            Vector3 expected = snap.p1Pos + TestOffset;

            Assert.AreEqual(expected, result,
                "ComputeDesiredPosition with Player1 target must return p1Pos + _cameraOffset.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // [06] ComputeDesiredPosition_Player2_ReturnsP2PlusCameraOffset
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void ComputeDesiredPosition_Player2_ReturnsP2PlusCameraOffset()
        {
            SetFollowTarget(SpectatorCameraController.FollowTarget.Player2);

            var snap     = new MatchStateSnapshot(0f, 100f, 100f, new Vector3(3f, 0f, 2f), new Vector3(-3f, 0f, 2f));
            Vector3 result   = _controller.ComputeDesiredPosition(snap);
            Vector3 expected = snap.p2Pos + TestOffset;

            Assert.AreEqual(expected, result,
                "ComputeDesiredPosition with Player2 target must return p2Pos + _cameraOffset.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // [07] ComputeDesiredPosition_Midpoint_ReturnsMidpointPlusCameraOffset
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void ComputeDesiredPosition_Midpoint_ReturnsMidpointPlusCameraOffset()
        {
            SetFollowTarget(SpectatorCameraController.FollowTarget.Midpoint);

            var p1   = new Vector3(4f, 0f, 0f);
            var p2   = new Vector3(-4f, 0f, 0f);
            var snap = new MatchStateSnapshot(0f, 100f, 100f, p1, p2);

            Vector3 result   = _controller.ComputeDesiredPosition(snap);
            Vector3 expected = Vector3.zero + TestOffset; // midpoint of (+4,0,0) and (-4,0,0) = (0,0,0)

            Assert.AreEqual(expected, result,
                "Midpoint target must average p1Pos and p2Pos before adding _cameraOffset.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // [08] DefaultFollowTarget_IsMidpoint
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void DefaultFollowTarget_IsMidpoint()
        {
            Assert.AreEqual(
                SpectatorCameraController.FollowTarget.Midpoint,
                _controller.CurrentTarget,
                "Default follow target should be Midpoint.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // [09] SwitchToPlayer1_ChangesCurrentTargetToPlayer1
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void SwitchToPlayer1_ChangesCurrentTargetToPlayer1()
        {
            _controller.SwitchToPlayer1();

            Assert.AreEqual(
                SpectatorCameraController.FollowTarget.Player1,
                _controller.CurrentTarget,
                "SwitchToPlayer1() must set CurrentTarget to Player1.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // [10] SwitchToPlayer2_ChangesCurrentTargetToPlayer2
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void SwitchToPlayer2_ChangesCurrentTargetToPlayer2()
        {
            _controller.SwitchToPlayer2();

            Assert.AreEqual(
                SpectatorCameraController.FollowTarget.Player2,
                _controller.CurrentTarget,
                "SwitchToPlayer2() must set CurrentTarget to Player2.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // [11] SwitchToMidpoint_ChangesCurrentTargetToMidpoint
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void SwitchToMidpoint_ChangesCurrentTargetToMidpoint()
        {
            _controller.SwitchToPlayer1(); // move away from default
            _controller.SwitchToMidpoint();

            Assert.AreEqual(
                SpectatorCameraController.FollowTarget.Midpoint,
                _controller.CurrentTarget,
                "SwitchToMidpoint() must restore CurrentTarget to Midpoint.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // [12] Tick_WhenPlaying_DesiredPositionReflectsCurrentSnapshot
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void Tick_WhenPlaying_DesiredPositionReflectsCurrentSnapshot()
        {
            SetFollowTarget(SpectatorCameraController.FollowTarget.Player1);
            StartPlayback();

            _controller.Tick(0.05f);

            // The expected position is computed independently using the same snapshot
            // that Tick will have stored in ReplayUI.CurrentSnapshot.
            MatchStateSnapshot snap     = _replayUI.CurrentSnapshot;
            Vector3            expected = snap.p1Pos + TestOffset;

            Assert.AreEqual(expected, _controller.DesiredPosition,
                "DesiredPosition must equal CurrentSnapshot.p1Pos + _cameraOffset " +
                "after a Tick with the Player1 follow target.");
        }
    }
}
