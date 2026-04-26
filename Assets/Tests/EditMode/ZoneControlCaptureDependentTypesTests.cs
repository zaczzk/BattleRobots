using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureDependentTypesTests
    {
        private static ZoneControlCaptureDependentTypesSO CreateSO(
            int typeWitnessesNeeded       = 6,
            int typeCheckingFailuresPerBot = 1,
            int bonusPerWitness           = 5050)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureDependentTypesSO>();
            typeof(ZoneControlCaptureDependentTypesSO)
                .GetField("_typeWitnessesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, typeWitnessesNeeded);
            typeof(ZoneControlCaptureDependentTypesSO)
                .GetField("_typeCheckingFailuresPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, typeCheckingFailuresPerBot);
            typeof(ZoneControlCaptureDependentTypesSO)
                .GetField("_bonusPerWitness", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerWitness);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureDependentTypesController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureDependentTypesController>();
        }

        [Test]
        public void SO_FreshInstance_TypeWitnesses_Zero()
        {
            var so = CreateSO();
            Assert.That(so.TypeWitnesses, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_WitnessCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.WitnessCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesWitnesses()
        {
            var so = CreateSO(typeWitnessesNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.TypeWitnesses, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(typeWitnessesNeeded: 3, bonusPerWitness: 5050);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,             Is.EqualTo(5050));
            Assert.That(so.WitnessCount,   Is.EqualTo(1));
            Assert.That(so.TypeWitnesses,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(typeWitnessesNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesTypeCheckingFailures()
        {
            var so = CreateSO(typeWitnessesNeeded: 6, typeCheckingFailuresPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.TypeWitnesses, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(typeWitnessesNeeded: 6, typeCheckingFailuresPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.TypeWitnesses, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_TypeWitnessProgress_Clamped()
        {
            var so = CreateSO(typeWitnessesNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.TypeWitnessProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnDependentTypesCompleted_FiresEvent()
        {
            var so    = CreateSO(typeWitnessesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureDependentTypesSO)
                .GetField("_onDependentTypesCompleted", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(typeWitnessesNeeded: 2, bonusPerWitness: 5050);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.TypeWitnesses,     Is.EqualTo(0));
            Assert.That(so.WitnessCount,      Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleWitnesses_Accumulate()
        {
            var so = CreateSO(typeWitnessesNeeded: 2, bonusPerWitness: 5050);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.WitnessCount,      Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(10100));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_DependentTypesSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.DependentTypesSO, Is.Null);
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
            typeof(ZoneControlCaptureDependentTypesController)
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
