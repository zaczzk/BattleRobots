using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureDaggerTests
    {
        private static ZoneControlCaptureDaggerSO CreateSO(
            int edgesNeeded  = 5,
            int reversePerBot = 2,
            int bonusPerDagger = 3070)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureDaggerSO>();
            typeof(ZoneControlCaptureDaggerSO)
                .GetField("_edgesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, edgesNeeded);
            typeof(ZoneControlCaptureDaggerSO)
                .GetField("_reversePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, reversePerBot);
            typeof(ZoneControlCaptureDaggerSO)
                .GetField("_bonusPerDagger", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerDagger);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureDaggerController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureDaggerController>();
        }

        [Test]
        public void SO_FreshInstance_Edges_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Edges, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_DaggerCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.DaggerCount, Is.EqualTo(0));
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
            var so    = CreateSO(edgesNeeded: 3, bonusPerDagger: 3070);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,           Is.EqualTo(3070));
            Assert.That(so.DaggerCount,  Is.EqualTo(1));
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
            var so = CreateSO(edgesNeeded: 5, reversePerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Edges, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(edgesNeeded: 5, reversePerBot: 10);
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
        public void SO_OnDaggerFormed_FiresEvent()
        {
            var so    = CreateSO(edgesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureDaggerSO)
                .GetField("_onDaggerFormed", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(edgesNeeded: 2, bonusPerDagger: 3070);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Edges,             Is.EqualTo(0));
            Assert.That(so.DaggerCount,       Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleDaggers_Accumulate()
        {
            var so = CreateSO(edgesNeeded: 2, bonusPerDagger: 3070);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.DaggerCount,       Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(6140));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_DaggerSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.DaggerSO, Is.Null);
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
            typeof(ZoneControlCaptureDaggerController)
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
