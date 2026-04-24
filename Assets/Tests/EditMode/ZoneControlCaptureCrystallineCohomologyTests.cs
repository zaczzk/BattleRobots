using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureCrystallineCohomologyTests
    {
        private static ZoneControlCaptureCrystallineCohomologySO CreateSO(
            int liftsNeeded            = 5,
            int breakPerBot            = 1,
            int bonusPerCrystallization = 3835)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureCrystallineCohomologySO>();
            typeof(ZoneControlCaptureCrystallineCohomologySO)
                .GetField("_liftsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, liftsNeeded);
            typeof(ZoneControlCaptureCrystallineCohomologySO)
                .GetField("_breakPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, breakPerBot);
            typeof(ZoneControlCaptureCrystallineCohomologySO)
                .GetField("_bonusPerCrystallization", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerCrystallization);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureCrystallineCohomologyController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureCrystallineCohomologyController>();
        }

        [Test]
        public void SO_FreshInstance_Lifts_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Lifts, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_CrystallizeCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CrystallizeCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesLifts()
        {
            var so = CreateSO(liftsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Lifts, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(liftsNeeded: 3, bonusPerCrystallization: 3835);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                Is.EqualTo(3835));
            Assert.That(so.CrystallizeCount,  Is.EqualTo(1));
            Assert.That(so.Lifts,             Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(liftsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesRamification()
        {
            var so = CreateSO(liftsNeeded: 5, breakPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Lifts, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(liftsNeeded: 5, breakPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Lifts, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_LiftProgress_Clamped()
        {
            var so = CreateSO(liftsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.LiftProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnCrystallineCohomologyCrystallized_FiresEvent()
        {
            var so    = CreateSO(liftsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureCrystallineCohomologySO)
                .GetField("_onCrystallineCohomologyCrystallized", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(liftsNeeded: 2, bonusPerCrystallization: 3835);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Lifts,             Is.EqualTo(0));
            Assert.That(so.CrystallizeCount,  Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleCrystallizations_Accumulate()
        {
            var so = CreateSO(liftsNeeded: 2, bonusPerCrystallization: 3835);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.CrystallizeCount,  Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(7670));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_CrystallineSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.CrystallineSO, Is.Null);
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
            typeof(ZoneControlCaptureCrystallineCohomologyController)
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
