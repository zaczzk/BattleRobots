using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCapturePushoutTests
    {
        private static ZoneControlCapturePushoutSO CreateSO(
            int arrowsNeeded   = 6,
            int retractPerBot  = 2,
            int bonusPerPushout = 2740)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCapturePushoutSO>();
            typeof(ZoneControlCapturePushoutSO)
                .GetField("_arrowsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, arrowsNeeded);
            typeof(ZoneControlCapturePushoutSO)
                .GetField("_retractPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, retractPerBot);
            typeof(ZoneControlCapturePushoutSO)
                .GetField("_bonusPerPushout", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerPushout);
            so.Reset();
            return so;
        }

        private static ZoneControlCapturePushoutController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCapturePushoutController>();
        }

        [Test]
        public void SO_FreshInstance_Arrows_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Arrows, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_PushoutCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.PushoutCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesArrows()
        {
            var so = CreateSO(arrowsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Arrows, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(arrowsNeeded: 3, bonusPerPushout: 2740);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,           Is.EqualTo(2740));
            Assert.That(so.PushoutCount, Is.EqualTo(1));
            Assert.That(so.Arrows,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(arrowsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesArrows()
        {
            var so = CreateSO(arrowsNeeded: 6, retractPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Arrows, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(arrowsNeeded: 6, retractPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Arrows, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ArrowProgress_Clamped()
        {
            var so = CreateSO(arrowsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.ArrowProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnPushoutPushed_FiresEvent()
        {
            var so    = CreateSO(arrowsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCapturePushoutSO)
                .GetField("_onPushoutPushed", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(arrowsNeeded: 2, bonusPerPushout: 2740);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Arrows,            Is.EqualTo(0));
            Assert.That(so.PushoutCount,      Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultiplePushouts_Accumulate()
        {
            var so = CreateSO(arrowsNeeded: 2, bonusPerPushout: 2740);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.PushoutCount,      Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(5480));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_PushoutSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.PushoutSO, Is.Null);
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
            typeof(ZoneControlCapturePushoutController)
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
