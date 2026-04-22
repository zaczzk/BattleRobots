using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureStackTests
    {
        private static ZoneControlCaptureStackSO CreateSO(
            int framesNeeded   = 5,
            int popPerBot      = 1,
            int bonusPerReturn = 1945)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureStackSO>();
            typeof(ZoneControlCaptureStackSO)
                .GetField("_framesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, framesNeeded);
            typeof(ZoneControlCaptureStackSO)
                .GetField("_popPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, popPerBot);
            typeof(ZoneControlCaptureStackSO)
                .GetField("_bonusPerReturn", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerReturn);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureStackController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureStackController>();
        }

        [Test]
        public void SO_FreshInstance_Frames_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Frames, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ReturnCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ReturnCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesFrames()
        {
            var so = CreateSO(framesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Frames, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(framesNeeded: 3, bonusPerReturn: 1945);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,           Is.EqualTo(1945));
            Assert.That(so.ReturnCount,  Is.EqualTo(1));
            Assert.That(so.Frames,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(framesNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesFrames()
        {
            var so = CreateSO(framesNeeded: 5, popPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Frames, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(framesNeeded: 5, popPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Frames, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FrameProgress_Clamped()
        {
            var so = CreateSO(framesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.FrameProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnStackReturned_FiresEvent()
        {
            var so    = CreateSO(framesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureStackSO)
                .GetField("_onStackReturned", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(framesNeeded: 2, bonusPerReturn: 1945);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Frames,            Is.EqualTo(0));
            Assert.That(so.ReturnCount,       Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleReturns_Accumulate()
        {
            var so = CreateSO(framesNeeded: 2, bonusPerReturn: 1945);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ReturnCount,       Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(3890));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_StackSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.StackSO, Is.Null);
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
            typeof(ZoneControlCaptureStackController)
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
