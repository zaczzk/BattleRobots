using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="MatchCountdownController"/>.
    ///
    /// Covers:
    ///   • OnEnable / OnDisable with null event channels → no throw.
    ///   • HandleTick (via IntGameEvent.Raise):
    ///       null panel, null text → no throw;
    ///       panel assigned       → panel becomes active;
    ///       text assigned        → text set to count.ToString().
    ///   • HandleComplete (via VoidGameEvent.Raise):
    ///       null panel, null text → no throw;
    ///       text assigned         → text set to "FIGHT!".
    ///   • OnDisable unregisters both callbacks (external counter pattern).
    ///
    /// Private handlers are exercised indirectly by raising the corresponding
    /// SO event channels — the same approach used in CombatHUDControllerTests.
    /// All tests run headless; no uGUI scene objects are required (panel and Text
    /// are created as needed via AddComponent on child GameObjects).
    /// </summary>
    public class MatchCountdownControllerTests
    {
        private GameObject                _go;
        private MatchCountdownController  _ctrl;

        // ── Reflection helpers ────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _go   = new GameObject("MatchCountdownController");
            _go.SetActive(false); // inactive so Awake/OnEnable don't fire during setup
            _ctrl = _go.AddComponent<MatchCountdownController>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        // ── OnEnable / OnDisable — null event channels ────────────────────────

        [Test]
        public void Enable_NullTickChannel_NullCompleteChannel_DoesNotThrow()
        {
            // Both event channels left null; ?. guards in OnEnable must silently skip.
            Assert.DoesNotThrow(() => _go.SetActive(true),
                "Activating MatchCountdownController with null event channels must not throw.");
        }

        [Test]
        public void Disable_NullTickChannel_NullCompleteChannel_DoesNotThrow()
        {
            _go.SetActive(true);
            // OnDisable must guard null channels on Unregister calls.
            Assert.DoesNotThrow(() => _go.SetActive(false),
                "Deactivating MatchCountdownController with null event channels must not throw.");
        }

        // ── HandleTick — null display refs ────────────────────────────────────

        [Test]
        public void TickRaised_NullPanel_NullText_DoesNotThrow()
        {
            var tick = ScriptableObject.CreateInstance<IntGameEvent>();
            SetField(_ctrl, "_onCountdownTick", tick);
            // _countdownPanel and _countdownText left null.
            _go.SetActive(true);

            Assert.DoesNotThrow(() => tick.Raise(3),
                "HandleTick with null _countdownPanel and _countdownText must not throw.");

            Object.DestroyImmediate(tick);
        }

        // ── HandleTick — panel visibility ─────────────────────────────────────

        [Test]
        public void TickRaised_WithPanel_ShowsPanel()
        {
            var tick  = ScriptableObject.CreateInstance<IntGameEvent>();
            var panel = new GameObject("Panel");

            SetField(_ctrl, "_onCountdownTick", tick);
            SetField(_ctrl, "_countdownPanel", panel);

            _go.SetActive(true); // Awake hides panel; OnEnable registers tick handler.

            Assert.IsFalse(panel.activeSelf,
                "Panel must start hidden after Awake().");

            tick.Raise(3);

            Assert.IsTrue(panel.activeSelf,
                "Raising tick must call SetActive(true) on _countdownPanel.");

            Object.DestroyImmediate(tick);
            Object.DestroyImmediate(panel);
        }

        // ── HandleTick — text content ─────────────────────────────────────────

        [Test]
        public void TickRaised_WithText_SetsTextToCountString()
        {
            var tick    = ScriptableObject.CreateInstance<IntGameEvent>();
            var textGO  = new GameObject("TextGO");
            var text    = textGO.AddComponent<UnityEngine.UI.Text>();

            SetField(_ctrl, "_onCountdownTick", tick);
            SetField(_ctrl, "_countdownText", text);

            _go.SetActive(true);

            tick.Raise(2);
            Assert.AreEqual("2", text.text,
                "After tick(2), _countdownText.text must be '2'.");

            tick.Raise(1);
            Assert.AreEqual("1", text.text,
                "After tick(1), _countdownText.text must be '1'.");

            Object.DestroyImmediate(tick);
            Object.DestroyImmediate(textGO);
        }

        // ── HandleComplete — null display refs ────────────────────────────────

        [Test]
        public void CompleteRaised_NullPanel_NullText_DoesNotThrow()
        {
            var complete = ScriptableObject.CreateInstance<VoidGameEvent>();
            SetField(_ctrl, "_onCountdownComplete", complete);
            SetField(_ctrl, "_fightDisplaySeconds", 0f); // skip coroutine
            // _countdownPanel and _countdownText left null.
            _go.SetActive(true);

            Assert.DoesNotThrow(() => complete.Raise(),
                "HandleComplete with null refs and _fightDisplaySeconds=0 must not throw.");

            Object.DestroyImmediate(complete);
        }

        // ── HandleComplete — text set to FIGHT! ───────────────────────────────

        [Test]
        public void CompleteRaised_WithText_SetsTextToFight()
        {
            var complete = ScriptableObject.CreateInstance<VoidGameEvent>();
            var textGO   = new GameObject("TextGO");
            var text     = textGO.AddComponent<UnityEngine.UI.Text>();

            SetField(_ctrl, "_onCountdownComplete", complete);
            SetField(_ctrl, "_countdownText", text);
            SetField(_ctrl, "_fightDisplaySeconds", 0f); // immediate hide; no coroutine timing

            _go.SetActive(true);

            complete.Raise();

            Assert.AreEqual("FIGHT!", text.text,
                "Raising _onCountdownComplete must set _countdownText to \"FIGHT!\".");

            Object.DestroyImmediate(complete);
            Object.DestroyImmediate(textGO);
        }

        // ── OnDisable — unregisters callbacks ─────────────────────────────────

        [Test]
        public void OnDisable_UnregistersTickCallback()
        {
            var tick = ScriptableObject.CreateInstance<IntGameEvent>();
            SetField(_ctrl, "_onCountdownTick", tick);
            _go.SetActive(true); // OnEnable registers handler.

            int count = 0;
            tick.RegisterCallback(v => count++); // external counter

            tick.Raise(3); // both internal + external handlers run → internal sets panel etc.
            int afterEnable = count;

            _go.SetActive(false); // OnDisable must unregister internal handler.

            tick.Raise(2); // only the external counter should fire now.
            int afterDisable = count;

            Assert.AreEqual(afterEnable + 1, afterDisable,
                "After OnDisable, only the external counter callback should still fire.");

            Object.DestroyImmediate(tick);
        }

        [Test]
        public void OnDisable_UnregistersCompleteCallback()
        {
            var complete = ScriptableObject.CreateInstance<VoidGameEvent>();
            SetField(_ctrl, "_onCountdownComplete", complete);
            SetField(_ctrl, "_fightDisplaySeconds", 0f);
            _go.SetActive(true); // OnEnable registers handler.

            int count = 0;
            complete.RegisterCallback(() => count++); // external counter

            complete.Raise(); // internal + external both fire.
            int afterEnable = count;

            _go.SetActive(false); // OnDisable must unregister internal handler.

            complete.Raise(); // only external counter fires now.
            int afterDisable = count;

            Assert.AreEqual(afterEnable + 1, afterDisable,
                "After OnDisable, only the external counter callback should still fire.");

            Object.DestroyImmediate(complete);
        }
    }
}
