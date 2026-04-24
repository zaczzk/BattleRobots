using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureOrderIdealTests
    {
        private static ZoneControlCaptureOrderIdealSO CreateSO(
            int idealsNeeded      = 7,
            int shrinkPerBot      = 2,
            int bonusPerExtension = 3355)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureOrderIdealSO>();
            typeof(ZoneControlCaptureOrderIdealSO)
                .GetField("_idealsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, idealsNeeded);
            typeof(ZoneControlCaptureOrderIdealSO)
                .GetField("_shrinkPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, shrinkPerBot);
            typeof(ZoneControlCaptureOrderIdealSO)
                .GetField("_bonusPerExtension", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerExtension);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureOrderIdealController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureOrderIdealController>();
        }

        [Test]
        public void SO_FreshInstance_Ideals_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Ideals, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ExtensionCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ExtensionCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesIdeals()
        {
            var so = CreateSO(idealsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.Ideals, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(idealsNeeded: 3, bonusPerExtension: 3355);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,              Is.EqualTo(3355));
            Assert.That(so.ExtensionCount, Is.EqualTo(1));
            Assert.That(so.Ideals,          Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(idealsNeeded: 7);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesIdeals()
        {
            var so = CreateSO(idealsNeeded: 7, shrinkPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Ideals, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(idealsNeeded: 7, shrinkPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Ideals, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_IdealProgress_Clamped()
        {
            var so = CreateSO(idealsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.IdealProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnOrderIdealExtended_FiresEvent()
        {
            var so    = CreateSO(idealsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureOrderIdealSO)
                .GetField("_onOrderIdealExtended", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(idealsNeeded: 2, bonusPerExtension: 3355);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Ideals,            Is.EqualTo(0));
            Assert.That(so.ExtensionCount,    Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleExtensions_Accumulate()
        {
            var so = CreateSO(idealsNeeded: 2, bonusPerExtension: 3355);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ExtensionCount,    Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(6710));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_OrderIdealSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.OrderIdealSO, Is.Null);
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
            typeof(ZoneControlCaptureOrderIdealController)
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
