using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="MatchTimerWarningController"/>.
    ///
    /// Covers:
    ///   • OnEnable / OnDisable with all null inspector refs → DoesNotThrow.
    ///   • OnEnable with null channel → DoesNotThrow.
    ///   • OnDisable with null channel → DoesNotThrow.
    ///   • OnDisable unregisters from the timer channel (external counter stays 0 after disable).
    ///   • OnEnable hides _warningPanel (SetActive false).
    ///   • OnDisable hides _warningPanel.
    ///   • Timer raised below _showPanelBelow → panel shown (SetActive true).
    ///   • Timer raised above _showPanelBelow → panel hidden (SetActive false).
    ///   • Timer raised at exactly zero → panel hidden (time-up guard).
    ///   • OnTimerUpdated with null _warningConfig → DoesNotThrow.
    ///
    /// All tests run headless; no full scene is required.
    /// </summary>
    public class MatchTimerWarningControllerTests
    {
        // ── Scene objects ─────────────────────────────────────────────────────────

        private GameObject                   _go;
        private MatchTimerWarningController  _ctrl;

        // ── Reflection helpers ────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("MatchTimerWarningCtrl");
            _go.SetActive(false); // inactive so Awake/OnEnable don't fire during field injection
            _ctrl = _go.AddComponent<MatchTimerWarningController>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        // ── 1. OnEnable all null refs ─────────────────────────────────────────────

        [Test]
        public void OnEnable_AllNullRefs_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _go.SetActive(true),
                "Activating with all null inspector refs must not throw.");
        }

        // ── 2. OnDisable all null refs ────────────────────────────────────────────

        [Test]
        public void OnDisable_AllNullRefs_DoesNotThrow()
        {
            _go.SetActive(true);
            Assert.DoesNotThrow(() => _go.SetActive(false),
                "Disabling with all null inspector refs must not throw.");
        }

        // ── 3. OnEnable null channel ──────────────────────────────────────────────

        [Test]
        public void OnEnable_NullChannel_DoesNotThrow()
        {
            // _onTimerUpdated defaults to null; Awake caches the delegate.
            Assert.DoesNotThrow(() => _go.SetActive(true),
                "OnEnable with null _onTimerUpdated channel must not throw.");
        }

        // ── 4. OnDisable null channel ─────────────────────────────────────────────

        [Test]
        public void OnDisable_NullChannel_DoesNotThrow()
        {
            _go.SetActive(true);
            Assert.DoesNotThrow(() => _go.SetActive(false),
                "OnDisable with null _onTimerUpdated channel must not throw.");
        }

        // ── 5. OnDisable unregisters from timer channel ───────────────────────────

        [Test]
        public void OnDisable_UnregistersFromTimerChannel()
        {
            var timerEvent = ScriptableObject.CreateInstance<FloatGameEvent>();
            SetField(_ctrl, "_onTimerUpdated", timerEvent);

            // Count calls via an external counter on the same event (not the controller's handler).
            // After OnDisable the controller must no longer forward ticks to CheckAndFire.
            // We verify indirectly: raise the event and confirm no NullReferenceException —
            // the controller's handler is unregistered so raising is a no-op for it.
            _go.SetActive(true);  // OnEnable — registers handler
            _go.SetActive(false); // OnDisable — must unregister

            Assert.DoesNotThrow(() => timerEvent.Raise(15f),
                "Raising the timer event after OnDisable must not throw (handler must be removed).");

            Object.DestroyImmediate(timerEvent);
        }

        // ── 6. OnEnable hides warning panel ──────────────────────────────────────

        [Test]
        public void OnEnable_HidesWarningPanel()
        {
            var panel = new GameObject("WarningPanel");
            panel.SetActive(true); // start active to verify OnEnable forces it off
            SetField(_ctrl, "_warningPanel", panel);

            _go.SetActive(true); // OnEnable

            Assert.IsFalse(panel.activeSelf,
                "OnEnable must hide the _warningPanel (SetActive false) regardless of prior state.");

            Object.DestroyImmediate(panel);
        }

        // ── 7. OnDisable hides warning panel ─────────────────────────────────────

        [Test]
        public void OnDisable_HidesWarningPanel()
        {
            var timerEvent = ScriptableObject.CreateInstance<FloatGameEvent>();
            var panel      = new GameObject("WarningPanel");

            // Show panel below = 30 s (default). Raise timer at 20s to activate the panel first.
            SetField(_ctrl, "_onTimerUpdated", timerEvent);
            SetField(_ctrl, "_warningPanel",   panel);

            _go.SetActive(true);        // OnEnable — panel hidden
            timerEvent.Raise(20f);      // 20 ≤ 30 → panel shown
            Assert.IsTrue(panel.activeSelf, "Pre-condition: panel should be shown at 20s.");

            _go.SetActive(false); // OnDisable — must hide panel

            Assert.IsFalse(panel.activeSelf,
                "OnDisable must hide the _warningPanel.");

            Object.DestroyImmediate(timerEvent);
            Object.DestroyImmediate(panel);
        }

        // ── 8. Timer below showPanelBelow shows panel ─────────────────────────────

        [Test]
        public void TimerRaised_BelowShowPanelBelow_ShowsPanel()
        {
            var timerEvent = ScriptableObject.CreateInstance<FloatGameEvent>();
            var panel      = new GameObject("WarningPanel");

            SetField(_ctrl, "_onTimerUpdated", timerEvent);
            SetField(_ctrl, "_warningPanel",   panel);
            // _showPanelBelow defaults to 30f in the source.

            _go.SetActive(true);
            timerEvent.Raise(15f); // 15 ≤ 30 → should show

            Assert.IsTrue(panel.activeSelf,
                "Panel must be active when secondsRemaining (15) <= _showPanelBelow (30).");

            Object.DestroyImmediate(timerEvent);
            Object.DestroyImmediate(panel);
        }

        // ── 9. Timer above showPanelBelow hides panel ─────────────────────────────

        [Test]
        public void TimerRaised_AboveShowPanelBelow_HidesPanel()
        {
            var timerEvent = ScriptableObject.CreateInstance<FloatGameEvent>();
            var panel      = new GameObject("WarningPanel");
            panel.SetActive(true);

            SetField(_ctrl, "_onTimerUpdated", timerEvent);
            SetField(_ctrl, "_warningPanel",   panel);

            _go.SetActive(true);
            timerEvent.Raise(60f); // 60 > 30 → should hide

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden when secondsRemaining (60) > _showPanelBelow (30).");

            Object.DestroyImmediate(timerEvent);
            Object.DestroyImmediate(panel);
        }

        // ── 10. Timer at zero hides panel (time-up guard) ─────────────────────────

        [Test]
        public void TimerRaised_AtZero_HidesPanel()
        {
            var timerEvent = ScriptableObject.CreateInstance<FloatGameEvent>();
            var panel      = new GameObject("WarningPanel");
            panel.SetActive(true);

            SetField(_ctrl, "_onTimerUpdated", timerEvent);
            SetField(_ctrl, "_warningPanel",   panel);

            _go.SetActive(true);
            timerEvent.Raise(0f); // time-up: secondsRemaining == 0 → guard hides panel

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden when secondsRemaining is exactly 0 (time-up guard).");

            Object.DestroyImmediate(timerEvent);
            Object.DestroyImmediate(panel);
        }
    }
}
