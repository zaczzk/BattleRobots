using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureDerivedFunctorTests
    {
        private static ZoneControlCaptureDerivedFunctorSO CreateSO(
            int resolutionsNeeded = 5,
            int destroyPerBot     = 1,
            int bonusPerDerive    = 3685)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureDerivedFunctorSO>();
            typeof(ZoneControlCaptureDerivedFunctorSO)
                .GetField("_resolutionsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, resolutionsNeeded);
            typeof(ZoneControlCaptureDerivedFunctorSO)
                .GetField("_destroyPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, destroyPerBot);
            typeof(ZoneControlCaptureDerivedFunctorSO)
                .GetField("_bonusPerDerive", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerDerive);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureDerivedFunctorController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureDerivedFunctorController>();
        }

        [Test]
        public void SO_FreshInstance_Resolutions_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Resolutions, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_DeriveCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.DeriveCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesResolutions()
        {
            var so = CreateSO(resolutionsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Resolutions, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(resolutionsNeeded: 3, bonusPerDerive: 3685);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,          Is.EqualTo(3685));
            Assert.That(so.DeriveCount, Is.EqualTo(1));
            Assert.That(so.Resolutions, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(resolutionsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_DestroysExactness()
        {
            var so = CreateSO(resolutionsNeeded: 5, destroyPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Resolutions, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(resolutionsNeeded: 5, destroyPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Resolutions, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ResolutionProgress_Clamped()
        {
            var so = CreateSO(resolutionsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.ResolutionProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnDerivedFunctorDerived_FiresEvent()
        {
            var so    = CreateSO(resolutionsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureDerivedFunctorSO)
                .GetField("_onDerivedFunctorDerived", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(resolutionsNeeded: 2, bonusPerDerive: 3685);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Resolutions,       Is.EqualTo(0));
            Assert.That(so.DeriveCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleDerivations_Accumulate()
        {
            var so = CreateSO(resolutionsNeeded: 2, bonusPerDerive: 3685);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.DeriveCount,       Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(7370));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_DerivedFunctorSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.DerivedFunctorSO, Is.Null);
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
            typeof(ZoneControlCaptureDerivedFunctorController)
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
