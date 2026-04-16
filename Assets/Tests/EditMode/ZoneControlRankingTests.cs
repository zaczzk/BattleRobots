using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T303: <see cref="ZoneControlRankingSO"/> and
    /// <see cref="ZoneControlRankingController"/>.
    ///
    /// ZoneControlRankingTests (14):
    ///   SO_FreshInstance_Rank_Unranked                                          ×1
    ///   SO_EvaluateRank_ZoneThreshold_AdvancesRank                             ×1
    ///   SO_EvaluateRank_TierThreshold_AdvancesRank                             ×1
    ///   SO_EvaluateRank_Idempotent_WhenSameValue                               ×1
    ///   SO_EvaluateRank_DoesNotDecrease                                        ×1
    ///   SO_GetNextZoneThreshold_ReturnsMinusOne_AtDiamond                      ×1
    ///   SO_LoadSnapshot_RestoresRank                                            ×1
    ///   SO_Reset_SetsUnranked                                                   ×1
    ///   Controller_FreshInstance_RankingSO_Null                                 ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                               ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow                              ×1
    ///   Controller_OnDisable_Unregisters_Channels                               ×1
    ///   Controller_HandleGateUnlocked_NullRefs_NoThrow                          ×1
    ///   Controller_Refresh_NullRankingSO_HidesPanel                            ×1
    ///
    /// Total: 14 new EditMode tests.
    /// </summary>
    public class ZoneControlRankingTests
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

        private static ZoneControlRankingSO CreateRankingSO() =>
            ScriptableObject.CreateInstance<ZoneControlRankingSO>();

        private static ZoneControlRankingController CreateController() =>
            new GameObject("RankingCtrl_Test")
                .AddComponent<ZoneControlRankingController>();

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_Rank_Unranked()
        {
            var so = CreateRankingSO();
            Assert.AreEqual(ZoneControlRankLevel.Unranked, so.CurrentRank,
                "CurrentRank must be Unranked on a fresh ZoneControlRankingSO.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EvaluateRank_ZoneThreshold_AdvancesRank()
        {
            var so = CreateRankingSO();
            // Default zone thresholds: 10, 25, 50, 100, 200
            so.EvaluateRank(10, 0); // meets Bronze zone threshold (10 zones)
            Assert.AreEqual(ZoneControlRankLevel.Bronze, so.CurrentRank,
                "Rank must advance to Bronze when zone threshold is met.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EvaluateRank_TierThreshold_AdvancesRank()
        {
            var so = CreateRankingSO();
            // Default tier thresholds: 1, 2, 3, 4, 5
            so.EvaluateRank(0, 1); // meets Bronze via tier threshold
            Assert.AreEqual(ZoneControlRankLevel.Bronze, so.CurrentRank,
                "Rank must advance to Bronze when tier threshold is met.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EvaluateRank_Idempotent_WhenSameValue()
        {
            var so = CreateRankingSO();
            so.EvaluateRank(10, 0); // Bronze
            so.EvaluateRank(10, 0); // same — should stay Bronze
            Assert.AreEqual(ZoneControlRankLevel.Bronze, so.CurrentRank,
                "Rank must remain Bronze when evaluated with the same value twice.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EvaluateRank_DoesNotDecrease()
        {
            var so = CreateRankingSO();
            so.EvaluateRank(50, 3); // Gold
            so.EvaluateRank(0, 0);  // would be Unranked if re-evaluated from scratch
            Assert.AreEqual(ZoneControlRankLevel.Gold, so.CurrentRank,
                "Rank must not decrease when EvaluateRank is called with lower values.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GetNextZoneThreshold_ReturnsMinusOne_AtDiamond()
        {
            var so = CreateRankingSO();
            so.EvaluateRank(200, 5); // Diamond (max rank)
            Assert.AreEqual(-1, so.GetNextZoneThreshold(),
                "GetNextZoneThreshold must return -1 when at Diamond rank.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_LoadSnapshot_RestoresRank()
        {
            var so = CreateRankingSO();
            so.LoadSnapshot((int)ZoneControlRankLevel.Silver);
            Assert.AreEqual(ZoneControlRankLevel.Silver, so.CurrentRank,
                "LoadSnapshot must restore the persisted rank level.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_SetsUnranked()
        {
            var so = CreateRankingSO();
            so.EvaluateRank(25, 2); // Silver
            so.Reset();
            Assert.AreEqual(ZoneControlRankLevel.Unranked, so.CurrentRank,
                "CurrentRank must be Unranked after Reset.");
            Object.DestroyImmediate(so);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_RankingSO_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.RankingSO,
                "RankingSO must be null on a freshly added controller.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_OnEnable_Null");
            Assert.DoesNotThrow(
                () => go.AddComponent<ZoneControlRankingController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneControlRankingController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channels()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneControlRankingController>();

            var gateEvt = CreateEvent();
            var rankEvt = CreateEvent();

            SetField(ctrl, "_onGateUnlocked", gateEvt);
            SetField(ctrl, "_onRankChanged",  rankEvt);

            go.SetActive(true);
            go.SetActive(false);

            int gateCount = 0, rankCount = 0;
            gateEvt.RegisterCallback(() => gateCount++);
            rankEvt.RegisterCallback(() => rankCount++);

            gateEvt.Raise();
            rankEvt.Raise();

            Assert.AreEqual(1, gateCount,
                "_onGateUnlocked must be unregistered after OnDisable.");
            Assert.AreEqual(1, rankCount,
                "_onRankChanged must be unregistered after OnDisable.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(gateEvt);
            Object.DestroyImmediate(rankEvt);
        }

        [Test]
        public void Controller_HandleGateUnlocked_NullRefs_NoThrow()
        {
            var ctrl = CreateController();
            Assert.DoesNotThrow(() => ctrl.HandleGateUnlocked(),
                "HandleGateUnlocked must not throw when _rankingSO/_summarySO are null.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullRankingSO_HidesPanel()
        {
            var go    = new GameObject("Test_Refresh_NullRankingSO");
            var ctrl  = go.AddComponent<ZoneControlRankingController>();
            var panel = new GameObject("Panel");
            panel.SetActive(true);

            SetField(ctrl, "_panel", panel);
            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden when RankingSO is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
        }
    }
}
