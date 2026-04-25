using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCapturePeriodDomainTests
    {
        private static ZoneControlCapturePeriodDomainSO CreateSO(
            int hodgeFiltrationsNeeded     = 7,
            int monodromyObstructionsPerBot = 2,
            int bonusPerPolarization        = 4375)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCapturePeriodDomainSO>();
            typeof(ZoneControlCapturePeriodDomainSO)
                .GetField("_hodgeFiltrationsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, hodgeFiltrationsNeeded);
            typeof(ZoneControlCapturePeriodDomainSO)
                .GetField("_monodromyObstructionsPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, monodromyObstructionsPerBot);
            typeof(ZoneControlCapturePeriodDomainSO)
                .GetField("_bonusPerPolarization", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerPolarization);
            so.Reset();
            return so;
        }

        private static ZoneControlCapturePeriodDomainController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCapturePeriodDomainController>();
        }

        [Test]
        public void SO_FreshInstance_HodgeFiltrations_Zero()
        {
            var so = CreateSO();
            Assert.That(so.HodgeFiltrations, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_PolarizationCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.PolarizationCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesHodgeFiltrations()
        {
            var so = CreateSO(hodgeFiltrationsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.HodgeFiltrations, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(hodgeFiltrationsNeeded: 3, bonusPerPolarization: 4375);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                Is.EqualTo(4375));
            Assert.That(so.PolarizationCount, Is.EqualTo(1));
            Assert.That(so.HodgeFiltrations,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(hodgeFiltrationsNeeded: 7);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesMonodromyObstructions()
        {
            var so = CreateSO(hodgeFiltrationsNeeded: 7, monodromyObstructionsPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.HodgeFiltrations, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(hodgeFiltrationsNeeded: 7, monodromyObstructionsPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.HodgeFiltrations, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_HodgeFiltrationProgress_Clamped()
        {
            var so = CreateSO(hodgeFiltrationsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.HodgeFiltrationProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnPeriodDomainPolarized_FiresEvent()
        {
            var so    = CreateSO(hodgeFiltrationsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCapturePeriodDomainSO)
                .GetField("_onPeriodDomainPolarized", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(hodgeFiltrationsNeeded: 2, bonusPerPolarization: 4375);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.HodgeFiltrations,  Is.EqualTo(0));
            Assert.That(so.PolarizationCount, Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultiplePolarizations_Accumulate()
        {
            var so = CreateSO(hodgeFiltrationsNeeded: 2, bonusPerPolarization: 4375);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.PolarizationCount, Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(8750));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_PeriodDomainSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.PeriodDomainSO, Is.Null);
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
            typeof(ZoneControlCapturePeriodDomainController)
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
