using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCapturePacketTests
    {
        private static ZoneControlCapturePacketSO CreateSO(
            int payloadsNeeded   = 6,
            int fragmentPerBot   = 2,
            int bonusPerDelivery = 1885)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCapturePacketSO>();
            typeof(ZoneControlCapturePacketSO)
                .GetField("_payloadsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, payloadsNeeded);
            typeof(ZoneControlCapturePacketSO)
                .GetField("_fragmentPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, fragmentPerBot);
            typeof(ZoneControlCapturePacketSO)
                .GetField("_bonusPerDelivery", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerDelivery);
            so.Reset();
            return so;
        }

        private static ZoneControlCapturePacketController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCapturePacketController>();
        }

        [Test]
        public void SO_FreshInstance_Payloads_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Payloads, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_DeliveryCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.DeliveryCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesPayloads()
        {
            var so = CreateSO(payloadsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Payloads, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(payloadsNeeded: 3, bonusPerDelivery: 1885);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,              Is.EqualTo(1885));
            Assert.That(so.DeliveryCount,   Is.EqualTo(1));
            Assert.That(so.Payloads,        Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(payloadsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesPayloads()
        {
            var so = CreateSO(payloadsNeeded: 6, fragmentPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Payloads, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(payloadsNeeded: 6, fragmentPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Payloads, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_PayloadProgress_Clamped()
        {
            var so = CreateSO(payloadsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.PayloadProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnPacketDelivered_FiresEvent()
        {
            var so    = CreateSO(payloadsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCapturePacketSO)
                .GetField("_onPacketDelivered", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(payloadsNeeded: 2, bonusPerDelivery: 1885);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Payloads,          Is.EqualTo(0));
            Assert.That(so.DeliveryCount,      Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleDeliveries_Accumulate()
        {
            var so = CreateSO(payloadsNeeded: 2, bonusPerDelivery: 1885);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.DeliveryCount,     Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(3770));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_PacketSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.PacketSO, Is.Null);
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
            typeof(ZoneControlCapturePacketController)
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
