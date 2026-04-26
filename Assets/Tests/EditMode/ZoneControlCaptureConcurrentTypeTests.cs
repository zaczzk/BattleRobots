using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureConcurrentTypeTests
    {
        private static ZoneControlCaptureConcurrentTypeSO CreateSO(
            int raceFreeDerivationsNeeded = 6,
            int dataRacesPerBot           = 1,
            int bonusPerDerivation        = 5215)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureConcurrentTypeSO>();
            typeof(ZoneControlCaptureConcurrentTypeSO)
                .GetField("_raceFreeDerivationsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, raceFreeDerivationsNeeded);
            typeof(ZoneControlCaptureConcurrentTypeSO)
                .GetField("_dataRacesPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, dataRacesPerBot);
            typeof(ZoneControlCaptureConcurrentTypeSO)
                .GetField("_bonusPerDerivation", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerDerivation);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureConcurrentTypeController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureConcurrentTypeController>();
        }

        [Test]
        public void SO_FreshInstance_RaceFreeDerivations_Zero()
        {
            var so = CreateSO();
            Assert.That(so.RaceFreeDerivations, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_DerivationCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.DerivationCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesRaceFreeDerivations()
        {
            var so = CreateSO(raceFreeDerivationsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.RaceFreeDerivations, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(raceFreeDerivationsNeeded: 3, bonusPerDerivation: 5215);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                   Is.EqualTo(5215));
            Assert.That(so.DerivationCount,      Is.EqualTo(1));
            Assert.That(so.RaceFreeDerivations,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(raceFreeDerivationsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesDataRaces()
        {
            var so = CreateSO(raceFreeDerivationsNeeded: 6, dataRacesPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.RaceFreeDerivations, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(raceFreeDerivationsNeeded: 6, dataRacesPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.RaceFreeDerivations, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RaceFreeDerivationProgress_Clamped()
        {
            var so = CreateSO(raceFreeDerivationsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.RaceFreeDerivationProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnConcurrentTypeCompleted_FiresEvent()
        {
            var so    = CreateSO(raceFreeDerivationsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureConcurrentTypeSO)
                .GetField("_onConcurrentTypeCompleted", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(raceFreeDerivationsNeeded: 2, bonusPerDerivation: 5215);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.RaceFreeDerivations, Is.EqualTo(0));
            Assert.That(so.DerivationCount,     Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,   Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleDerivations_Accumulate()
        {
            var so = CreateSO(raceFreeDerivationsNeeded: 2, bonusPerDerivation: 5215);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.DerivationCount,   Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(10430));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ConcurrentTypeSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ConcurrentTypeSO, Is.Null);
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
            typeof(ZoneControlCaptureConcurrentTypeController)
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
