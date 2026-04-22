using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureCacheTests
    {
        private static ZoneControlCaptureCacheSO CreateSO(
            int slotsNeeded  = 6,
            int evictPerBot  = 2,
            int bonusPerHit  = 1840)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureCacheSO>();
            typeof(ZoneControlCaptureCacheSO)
                .GetField("_slotsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, slotsNeeded);
            typeof(ZoneControlCaptureCacheSO)
                .GetField("_evictPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evictPerBot);
            typeof(ZoneControlCaptureCacheSO)
                .GetField("_bonusPerHit", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerHit);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureCacheController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureCacheController>();
        }

        [Test]
        public void SO_FreshInstance_Slots_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Slots, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_HitCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.HitCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesSlots()
        {
            var so = CreateSO(slotsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Slots, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(slotsNeeded: 3, bonusPerHit: 1840);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,         Is.EqualTo(1840));
            Assert.That(so.HitCount,   Is.EqualTo(1));
            Assert.That(so.Slots,      Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(slotsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesSlots()
        {
            var so = CreateSO(slotsNeeded: 6, evictPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Slots, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(slotsNeeded: 6, evictPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Slots, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SlotProgress_Clamped()
        {
            var so = CreateSO(slotsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.SlotProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnCacheHit_FiresEvent()
        {
            var so    = CreateSO(slotsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureCacheSO)
                .GetField("_onCacheHit", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(slotsNeeded: 2, bonusPerHit: 1840);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Slots,            Is.EqualTo(0));
            Assert.That(so.HitCount,         Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleHits_Accumulate()
        {
            var so = CreateSO(slotsNeeded: 2, bonusPerHit: 1840);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.HitCount,         Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(3680));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_CacheSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.CacheSO, Is.Null);
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
            typeof(ZoneControlCaptureCacheController)
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
