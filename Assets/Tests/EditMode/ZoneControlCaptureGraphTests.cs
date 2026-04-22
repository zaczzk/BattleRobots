using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureGraphTests
    {
        private static ZoneControlCaptureGraphSO CreateSO(
            int edgesNeeded    = 5,
            int removePerBot   = 1,
            int bonusPerConnect = 2020)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureGraphSO>();
            typeof(ZoneControlCaptureGraphSO)
                .GetField("_edgesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, edgesNeeded);
            typeof(ZoneControlCaptureGraphSO)
                .GetField("_removePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, removePerBot);
            typeof(ZoneControlCaptureGraphSO)
                .GetField("_bonusPerConnect", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerConnect);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureGraphController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureGraphController>();
        }

        [Test]
        public void SO_FreshInstance_Edges_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Edges, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ConnectCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ConnectCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesEdges()
        {
            var so = CreateSO(edgesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Edges, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(edgesNeeded: 3, bonusPerConnect: 2020);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,           Is.EqualTo(2020));
            Assert.That(so.ConnectCount, Is.EqualTo(1));
            Assert.That(so.Edges,        Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(edgesNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesEdges()
        {
            var so = CreateSO(edgesNeeded: 5, removePerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Edges, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(edgesNeeded: 5, removePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Edges, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EdgeProgress_Clamped()
        {
            var so = CreateSO(edgesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.EdgeProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnGraphConnected_FiresEvent()
        {
            var so    = CreateSO(edgesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureGraphSO)
                .GetField("_onGraphConnected", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(edgesNeeded: 2, bonusPerConnect: 2020);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Edges,             Is.EqualTo(0));
            Assert.That(so.ConnectCount,      Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleConnects_Accumulate()
        {
            var so = CreateSO(edgesNeeded: 2, bonusPerConnect: 2020);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ConnectCount,      Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(4040));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_GraphSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.GraphSO, Is.Null);
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
            typeof(ZoneControlCaptureGraphController)
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
