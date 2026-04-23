using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureCoalgebraTests
    {
        private static ZoneControlCaptureCoalgebraSO CreateSO(
            int statesNeeded   = 5,
            int dissolvePerBot = 1,
            int bonusPerUnfold = 2485)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureCoalgebraSO>();
            typeof(ZoneControlCaptureCoalgebraSO)
                .GetField("_statesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, statesNeeded);
            typeof(ZoneControlCaptureCoalgebraSO)
                .GetField("_dissolvePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, dissolvePerBot);
            typeof(ZoneControlCaptureCoalgebraSO)
                .GetField("_bonusPerUnfold", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerUnfold);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureCoalgebraController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureCoalgebraController>();
        }

        [Test]
        public void SO_FreshInstance_States_Zero()
        {
            var so = CreateSO();
            Assert.That(so.States, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_UnfoldCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.UnfoldCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesStates()
        {
            var so = CreateSO(statesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.States, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(statesNeeded: 3, bonusPerUnfold: 2485);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,           Is.EqualTo(2485));
            Assert.That(so.UnfoldCount,  Is.EqualTo(1));
            Assert.That(so.States,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(statesNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesStates()
        {
            var so = CreateSO(statesNeeded: 5, dissolvePerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.States, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(statesNeeded: 5, dissolvePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.States, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StateProgress_Clamped()
        {
            var so = CreateSO(statesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.StateProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnCoalgebraUnfolded_FiresEvent()
        {
            var so    = CreateSO(statesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureCoalgebraSO)
                .GetField("_onCoalgebraUnfolded", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(statesNeeded: 2, bonusPerUnfold: 2485);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.States,            Is.EqualTo(0));
            Assert.That(so.UnfoldCount,       Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleUnfolds_Accumulate()
        {
            var so = CreateSO(statesNeeded: 2, bonusPerUnfold: 2485);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.UnfoldCount,       Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(4970));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_CoalgebraSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.CoalgebraSO, Is.Null);
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
            typeof(ZoneControlCaptureCoalgebraController)
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
