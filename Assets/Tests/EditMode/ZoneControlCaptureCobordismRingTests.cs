using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureCobordismRingTests
    {
        private static ZoneControlCaptureCobordismRingSO CreateSO(
            int generatorsNeeded       = 6,
            int relationsPerBot        = 2,
            int bonusPerMultiplication = 4045)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureCobordismRingSO>();
            typeof(ZoneControlCaptureCobordismRingSO)
                .GetField("_generatorsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, generatorsNeeded);
            typeof(ZoneControlCaptureCobordismRingSO)
                .GetField("_relationsPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, relationsPerBot);
            typeof(ZoneControlCaptureCobordismRingSO)
                .GetField("_bonusPerMultiplication", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerMultiplication);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureCobordismRingController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureCobordismRingController>();
        }

        [Test]
        public void SO_FreshInstance_Generators_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Generators, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_MultiplicationCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.MultiplicationCount, Is.EqualTo(0));
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
            var so    = CreateSO(generatorsNeeded: 3, bonusPerMultiplication: 4045);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                    Is.EqualTo(4045));
            Assert.That(so.MultiplicationCount,   Is.EqualTo(1));
            Assert.That(so.Generators,            Is.EqualTo(0));
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
        public void SO_RecordBotCapture_IntroducesRelations()
        {
            var so = CreateSO(generatorsNeeded: 6, relationsPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Generators, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(generatorsNeeded: 6, relationsPerBot: 10);
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
        public void SO_OnCobordismRingMultiplied_FiresEvent()
        {
            var so    = CreateSO(generatorsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureCobordismRingSO)
                .GetField("_onCobordismRingMultiplied", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(generatorsNeeded: 2, bonusPerMultiplication: 4045);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Generators,          Is.EqualTo(0));
            Assert.That(so.MultiplicationCount, Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,   Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleMultiplications_Accumulate()
        {
            var so = CreateSO(generatorsNeeded: 2, bonusPerMultiplication: 4045);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.MultiplicationCount, Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded,   Is.EqualTo(8090));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_CobordismRingSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.CobordismRingSO, Is.Null);
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
            typeof(ZoneControlCaptureCobordismRingController)
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
