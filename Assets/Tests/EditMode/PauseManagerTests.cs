using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="PauseManager"/>.
    ///
    /// Covers:
    ///   • Initial state: IsPaused == false.
    ///   • Pause(): sets IsPaused = true, Time.timeScale = 0, fires _onPaused.
    ///   • Pause() is idempotent (no double event on second call).
    ///   • Resume(): clears IsPaused, restores Time.timeScale = 1, fires _onResumed.
    ///   • Resume() is idempotent when not paused (no spurious event).
    ///   • TogglePause(): alternates state; two calls return to original state.
    ///   • HandleMatchEnded (via reflection): auto-resumes if paused and fires _onResumed.
    ///   • HandleMatchEnded when not paused: no _onResumed event fires.
    ///
    /// PauseManager is a MonoBehaviour; a headless GameObject is used.
    /// HandleMatchStarted / HandleMatchEnded are private and invoked via reflection.
    /// VoidGameEvent channels are injected via reflection and the component is
    /// toggled disabled → enabled to re-subscribe (safe when IsPaused = false).
    ///
    /// TearDown always resets Time.timeScale to 1 to avoid leaking state between tests.
    /// </summary>
    public class PauseManagerTests
    {
        // ── Scene objects ─────────────────────────────────────────────────────
        private GameObject   _go;
        private PauseManager _pm;

        // ── Event channel SOs ─────────────────────────────────────────────────
        private VoidGameEvent _onMatchStarted;
        private VoidGameEvent _onMatchEnded;
        private VoidGameEvent _onPaused;
        private VoidGameEvent _onResumed;

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{fieldName}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static void CallPrivate(object target, string methodName)
        {
            MethodInfo mi = target.GetType()
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(mi, $"Method '{methodName}' not found on {target.GetType().Name}.");
            mi.Invoke(target, null);
        }

        /// <summary>
        /// Injects all four VoidGameEvent channels and re-subscribes by toggling
        /// the component.  Safe to call only when IsPaused = false (OnDisable
        /// would otherwise call ForceResume and fire _onResumed unexpectedly).
        /// </summary>
        private void WireChannels()
        {
            SetField(_pm, "_onMatchStarted", _onMatchStarted);
            SetField(_pm, "_onMatchEnded",   _onMatchEnded);
            SetField(_pm, "_onPaused",       _onPaused);
            SetField(_pm, "_onResumed",      _onResumed);

            // Toggle to trigger OnEnable → RegisterCallback with the injected channels.
            // OnDisable at this point is safe because _isPaused == false.
            _pm.enabled = false;
            _pm.enabled = true;
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("TestPauseManager");
            _pm = _go.AddComponent<PauseManager>();

            _onMatchStarted = ScriptableObject.CreateInstance<VoidGameEvent>();
            _onMatchEnded   = ScriptableObject.CreateInstance<VoidGameEvent>();
            _onPaused       = ScriptableObject.CreateInstance<VoidGameEvent>();
            _onResumed      = ScriptableObject.CreateInstance<VoidGameEvent>();
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f; // always restore — prevents contaminating other tests

            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_onMatchStarted);
            Object.DestroyImmediate(_onMatchEnded);
            Object.DestroyImmediate(_onPaused);
            Object.DestroyImmediate(_onResumed);

            _go             = null;
            _pm             = null;
            _onMatchStarted = null;
            _onMatchEnded   = null;
            _onPaused       = null;
            _onResumed      = null;
        }

        // ── Initial state ─────────────────────────────────────────────────────

        [Test]
        public void InitialState_IsNotPaused()
        {
            Assert.IsFalse(_pm.IsPaused);
        }

        // ── Pause() ───────────────────────────────────────────────────────────

        [Test]
        public void Pause_SetsIsPausedTrue()
        {
            _pm.Pause();
            Assert.IsTrue(_pm.IsPaused);
        }

        [Test]
        public void Pause_SetsTimeScaleToZero()
        {
            _pm.Pause();
            Assert.AreEqual(0f, Time.timeScale, 0.001f);
        }

        [Test]
        public void Pause_FiresOnPausedEvent()
        {
            WireChannels();
            int fireCount = 0;
            _onPaused.RegisterCallback(() => fireCount++);

            _pm.Pause();

            Assert.AreEqual(1, fireCount, "_onPaused must fire exactly once on Pause().");
        }

        [Test]
        public void Pause_WhenAlreadyPaused_IsIdempotent()
        {
            WireChannels();
            int fireCount = 0;
            _onPaused.RegisterCallback(() => fireCount++);

            _pm.Pause();
            _pm.Pause(); // second call must be a no-op

            Assert.AreEqual(1, fireCount, "Pause() must fire _onPaused only once even if called twice.");
            Assert.IsTrue(_pm.IsPaused);
        }

        // ── Resume() ──────────────────────────────────────────────────────────

        [Test]
        public void Resume_WhenPaused_SetsIsPausedFalse()
        {
            _pm.Pause();
            _pm.Resume();
            Assert.IsFalse(_pm.IsPaused);
        }

        [Test]
        public void Resume_WhenPaused_RestoresTimeScaleToOne()
        {
            _pm.Pause();
            _pm.Resume();
            Assert.AreEqual(1f, Time.timeScale, 0.001f);
        }

        [Test]
        public void Resume_WhenPaused_FiresOnResumedEvent()
        {
            WireChannels();
            int fireCount = 0;
            _onResumed.RegisterCallback(() => fireCount++);

            _pm.Pause();
            _pm.Resume();

            Assert.AreEqual(1, fireCount, "_onResumed must fire exactly once on Resume().");
        }

        [Test]
        public void Resume_WhenNotPaused_IsIdempotent()
        {
            WireChannels();
            int fireCount = 0;
            _onResumed.RegisterCallback(() => fireCount++);

            _pm.Resume(); // no-op — not paused

            Assert.AreEqual(0, fireCount, "Resume() must not fire _onResumed when not paused.");
            Assert.IsFalse(_pm.IsPaused);
        }

        // ── TogglePause() ─────────────────────────────────────────────────────

        [Test]
        public void TogglePause_WhenUnpaused_SetsPaused()
        {
            _pm.TogglePause();
            Assert.IsTrue(_pm.IsPaused);
        }

        [Test]
        public void TogglePause_WhenPaused_SetsUnpaused()
        {
            _pm.Pause();
            _pm.TogglePause();
            Assert.IsFalse(_pm.IsPaused);
        }

        [Test]
        public void TogglePause_Twice_ResetsToOriginalState()
        {
            _pm.TogglePause();
            _pm.TogglePause();
            Assert.IsFalse(_pm.IsPaused);
        }

        // ── HandleMatchEnded (via reflection) ─────────────────────────────────

        [Test]
        public void HandleMatchEnded_WhenPaused_AutoResumesAndFiresEvent()
        {
            WireChannels();
            int resumeCount = 0;
            _onResumed.RegisterCallback(() => resumeCount++);

            // Force paused + match-running state via reflection.
            SetField(_pm, "_isPaused",     true);
            SetField(_pm, "_matchRunning", true);
            Time.timeScale = 0f;

            CallPrivate(_pm, "HandleMatchEnded");

            Assert.IsFalse(_pm.IsPaused,          "Must be unpaused after match ends.");
            Assert.AreEqual(1f, Time.timeScale, 0.001f, "timeScale must be restored.");
            Assert.AreEqual(1, resumeCount,        "_onResumed must fire once on auto-resume.");
        }

        [Test]
        public void HandleMatchEnded_WhenNotPaused_DoesNotFireResumedEvent()
        {
            WireChannels();
            int resumeCount = 0;
            _onResumed.RegisterCallback(() => resumeCount++);

            SetField(_pm, "_matchRunning", true);
            // _isPaused = false (default — no Pause() called)

            CallPrivate(_pm, "HandleMatchEnded");

            Assert.AreEqual(0, resumeCount, "_onResumed must not fire when game was not paused.");
            Assert.IsFalse(_pm.IsPaused);
        }
    }
}
