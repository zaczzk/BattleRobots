using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureLocalizationTests
    {
        private static ZoneControlCaptureLocalizationSO CreateSO(
            int primesNeeded         = 5,
            int globalPerBot         = 1,
            int bonusPerLocalization = 2650)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureLocalizationSO>();
            typeof(ZoneControlCaptureLocalizationSO)
                .GetField("_primesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, primesNeeded);
            typeof(ZoneControlCaptureLocalizationSO)
                .GetField("_globalPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, globalPerBot);
            typeof(ZoneControlCaptureLocalizationSO)
                .GetField("_bonusPerLocalization", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerLocalization);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureLocalizationController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureLocalizationController>();
        }

        [Test]
        public void SO_FreshInstance_Primes_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Primes, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_LocalizationCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.LocalizationCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesPrimes()
        {
            var so = CreateSO(primesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Primes, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(primesNeeded: 3, bonusPerLocalization: 2650);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                  Is.EqualTo(2650));
            Assert.That(so.LocalizationCount,   Is.EqualTo(1));
            Assert.That(so.Primes,              Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(primesNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesPrimes()
        {
            var so = CreateSO(primesNeeded: 5, globalPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Primes, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(primesNeeded: 5, globalPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Primes, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_PrimeProgress_Clamped()
        {
            var so = CreateSO(primesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.PrimeProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnLocalizationApplied_FiresEvent()
        {
            var so    = CreateSO(primesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureLocalizationSO)
                .GetField("_onLocalizationApplied", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(primesNeeded: 2, bonusPerLocalization: 2650);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Primes,             Is.EqualTo(0));
            Assert.That(so.LocalizationCount,  Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleLocalizations_Accumulate()
        {
            var so = CreateSO(primesNeeded: 2, bonusPerLocalization: 2650);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.LocalizationCount,  Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded,  Is.EqualTo(5300));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_LocalizationSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.LocalizationSO, Is.Null);
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
            typeof(ZoneControlCaptureLocalizationController)
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
