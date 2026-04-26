using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCapturePolymorphicTypeTests
    {
        private static ZoneControlCapturePolymorphicTypeSO CreateSO(
            int typeInstantiationsNeeded  = 6,
            int typeVariableClashesPerBot = 1,
            int bonusPerInstantiation     = 5125)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCapturePolymorphicTypeSO>();
            typeof(ZoneControlCapturePolymorphicTypeSO)
                .GetField("_typeInstantiationsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, typeInstantiationsNeeded);
            typeof(ZoneControlCapturePolymorphicTypeSO)
                .GetField("_typeVariableClashesPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, typeVariableClashesPerBot);
            typeof(ZoneControlCapturePolymorphicTypeSO)
                .GetField("_bonusPerInstantiation", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerInstantiation);
            so.Reset();
            return so;
        }

        private static ZoneControlCapturePolymorphicTypeController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCapturePolymorphicTypeController>();
        }

        [Test]
        public void SO_FreshInstance_TypeInstantiations_Zero()
        {
            var so = CreateSO();
            Assert.That(so.TypeInstantiations, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_InstantiationCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.InstantiationCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesTypeInstantiations()
        {
            var so = CreateSO(typeInstantiationsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.TypeInstantiations, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(typeInstantiationsNeeded: 3, bonusPerInstantiation: 5125);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                  Is.EqualTo(5125));
            Assert.That(so.InstantiationCount,  Is.EqualTo(1));
            Assert.That(so.TypeInstantiations,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(typeInstantiationsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesTypeVariableClashes()
        {
            var so = CreateSO(typeInstantiationsNeeded: 6, typeVariableClashesPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.TypeInstantiations, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(typeInstantiationsNeeded: 6, typeVariableClashesPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.TypeInstantiations, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_TypeInstantiationProgress_Clamped()
        {
            var so = CreateSO(typeInstantiationsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.TypeInstantiationProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnPolymorphicTypeCompleted_FiresEvent()
        {
            var so    = CreateSO(typeInstantiationsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCapturePolymorphicTypeSO)
                .GetField("_onPolymorphicTypeCompleted", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(typeInstantiationsNeeded: 2, bonusPerInstantiation: 5125);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.TypeInstantiations, Is.EqualTo(0));
            Assert.That(so.InstantiationCount, Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleInstantiations_Accumulate()
        {
            var so = CreateSO(typeInstantiationsNeeded: 2, bonusPerInstantiation: 5125);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.InstantiationCount, Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded,  Is.EqualTo(10250));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_PolymorphicTypeSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.PolymorphicTypeSO, Is.Null);
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
            typeof(ZoneControlCapturePolymorphicTypeController)
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
