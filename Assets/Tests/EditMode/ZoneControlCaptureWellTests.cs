using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureWellTests
    {
        private static ZoneControlCaptureWellSO CreateSO(
            int bucketsNeeded = 5,
            int drainPerBot   = 1,
            int bonusPerDraw  = 495)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureWellSO>();
            typeof(ZoneControlCaptureWellSO)
                .GetField("_bucketsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bucketsNeeded);
            typeof(ZoneControlCaptureWellSO)
                .GetField("_drainPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, drainPerBot);
            typeof(ZoneControlCaptureWellSO)
                .GetField("_bonusPerDraw", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerDraw);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureWellController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureWellController>();
        }

        [Test]
        public void SO_FreshInstance_Buckets_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Buckets, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_DrawCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.DrawCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesBuckets()
        {
            var so = CreateSO(bucketsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Buckets, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_DrawsAtThreshold()
        {
            var so    = CreateSO(bucketsNeeded: 3, bonusPerDraw: 495);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,        Is.EqualTo(495));
            Assert.That(so.DrawCount, Is.EqualTo(1));
            Assert.That(so.Buckets,   Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroBeforeDraw()
        {
            var so    = CreateSO(bucketsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_DrainsBuckets()
        {
            var so = CreateSO(bucketsNeeded: 5, drainPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Buckets, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(bucketsNeeded: 5, drainPerBot: 4);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Buckets, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_BucketProgress_Clamped()
        {
            var so = CreateSO(bucketsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.BucketProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnWellDrawn_FiresEvent()
        {
            var so    = CreateSO(bucketsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureWellSO)
                .GetField("_onWellDrawn", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(bucketsNeeded: 2, bonusPerDraw: 495);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Buckets,           Is.EqualTo(0));
            Assert.That(so.DrawCount,         Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleDraws_Accumulate()
        {
            var so = CreateSO(bucketsNeeded: 2, bonusPerDraw: 495);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.DrawCount,         Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(990));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_WellSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.WellSO, Is.Null);
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
            typeof(ZoneControlCaptureWellController)
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
