using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode integration tests for T083 — ReplayController.
    ///
    /// Coverage (7 cases):
    ///
    /// Null guards
    ///   [01] Awake_NullSpectatorCamera_DoesNotThrow
    ///   [02] Update_NullReplayUI_DoesNotThrow
    ///
    /// DisableInternalPlaybackAdvance wiring
    ///   [03] Awake_WithSpectatorCamera_DisablesInternalPlaybackAdvance
    ///
    /// AdvancePlayback via Update
    ///   [04] Update_WhenNotPlaying_PlayheadDoesNotAdvance
    ///   [05] Update_WhenPlaying_CallsAdvancePlayback
    ///   [06] Update_NullSpectatorCamera_DoesNotThrow
    ///   [07] Update_WithSpectatorCamera_DoesNotThrow
    /// </summary>
    [TestFixture]
    public sealed class ReplayControllerTests
    {
        // ── Reflection field/method names ──────────────────────────────────────

        private const string FieldReplayUI        = "_replayUI";
        private const string FieldSpectatorCamera = "_spectatorCamera";
        private const string FieldAdvancePlayback = "_advancePlayback";
        private const string FieldReplayInUI      = "_replay";  // ReplayUI's ReplaySO field
        private const string MethodUpdate         = "Update";
        private const string MethodAwake          = "Awake";

        private const BindingFlags NonPublicInstance =
            BindingFlags.NonPublic | BindingFlags.Instance;

        // ── Fixtures ──────────────────────────────────────────────────────────

        private GameObject        _controllerGO;
        private ReplayController  _controller;
        private GameObject        _replayUIGO;
        private ReplayUI          _replayUI;
        private ReplaySO          _replaySO;

        [SetUp]
        public void SetUp()
        {
            // ── Build ReplaySO with a handful of recorded snapshots ────────────

            _replaySO = ScriptableObject.CreateInstance<ReplaySO>();
            _replaySO.StartRecording();
            _replaySO.Record(new MatchStateSnapshot(0.0f, 100f, 100f, Vector3.zero, Vector3.right * 5f));
            _replaySO.Record(new MatchStateSnapshot(0.1f,  90f, 100f, Vector3.forward, Vector3.right * 5f));
            _replaySO.Record(new MatchStateSnapshot(0.2f,  80f,  90f, Vector3.up, Vector3.right * 6f));
            _replaySO.StopRecording();

            // ── Build ReplayUI and inject _replay SO ──────────────────────────

            _replayUIGO = new GameObject("ReplayUI");
            _replayUI   = _replayUIGO.AddComponent<ReplayUI>();

            // Awake() deactivates the GO; re-activate before injecting
            _replayUIGO.SetActive(true);

            typeof(ReplayUI)
                .GetField(FieldReplayInUI, NonPublicInstance)
                ?.SetValue(_replayUI, _replaySO);

            // ── Build ReplayController (without spectator camera by default) ───

            _controllerGO = new GameObject("ReplayController");
            _controller   = _controllerGO.AddComponent<ReplayController>();

            typeof(ReplayController)
                .GetField(FieldReplayUI, NonPublicInstance)
                ?.SetValue(_controller, _replayUI);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_controllerGO);
            Object.DestroyImmediate(_replayUIGO);
            Object.DestroyImmediate(_replaySO);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void InvokeUpdate()
        {
            typeof(ReplayController)
                .GetMethod(MethodUpdate, NonPublicInstance)
                ?.Invoke(_controller, null);
        }

        private void InvokeAwake()
        {
            typeof(ReplayController)
                .GetMethod(MethodAwake, NonPublicInstance)
                ?.Invoke(_controller, null);
        }

        /// <summary>Starts playback on the ReplayUI panel.</summary>
        private void StartPlayback()
        {
            _replayUI.OnReplayReady();
            _replayUI.OnPlayClicked();
        }

        // ─────────────────────────────────────────────────────────────────────
        // [01] Awake_NullSpectatorCamera_DoesNotThrow
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void Awake_NullSpectatorCamera_DoesNotThrow()
        {
            // _spectatorCamera is null by default (never injected).
            Assert.DoesNotThrow(
                () => InvokeAwake(),
                "Awake must not throw when _spectatorCamera is null.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // [02] Update_NullReplayUI_DoesNotThrow
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void Update_NullReplayUI_DoesNotThrow()
        {
            typeof(ReplayController)
                .GetField(FieldReplayUI, NonPublicInstance)
                ?.SetValue(_controller, null);

            Assert.DoesNotThrow(
                () => InvokeUpdate(),
                "Update must be a no-op (no throw) when _replayUI is null.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // [03] Awake_WithSpectatorCamera_DisablesInternalPlaybackAdvance
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void Awake_WithSpectatorCamera_DisablesInternalPlaybackAdvance()
        {
            // Create a SpectatorCameraController and inject it before Awake.
            var cameraGO = new GameObject("SpectatorCamera");
            var camera   = cameraGO.AddComponent<SpectatorCameraController>();

            try
            {
                typeof(ReplayController)
                    .GetField(FieldSpectatorCamera, NonPublicInstance)
                    ?.SetValue(_controller, camera);

                InvokeAwake();

                // _advancePlayback on SpectatorCameraController must now be false.
                bool advancePlayback = (bool)(typeof(SpectatorCameraController)
                    .GetField(FieldAdvancePlayback, NonPublicInstance)
                    ?.GetValue(camera) ?? true);

                Assert.IsFalse(advancePlayback,
                    "Awake must call DisableInternalPlaybackAdvance() on the assigned " +
                    "SpectatorCameraController, setting _advancePlayback to false.");
            }
            finally
            {
                Object.DestroyImmediate(cameraGO);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // [04] Update_WhenNotPlaying_PlayheadDoesNotAdvance
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void Update_WhenNotPlaying_PlayheadDoesNotAdvance()
        {
            // ReplayUI is not playing (IsPlaying == false by default).
            float before = _replayUI.PlayheadTime;
            InvokeUpdate();
            float after = _replayUI.PlayheadTime;

            Assert.AreEqual(before, after,
                "Update must not advance the playhead when ReplayUI.IsPlaying is false.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // [05] Update_WhenPlaying_CallsAdvancePlayback
        //
        // In EditMode Time.deltaTime == 0, so AdvancePlayback(0) alone cannot
        // advance the playhead. Instead we use a single-snapshot ReplaySO whose
        // TotalDuration is 0. When AdvancePlayback(0) is called it computes
        // PlayheadTime = Mathf.Min(0+0, 0) = 0, then Mathf.Approximately(0,0)
        // fires the auto-stop and sets IsPlaying = false — an observable signal
        // that Update actually invoked AdvancePlayback.
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void Update_WhenPlaying_CallsAdvancePlayback()
        {
            // Build a zero-duration ReplaySO (single snapshot at t=0).
            var zeroDurationReplay = ScriptableObject.CreateInstance<ReplaySO>();
            zeroDurationReplay.StartRecording();
            zeroDurationReplay.Record(new MatchStateSnapshot(0f, 100f, 100f, Vector3.zero, Vector3.zero));
            zeroDurationReplay.StopRecording();

            // Build a fresh ReplayUI wired to this zero-duration replay.
            var uiGO = new GameObject("ReplayUI_ZeroDuration");
            var ui   = uiGO.AddComponent<ReplayUI>();
            uiGO.SetActive(true);

            typeof(ReplayUI)
                .GetField(FieldReplayInUI, NonPublicInstance)
                ?.SetValue(ui, zeroDurationReplay);

            typeof(ReplayController)
                .GetField(FieldReplayUI, NonPublicInstance)
                ?.SetValue(_controller, ui);

            try
            {
                ui.OnReplayReady();   // resets playhead to 0, activates panel
                ui.OnPlayClicked();   // IsPlaying = true

                Assert.IsTrue(ui.IsPlaying, "Precondition: IsPlaying must be true before Update.");

                // Update calls AdvancePlayback(Time.deltaTime = 0).
                // PlayheadTime(0) ≈ TotalDuration(0) → auto-stop fires → IsPlaying = false.
                InvokeUpdate();

                Assert.IsFalse(ui.IsPlaying,
                    "Update must call AdvancePlayback; auto-stop should set IsPlaying=false " +
                    "for a zero-duration replay after a single Update tick.");
            }
            finally
            {
                Object.DestroyImmediate(uiGO);
                Object.DestroyImmediate(zeroDurationReplay);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // [06] Update_NullSpectatorCamera_DoesNotThrow
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void Update_NullSpectatorCamera_DoesNotThrow()
        {
            // _spectatorCamera is null (never injected).
            StartPlayback();

            Assert.DoesNotThrow(
                () => InvokeUpdate(),
                "Update must not throw when _spectatorCamera is null.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // [07] Update_WithSpectatorCamera_DoesNotThrow
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void Update_WithSpectatorCamera_DoesNotThrow()
        {
            var cameraGO = new GameObject("SpectatorCamera");
            var camera   = cameraGO.AddComponent<SpectatorCameraController>();

            try
            {
                typeof(SpectatorCameraController)
                    .GetField("_replayUI", NonPublicInstance)
                    ?.SetValue(camera, _replayUI);

                typeof(ReplayController)
                    .GetField(FieldSpectatorCamera, NonPublicInstance)
                    ?.SetValue(_controller, camera);

                InvokeAwake(); // wires DisableInternalPlaybackAdvance
                StartPlayback();

                Assert.DoesNotThrow(
                    () => InvokeUpdate(),
                    "Update must not throw when both _replayUI and _spectatorCamera are assigned.");
            }
            finally
            {
                Object.DestroyImmediate(cameraGO);
            }
        }
    }
}
