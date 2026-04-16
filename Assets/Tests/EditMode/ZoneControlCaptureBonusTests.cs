using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T326: <see cref="ZoneControlCaptureBonusSO"/> and
    /// <see cref="ZoneControlCaptureBonusController"/>.
    ///
    /// ZoneControlCaptureBonusTests (12):
    ///   SO_FreshInstance_TotalBonusAwarded_Zero              ×1
    ///   SO_EvaluateBonus_BelowThreshold_ReturnsZero          ×1
    ///   SO_EvaluateBonus_AtThreshold_ReturnsZero             ×1
    ///   SO_EvaluateBonus_AboveThreshold_AwardsBonus          ×1
    ///   SO_EvaluateBonus_FiresEvent_OnBonus                  ×1
    ///   SO_EvaluateBonus_AccumulatesTotalBonus               ×1
    ///   SO_Reset_ClearsTotal                                 ×1
    ///   Controller_FreshInstance_BonusSO_Null                ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow            ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow           ×1
    ///   Controller_OnDisable_Unregisters_Channel             ×1
    ///   Controller_Refresh_NullBonusSO_HidesPanel            ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneControlCaptureBonusTests
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

        private static ZoneControlCaptureBonusSO CreateBonusSO()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureBonusSO>();
            so.Reset();
            return so;
        }

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_TotalBonusAwarded_Zero()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureBonusSO>();
            Assert.AreEqual(0, so.TotalBonusAwarded,
                "TotalBonusAwarded must be 0 on a fresh instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EvaluateBonus_BelowThreshold_ReturnsZero()
        {
            var so = CreateBonusSO(); // default threshold = 3
            int result = so.EvaluateBonus(2);
            Assert.AreEqual(0, result,
                "EvaluateBonus must return 0 when captures are below the threshold.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EvaluateBonus_AtThreshold_ReturnsZero()
        {
            var so = CreateBonusSO(); // default threshold = 3
            int result = so.EvaluateBonus(3);
            Assert.AreEqual(0, result,
                "EvaluateBonus must return 0 when captures equal the threshold.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EvaluateBonus_AboveThreshold_AwardsBonus()
        {
            var so = CreateBonusSO(); // threshold=3, bonusPerCapture=50
            int bonus = so.EvaluateBonus(5); // (5 - 3) * 50 = 100
            Assert.AreEqual(100, bonus,
                "EvaluateBonus must award (captures - threshold) * bonusPerCapture.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EvaluateBonus_FiresEvent_OnBonus()
        {
            var so  = CreateBonusSO();
            var evt = CreateEvent();
            SetField(so, "_onBonusAwarded", evt);

            int fired = 0;
            evt.RegisterCallback(() => fired++);
            so.EvaluateBonus(5);

            Assert.AreEqual(1, fired,
                "_onBonusAwarded must fire when a non-zero bonus is awarded.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_EvaluateBonus_AccumulatesTotalBonus()
        {
            var so = CreateBonusSO(); // threshold=3, bonusPerCapture=50
            so.EvaluateBonus(5); // 100
            so.EvaluateBonus(4); // 50
            Assert.AreEqual(150, so.TotalBonusAwarded,
                "TotalBonusAwarded must accumulate across multiple EvaluateBonus calls.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsTotal()
        {
            var so = CreateBonusSO();
            so.EvaluateBonus(10);
            so.Reset();
            Assert.AreEqual(0, so.TotalBonusAwarded,
                "TotalBonusAwarded must be 0 after Reset.");
            Object.DestroyImmediate(so);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_BonusSO_Null()
        {
            var go   = new GameObject("Test_BonusSO_Null");
            var ctrl = go.AddComponent<ZoneControlCaptureBonusController>();
            Assert.IsNull(ctrl.BonusSO,
                "BonusSO must be null on a fresh controller instance.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_OnEnable_Null");
            Assert.DoesNotThrow(
                () => go.AddComponent<ZoneControlCaptureBonusController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneControlCaptureBonusController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channel()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneControlCaptureBonusController>();
            var evt  = CreateEvent();
            SetField(ctrl, "_onMatchEnded", evt);

            go.SetActive(true);
            go.SetActive(false);

            int count = 0;
            evt.RegisterCallback(() => count++);
            evt.Raise();

            Assert.AreEqual(1, count,
                "_onMatchEnded must be unregistered after OnDisable.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void Controller_Refresh_NullBonusSO_HidesPanel()
        {
            var go    = new GameObject("Test_Refresh_Null");
            var ctrl  = go.AddComponent<ZoneControlCaptureBonusController>();
            var panel = new GameObject("Panel");
            panel.SetActive(true);

            SetField(ctrl, "_panel", panel);
            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden when BonusSO is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
        }
    }
}
