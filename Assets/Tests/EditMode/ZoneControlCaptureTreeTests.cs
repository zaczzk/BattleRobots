using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureTreeTests
    {
        private static ZoneControlCaptureTreeSO CreateSO(
            int nodesNeeded = 6,
            int prunePerBot = 2,
            int bonusPerGrow = 2005)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureTreeSO>();
            typeof(ZoneControlCaptureTreeSO)
                .GetField("_nodesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, nodesNeeded);
            typeof(ZoneControlCaptureTreeSO)
                .GetField("_prunePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, prunePerBot);
            typeof(ZoneControlCaptureTreeSO)
                .GetField("_bonusPerGrow", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerGrow);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureTreeController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureTreeController>();
        }

        [Test]
        public void SO_FreshInstance_Nodes_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Nodes, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_GrowCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.GrowCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesNodes()
        {
            var so = CreateSO(nodesNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Nodes, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(nodesNeeded: 3, bonusPerGrow: 2005);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,        Is.EqualTo(2005));
            Assert.That(so.GrowCount, Is.EqualTo(1));
            Assert.That(so.Nodes,     Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(nodesNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_PrunesNodes()
        {
            var so = CreateSO(nodesNeeded: 6, prunePerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Nodes, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(nodesNeeded: 6, prunePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Nodes, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_NodeProgress_Clamped()
        {
            var so = CreateSO(nodesNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.NodeProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnTreeGrown_FiresEvent()
        {
            var so    = CreateSO(nodesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureTreeSO)
                .GetField("_onTreeGrown", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(nodesNeeded: 2, bonusPerGrow: 2005);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Nodes,            Is.EqualTo(0));
            Assert.That(so.GrowCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleGrowths_Accumulate()
        {
            var so = CreateSO(nodesNeeded: 2, bonusPerGrow: 2005);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.GrowCount,         Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(4010));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_TreeSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.TreeSO, Is.Null);
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
            typeof(ZoneControlCaptureTreeController)
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
