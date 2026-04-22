using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureHashTests
    {
        private static ZoneControlCaptureHashSO CreateSO(
            int bucketsNeeded   = 7,
            int collisionPerBot = 2,
            int bonusPerHash    = 1990)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureHashSO>();
            typeof(ZoneControlCaptureHashSO)
                .GetField("_bucketsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bucketsNeeded);
            typeof(ZoneControlCaptureHashSO)
                .GetField("_collisionPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, collisionPerBot);
            typeof(ZoneControlCaptureHashSO)
                .GetField("_bonusPerHash", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerHash);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureHashController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureHashController>();
        }

        [Test]
        public void SO_FreshInstance_Buckets_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Buckets, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_HashCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.HashCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesBuckets()
        {
            var so = CreateSO(bucketsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.Buckets, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(bucketsNeeded: 3, bonusPerHash: 1990);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,         Is.EqualTo(1990));
            Assert.That(so.HashCount,  Is.EqualTo(1));
            Assert.That(so.Buckets,    Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(bucketsNeeded: 7);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesBuckets()
        {
            var so = CreateSO(bucketsNeeded: 7, collisionPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Buckets, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(bucketsNeeded: 7, collisionPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Buckets, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_BucketProgress_Clamped()
        {
            var so = CreateSO(bucketsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.BucketProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnHashResolved_FiresEvent()
        {
            var so    = CreateSO(bucketsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureHashSO)
                .GetField("_onHashResolved", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(bucketsNeeded: 2, bonusPerHash: 1990);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Buckets,           Is.EqualTo(0));
            Assert.That(so.HashCount,         Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleHashes_Accumulate()
        {
            var so = CreateSO(bucketsNeeded: 2, bonusPerHash: 1990);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.HashCount,         Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(3980));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_HashSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.HashSO, Is.Null);
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
            typeof(ZoneControlCaptureHashController)
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
