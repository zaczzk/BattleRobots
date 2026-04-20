using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureForestTests
    {
        private static ZoneControlCaptureForestSO CreateSO(
            int treesNeeded      = 5,
            int clearPerBot      = 1,
            int bonusPerFlourish = 565)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureForestSO>();
            typeof(ZoneControlCaptureForestSO)
                .GetField("_treesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, treesNeeded);
            typeof(ZoneControlCaptureForestSO)
                .GetField("_clearPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, clearPerBot);
            typeof(ZoneControlCaptureForestSO)
                .GetField("_bonusPerFlourish", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerFlourish);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureForestController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureForestController>();
        }

        [Test]
        public void SO_FreshInstance_Trees_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Trees, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_FlourishCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.FlourishCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesTrees()
        {
            var so = CreateSO(treesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Trees, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_FlourishesAtThreshold()
        {
            var so    = CreateSO(treesNeeded: 3, bonusPerFlourish: 565);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,             Is.EqualTo(565));
            Assert.That(so.FlourishCount,  Is.EqualTo(1));
            Assert.That(so.Trees,          Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(treesNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClearsTrees()
        {
            var so = CreateSO(treesNeeded: 5, clearPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Trees, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(treesNeeded: 5, clearPerBot: 3);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Trees, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_TreeProgress_Clamped()
        {
            var so = CreateSO(treesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.TreeProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnForestFlourished_FiresEvent()
        {
            var so    = CreateSO(treesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureForestSO)
                .GetField("_onForestFlourished", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(treesNeeded: 2, bonusPerFlourish: 565);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Trees,             Is.EqualTo(0));
            Assert.That(so.FlourishCount,     Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleFlourishers_Accumulate()
        {
            var so = CreateSO(treesNeeded: 2, bonusPerFlourish: 565);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.FlourishCount,     Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(1130));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ForestSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ForestSO, Is.Null);
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
            typeof(ZoneControlCaptureForestController)
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
