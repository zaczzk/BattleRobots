using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureSpireTests
    {
        private static ZoneControlCaptureSpireSO CreateSO(
            int channelsNeeded  = 4,
            int disruptionPerBot = 1,
            int bonusPerChannel  = 440)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureSpireSO>();
            typeof(ZoneControlCaptureSpireSO)
                .GetField("_channelsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channelsNeeded);
            typeof(ZoneControlCaptureSpireSO)
                .GetField("_disruptionPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, disruptionPerBot);
            typeof(ZoneControlCaptureSpireSO)
                .GetField("_bonusPerChannel", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerChannel);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureSpireController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureSpireController>();
        }

        [Test]
        public void SO_FreshInstance_Energy_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Energy, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ChannelCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ChannelCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesEnergy()
        {
            var so = CreateSO(channelsNeeded: 4);
            so.RecordPlayerCapture();
            Assert.That(so.Energy, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ChannelsAtThreshold()
        {
            var so    = CreateSO(channelsNeeded: 3, bonusPerChannel: 440);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,          Is.EqualTo(440));
            Assert.That(so.ChannelCount, Is.EqualTo(1));
            Assert.That(so.Energy,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileChanneling()
        {
            var so    = CreateSO(channelsNeeded: 4);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_DisruptsEnergy()
        {
            var so = CreateSO(channelsNeeded: 4, disruptionPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Energy, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(channelsNeeded: 4, disruptionPerBot: 3);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Energy, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EnergyProgress_Clamped()
        {
            var so = CreateSO(channelsNeeded: 4);
            so.RecordPlayerCapture();
            Assert.That(so.EnergyProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnSpireChanneled_FiresEvent()
        {
            var so    = CreateSO(channelsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureSpireSO)
                .GetField("_onSpireChanneled", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(channelsNeeded: 2, bonusPerChannel: 440);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Energy,            Is.EqualTo(0));
            Assert.That(so.ChannelCount,      Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleChannels_Accumulate()
        {
            var so = CreateSO(channelsNeeded: 2, bonusPerChannel: 440);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ChannelCount,      Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(880));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_SpireSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.SpireSO, Is.Null);
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
            typeof(ZoneControlCaptureSpireController)
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
