using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureNetworkTests
    {
        private static ZoneControlCaptureNetworkSO CreateSO(int nodeCount = 4, int bonusPerNetwork = 500)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureNetworkSO>();
            typeof(ZoneControlCaptureNetworkSO)
                .GetField("_nodeCount", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, nodeCount);
            typeof(ZoneControlCaptureNetworkSO)
                .GetField("_bonusPerNetwork", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerNetwork);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureNetworkController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureNetworkController>();
        }

        [Test]
        public void SO_FreshInstance_NetworkCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.NetworkCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ActiveNodes_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ActiveNodes, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ActivatesNode()
        {
            var so = CreateSO(nodeCount: 4);
            so.RecordPlayerCapture();
            Assert.That(so.ActiveNodes, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_DeactivatesNode()
        {
            var so = CreateSO(nodeCount: 4);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.ActiveNodes, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_AtZeroNodes_ClampsToZero()
        {
            var so = CreateSO(nodeCount: 4);
            so.RecordBotCapture();
            Assert.That(so.ActiveNodes, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AllNodesActivated_ReturnsBonus()
        {
            var so = CreateSO(nodeCount: 3, bonusPerNetwork: 500);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(500));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AllNodesActivated_FiresEvent()
        {
            var so    = CreateSO(nodeCount: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureNetworkSO)
                .GetField("_onNetworkFired", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_AllNodesActivated_ResetsNodes()
        {
            var so = CreateSO(nodeCount: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.ActiveNodes, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AllNodesActivated_IncrementsNetworkCount()
        {
            var so = CreateSO(nodeCount: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.NetworkCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_NodeProgress_IntermediateValue()
        {
            var so = CreateSO(nodeCount: 4);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.NodeProgress, Is.EqualTo(0.5f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(nodeCount: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.NetworkCount,      Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Assert.That(so.ActiveNodes,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_NetworkSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.NetworkSO, Is.Null);
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
            typeof(ZoneControlCaptureNetworkController)
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
