using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="MatchStatisticsHUDController"/> (M25 T172).
    ///
    /// Covers:
    ///   • OnEnable / OnDisable with all null refs — no throw.
    ///   • OnEnable / OnDisable with null channel — no throw.
    ///   • OnDisable unregisters callback (external-event pattern).
    ///   • Refresh with null _statistics → panel hidden.
    ///   • Refresh with null _statsPanel → no throw.
    ///   • Refresh with valid SO → panel shown.
    ///   • Refresh sets _totalDealtText correctly.
    ///   • Refresh sets _physicalBar value correctly.
    ///   • OnStatisticsUpdated raise triggers Refresh (integration).
    ///   • OnDisable hides the panel.
    ///
    /// All tests are headless (no scene / Canvas required).
    /// </summary>
    public class MatchStatisticsHUDControllerTests
    {
        // ── Reflection helpers ────────────────────────────────────────────────

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{fieldName}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        // ── Factory helpers ───────────────────────────────────────────────────

        private static (GameObject go, MatchStatisticsHUDController ctrl) MakeCtrl()
        {
            var go   = new GameObject("MatchStatisticsHUDController");
            go.SetActive(false);
            var ctrl = go.AddComponent<MatchStatisticsHUDController>();
            return (go, ctrl);
        }

        private static MatchStatisticsSO MakeStats(float physical = 0f, float energy = 0f)
        {
            var stats = ScriptableObject.CreateInstance<MatchStatisticsSO>();
            if (physical > 0f)
                stats.RecordDamageDealt(new DamageInfo(physical, "p", default, null, DamageType.Physical));
            if (energy > 0f)
                stats.RecordDamageDealt(new DamageInfo(energy, "p", default, null, DamageType.Energy));
            return stats;
        }

        // ═════════════════════════════════════════════════════════════════════
        // Null-guard / lifecycle tests
        // ═════════════════════════════════════════════════════════════════════

        [Test]
        public void OnEnable_AllNullRefs_DoesNotThrow()
        {
            var (go, _) = MakeCtrl();
            Assert.DoesNotThrow(() => go.SetActive(true));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_AllNullRefs_DoesNotThrow()
        {
            var (go, _) = MakeCtrl();
            go.SetActive(true);
            Assert.DoesNotThrow(() => go.SetActive(false));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnEnable_NullChannel_DoesNotThrow()
        {
            var stats      = ScriptableObject.CreateInstance<MatchStatisticsSO>();
            var (go, ctrl) = MakeCtrl();
            SetField(ctrl, "_statistics", stats);
            // _onStatisticsUpdated remains null

            Assert.DoesNotThrow(() => go.SetActive(true));

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(stats);
        }

        [Test]
        public void OnDisable_NullChannel_DoesNotThrow()
        {
            var (go, _) = MakeCtrl();
            go.SetActive(true);
            Assert.DoesNotThrow(() => go.SetActive(false));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_UnregistersCallback_RaiseAfterDisableIsNoOp()
        {
            var channel    = ScriptableObject.CreateInstance<VoidGameEvent>();
            var stats      = MakeStats(physical: 50f);
            var (go, ctrl) = MakeCtrl();
            var panelGO    = new GameObject("StatsPanel");

            SetField(ctrl, "_onStatisticsUpdated", channel);
            SetField(ctrl, "_statistics",          stats);
            SetField(ctrl, "_statsPanel",          panelGO);

            go.SetActive(true);   // subscribes + Refresh → panel shown
            go.SetActive(false);  // unsubscribes + panel hidden

            // Panel is now hidden. Raising channel must NOT call Refresh and re-show it.
            channel.Raise();

            Assert.IsFalse(panelGO.activeSelf,
                "Panel must stay hidden after unregistering the refresh callback.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panelGO);
            Object.DestroyImmediate(stats);
            Object.DestroyImmediate(channel);
        }

        // ═════════════════════════════════════════════════════════════════════
        // Refresh tests
        // ═════════════════════════════════════════════════════════════════════

        [Test]
        public void Refresh_NullStatistics_HidesPanel()
        {
            var (go, ctrl) = MakeCtrl();
            var panelGO    = new GameObject("StatsPanel");
            panelGO.SetActive(true);

            SetField(ctrl, "_statsPanel", panelGO);
            // _statistics left null

            go.SetActive(true); // OnEnable calls Refresh

            Assert.IsFalse(panelGO.activeSelf,
                "Refresh must hide the panel when _statistics is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panelGO);
        }

        [Test]
        public void Refresh_NullPanel_DoesNotThrow()
        {
            var stats      = MakeStats(physical: 50f);
            var (go, ctrl) = MakeCtrl();
            SetField(ctrl, "_statistics", stats);
            // _statsPanel left null

            go.SetActive(true);
            Assert.DoesNotThrow(() => ctrl.Refresh());

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(stats);
        }

        [Test]
        public void Refresh_ValidStatistics_ShowsPanel()
        {
            var stats      = MakeStats(physical: 100f);
            var (go, ctrl) = MakeCtrl();
            var panelGO    = new GameObject("StatsPanel");
            panelGO.SetActive(false);

            SetField(ctrl, "_statistics", stats);
            SetField(ctrl, "_statsPanel", panelGO);

            go.SetActive(true);

            Assert.IsTrue(panelGO.activeSelf,
                "Refresh must show the panel when _statistics is assigned.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panelGO);
            Object.DestroyImmediate(stats);
        }

        [Test]
        public void Refresh_SetsTotalDealtTextCorrectly()
        {
            var stats      = MakeStats(physical: 60f, energy: 40f); // total = 100
            var (go, ctrl) = MakeCtrl();
            var textGO     = new GameObject("TotalDealtText");
            var text       = textGO.AddComponent<Text>();

            SetField(ctrl, "_statistics",    stats);
            SetField(ctrl, "_totalDealtText", text);

            go.SetActive(true);

            Assert.AreEqual("Dealt: 100", text.text,
                "_totalDealtText must show 'Dealt: N' with rounded TotalDamageDealt.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(textGO);
            Object.DestroyImmediate(stats);
        }

        [Test]
        public void Refresh_SetsPhysicalBarCorrectly()
        {
            // 75 physical out of 100 total → ratio 0.75
            var stats      = MakeStats(physical: 75f, energy: 25f);
            var (go, ctrl) = MakeCtrl();
            var sliderGO   = new GameObject("PhysicalBar");
            var slider     = sliderGO.AddComponent<Slider>();

            SetField(ctrl, "_statistics",  stats);
            SetField(ctrl, "_physicalBar", slider);

            go.SetActive(true);

            Assert.AreEqual(0.75f, slider.value, 0.0001f,
                "_physicalBar.value must equal DamageTypeRatio(Physical).");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(sliderGO);
            Object.DestroyImmediate(stats);
        }

        // ═════════════════════════════════════════════════════════════════════
        // Event integration
        // ═════════════════════════════════════════════════════════════════════

        [Test]
        public void OnStatisticsUpdated_Raise_CallsRefresh()
        {
            var channel    = ScriptableObject.CreateInstance<VoidGameEvent>();
            var stats      = ScriptableObject.CreateInstance<MatchStatisticsSO>();
            var (go, ctrl) = MakeCtrl();
            var panelGO    = new GameObject("StatsPanel");

            SetField(ctrl, "_onStatisticsUpdated", channel);
            SetField(ctrl, "_statistics",          stats);
            SetField(ctrl, "_statsPanel",          panelGO);

            go.SetActive(true); // subscribes + initial Refresh

            // Record damage after enable, then raise to trigger Refresh.
            stats.RecordDamageDealt(
                new DamageInfo(50f, "p", default, null, DamageType.Physical));
            channel.Raise();

            Assert.IsTrue(panelGO.activeSelf,
                "Raising _onStatisticsUpdated must call Refresh and show the panel.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panelGO);
            Object.DestroyImmediate(stats);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void OnDisable_HidesPanel()
        {
            var stats      = MakeStats(physical: 50f);
            var (go, ctrl) = MakeCtrl();
            var panelGO    = new GameObject("StatsPanel");

            SetField(ctrl, "_statistics", stats);
            SetField(ctrl, "_statsPanel", panelGO);

            go.SetActive(true);  // Refresh shows panel
            Assert.IsTrue(panelGO.activeSelf, "Pre-condition: panel should be visible.");

            go.SetActive(false); // OnDisable hides panel

            Assert.IsFalse(panelGO.activeSelf,
                "OnDisable must hide the stats panel.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panelGO);
            Object.DestroyImmediate(stats);
        }
    }
}
