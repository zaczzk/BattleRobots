using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="PostMatchDamageTypeController"/> (M25 T171).
    ///
    /// Covers:
    ///   • OnEnable / OnDisable with all null refs — no throw.
    ///   • OnEnable / OnDisable with null channels — no throw.
    ///   • OnDisable unregisters from _onMatchEnded (external-counter pattern).
    ///   • ShowResults with null MatchStatisticsSO → panel hidden.
    ///   • ShowResults with valid SO → panel shown.
    ///   • ShowResults sets _physicalText correctly.
    ///   • ShowResults sets _totalDamageText correctly.
    ///   • ShowResults with null _statisticsPanel → no throw.
    ///   • ResetView with null UI refs → no throw.
    ///   • OnMatchEnded raise triggers ShowResults (integration).
    ///   • OnMatchStarted raise triggers ResetView (integration).
    ///   • ShowResults sets _energyText correctly.
    ///
    /// All tests are headless (no scene / Canvas required).
    /// </summary>
    public class PostMatchDamageTypeControllerTests
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

        private static (GameObject go, PostMatchDamageTypeController ctrl) MakeCtrl()
        {
            var go   = new GameObject("PostMatchDamageTypeController");
            go.SetActive(false);
            var ctrl = go.AddComponent<PostMatchDamageTypeController>();
            return (go, ctrl);
        }

        private static MatchStatisticsSO MakeStats(
            float physical = 0f, float energy = 0f,
            float thermal  = 0f, float shock  = 0f)
        {
            var stats = ScriptableObject.CreateInstance<MatchStatisticsSO>();
            if (physical > 0f)
                stats.RecordDamageDealt(new DamageInfo(physical, "p", default, null, DamageType.Physical));
            if (energy > 0f)
                stats.RecordDamageDealt(new DamageInfo(energy,   "p", default, null, DamageType.Energy));
            if (thermal > 0f)
                stats.RecordDamageDealt(new DamageInfo(thermal,  "p", default, null, DamageType.Thermal));
            if (shock > 0f)
                stats.RecordDamageDealt(new DamageInfo(shock,    "p", default, null, DamageType.Shock));
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
        public void OnEnable_NullChannels_DoesNotThrow()
        {
            // Channels null, stats assigned — should still not throw on enable.
            var stats  = ScriptableObject.CreateInstance<MatchStatisticsSO>();
            var (go, ctrl) = MakeCtrl();
            SetField(ctrl, "_statistics", stats);
            // _onMatchEnded and _onMatchStarted remain null

            Assert.DoesNotThrow(() => go.SetActive(true));

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(stats);
        }

        [Test]
        public void OnDisable_UnregistersFromMatchEndedChannel()
        {
            // After OnDisable, raising the channel must not invoke ShowResults.
            var matchEnded = ScriptableObject.CreateInstance<VoidGameEvent>();
            var stats      = MakeStats(physical: 50f);
            var (go, ctrl) = MakeCtrl();
            var panelGO    = new GameObject("Panel");

            SetField(ctrl, "_onMatchEnded",    matchEnded);
            SetField(ctrl, "_statistics",      stats);
            SetField(ctrl, "_statisticsPanel", panelGO);

            go.SetActive(true);   // subscribes
            go.SetActive(false);  // unsubscribes

            // Panel was hidden by OnDisable's ResetView; ShowResults must NOT re-show it.
            matchEnded.Raise();

            Assert.IsFalse(panelGO.activeSelf,
                "Panel must remain hidden after unregistering from _onMatchEnded.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panelGO);
            Object.DestroyImmediate(stats);
            Object.DestroyImmediate(matchEnded);
        }

        // ═════════════════════════════════════════════════════════════════════
        // ShowResults tests
        // ═════════════════════════════════════════════════════════════════════

        [Test]
        public void ShowResults_NullStatistics_HidesPanel()
        {
            var (go, ctrl) = MakeCtrl();
            var panelGO    = new GameObject("Panel");
            panelGO.SetActive(true); // start visible

            SetField(ctrl, "_statisticsPanel", panelGO);
            // _statistics intentionally left null

            go.SetActive(true);
            ctrl.ShowResults();

            Assert.IsFalse(panelGO.activeSelf,
                "Panel must be hidden when _statistics is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panelGO);
        }

        [Test]
        public void ShowResults_ValidStatistics_ShowsPanel()
        {
            var stats      = MakeStats(physical: 100f);
            var (go, ctrl) = MakeCtrl();
            var panelGO    = new GameObject("Panel");
            panelGO.SetActive(false);

            SetField(ctrl, "_statistics",      stats);
            SetField(ctrl, "_statisticsPanel", panelGO);

            go.SetActive(true);
            ctrl.ShowResults();

            Assert.IsTrue(panelGO.activeSelf,
                "Panel must be shown when _statistics is assigned.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panelGO);
            Object.DestroyImmediate(stats);
        }

        [Test]
        public void ShowResults_SetsPhysicalTextCorrectly()
        {
            var stats      = MakeStats(physical: 75f);
            var (go, ctrl) = MakeCtrl();
            var textGO     = new GameObject("PhysicalText");
            var text       = textGO.AddComponent<Text>();

            SetField(ctrl, "_statistics",  stats);
            SetField(ctrl, "_physicalText", text);

            go.SetActive(true);
            ctrl.ShowResults();

            Assert.AreEqual("Physical: 75", text.text,
                "Physical text must show rounded dealt amount.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(textGO);
            Object.DestroyImmediate(stats);
        }

        [Test]
        public void ShowResults_SetsEnergyTextCorrectly()
        {
            var stats      = MakeStats(energy: 40f);
            var (go, ctrl) = MakeCtrl();
            var textGO     = new GameObject("EnergyText");
            var text       = textGO.AddComponent<Text>();

            SetField(ctrl, "_statistics", stats);
            SetField(ctrl, "_energyText", text);

            go.SetActive(true);
            ctrl.ShowResults();

            Assert.AreEqual("Energy: 40", text.text);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(textGO);
            Object.DestroyImmediate(stats);
        }

        [Test]
        public void ShowResults_SetsTotalDamageTextCorrectly()
        {
            var stats      = MakeStats(physical: 60f, energy: 40f);
            var (go, ctrl) = MakeCtrl();
            var textGO     = new GameObject("TotalText");
            var text       = textGO.AddComponent<Text>();

            SetField(ctrl, "_statistics",     stats);
            SetField(ctrl, "_totalDamageText", text);

            go.SetActive(true);
            ctrl.ShowResults();

            Assert.AreEqual("Total: 100", text.text,
                "Total text must show sum of all dealt damage.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(textGO);
            Object.DestroyImmediate(stats);
        }

        [Test]
        public void ShowResults_NullPanel_DoesNotThrow()
        {
            var stats      = MakeStats(physical: 50f);
            var (go, ctrl) = MakeCtrl();
            SetField(ctrl, "_statistics", stats);
            // _statisticsPanel left null

            go.SetActive(true);
            Assert.DoesNotThrow(() => ctrl.ShowResults());

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(stats);
        }

        // ═════════════════════════════════════════════════════════════════════
        // ResetView tests
        // ═════════════════════════════════════════════════════════════════════

        [Test]
        public void ResetView_NullUIRefs_DoesNotThrow()
        {
            var (go, ctrl) = MakeCtrl();
            go.SetActive(true);
            Assert.DoesNotThrow(() => ctrl.ResetView(),
                "ResetView with no UI refs assigned must not throw.");
            Object.DestroyImmediate(go);
        }

        // ═════════════════════════════════════════════════════════════════════
        // Event integration tests
        // ═════════════════════════════════════════════════════════════════════

        [Test]
        public void OnMatchEnded_Raise_TriggersShowResults()
        {
            var matchEnded = ScriptableObject.CreateInstance<VoidGameEvent>();
            var stats      = MakeStats(physical: 80f);
            var (go, ctrl) = MakeCtrl();
            var panelGO    = new GameObject("Panel");
            panelGO.SetActive(false);

            SetField(ctrl, "_onMatchEnded",    matchEnded);
            SetField(ctrl, "_statistics",      stats);
            SetField(ctrl, "_statisticsPanel", panelGO);

            go.SetActive(true);
            matchEnded.Raise();

            Assert.IsTrue(panelGO.activeSelf,
                "Raising _onMatchEnded must trigger ShowResults and show the panel.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panelGO);
            Object.DestroyImmediate(stats);
            Object.DestroyImmediate(matchEnded);
        }

        [Test]
        public void OnMatchStarted_Raise_TriggersResetView()
        {
            var matchStarted = ScriptableObject.CreateInstance<VoidGameEvent>();
            var stats        = MakeStats(physical: 50f);
            var (go, ctrl)   = MakeCtrl();
            var panelGO      = new GameObject("Panel");
            panelGO.SetActive(true); // visible before reset

            SetField(ctrl, "_onMatchStarted",  matchStarted);
            SetField(ctrl, "_statistics",      stats);
            SetField(ctrl, "_statisticsPanel", panelGO);

            go.SetActive(true);

            // Manually show the panel, then fire match-started to reset.
            ctrl.ShowResults();
            Assert.IsTrue(panelGO.activeSelf, "Pre-condition: panel should be visible.");

            matchStarted.Raise();

            Assert.IsFalse(panelGO.activeSelf,
                "Raising _onMatchStarted must trigger ResetView and hide the panel.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panelGO);
            Object.DestroyImmediate(stats);
            Object.DestroyImmediate(matchStarted);
        }
    }
}
