using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureFrameTests
    {
        private static ZoneControlCaptureFrameSO CreateSO(
            int framesNeeded     = 7,
            int corruptPerBot    = 2,
            int bonusPerTransmit = 1915)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureFrameSO>();
            typeof(ZoneControlCaptureFrameSO)
                .GetField("_framesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, framesNeeded);
            typeof(ZoneControlCaptureFrameSO)
                .GetField("_corruptPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, corruptPerBot);
            typeof(ZoneControlCaptureFrameSO)
                .GetField("_bonusPerTransmit", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerTransmit);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureFrameController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureFrameController>();
        }

        [Test]
        public void SO_FreshInstance_Frames_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Frames, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_TransmitCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.TransmitCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesFrames()
        {
            var so = CreateSO(framesNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.Frames, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(framesNeeded: 3, bonusPerTransmit: 1915);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,             Is.EqualTo(1915));
            Assert.That(so.TransmitCount,  Is.EqualTo(1));
            Assert.That(so.Frames,         Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(framesNeeded: 7);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesFrames()
        {
            var so = CreateSO(framesNeeded: 7, corruptPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Frames, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(framesNeeded: 7, corruptPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Frames, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FrameProgress_Clamped()
        {
            var so = CreateSO(framesNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.FrameProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnFrameTransmitted_FiresEvent()
        {
            var so    = CreateSO(framesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureFrameSO)
                .GetField("_onFrameTransmitted", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(framesNeeded: 2, bonusPerTransmit: 1915);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Frames,           Is.EqualTo(0));
            Assert.That(so.TransmitCount,    Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleTransmits_Accumulate()
        {
            var so = CreateSO(framesNeeded: 2, bonusPerTransmit: 1915);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.TransmitCount,    Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(3830));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_FrameSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.FrameSO, Is.Null);
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
            typeof(ZoneControlCaptureFrameController)
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
