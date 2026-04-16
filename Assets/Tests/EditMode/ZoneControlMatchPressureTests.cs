using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T319: <see cref="ZoneControlMatchPressureSO"/> and
    /// <see cref="ZoneControlMatchPressureController"/>.
    ///
    /// ZoneControlMatchPressureTests (12):
    ///   SO_FreshInstance_Pressure_Zero                                ×1
    ///   SO_IncreasePressure_BelowThreshold_FiresNothing              ×1
    ///   SO_IncreasePressure_CrossesThreshold_FiresHighPressure       ×1
    ///   SO_DecreasePressure_CrossesBack_FiresPressureRelieved        ×1
    ///   SO_EvaluatePressure_BotLeads_Increases                       ×1
    ///   SO_EvaluatePressure_PlayerLeads_Decreases                    ×1
    ///   SO_Reset_ClearsPressure                                       ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                    ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow                   ×1
    ///   Controller_OnDisable_Unregisters_Channel                     ×1
    ///   Controller_HandleScoreboardUpdated_NullRefs_NoThrow          ×1
    ///   Controller_HandleScoreboardUpdated_BotLeads_IncreasesPressure×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneControlMatchPressureTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static ZoneControlMatchPressureSO CreatePressureSO(
            float increment = 0.25f, float decay = 0.25f, float threshold = 0.75f)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlMatchPressureSO>();
            SetField(so, "_pressureIncrement",     increment);
            SetField(so, "_pressureDecay",         decay);
            SetField(so, "_highPressureThreshold", threshold);
            so.Reset();
            return so;
        }

        private static ZoneControlMatchPressureController CreateController() =>
            new GameObject("PressureCtrl_Test")
                .AddComponent<ZoneControlMatchPressureController>();

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_Pressure_Zero()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlMatchPressureSO>();
            Assert.AreEqual(0f, so.Pressure,
                "Pressure must be 0 on a fresh instance.");
            Assert.IsFalse(so.IsHighPressure,
                "IsHighPressure must be false on a fresh instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_IncreasePressure_BelowThreshold_FiresNothing()
        {
            var so  = CreatePressureSO(increment: 0.25f, threshold: 0.75f);
            var evt = CreateEvent();
            SetField(so, "_onHighPressure", evt);

            int fired = 0;
            evt.RegisterCallback(() => fired++);

            so.EvaluatePressure(true); // pressure = 0.25 — below threshold
            Assert.AreEqual(0, fired,
                "_onHighPressure must not fire when pressure is below threshold.");
            Assert.IsFalse(so.IsHighPressure);

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_IncreasePressure_CrossesThreshold_FiresHighPressure()
        {
            // threshold = 0.5 so two increments of 0.25 cross it.
            var so  = CreatePressureSO(increment: 0.25f, threshold: 0.5f);
            var evt = CreateEvent();
            SetField(so, "_onHighPressure", evt);

            int fired = 0;
            evt.RegisterCallback(() => fired++);

            so.EvaluatePressure(true); // 0.25
            so.EvaluatePressure(true); // 0.50 — crosses threshold exactly

            Assert.AreEqual(1, fired,
                "_onHighPressure must fire once when pressure crosses threshold.");
            Assert.IsTrue(so.IsHighPressure);

            // Second evaluation must not re-fire.
            so.EvaluatePressure(true);
            Assert.AreEqual(1, fired,
                "_onHighPressure must not fire again while already in high-pressure.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_DecreasePressure_CrossesBack_FiresPressureRelieved()
        {
            var so        = CreatePressureSO(increment: 1f, decay: 0.5f, threshold: 0.5f);
            var relievedEvt = CreateEvent();
            SetField(so, "_onPressureRelieved", relievedEvt);

            int relieved = 0;
            relievedEvt.RegisterCallback(() => relieved++);

            so.EvaluatePressure(true);  // pressure = 1.0 → high pressure
            so.EvaluatePressure(false); // pressure = 0.5 — still at threshold (not below)
            Assert.AreEqual(0, relieved, "Pressure at threshold must not trigger relief.");

            so.EvaluatePressure(false); // pressure = 0.0 — below threshold
            Assert.AreEqual(1, relieved,
                "_onPressureRelieved must fire once when pressure drops below threshold.");
            Assert.IsFalse(so.IsHighPressure);

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(relievedEvt);
        }

        [Test]
        public void SO_EvaluatePressure_BotLeads_Increases()
        {
            var so = CreatePressureSO(increment: 0.4f, threshold: 0.75f);
            so.EvaluatePressure(true);
            Assert.AreEqual(0.4f, so.Pressure, 0.001f,
                "Pressure must increase by _pressureIncrement when bot leads.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EvaluatePressure_PlayerLeads_Decreases()
        {
            // Pre-load pressure to 1f then decrease.
            var so = CreatePressureSO(increment: 1f, decay: 0.3f, threshold: 0.75f);
            so.EvaluatePressure(true); // pressure = 1.0
            so.EvaluatePressure(false);
            Assert.AreEqual(0.7f, so.Pressure, 0.001f,
                "Pressure must decrease by _pressureDecay when player leads.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsPressure()
        {
            var so = CreatePressureSO(increment: 1f, threshold: 0.5f);
            so.EvaluatePressure(true); // pressure = 1.0, isHighPressure = true
            so.Reset();
            Assert.AreEqual(0f, so.Pressure, "Pressure must be 0 after Reset.");
            Assert.IsFalse(so.IsHighPressure, "IsHighPressure must be false after Reset.");
            Object.DestroyImmediate(so);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_OnEnable_Null");
            Assert.DoesNotThrow(
                () => go.AddComponent<ZoneControlMatchPressureController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneControlMatchPressureController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channel()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneControlMatchPressureController>();

            var evt = CreateEvent();
            SetField(ctrl, "_onScoreboardUpdated", evt);

            go.SetActive(true);
            go.SetActive(false);

            int count = 0;
            evt.RegisterCallback(() => count++);
            evt.Raise();

            Assert.AreEqual(1, count,
                "_onScoreboardUpdated must be unregistered after OnDisable.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void Controller_HandleScoreboardUpdated_NullRefs_NoThrow()
        {
            var go   = new GameObject("Test_NullHandler");
            var ctrl = go.AddComponent<ZoneControlMatchPressureController>();
            Assert.DoesNotThrow(() => ctrl.HandleScoreboardUpdated(),
                "HandleScoreboardUpdated with null SOs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_HandleScoreboardUpdated_BotLeads_IncreasesPressure()
        {
            var go   = new GameObject("Test_BotLeads");
            var ctrl = go.AddComponent<ZoneControlMatchPressureController>();

            var pressureSO   = CreatePressureSO(increment: 0.25f, threshold: 0.75f);
            var scoreboardSO = ScriptableObject.CreateInstance<ZoneControlScoreboardSO>();

            SetField(ctrl, "_pressureSO",   pressureSO);
            SetField(ctrl, "_scoreboardSO", scoreboardSO);

            // Give the bot a capture so PlayerRank > 1.
            scoreboardSO.RecordBotCapture(0); // bot[0] = 1, player = 0 → PlayerRank = 2

            ctrl.HandleScoreboardUpdated();

            Assert.Greater(pressureSO.Pressure, 0f,
                "Pressure must increase after HandleScoreboardUpdated when bots lead.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(pressureSO);
            Object.DestroyImmediate(scoreboardSO);
        }
    }
}
