using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureQueueTests
    {
        private static ZoneControlCaptureQueueSO CreateSO(
            int messagesNeeded   = 5,
            int dropPerBot       = 1,
            int bonusPerDispatch = 1975)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureQueueSO>();
            typeof(ZoneControlCaptureQueueSO)
                .GetField("_messagesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, messagesNeeded);
            typeof(ZoneControlCaptureQueueSO)
                .GetField("_dropPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, dropPerBot);
            typeof(ZoneControlCaptureQueueSO)
                .GetField("_bonusPerDispatch", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerDispatch);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureQueueController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureQueueController>();
        }

        [Test]
        public void SO_FreshInstance_Messages_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Messages, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_DispatchCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.DispatchCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesMessages()
        {
            var so = CreateSO(messagesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Messages, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(messagesNeeded: 3, bonusPerDispatch: 1975);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,             Is.EqualTo(1975));
            Assert.That(so.DispatchCount,  Is.EqualTo(1));
            Assert.That(so.Messages,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(messagesNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesMessages()
        {
            var so = CreateSO(messagesNeeded: 5, dropPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Messages, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(messagesNeeded: 5, dropPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Messages, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MessageProgress_Clamped()
        {
            var so = CreateSO(messagesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.MessageProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnQueueDispatched_FiresEvent()
        {
            var so    = CreateSO(messagesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureQueueSO)
                .GetField("_onQueueDispatched", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(messagesNeeded: 2, bonusPerDispatch: 1975);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Messages,          Is.EqualTo(0));
            Assert.That(so.DispatchCount,     Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleDispatches_Accumulate()
        {
            var so = CreateSO(messagesNeeded: 2, bonusPerDispatch: 1975);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.DispatchCount,     Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(3950));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_QueueSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.QueueSO, Is.Null);
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
            typeof(ZoneControlCaptureQueueController)
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
