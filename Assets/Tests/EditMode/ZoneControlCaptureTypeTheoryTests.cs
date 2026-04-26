using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureTypeTheoryTests
    {
        private static ZoneControlCaptureTypeTheorySO CreateSO(
            int typeDerivationsNeeded = 6,
            int typeErrorsPerBot      = 1,
            int bonusPerDerivation    = 5020)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureTypeTheorySO>();
            typeof(ZoneControlCaptureTypeTheorySO)
                .GetField("_typeDerivationsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, typeDerivationsNeeded);
            typeof(ZoneControlCaptureTypeTheorySO)
                .GetField("_typeErrorsPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, typeErrorsPerBot);
            typeof(ZoneControlCaptureTypeTheorySO)
                .GetField("_bonusPerDerivation", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerDerivation);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureTypeTheoryController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureTypeTheoryController>();
        }

        [Test]
        public void SO_FreshInstance_TypeDerivations_Zero()
        {
            var so = CreateSO();
            Assert.That(so.TypeDerivations, Is.EqualTo(0));
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
        public void SO_RecordPlayerCapture_AccumulatesDerivations()
        {
            var so = CreateSO(typeDerivationsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.TypeDerivations, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(typeDerivationsNeeded: 3, bonusPerDerivation: 5020);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,               Is.EqualTo(5020));
            Assert.That(so.DerivationCount,  Is.EqualTo(1));
            Assert.That(so.TypeDerivations,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(typeDerivationsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesTypeErrors()
        {
            var so = CreateSO(typeDerivationsNeeded: 6, typeErrorsPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.TypeDerivations, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(typeDerivationsNeeded: 6, typeErrorsPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.TypeDerivations, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_TypeDerivationProgress_Clamped()
        {
            var so = CreateSO(typeDerivationsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.TypeDerivationProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnTypeTheoryCompleted_FiresEvent()
        {
            var so    = CreateSO(typeDerivationsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureTypeTheorySO)
                .GetField("_onTypeTheoryCompleted", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(typeDerivationsNeeded: 2, bonusPerDerivation: 5020);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.TypeDerivations,   Is.EqualTo(0));
            Assert.That(so.DerivationCount,   Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleDerivations_Accumulate()
        {
            var so = CreateSO(typeDerivationsNeeded: 2, bonusPerDerivation: 5020);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.DerivationCount,   Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(10040));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_TypeTheorySO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.TypeTheorySO, Is.Null);
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
            typeof(ZoneControlCaptureTypeTheoryController)
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
