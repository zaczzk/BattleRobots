using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureExponentialTests
    {
        private static ZoneControlCaptureExponentialSO CreateSO(
            int basesNeeded     = 5,
            int reducePerBot    = 1,
            int bonusPerExponent = 2905)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureExponentialSO>();
            typeof(ZoneControlCaptureExponentialSO)
                .GetField("_basesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, basesNeeded);
            typeof(ZoneControlCaptureExponentialSO)
                .GetField("_reducePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, reducePerBot);
            typeof(ZoneControlCaptureExponentialSO)
                .GetField("_bonusPerExponent", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerExponent);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureExponentialController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureExponentialController>();
        }

        [Test]
        public void SO_FreshInstance_Bases_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Bases, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ExponentCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ExponentCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesBases()
        {
            var so = CreateSO(basesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Bases, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(basesNeeded: 3, bonusPerExponent: 2905);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,            Is.EqualTo(2905));
            Assert.That(so.ExponentCount, Is.EqualTo(1));
            Assert.That(so.Bases,         Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(basesNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesBases()
        {
            var so = CreateSO(basesNeeded: 5, reducePerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Bases, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(basesNeeded: 5, reducePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Bases, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_BaseProgress_Clamped()
        {
            var so = CreateSO(basesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.BaseProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnExponentRaised_FiresEvent()
        {
            var so    = CreateSO(basesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureExponentialSO)
                .GetField("_onExponentRaised", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(basesNeeded: 2, bonusPerExponent: 2905);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Bases,             Is.EqualTo(0));
            Assert.That(so.ExponentCount,     Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleExponents_Accumulate()
        {
            var so = CreateSO(basesNeeded: 2, bonusPerExponent: 2905);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ExponentCount,     Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(5810));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ExponentialSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ExponentialSO, Is.Null);
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
            typeof(ZoneControlCaptureExponentialController)
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
