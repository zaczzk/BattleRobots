using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureHigherAlgebraTests
    {
        private static ZoneControlCaptureHigherAlgebraSO CreateSO(
            int structureMapsNeeded    = 5,
            int coherenceFailuresPerBot = 1,
            int bonusPerComposition     = 4225)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureHigherAlgebraSO>();
            typeof(ZoneControlCaptureHigherAlgebraSO)
                .GetField("_structureMapsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, structureMapsNeeded);
            typeof(ZoneControlCaptureHigherAlgebraSO)
                .GetField("_coherenceFailuresPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, coherenceFailuresPerBot);
            typeof(ZoneControlCaptureHigherAlgebraSO)
                .GetField("_bonusPerComposition", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerComposition);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureHigherAlgebraController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureHigherAlgebraController>();
        }

        [Test]
        public void SO_FreshInstance_StructureMaps_Zero()
        {
            var so = CreateSO();
            Assert.That(so.StructureMaps, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_CompositionCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CompositionCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesStructureMaps()
        {
            var so = CreateSO(structureMapsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.StructureMaps, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(structureMapsNeeded: 3, bonusPerComposition: 4225);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                Is.EqualTo(4225));
            Assert.That(so.CompositionCount,  Is.EqualTo(1));
            Assert.That(so.StructureMaps,     Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(structureMapsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesCoherenceFailures()
        {
            var so = CreateSO(structureMapsNeeded: 5, coherenceFailuresPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.StructureMaps, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(structureMapsNeeded: 5, coherenceFailuresPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.StructureMaps, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StructureMapProgress_Clamped()
        {
            var so = CreateSO(structureMapsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.StructureMapProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnHigherAlgebraComposed_FiresEvent()
        {
            var so    = CreateSO(structureMapsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureHigherAlgebraSO)
                .GetField("_onHigherAlgebraComposed", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(structureMapsNeeded: 2, bonusPerComposition: 4225);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.StructureMaps,     Is.EqualTo(0));
            Assert.That(so.CompositionCount,  Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleCompositions_Accumulate()
        {
            var so = CreateSO(structureMapsNeeded: 2, bonusPerComposition: 4225);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.CompositionCount,  Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(8450));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_HigherAlgebraSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.HigherAlgebraSO, Is.Null);
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
            typeof(ZoneControlCaptureHigherAlgebraController)
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
