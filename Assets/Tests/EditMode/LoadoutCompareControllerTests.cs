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
    /// EditMode tests for T211:
    ///   <see cref="LoadoutCompareController"/>.
    ///
    /// LoadoutCompareControllerTests (12):
    ///   FreshInstance_CurrentLoadoutIsNull                              ×1
    ///   FreshInstance_LoadoutHistoryIsNull                              ×1
    ///   OnEnable_AllNullRefs_DoesNotThrow                               ×1
    ///   OnDisable_AllNullRefs_DoesNotThrow                              ×1
    ///   OnDisable_Unregisters                                           ×1
    ///   Refresh_NullLoadout_HidesPanel                                  ×1
    ///   Refresh_EmptyHistory_HidesPanel                                 ×1
    ///   Refresh_BothData_ShowsPanel                                     ×1
    ///   Refresh_CurrentPartsLabel                                       ×1
    ///   Refresh_PartDelta_Positive                                      ×1
    ///   Refresh_PartDelta_Negative                                      ×1
    ///   Refresh_WinRateLabel                                            ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class LoadoutCompareControllerTests
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

        private static PlayerLoadout CreateLoadout(params string[] partIds)
        {
            var so = ScriptableObject.CreateInstance<PlayerLoadout>();
            so.SetLoadout(new List<string>(partIds));
            return so;
        }

        private static LoadoutHistorySO CreateHistory(int entries, bool[] wins = null)
        {
            var so = ScriptableObject.CreateInstance<LoadoutHistorySO>();
            SetField(so, "_maxHistory", 10);
            for (int i = 0; i < entries; i++)
            {
                bool won    = wins != null && i < wins.Length ? wins[i] : true;
                string[] ps = new[] { "part_a", "part_b" }; // 2 parts per entry
                so.AddEntry(ps, won, 0.0);
            }
            return so;
        }

        private static LoadoutCompareController CreateController()
        {
            var go = new GameObject("LoadoutCompare_Test");
            return go.AddComponent<LoadoutCompareController>();
        }

        private static Text AddText(GameObject parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent.transform);
            return child.AddComponent<Text>();
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_CurrentLoadoutIsNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.CurrentLoadout);
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
            SetField(ctrl, "_onLoadoutChanged", ch);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            int count = 0;
            ch.RegisterCallback(() => count++);
            InvokePrivate(ctrl, "OnDisable");
            ch.Raise();

            Assert.AreEqual(1, count, "After OnDisable only manually registered callback fires.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void Refresh_NullLoadout_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("panel");
            panel.SetActive(true);
            SetField(ctrl, "_comparePanel",   panel);
            SetField(ctrl, "_loadoutHistory", CreateHistory(1));
            // _currentLoadout left null
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf, "Null loadout must hide the panel.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Refresh_EmptyHistory_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("panel");
            panel.SetActive(true);
            var history = ScriptableObject.CreateInstance<LoadoutHistorySO>();
            SetField(history, "_maxHistory", 5);

            SetField(ctrl, "_comparePanel",    panel);
            SetField(ctrl, "_currentLoadout",  CreateLoadout("part_a"));
            SetField(ctrl, "_loadoutHistory",  history); // empty — Count = 0
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf, "Empty history must hide the panel.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(history);
        }

        [Test]
        public void Refresh_BothData_ShowsPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("panel");
            panel.SetActive(false);
            SetField(ctrl, "_comparePanel",   panel);
            SetField(ctrl, "_currentLoadout", CreateLoadout("part_a", "part_b"));
            SetField(ctrl, "_loadoutHistory", CreateHistory(1));
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.IsTrue(panel.activeSelf, "Both data present — panel must be shown.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Refresh_CurrentPartsLabel()
        {
            var ctrl    = CreateController();
            var currLbl = AddText(ctrl.gameObject, "current");
            SetField(ctrl, "_currentPartsLabel", currLbl);
            SetField(ctrl, "_currentLoadout",    CreateLoadout("a", "b", "c")); // 3 parts
            SetField(ctrl, "_loadoutHistory",    CreateHistory(1));
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.AreEqual("Current: 3 parts", currLbl.text);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Refresh_PartDelta_Positive()
        {
            var ctrl      = CreateController();
            var deltaLbl  = AddText(ctrl.gameObject, "delta");
            // Current: 4 parts, History: 2 parts → delta = +2
            SetField(ctrl, "_partDeltaLabel", deltaLbl);
            SetField(ctrl, "_currentLoadout", CreateLoadout("a", "b", "c", "d")); // 4
            SetField(ctrl, "_loadoutHistory", CreateHistory(1));                   // history has 2 per entry
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.AreEqual("+2", deltaLbl.text);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Refresh_PartDelta_Negative()
        {
            var ctrl     = CreateController();
            var deltaLbl = AddText(ctrl.gameObject, "delta");
            // Current: 1 part, History: 2 parts → delta = -1
            SetField(ctrl, "_partDeltaLabel", deltaLbl);
            SetField(ctrl, "_currentLoadout", CreateLoadout("a"));    // 1
            SetField(ctrl, "_loadoutHistory", CreateHistory(1));      // history has 2 per entry
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.AreEqual("-1", deltaLbl.text);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Refresh_WinRateLabel()
        {
            var ctrl      = CreateController();
            var rateLbl   = AddText(ctrl.gameObject, "winrate");
            // 2 entries: 1 win, 1 loss → 50%
            SetField(ctrl, "_winRateLabel",  rateLbl);
            SetField(ctrl, "_currentLoadout", CreateLoadout("a"));
            SetField(ctrl, "_loadoutHistory", CreateHistory(2, new[] { true, false }));
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.AreEqual("Win Rate: 50%", rateLbl.text);
            Object.DestroyImmediate(ctrl.gameObject);
        }
    }
}
