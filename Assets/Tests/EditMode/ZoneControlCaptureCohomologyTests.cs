using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureCohomologyTests
    {
        private static ZoneControlCaptureCohomologySO CreateSO(
            int cocyclesNeeded     = 5,
            int boundaryPerBot     = 1,
            int bonusPerCohomology = 2605)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureCohomologySO>();
            typeof(ZoneControlCaptureCohomologySO)
                .GetField("_cocyclesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, cocyclesNeeded);
            typeof(ZoneControlCaptureCohomologySO)
                .GetField("_boundaryPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, boundaryPerBot);
            typeof(ZoneControlCaptureCohomologySO)
                .GetField("_bonusPerCohomology", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerCohomology);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureCohomologyController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureCohomologyController>();
        }

        [Test]
        public void SO_FreshInstance_Cocycles_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Cocycles, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ComputationCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ComputationCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesCocycles()
        {
            var so = CreateSO(cocyclesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Cocycles, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(cocyclesNeeded: 3, bonusPerCohomology: 2605);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                 Is.EqualTo(2605));
            Assert.That(so.ComputationCount,   Is.EqualTo(1));
            Assert.That(so.Cocycles,           Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(cocyclesNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesCocycles()
        {
            var so = CreateSO(cocyclesNeeded: 5, boundaryPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Cocycles, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(cocyclesNeeded: 5, boundaryPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Cocycles, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_CocycleProgress_Clamped()
        {
            var so = CreateSO(cocyclesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.CocycleProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnCohomologyComputed_FiresEvent()
        {
            var so    = CreateSO(cocyclesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureCohomologySO)
                .GetField("_onCohomologyComputed", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(cocyclesNeeded: 2, bonusPerCohomology: 2605);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Cocycles,          Is.EqualTo(0));
            Assert.That(so.ComputationCount,  Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleComputations_Accumulate()
        {
            var so = CreateSO(cocyclesNeeded: 2, bonusPerCohomology: 2605);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ComputationCount,  Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(5210));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_CohomologySO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.CohomologySO, Is.Null);
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
            typeof(ZoneControlCaptureCohomologyController)
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
