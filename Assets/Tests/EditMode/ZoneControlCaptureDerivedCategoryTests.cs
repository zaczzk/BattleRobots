using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureDerivedCategoryTests
    {
        private static ZoneControlCaptureDerivedCategorySO CreateSO(
            int quasiIsosNeeded = 5,
            int introducePerBot = 1,
            int bonusPerDerive  = 3670)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureDerivedCategorySO>();
            typeof(ZoneControlCaptureDerivedCategorySO)
                .GetField("_quasiIsosNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, quasiIsosNeeded);
            typeof(ZoneControlCaptureDerivedCategorySO)
                .GetField("_introducePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, introducePerBot);
            typeof(ZoneControlCaptureDerivedCategorySO)
                .GetField("_bonusPerDerive", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerDerive);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureDerivedCategoryController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureDerivedCategoryController>();
        }

        [Test]
        public void SO_FreshInstance_QuasiIsos_Zero()
        {
            var so = CreateSO();
            Assert.That(so.QuasiIsos, Is.EqualTo(0));
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
        public void SO_RecordPlayerCapture_AccumulatesQuasiIsos()
        {
            var so = CreateSO(quasiIsosNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.QuasiIsos, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(quasiIsosNeeded: 3, bonusPerDerive: 3670);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,          Is.EqualTo(3670));
            Assert.That(so.DeriveCount, Is.EqualTo(1));
            Assert.That(so.QuasiIsos,   Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(quasiIsosNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesNonExactSequences()
        {
            var so = CreateSO(quasiIsosNeeded: 5, introducePerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.QuasiIsos, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(quasiIsosNeeded: 5, introducePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.QuasiIsos, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_QuasiIsoProgress_Clamped()
        {
            var so = CreateSO(quasiIsosNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.QuasiIsoProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnDerivedCategoryDerived_FiresEvent()
        {
            var so    = CreateSO(quasiIsosNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureDerivedCategorySO)
                .GetField("_onDerivedCategoryDerived", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(quasiIsosNeeded: 2, bonusPerDerive: 3670);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.QuasiIsos,         Is.EqualTo(0));
            Assert.That(so.DeriveCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleDerivations_Accumulate()
        {
            var so = CreateSO(quasiIsosNeeded: 2, bonusPerDerive: 3670);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.DeriveCount,       Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(7340));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_DerivedCategorySO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.DerivedCategorySO, Is.Null);
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
            typeof(ZoneControlCaptureDerivedCategoryController)
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
