using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureRotorTests
    {
        private static ZoneControlCaptureRotorSO CreateSO(
            int segmentsNeeded     = 7,
            int dragPerBot         = 2,
            int bonusPerRevolution = 1330)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureRotorSO>();
            typeof(ZoneControlCaptureRotorSO)
                .GetField("_segmentsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, segmentsNeeded);
            typeof(ZoneControlCaptureRotorSO)
                .GetField("_dragPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, dragPerBot);
            typeof(ZoneControlCaptureRotorSO)
                .GetField("_bonusPerRevolution", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerRevolution);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureRotorController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureRotorController>();
        }

        [Test]
        public void SO_FreshInstance_Segments_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Segments, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_RevolutionCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.RevolutionCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesSegments()
        {
            var so = CreateSO(segmentsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.Segments, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_SegmentsAtThreshold()
        {
            var so    = CreateSO(segmentsNeeded: 3, bonusPerRevolution: 1330);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,               Is.EqualTo(1330));
            Assert.That(so.RevolutionCount,  Is.EqualTo(1));
            Assert.That(so.Segments,         Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(segmentsNeeded: 7);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesSegments()
        {
            var so = CreateSO(segmentsNeeded: 7, dragPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Segments, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(segmentsNeeded: 7, dragPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Segments, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SegmentProgress_Clamped()
        {
            var so = CreateSO(segmentsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.SegmentProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnRotorRevolved_FiresEvent()
        {
            var so    = CreateSO(segmentsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureRotorSO)
                .GetField("_onRotorRevolved", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(segmentsNeeded: 2, bonusPerRevolution: 1330);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Segments,          Is.EqualTo(0));
            Assert.That(so.RevolutionCount,   Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleRevolutions_Accumulate()
        {
            var so = CreateSO(segmentsNeeded: 2, bonusPerRevolution: 1330);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.RevolutionCount,   Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(2660));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_RotorSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.RotorSO, Is.Null);
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
            typeof(ZoneControlCaptureRotorController)
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
