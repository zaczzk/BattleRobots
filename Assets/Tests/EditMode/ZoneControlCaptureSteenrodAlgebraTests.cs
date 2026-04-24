using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureSteenrodAlgebraTests
    {
        private static ZoneControlCaptureSteenrodAlgebraSO CreateSO(
            int sqOpsNeeded        = 5,
            int instabilityPerBot  = 1,
            int bonusPerApplication = 4015)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureSteenrodAlgebraSO>();
            typeof(ZoneControlCaptureSteenrodAlgebraSO)
                .GetField("_sqOpsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, sqOpsNeeded);
            typeof(ZoneControlCaptureSteenrodAlgebraSO)
                .GetField("_instabilityPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, instabilityPerBot);
            typeof(ZoneControlCaptureSteenrodAlgebraSO)
                .GetField("_bonusPerApplication", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerApplication);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureSteenrodAlgebraController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureSteenrodAlgebraController>();
        }

        [Test]
        public void SO_FreshInstance_SqOps_Zero()
        {
            var so = CreateSO();
            Assert.That(so.SqOps, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ApplicationCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ApplicationCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesSqOps()
        {
            var so = CreateSO(sqOpsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.SqOps, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(sqOpsNeeded: 3, bonusPerApplication: 4015);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                Is.EqualTo(4015));
            Assert.That(so.ApplicationCount,  Is.EqualTo(1));
            Assert.That(so.SqOps,             Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(sqOpsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesInstability()
        {
            var so = CreateSO(sqOpsNeeded: 5, instabilityPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.SqOps, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(sqOpsNeeded: 5, instabilityPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.SqOps, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SqOpProgress_Clamped()
        {
            var so = CreateSO(sqOpsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.SqOpProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnSteenrodAlgebraApplied_FiresEvent()
        {
            var so    = CreateSO(sqOpsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureSteenrodAlgebraSO)
                .GetField("_onSteenrodAlgebraApplied", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(sqOpsNeeded: 2, bonusPerApplication: 4015);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.SqOps,             Is.EqualTo(0));
            Assert.That(so.ApplicationCount,  Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleApplications_Accumulate()
        {
            var so = CreateSO(sqOpsNeeded: 2, bonusPerApplication: 4015);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ApplicationCount,  Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(8030));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_SteenrodAlgebraSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.SteenrodAlgebraSO, Is.Null);
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
            typeof(ZoneControlCaptureSteenrodAlgebraController)
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
