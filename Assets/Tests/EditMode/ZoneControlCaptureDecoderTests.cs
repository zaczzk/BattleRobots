using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureDecoderTests
    {
        private static ZoneControlCaptureDecoderSO CreateSO(
            int packetsNeeded  = 7,
            int dropPerBot     = 2,
            int bonusPerDecode = 1675)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureDecoderSO>();
            typeof(ZoneControlCaptureDecoderSO)
                .GetField("_packetsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, packetsNeeded);
            typeof(ZoneControlCaptureDecoderSO)
                .GetField("_dropPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, dropPerBot);
            typeof(ZoneControlCaptureDecoderSO)
                .GetField("_bonusPerDecode", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerDecode);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureDecoderController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureDecoderController>();
        }

        [Test]
        public void SO_FreshInstance_Packets_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Packets, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_DecodeCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.DecodeCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesPackets()
        {
            var so = CreateSO(packetsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.Packets, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(packetsNeeded: 3, bonusPerDecode: 1675);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,           Is.EqualTo(1675));
            Assert.That(so.DecodeCount, Is.EqualTo(1));
            Assert.That(so.Packets,     Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(packetsNeeded: 7);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesPackets()
        {
            var so = CreateSO(packetsNeeded: 7, dropPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Packets, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(packetsNeeded: 7, dropPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Packets, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_PacketProgress_Clamped()
        {
            var so = CreateSO(packetsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.PacketProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnDecoderDecoded_FiresEvent()
        {
            var so    = CreateSO(packetsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureDecoderSO)
                .GetField("_onDecoderDecoded", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(packetsNeeded: 2, bonusPerDecode: 1675);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Packets,           Is.EqualTo(0));
            Assert.That(so.DecodeCount,       Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleDecodes_Accumulate()
        {
            var so = CreateSO(packetsNeeded: 2, bonusPerDecode: 1675);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.DecodeCount,       Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(3350));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_DecoderSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.DecoderSO, Is.Null);
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
            typeof(ZoneControlCaptureDecoderController)
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
