using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureStringTopologyTests
    {
        private static ZoneControlCaptureStringTopologySO CreateSO(
            int loopsNeeded          = 7,
            int nullHomotopiesPerBot = 2,
            int bonusPerIntersection = 4135)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureStringTopologySO>();
            typeof(ZoneControlCaptureStringTopologySO)
                .GetField("_loopsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, loopsNeeded);
            typeof(ZoneControlCaptureStringTopologySO)
                .GetField("_nullHomotopiesPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, nullHomotopiesPerBot);
            typeof(ZoneControlCaptureStringTopologySO)
                .GetField("_bonusPerIntersection", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerIntersection);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureStringTopologyController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureStringTopologyController>();
        }

        [Test]
        public void SO_FreshInstance_Loops_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Loops, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_IntersectionCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.IntersectionCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesLoops()
        {
            var so = CreateSO(loopsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.Loops, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(loopsNeeded: 3, bonusPerIntersection: 4135);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                Is.EqualTo(4135));
            Assert.That(so.IntersectionCount, Is.EqualTo(1));
            Assert.That(so.Loops,             Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(loopsNeeded: 7);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesNullHomotopies()
        {
            var so = CreateSO(loopsNeeded: 7, nullHomotopiesPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Loops, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(loopsNeeded: 7, nullHomotopiesPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Loops, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_LoopProgress_Clamped()
        {
            var so = CreateSO(loopsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.LoopProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnStringTopologyIntersected_FiresEvent()
        {
            var so    = CreateSO(loopsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureStringTopologySO)
                .GetField("_onStringTopologyIntersected", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(loopsNeeded: 2, bonusPerIntersection: 4135);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Loops,             Is.EqualTo(0));
            Assert.That(so.IntersectionCount, Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleIntersections_Accumulate()
        {
            var so = CreateSO(loopsNeeded: 2, bonusPerIntersection: 4135);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.IntersectionCount, Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(8270));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_StringTopologySO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.StringTopologySO, Is.Null);
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
            typeof(ZoneControlCaptureStringTopologyController)
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
