using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureHeapTests
    {
        private static ZoneControlCaptureHeapSO CreateSO(
            int allocsNeeded  = 6,
            int gcPerBot      = 2,
            int bonusPerAlloc = 1960)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureHeapSO>();
            typeof(ZoneControlCaptureHeapSO)
                .GetField("_allocsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, allocsNeeded);
            typeof(ZoneControlCaptureHeapSO)
                .GetField("_gcPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, gcPerBot);
            typeof(ZoneControlCaptureHeapSO)
                .GetField("_bonusPerAlloc", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerAlloc);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureHeapController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureHeapController>();
        }

        [Test]
        public void SO_FreshInstance_Allocs_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Allocs, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_AllocCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.AllocCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesAllocs()
        {
            var so = CreateSO(allocsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Allocs, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(allocsNeeded: 3, bonusPerAlloc: 1960);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,          Is.EqualTo(1960));
            Assert.That(so.AllocCount,  Is.EqualTo(1));
            Assert.That(so.Allocs,      Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(allocsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesAllocs()
        {
            var so = CreateSO(allocsNeeded: 6, gcPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Allocs, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(allocsNeeded: 6, gcPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Allocs, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AllocProgress_Clamped()
        {
            var so = CreateSO(allocsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.AllocProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnHeapAllocated_FiresEvent()
        {
            var so    = CreateSO(allocsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureHeapSO)
                .GetField("_onHeapAllocated", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(allocsNeeded: 2, bonusPerAlloc: 1960);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Allocs,            Is.EqualTo(0));
            Assert.That(so.AllocCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleAllocs_Accumulate()
        {
            var so = CreateSO(allocsNeeded: 2, bonusPerAlloc: 1960);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.AllocCount,        Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(3920));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_HeapSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.HeapSO, Is.Null);
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
            typeof(ZoneControlCaptureHeapController)
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
