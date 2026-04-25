using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureChowGroupTests
    {
        private static ZoneControlCaptureChowGroupSO CreateSO(
            int rationalCyclesNeeded      = 5,
            int rationalEquivalencesPerBot = 1,
            int bonusPerIntersection       = 4345)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureChowGroupSO>();
            typeof(ZoneControlCaptureChowGroupSO)
                .GetField("_rationalCyclesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, rationalCyclesNeeded);
            typeof(ZoneControlCaptureChowGroupSO)
                .GetField("_rationalEquivalencesPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, rationalEquivalencesPerBot);
            typeof(ZoneControlCaptureChowGroupSO)
                .GetField("_bonusPerIntersection", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerIntersection);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureChowGroupController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureChowGroupController>();
        }

        [Test]
        public void SO_FreshInstance_RationalCycles_Zero()
        {
            var so = CreateSO();
            Assert.That(so.RationalCycles, Is.EqualTo(0));
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
        public void SO_RecordPlayerCapture_AccumulatesRationalCycles()
        {
            var so = CreateSO(rationalCyclesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.RationalCycles, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(rationalCyclesNeeded: 3, bonusPerIntersection: 4345);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                Is.EqualTo(4345));
            Assert.That(so.IntersectionCount, Is.EqualTo(1));
            Assert.That(so.RationalCycles,    Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(rationalCyclesNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesRationalEquivalences()
        {
            var so = CreateSO(rationalCyclesNeeded: 5, rationalEquivalencesPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.RationalCycles, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(rationalCyclesNeeded: 5, rationalEquivalencesPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.RationalCycles, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RationalCycleProgress_Clamped()
        {
            var so = CreateSO(rationalCyclesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.RationalCycleProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnChowGroupIntersected_FiresEvent()
        {
            var so    = CreateSO(rationalCyclesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureChowGroupSO)
                .GetField("_onChowGroupIntersected", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(rationalCyclesNeeded: 2, bonusPerIntersection: 4345);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.RationalCycles,    Is.EqualTo(0));
            Assert.That(so.IntersectionCount, Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleIntersections_Accumulate()
        {
            var so = CreateSO(rationalCyclesNeeded: 2, bonusPerIntersection: 4345);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.IntersectionCount, Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(8690));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ChowGroupSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ChowGroupSO, Is.Null);
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
            typeof(ZoneControlCaptureChowGroupController)
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
