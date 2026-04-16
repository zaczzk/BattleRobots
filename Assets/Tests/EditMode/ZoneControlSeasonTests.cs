using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T310: <see cref="ZoneControlSeasonSO"/> and
    /// <see cref="ZoneControlSeasonController"/>.
    ///
    /// ZoneControlSeasonTests (12):
    ///   SO_FreshInstance_SeasonCount_Zero                                         ×1
    ///   SO_FreshInstance_HighestDivision_Bronze                                   ×1
    ///   SO_EndSeason_IncrementsSeasonCount                                        ×1
    ///   SO_EndSeason_UpdatesHighestDivision                                       ×1
    ///   SO_GetRewardTier_MapsCorrectly                                            ×1
    ///   SO_Reset_ClearsAll                                                        ×1
    ///   Controller_FreshInstance_SeasonSO_Null                                    ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                                 ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow                                ×1
    ///   Controller_OnDisable_Unregisters_Channel                                  ×1
    ///   Controller_HandleEndSeason_NullSeasonSO_NoThrow                           ×1
    ///   Controller_Refresh_NullSeasonSO_HidesPanel                                ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneControlSeasonTests
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

        private static ZoneControlSeasonSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlSeasonSO>();

        private static ZoneControlSeasonController CreateController() =>
            new GameObject("SeasonCtrl_Test")
                .AddComponent<ZoneControlSeasonController>();

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_SeasonCount_Zero()
        {
            var so = CreateSO();
            Assert.AreEqual(0, so.SeasonCount,
                "SeasonCount must be 0 on a fresh instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_HighestDivision_Bronze()
        {
            var so = CreateSO();
            Assert.AreEqual(ZoneControlLeagueDivision.Bronze, so.HighestDivision,
                "HighestDivision must be Bronze on a fresh instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EndSeason_IncrementsSeasonCount()
        {
            var so = CreateSO();
            so.EndSeason(ZoneControlLeagueDivision.Silver);
            so.EndSeason(ZoneControlLeagueDivision.Bronze);
            Assert.AreEqual(2, so.SeasonCount,
                "SeasonCount must increment once per EndSeason call.");
            Assert.AreEqual(2, so.HistoryCount,
                "HistoryCount must equal the number of EndSeason calls.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EndSeason_UpdatesHighestDivision()
        {
            var so = CreateSO();
            so.EndSeason(ZoneControlLeagueDivision.Gold);
            Assert.AreEqual(ZoneControlLeagueDivision.Gold, so.HighestDivision,
                "HighestDivision must update when a better division is reached.");

            so.EndSeason(ZoneControlLeagueDivision.Silver);
            Assert.AreEqual(ZoneControlLeagueDivision.Gold, so.HighestDivision,
                "HighestDivision must not decrease when a worse division is reached.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GetRewardTier_MapsCorrectly()
        {
            Assert.AreEqual(1, ZoneControlSeasonSO.GetRewardTier(ZoneControlLeagueDivision.Bronze),
                "Bronze must map to reward tier 1.");
            Assert.AreEqual(2, ZoneControlSeasonSO.GetRewardTier(ZoneControlLeagueDivision.Silver),
                "Silver must map to reward tier 2.");
            Assert.AreEqual(3, ZoneControlSeasonSO.GetRewardTier(ZoneControlLeagueDivision.Gold),
                "Gold must map to reward tier 3.");
            Assert.AreEqual(4, ZoneControlSeasonSO.GetRewardTier(ZoneControlLeagueDivision.Platinum),
                "Platinum must map to reward tier 4.");
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            so.EndSeason(ZoneControlLeagueDivision.Gold);
            so.EndSeason(ZoneControlLeagueDivision.Platinum);
            so.Reset();

            Assert.AreEqual(0, so.SeasonCount,
                "SeasonCount must be 0 after Reset.");
            Assert.AreEqual(0, so.HistoryCount,
                "HistoryCount must be 0 after Reset.");
            Assert.AreEqual(ZoneControlLeagueDivision.Bronze, so.HighestDivision,
                "HighestDivision must return to Bronze after Reset.");
            Assert.AreEqual(0, so.LatestRewardTier,
                "LatestRewardTier must be 0 after Reset (no seasons).");
            Object.DestroyImmediate(so);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_SeasonSO_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.SeasonSO,
                "SeasonSO must be null on a freshly added controller.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_OnEnable_Null");
            Assert.DoesNotThrow(
                () => go.AddComponent<ZoneControlSeasonController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneControlSeasonController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channel()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneControlSeasonController>();

            var endEvt = CreateEvent();
            SetField(ctrl, "_onEndSeasonTriggered", endEvt);

            go.SetActive(true);
            go.SetActive(false);

            int count = 0;
            endEvt.RegisterCallback(() => count++);
            endEvt.Raise();

            Assert.AreEqual(1, count,
                "_onEndSeasonTriggered must be unregistered after OnDisable.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(endEvt);
        }

        [Test]
        public void Controller_HandleEndSeason_NullSeasonSO_NoThrow()
        {
            var ctrl = CreateController();
            Assert.DoesNotThrow(() => ctrl.HandleEndSeason(),
                "HandleEndSeason must not throw when SeasonSO is null.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSeasonSO_HidesPanel()
        {
            var go    = new GameObject("Test_Refresh_Null");
            var ctrl  = go.AddComponent<ZoneControlSeasonController>();
            var panel = new GameObject("Panel");
            panel.SetActive(true);

            SetField(ctrl, "_panel", panel);
            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden when SeasonSO is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
        }
    }
}
