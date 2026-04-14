using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T200:
    ///   <see cref="MatchRatingController"/>.
    ///
    /// MatchRatingControllerTests (14):
    ///   FreshInstance_MatchResultIsNull                          ×1
    ///   FreshInstance_PersonalBestIsNull                         ×1
    ///   FreshInstance_DefaultEfficiencyThreshold_IsPointFive     ×1
    ///   OnEnable_AllNullRefs_DoesNotThrow                        ×1
    ///   OnDisable_AllNullRefs_DoesNotThrow                       ×1
    ///   OnDisable_Unregisters                                    ×1
    ///   ComputeStars_NullMatchResult_ReturnsOne                  ×1
    ///   ComputeStars_Loss_LowEfficiency_ReturnsOne               ×1
    ///   ComputeStars_Win_LowEfficiency_ReturnsTwo                ×1
    ///   ComputeStars_Loss_GoodEfficiency_ReturnsTwo              ×1
    ///   ComputeStars_Win_GoodEfficiency_ReturnsThree             ×1
    ///   ComputeStars_Loss_GoodEfficiency_NewBest_ReturnsThree    ×1
    ///   ComputeStars_Win_LowEfficiency_NewBest_ReturnsFour       ×1
    ///   ComputeStars_Win_GoodEfficiency_NewBest_ReturnsFive      ×1
    ///
    /// Total: 14 new EditMode tests.
    /// </summary>
    public class MatchRatingControllerTests
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

        private static MatchResultSO CreateResult(bool won, float done, float taken)
        {
            var so = ScriptableObject.CreateInstance<MatchResultSO>();
            so.Write(playerWon: won, durationSeconds: 60f,
                     currencyEarned: 100, newWalletBalance: 100,
                     damageDone: done, damageTaken: taken);
            return so;
        }

        private static PersonalBestSO CreatePersonalBest(bool isNewBest)
        {
            var so = ScriptableObject.CreateInstance<PersonalBestSO>();
            // Pre-seed a best score so Submit can produce IsNewBest = true.
            if (!isNewBest)
            {
                so.Submit(1000); // set best = 1000
                so.Submit(500);  // current = 500, not a new best
            }
            else
            {
                so.Submit(500);  // set best = 500
                so.Submit(1000); // current = 1000 > best → IsNewBest = true
            }
            return so;
        }

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static MatchRatingController CreateController()
        {
            var go = new GameObject("MatchRatingCtrl_Test");
            return go.AddComponent<MatchRatingController>();
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void Ctrl_FreshInstance_MatchResultIsNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.MatchResult);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_FreshInstance_PersonalBestIsNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.PersonalBest);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_FreshInstance_DefaultEfficiencyThreshold_IsPointFive()
        {
            var ctrl = CreateController();
            Assert.AreEqual(0.5f, ctrl.EfficiencyThreshold, 0.001f);
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
        public void Ctrl_OnDisable_Unregisters()
        {
            var ctrl = CreateController();
            var ch   = CreateEvent();
            SetField(ctrl, "_onMatchEnded", ch);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            int callCount = 0;
            ch.RegisterCallback(() => callCount++);
            InvokePrivate(ctrl, "OnDisable");
            ch.Raise();

            Assert.AreEqual(1, callCount,
                "After OnDisable only the manually registered callback should fire.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void ComputeStars_NullMatchResult_ReturnsOne()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");

            Assert.AreEqual(1, ctrl.ComputeStars(),
                "Null MatchResultSO must return the baseline 1-star rating.");

            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void ComputeStars_Loss_LowEfficiency_ReturnsOne()
        {
            var ctrl   = CreateController();
            var result = CreateResult(won: false, done: 10f, taken: 200f); // low efficiency

            SetField(ctrl, "_matchResult", result);
            InvokePrivate(ctrl, "Awake");

            Assert.AreEqual(1, ctrl.ComputeStars(),
                "Loss with low efficiency and no personal best must return 1 star.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void ComputeStars_Win_LowEfficiency_ReturnsTwo()
        {
            var ctrl   = CreateController();
            var result = CreateResult(won: true, done: 10f, taken: 200f); // efficiency ~ 0.05

            SetField(ctrl, "_matchResult", result);
            InvokePrivate(ctrl, "Awake");

            Assert.AreEqual(2, ctrl.ComputeStars(),
                "Win with low efficiency and no personal best must return 2 stars.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void ComputeStars_Loss_GoodEfficiency_ReturnsTwo()
        {
            var ctrl   = CreateController();
            // efficiency = 60 / (60 + 40) = 0.6 >= 0.5
            var result = CreateResult(won: false, done: 60f, taken: 40f);

            SetField(ctrl, "_matchResult", result);
            InvokePrivate(ctrl, "Awake");

            Assert.AreEqual(2, ctrl.ComputeStars(),
                "Loss with good efficiency and no personal best must return 2 stars.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void ComputeStars_Win_GoodEfficiency_ReturnsThree()
        {
            var ctrl   = CreateController();
            // efficiency = 70 / 100 = 0.7 >= 0.5
            var result = CreateResult(won: true, done: 70f, taken: 30f);

            SetField(ctrl, "_matchResult", result);
            InvokePrivate(ctrl, "Awake");

            Assert.AreEqual(3, ctrl.ComputeStars(),
                "Win with good efficiency and no personal best must return 3 stars.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void ComputeStars_Loss_GoodEfficiency_NewBest_ReturnsThree()
        {
            var ctrl   = CreateController();
            var result = CreateResult(won: false, done: 60f, taken: 40f); // efficiency 0.6
            var pb     = CreatePersonalBest(isNewBest: true);

            SetField(ctrl, "_matchResult",  result);
            SetField(ctrl, "_personalBest", pb);
            InvokePrivate(ctrl, "Awake");

            // 1 (base) + 0 (loss) + 1 (efficiency) + 1 (PB) + 0 (no win+PB) = 3
            Assert.AreEqual(3, ctrl.ComputeStars(),
                "Loss with good efficiency and new personal best must return 3 stars.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(result);
            Object.DestroyImmediate(pb);
        }

        [Test]
        public void ComputeStars_Win_LowEfficiency_NewBest_ReturnsFour()
        {
            var ctrl   = CreateController();
            var result = CreateResult(won: true, done: 10f, taken: 200f); // low efficiency
            var pb     = CreatePersonalBest(isNewBest: true);

            SetField(ctrl, "_matchResult",  result);
            SetField(ctrl, "_personalBest", pb);
            InvokePrivate(ctrl, "Awake");

            // 1 (base) + 1 (win) + 0 (low efficiency) + 1 (PB) + 1 (win+PB) = 4
            Assert.AreEqual(4, ctrl.ComputeStars(),
                "Win with low efficiency and new personal best must return 4 stars.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(result);
            Object.DestroyImmediate(pb);
        }

        [Test]
        public void ComputeStars_Win_GoodEfficiency_NewBest_ReturnsFive()
        {
            var ctrl   = CreateController();
            var result = CreateResult(won: true, done: 70f, taken: 30f); // efficiency 0.7
            var pb     = CreatePersonalBest(isNewBest: true);

            SetField(ctrl, "_matchResult",  result);
            SetField(ctrl, "_personalBest", pb);
            InvokePrivate(ctrl, "Awake");

            // 1 (base) + 1 (win) + 1 (efficiency) + 1 (PB) + 1 (win+PB) = 5
            Assert.AreEqual(5, ctrl.ComputeStars(),
                "Win with good efficiency and new personal best must return 5 stars.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(result);
            Object.DestroyImmediate(pb);
        }
    }
}
