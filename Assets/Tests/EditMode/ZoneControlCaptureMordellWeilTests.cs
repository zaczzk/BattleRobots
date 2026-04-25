using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureMordellWeilTests
    {
        private static ZoneControlCaptureMordellWeilSO CreateSO(
            int generatorsNeeded   = 6,
            int torsionPerBot      = 1,
            int bonusPerGeneration = 4465)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureMordellWeilSO>();
            typeof(ZoneControlCaptureMordellWeilSO)
                .GetField("_generatorsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, generatorsNeeded);
            typeof(ZoneControlCaptureMordellWeilSO)
                .GetField("_torsionPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, torsionPerBot);
            typeof(ZoneControlCaptureMordellWeilSO)
                .GetField("_bonusPerGeneration", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerGeneration);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureMordellWeilController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureMordellWeilController>();
        }

        [Test]
        public void SO_FreshInstance_Generators_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Generators, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_GenerationCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.GenerationCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesGenerators()
        {
            var so = CreateSO(generatorsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Generators, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(generatorsNeeded: 3, bonusPerGeneration: 4465);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,               Is.EqualTo(4465));
            Assert.That(so.GenerationCount,  Is.EqualTo(1));
            Assert.That(so.Generators,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(generatorsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ReducesGenerators()
        {
            var so = CreateSO(generatorsNeeded: 6, torsionPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Generators, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(generatorsNeeded: 6, torsionPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Generators, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GeneratorProgress_Clamped()
        {
            var so = CreateSO(generatorsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.GeneratorProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnMordellWeilGenerated_FiresEvent()
        {
            var so    = CreateSO(generatorsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureMordellWeilSO)
                .GetField("_onMordellWeilGenerated", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(generatorsNeeded: 2, bonusPerGeneration: 4465);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Generators,       Is.EqualTo(0));
            Assert.That(so.GenerationCount,  Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleGenerations_Accumulate()
        {
            var so = CreateSO(generatorsNeeded: 2, bonusPerGeneration: 4465);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.GenerationCount,   Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(8930));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_MordellWeilSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.MordellWeilSO, Is.Null);
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
            typeof(ZoneControlCaptureMordellWeilController)
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
