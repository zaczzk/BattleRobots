using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T182:
    ///   <see cref="DamageHistoryHUDController"/>.
    ///
    /// DamageHistoryHUDControllerTests (16):
    ///   FreshInstance_HistoryIsNull ×1
    ///   OnEnable_AllNullRefs_DoesNotThrow ×1
    ///   OnDisable_AllNullRefs_DoesNotThrow ×1
    ///   OnDisable_HidesPanel ×1
    ///   Refresh_NullHistory_HidesPanel ×1
    ///   Refresh_EmptyHistory_HidesPanel ×1
    ///   Refresh_OneEntry_ShowsPanel ×1
    ///   Refresh_OneEntry_SetsAvgLabels ×1
    ///   Refresh_OneEntry_SetsRatioBarsToOne_WhenSingleType ×1
    ///   Refresh_MixedEntries_RatiosSumToOne ×1
    ///   Refresh_AllZeroValues_BarsAreZero ×1
    ///   Refresh_NullPanel_DoesNotThrow ×1
    ///   Refresh_NullLabels_DoesNotThrow ×1
    ///   Refresh_NullBars_DoesNotThrow ×1
    ///   OnDisable_Unregisters ×1
    ///   OnEnable_CallsRefresh_ShowsPanelWhenData ×1
    ///
    /// Total: 16 new EditMode tests.
    /// </summary>
    public class DamageHistoryHUDControllerTests
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

        private static MatchDamageHistorySO CreateHistory(int maxHistory = 10)
        {
            var so = ScriptableObject.CreateInstance<MatchDamageHistorySO>();
            SetField(so, "_maxHistory", maxHistory);
            return so;
        }

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static DamageHistoryHUDController CreateController()
        {
            var go = new GameObject("DamageHistoryHUD_Test");
            return go.AddComponent<DamageHistoryHUDController>();
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void Ctrl_FreshInstance_HistoryIsNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.History);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_OnEnable_AllNullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnEnable"));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_OnDisable_AllNullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnDisable"));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_OnDisable_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("Panel");
            panel.SetActive(true);
            SetField(ctrl, "_panel", panel);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnDisable");

            Assert.IsFalse(panel.activeSelf,
                "OnDisable must hide the panel.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Ctrl_Refresh_NullHistory_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("Panel");
            panel.SetActive(true);
            SetField(ctrl, "_panel", panel);
            // _history is null

            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Null history must hide the panel.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Ctrl_Refresh_EmptyHistory_HidesPanel()
        {
            var ctrl    = CreateController();
            var panel   = new GameObject("Panel");
            var history = CreateHistory();
            panel.SetActive(true);
            SetField(ctrl, "_panel",   panel);
            SetField(ctrl, "_history", history);
            // No entries added → Count == 0

            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Empty history must hide the panel.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(history);
        }

        [Test]
        public void Ctrl_Refresh_OneEntry_ShowsPanel()
        {
            var ctrl    = CreateController();
            var panel   = new GameObject("Panel");
            var history = CreateHistory();
            history.AddEntry(new DamageTypeSnapshot { physical = 100f });
            panel.SetActive(false);
            SetField(ctrl, "_panel",   panel);
            SetField(ctrl, "_history", history);

            ctrl.Refresh();

            Assert.IsTrue(panel.activeSelf,
                "Panel must be shown when history has at least one entry.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(history);
        }

        [Test]
        public void Ctrl_Refresh_OneEntry_SetsAvgLabels()
        {
            var ctrl    = CreateController();
            var history = CreateHistory();
            history.AddEntry(new DamageTypeSnapshot { physical = 50f, energy = 30f });

            var physLabelGO = new GameObject("PhysLabel");
            var physText    = physLabelGO.AddComponent<Text>();

            SetField(ctrl, "_history",        history);
            SetField(ctrl, "_physicalAvgText", physText);

            ctrl.Refresh();

            Assert.AreEqual("50", physText.text,
                "_physicalAvgText must show rounded Physical average.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(physLabelGO);
            Object.DestroyImmediate(history);
        }

        [Test]
        public void Ctrl_Refresh_OneEntry_SetsRatioBar_WhenSingleType()
        {
            var ctrl    = CreateController();
            var history = CreateHistory();
            history.AddEntry(new DamageTypeSnapshot { physical = 100f }); // only physical

            var barGO = new GameObject("PhysBar");
            var bar   = barGO.AddComponent<Slider>();
            bar.minValue = 0f;
            bar.maxValue = 1f;

            SetField(ctrl, "_history",     history);
            SetField(ctrl, "_physicalBar", bar);

            ctrl.Refresh();

            Assert.AreEqual(1f, bar.value, 0.001f,
                "Physical bar ratio must be 1.0 when all damage is Physical.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(barGO);
            Object.DestroyImmediate(history);
        }

        [Test]
        public void Ctrl_Refresh_MixedEntries_RatiosSumToOne()
        {
            var ctrl    = CreateController();
            var history = CreateHistory();
            history.AddEntry(new DamageTypeSnapshot
            {
                physical = 40f, energy = 30f, thermal = 20f, shock = 10f,
            });

            var physBarGO    = new GameObject("PhysBar");
            var energyBarGO  = new GameObject("EnergyBar");
            var thermalBarGO = new GameObject("ThermalBar");
            var shockBarGO   = new GameObject("ShockBar");

            var physBar    = physBarGO.AddComponent<Slider>();
            var energyBar  = energyBarGO.AddComponent<Slider>();
            var thermalBar = thermalBarGO.AddComponent<Slider>();
            var shockBar   = shockBarGO.AddComponent<Slider>();

            foreach (var s in new[] { physBar, energyBar, thermalBar, shockBar })
            {
                s.minValue = 0f;
                s.maxValue = 1f;
            }

            SetField(ctrl, "_history",     history);
            SetField(ctrl, "_physicalBar", physBar);
            SetField(ctrl, "_energyBar",   energyBar);
            SetField(ctrl, "_thermalBar",  thermalBar);
            SetField(ctrl, "_shockBar",    shockBar);

            ctrl.Refresh();

            float sum = physBar.value + energyBar.value + thermalBar.value + shockBar.value;
            Assert.AreEqual(1f, sum, 0.01f,
                "All four ratio bars must sum to 1.0.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(physBarGO);
            Object.DestroyImmediate(energyBarGO);
            Object.DestroyImmediate(thermalBarGO);
            Object.DestroyImmediate(shockBarGO);
            Object.DestroyImmediate(history);
        }

        [Test]
        public void Ctrl_Refresh_AllZeroValues_BarsAreZero()
        {
            var ctrl    = CreateController();
            var history = CreateHistory();
            history.AddEntry(new DamageTypeSnapshot()); // all zeros

            var barGO = new GameObject("PhysBar");
            var bar   = barGO.AddComponent<Slider>();
            bar.minValue = 0f;
            bar.maxValue = 1f;
            bar.value    = 0.5f; // pre-set to non-zero

            SetField(ctrl, "_history",     history);
            SetField(ctrl, "_physicalBar", bar);

            ctrl.Refresh();

            Assert.AreEqual(0f, bar.value, 0.001f,
                "When all averages are zero, ratio bars must be 0.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(barGO);
            Object.DestroyImmediate(history);
        }

        [Test]
        public void Ctrl_Refresh_NullPanel_DoesNotThrow()
        {
            var ctrl    = CreateController();
            var history = CreateHistory();
            history.AddEntry(new DamageTypeSnapshot { physical = 10f });
            SetField(ctrl, "_history", history);
            // _panel is null

            Assert.DoesNotThrow(() => ctrl.Refresh(),
                "Null panel must not cause an exception.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(history);
        }

        [Test]
        public void Ctrl_Refresh_NullLabels_DoesNotThrow()
        {
            var ctrl    = CreateController();
            var history = CreateHistory();
            history.AddEntry(new DamageTypeSnapshot { physical = 10f });
            SetField(ctrl, "_history", history);
            // All Text labels are null

            Assert.DoesNotThrow(() => ctrl.Refresh(),
                "Null text labels must not cause an exception.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(history);
        }

        [Test]
        public void Ctrl_Refresh_NullBars_DoesNotThrow()
        {
            var ctrl    = CreateController();
            var history = CreateHistory();
            history.AddEntry(new DamageTypeSnapshot { energy = 20f });
            SetField(ctrl, "_history", history);
            // All Slider bars are null

            Assert.DoesNotThrow(() => ctrl.Refresh(),
                "Null slider bars must not cause an exception.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(history);
        }

        [Test]
        public void Ctrl_OnDisable_Unregisters()
        {
            var ctrl = CreateController();
            var ch   = CreateEvent();
            SetField(ctrl, "_onHistoryUpdated", ch);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            int callCount = 0;
            ch.RegisterCallback(() => callCount++);
            InvokePrivate(ctrl, "OnDisable");
            ch.Raise();

            Assert.AreEqual(1, callCount,
                "After OnDisable, only the manually registered callback should fire.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void Ctrl_OnEnable_CallsRefresh_ShowsPanelWhenData()
        {
            var ctrl    = CreateController();
            var panel   = new GameObject("Panel");
            var history = CreateHistory();
            history.AddEntry(new DamageTypeSnapshot { physical = 50f });
            panel.SetActive(false);

            SetField(ctrl, "_panel",   panel);
            SetField(ctrl, "_history", history);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            Assert.IsTrue(panel.activeSelf,
                "OnEnable must call Refresh() which shows the panel when history has data.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(history);
        }
    }
}
