using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureNodeTests
    {
        private static ZoneControlCaptureNodeSO CreateSO(
            int linksNeeded  = 5,
            int cutPerBot    = 1,
            int bonusPerChain = 2050)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureNodeSO>();
            typeof(ZoneControlCaptureNodeSO)
                .GetField("_linksNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, linksNeeded);
            typeof(ZoneControlCaptureNodeSO)
                .GetField("_cutPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, cutPerBot);
            typeof(ZoneControlCaptureNodeSO)
                .GetField("_bonusPerChain", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerChain);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureNodeController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureNodeController>();
        }

        [Test]
        public void SO_FreshInstance_Links_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Links, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ChainCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ChainCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesLinks()
        {
            var so = CreateSO(linksNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Links, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(linksNeeded: 3, bonusPerChain: 2050);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,         Is.EqualTo(2050));
            Assert.That(so.ChainCount, Is.EqualTo(1));
            Assert.That(so.Links,      Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(linksNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_CutsLinks()
        {
            var so = CreateSO(linksNeeded: 5, cutPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Links, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(linksNeeded: 5, cutPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Links, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_LinkProgress_Clamped()
        {
            var so = CreateSO(linksNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.LinkProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnNodeChained_FiresEvent()
        {
            var so    = CreateSO(linksNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureNodeSO)
                .GetField("_onNodeChained", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(linksNeeded: 2, bonusPerChain: 2050);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Links,            Is.EqualTo(0));
            Assert.That(so.ChainCount,       Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleChains_Accumulate()
        {
            var so = CreateSO(linksNeeded: 2, bonusPerChain: 2050);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ChainCount,        Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(4100));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_NodeSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.NodeSO, Is.Null);
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
            typeof(ZoneControlCaptureNodeController)
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
