using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureArakelovTheoryTests
    {
        private static ZoneControlCaptureArakelovTheorySO CreateSO(
            int arithmeticDivisorsNeeded     = 5,
            int badReductionsPerBot           = 1,
            int bonusPerArakelovIntersection  = 4390)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureArakelovTheorySO>();
            typeof(ZoneControlCaptureArakelovTheorySO)
                .GetField("_arithmeticDivisorsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, arithmeticDivisorsNeeded);
            typeof(ZoneControlCaptureArakelovTheorySO)
                .GetField("_badReductionsPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, badReductionsPerBot);
            typeof(ZoneControlCaptureArakelovTheorySO)
                .GetField("_bonusPerArakelovIntersection", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerArakelovIntersection);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureArakelovTheoryController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureArakelovTheoryController>();
        }

        [Test]
        public void SO_FreshInstance_ArithmeticDivisors_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ArithmeticDivisors, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ArakelovIntersectionCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ArakelovIntersectionCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesArithmeticDivisors()
        {
            var so = CreateSO(arithmeticDivisorsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.ArithmeticDivisors, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(arithmeticDivisorsNeeded: 3, bonusPerArakelovIntersection: 4390);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                        Is.EqualTo(4390));
            Assert.That(so.ArakelovIntersectionCount, Is.EqualTo(1));
            Assert.That(so.ArithmeticDivisors,        Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(arithmeticDivisorsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesBadReductions()
        {
            var so = CreateSO(arithmeticDivisorsNeeded: 5, badReductionsPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.ArithmeticDivisors, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(arithmeticDivisorsNeeded: 5, badReductionsPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.ArithmeticDivisors, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ArithmeticDivisorProgress_Clamped()
        {
            var so = CreateSO(arithmeticDivisorsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.ArithmeticDivisorProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnArakelovTheoryIntersected_FiresEvent()
        {
            var so    = CreateSO(arithmeticDivisorsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureArakelovTheorySO)
                .GetField("_onArakelovTheoryIntersected", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(arithmeticDivisorsNeeded: 2, bonusPerArakelovIntersection: 4390);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.ArithmeticDivisors,        Is.EqualTo(0));
            Assert.That(so.ArakelovIntersectionCount, Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,         Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleIntersections_Accumulate()
        {
            var so = CreateSO(arithmeticDivisorsNeeded: 2, bonusPerArakelovIntersection: 4390);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ArakelovIntersectionCount, Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded,         Is.EqualTo(8780));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ArakelovTheorySO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ArakelovTheorySO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(true));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlCaptureArakelovTheoryController)
                .GetField("_panel", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, panel);
            panel.SetActive(true);
            ctrl.Refresh();
            Assert.That(panel.activeSelf, Is.False);
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }
    }
}
