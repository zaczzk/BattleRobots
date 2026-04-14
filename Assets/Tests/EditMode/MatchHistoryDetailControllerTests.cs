using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T210:
    ///   <see cref="MatchHistoryDetailController"/>.
    ///
    /// MatchHistoryDetailControllerTests (14):
    ///   FreshInstance_MatchStatisticsIsNull                             ×1
    ///   FreshInstance_LoadoutHistoryIsNull                              ×1
    ///   OnEnable_AllNullRefs_DoesNotThrow                               ×1
    ///   OnDisable_AllNullRefs_DoesNotThrow                              ×1
    ///   OnDisable_Unregisters                                           ×1
    ///   OnEnable_HidesDetailPanel                                       ×1
    ///   Refresh_BothNull_HidesPanel                                     ×1
    ///   Refresh_NullStats_WithHistory_ShowsPanel                        ×1
    ///   Refresh_WithStats_ShowsPhysicalLabel                            ×1
    ///   Refresh_WithStats_ShowsEnergyLabel                              ×1
    ///   Refresh_WithHistory_ShowsPartCount                              ×1
    ///   Refresh_WithHistory_ShowsOutcomeWin                             ×1
    ///   Refresh_WithHistory_ShowsOutcomeLoss                            ×1
    ///   OnMatchEnded_Raise_CallsRefresh                                 ×1
    ///
    /// Total: 14 new EditMode tests.
    /// </summary>
    public class MatchHistoryDetailControllerTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void InvokePrivate(object target, string method)
        {
            MethodInfo mi = target.GetType()
                .GetMethod(method,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(mi, $"Method '{method}' not found on {target.GetType().Name}.");
            mi.Invoke(target, null);
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static MatchStatisticsSO CreateStats(float physical = 0f, float energy = 0f)
        {
            var so = ScriptableObject.CreateInstance<MatchStatisticsSO>();
            so.Reset();
            if (physical > 0f)
                so.RecordDamageDealt(new DamageInfo(physical, "Test",
                    UnityEngine.Vector3.zero, null, DamageType.Physical));
            if (energy > 0f)
                so.RecordDamageDealt(new DamageInfo(energy, "Test",
                    UnityEngine.Vector3.zero, null, DamageType.Energy));
            return so;
        }

        private static LoadoutHistorySO CreateHistory(bool playerWon = true, int partCount = 3)
        {
            var so = ScriptableObject.CreateInstance<LoadoutHistorySO>();
            // Trigger OnEnable to init buffer
            SetField(so, "_maxHistory", 5);

            string[] parts = new string[partCount];
            for (int i = 0; i < partCount; i++) parts[i] = $"part_{i}";
            so.AddEntry(parts, playerWon, 0.0);
            return so;
        }

        private static MatchHistoryDetailController CreateController()
        {
            var go = new GameObject("MatchHistoryDetail_Test");
            return go.AddComponent<MatchHistoryDetailController>();
        }

        private static Text AddText(GameObject parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent.transform);
            return child.AddComponent<Text>();
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_MatchStatisticsIsNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.MatchStatistics);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void FreshInstance_LoadoutHistoryIsNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.LoadoutHistory);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnEnable_AllNullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnEnable"));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnDisable_AllNullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnDisable"));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnDisable_Unregisters()
        {
            var ctrl = CreateController();
            var ch   = CreateEvent();
            SetField(ctrl, "_onMatchEnded", ch);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            int count = 0;
            ch.RegisterCallback(() => count++);
            InvokePrivate(ctrl, "OnDisable");
            ch.Raise();

            Assert.AreEqual(1, count, "After OnDisable only the manually registered callback fires.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void OnEnable_HidesDetailPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("panel");
            panel.SetActive(true);
            SetField(ctrl, "_detailPanel", panel);
            InvokePrivate(ctrl, "Awake");

            InvokePrivate(ctrl, "OnEnable");

            Assert.IsFalse(panel.activeSelf, "OnEnable should hide the detail panel.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Refresh_BothNull_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("panel");
            panel.SetActive(true);
            SetField(ctrl, "_detailPanel", panel);
            // Both stats and history left null
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf, "Both null should hide the panel.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Refresh_NullStats_WithHistory_ShowsPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("panel");
            panel.SetActive(false);
            SetField(ctrl, "_detailPanel",   panel);
            SetField(ctrl, "_loadoutHistory", CreateHistory());
            // _matchStatistics left null
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.IsTrue(panel.activeSelf, "History present — panel should be shown.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Refresh_WithStats_ShowsPhysicalLabel()
        {
            var ctrl     = CreateController();
            var physLbl  = AddText(ctrl.gameObject, "physical");

            var stats = ScriptableObject.CreateInstance<MatchStatisticsSO>();
            stats.Reset();
            stats.RecordDamageDealt(new DamageInfo(42f, "P",
                UnityEngine.Vector3.zero, null, DamageType.Physical));

            SetField(ctrl, "_matchStatistics",    stats);
            SetField(ctrl, "_physicalDamageLabel", physLbl);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.AreEqual("Physical: 42", physLbl.text);
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(stats);
        }

        [Test]
        public void Refresh_WithStats_ShowsEnergyLabel()
        {
            var ctrl      = CreateController();
            var energyLbl = AddText(ctrl.gameObject, "energy");

            var stats = ScriptableObject.CreateInstance<MatchStatisticsSO>();
            stats.Reset();
            stats.RecordDamageDealt(new DamageInfo(75f, "P",
                UnityEngine.Vector3.zero, null, DamageType.Energy));

            SetField(ctrl, "_matchStatistics",  stats);
            SetField(ctrl, "_energyDamageLabel", energyLbl);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.AreEqual("Energy: 75", energyLbl.text);
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(stats);
        }

        [Test]
        public void Refresh_WithHistory_ShowsPartCount()
        {
            var ctrl       = CreateController();
            var partLbl    = AddText(ctrl.gameObject, "parts");

            SetField(ctrl, "_loadoutHistory", CreateHistory(partCount: 5));
            SetField(ctrl, "_partCountLabel", partLbl);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.AreEqual("5 parts equipped", partLbl.text);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Refresh_WithHistory_ShowsOutcomeWin()
        {
            var ctrl       = CreateController();
            var outcomeLbl = AddText(ctrl.gameObject, "outcome");

            SetField(ctrl, "_loadoutHistory", CreateHistory(playerWon: true));
            SetField(ctrl, "_outcomeLabel",   outcomeLbl);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.AreEqual("WIN", outcomeLbl.text);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Refresh_WithHistory_ShowsOutcomeLoss()
        {
            var ctrl       = CreateController();
            var outcomeLbl = AddText(ctrl.gameObject, "outcome");

            SetField(ctrl, "_loadoutHistory", CreateHistory(playerWon: false));
            SetField(ctrl, "_outcomeLabel",   outcomeLbl);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.AreEqual("LOSS", outcomeLbl.text);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnMatchEnded_Raise_CallsRefresh()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("panel");
            panel.SetActive(false);
            var ch    = CreateEvent();

            SetField(ctrl, "_detailPanel",   panel);
            SetField(ctrl, "_onMatchEnded",  ch);
            SetField(ctrl, "_loadoutHistory", CreateHistory());
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            ch.Raise();

            Assert.IsTrue(panel.activeSelf, "Raising _onMatchEnded should trigger Refresh and show the panel.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(ch);
        }
    }
}
