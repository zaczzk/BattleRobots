using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T309: <see cref="ZoneControlLeagueSO"/> and
    /// <see cref="ZoneControlLeagueController"/>.
    ///
    /// ZoneControlLeagueTests (12):
    ///   SO_FreshInstance_Division_Bronze                                          ×1
    ///   SO_FreshInstance_Points_Zero                                              ×1
    ///   SO_AddRatingPoints_IncrementsPoints                                       ×1
    ///   SO_EvaluatePromotion_Promotes                                             ×1
    ///   SO_EvaluatePromotion_DoesNotExceedPlatinum                                ×1
    ///   SO_Reset_ClearsAll                                                        ×1
    ///   Controller_FreshInstance_LeagueSO_Null                                    ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                                 ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow                                ×1
    ///   Controller_OnDisable_Unregisters_Channels                                 ×1
    ///   Controller_HandleRatingSet_NullRefs_NoThrow                               ×1
    ///   Controller_Refresh_NullLeagueSO_HidesPanel                                ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneControlLeagueTests
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

        private static ZoneControlLeagueSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlLeagueSO>();

        private static ZoneControlLeagueController CreateController() =>
            new GameObject("LeagueCtrl_Test")
                .AddComponent<ZoneControlLeagueController>();

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_Division_Bronze()
        {
            var so = CreateSO();
            Assert.AreEqual(ZoneControlLeagueDivision.Bronze, so.CurrentDivision,
                "CurrentDivision must be Bronze on a fresh instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_Points_Zero()
        {
            var so = CreateSO();
            Assert.AreEqual(0, so.CurrentPoints,
                "CurrentPoints must be 0 on a fresh instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AddRatingPoints_IncrementsPoints()
        {
            var so = CreateSO();
            // Default threshold = 100; adding rating 3 with default 10 pts/rating = 30 pts.
            so.AddRatingPoints(3);
            Assert.AreEqual(30, so.CurrentPoints,
                "CurrentPoints must increase by rating × PointsPerRating.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EvaluatePromotion_Promotes()
        {
            var so  = CreateSO();
            var evt = CreateEvent();
            // Use a low threshold to test easily.
            SetField(so, "_promotionThreshold", 10);
            SetField(so, "_onPromotion", evt);

            int count = 0;
            evt.RegisterCallback(() => count++);

            // 1 star × 10 pts = 10 pts → reaches threshold → promote.
            so.AddRatingPoints(1);

            Assert.AreEqual(ZoneControlLeagueDivision.Silver, so.CurrentDivision,
                "Division must advance to Silver after reaching the promotion threshold.");
            Assert.AreEqual(0, so.CurrentPoints,
                "Points must reset to 0 after promotion.");
            Assert.AreEqual(1, count,
                "_onPromotion must fire exactly once.");
            Assert.AreEqual(1, so.PromotionCount,
                "PromotionCount must be 1 after first promotion.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_EvaluatePromotion_DoesNotExceedPlatinum()
        {
            var so = CreateSO();
            SetField(so, "_promotionThreshold", 10);
            // Force division to Platinum.
            so.LoadSnapshot((int)ZoneControlLeagueDivision.Platinum, 0, 4, 0);

            so.AddRatingPoints(5); // Would promote beyond Platinum.

            Assert.AreEqual(ZoneControlLeagueDivision.Platinum, so.CurrentDivision,
                "Division must not exceed Platinum.");

            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            SetField(so, "_promotionThreshold", 10);
            so.AddRatingPoints(1); // Triggers promotion to Silver.
            so.Reset();

            Assert.AreEqual(ZoneControlLeagueDivision.Bronze, so.CurrentDivision,
                "CurrentDivision must return to Bronze after Reset.");
            Assert.AreEqual(0, so.CurrentPoints,    "CurrentPoints must be 0 after Reset.");
            Assert.AreEqual(0, so.PromotionCount,   "PromotionCount must be 0 after Reset.");
            Assert.AreEqual(0, so.RelegationCount,  "RelegationCount must be 0 after Reset.");

            Object.DestroyImmediate(so);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_LeagueSO_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.LeagueSO,
                "LeagueSO must be null on a freshly added controller.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_OnEnable_Null");
            Assert.DoesNotThrow(
                () => go.AddComponent<ZoneControlLeagueController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneControlLeagueController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channels()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneControlLeagueController>();

            var ratingEvt = CreateEvent();
            SetField(ctrl, "_onRatingSet", ratingEvt);

            go.SetActive(true);
            go.SetActive(false);

            int count = 0;
            ratingEvt.RegisterCallback(() => count++);
            ratingEvt.Raise();

            Assert.AreEqual(1, count,
                "_onRatingSet must be unregistered after OnDisable.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(ratingEvt);
        }

        [Test]
        public void Controller_HandleRatingSet_NullRefs_NoThrow()
        {
            var ctrl = CreateController();
            Assert.DoesNotThrow(() => ctrl.HandleRatingSet(),
                "HandleRatingSet must not throw when all refs are null.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullLeagueSO_HidesPanel()
        {
            var go    = new GameObject("Test_Refresh_Null");
            var ctrl  = go.AddComponent<ZoneControlLeagueController>();
            var panel = new GameObject("Panel");
            panel.SetActive(true);

            SetField(ctrl, "_panel", panel);
            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden when LeagueSO is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
        }
    }
}
