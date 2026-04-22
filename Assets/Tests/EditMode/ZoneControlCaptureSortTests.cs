using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureSortTests
    {
        private static ZoneControlCaptureSortSO CreateSO(
            int comparisonsNeeded = 5,
            int swapPerBot        = 1,
            int bonusPerSort      = 2110)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureSortSO>();
            typeof(ZoneControlCaptureSortSO)
                .GetField("_comparisonsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, comparisonsNeeded);
            typeof(ZoneControlCaptureSortSO)
                .GetField("_swapPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, swapPerBot);
            typeof(ZoneControlCaptureSortSO)
                .GetField("_bonusPerSort", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerSort);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureSortController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureSortController>();
        }

        [Test]
        public void SO_FreshInstance_Comparisons_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Comparisons, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_SortCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.SortCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesComparisons()
        {
            var so = CreateSO(comparisonsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Comparisons, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(comparisonsNeeded: 3, bonusPerSort: 2110);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,          Is.EqualTo(2110));
            Assert.That(so.SortCount,   Is.EqualTo(1));
            Assert.That(so.Comparisons, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(comparisonsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_SwapsComparisons()
        {
            var so = CreateSO(comparisonsNeeded: 5, swapPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Comparisons, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(comparisonsNeeded: 5, swapPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Comparisons, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ComparisonProgress_Clamped()
        {
            var so = CreateSO(comparisonsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.ComparisonProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnSortComplete_FiresEvent()
        {
            var so    = CreateSO(comparisonsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureSortSO)
                .GetField("_onSortComplete", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(comparisonsNeeded: 2, bonusPerSort: 2110);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Comparisons,       Is.EqualTo(0));
            Assert.That(so.SortCount,         Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleSorts_Accumulate()
        {
            var so = CreateSO(comparisonsNeeded: 2, bonusPerSort: 2110);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.SortCount,         Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(4220));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_SortSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.SortSO, Is.Null);
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
            typeof(ZoneControlCaptureSortController)
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
