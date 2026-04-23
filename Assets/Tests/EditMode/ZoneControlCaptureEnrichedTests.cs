using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureEnrichedTests
    {
        private static ZoneControlCaptureEnrichedSO CreateSO(
            int morphismsNeeded  = 5,
            int dilutionPerBot   = 1,
            int bonusPerEnriched = 3025)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureEnrichedSO>();
            typeof(ZoneControlCaptureEnrichedSO)
                .GetField("_morphismsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, morphismsNeeded);
            typeof(ZoneControlCaptureEnrichedSO)
                .GetField("_dilutionPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, dilutionPerBot);
            typeof(ZoneControlCaptureEnrichedSO)
                .GetField("_bonusPerEnriched", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerEnriched);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureEnrichedController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureEnrichedController>();
        }

        [Test]
        public void SO_FreshInstance_Morphisms_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Morphisms, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_EnrichedCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.EnrichedCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesMorphisms()
        {
            var so = CreateSO(morphismsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Morphisms, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(morphismsNeeded: 3, bonusPerEnriched: 3025);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,              Is.EqualTo(3025));
            Assert.That(so.EnrichedCount,   Is.EqualTo(1));
            Assert.That(so.Morphisms,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(morphismsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesMorphisms()
        {
            var so = CreateSO(morphismsNeeded: 5, dilutionPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Morphisms, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(morphismsNeeded: 5, dilutionPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Morphisms, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MorphismProgress_Clamped()
        {
            var so = CreateSO(morphismsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.MorphismProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnEnrichedCategoryFormed_FiresEvent()
        {
            var so    = CreateSO(morphismsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureEnrichedSO)
                .GetField("_onEnrichedCategoryFormed", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(morphismsNeeded: 2, bonusPerEnriched: 3025);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Morphisms,         Is.EqualTo(0));
            Assert.That(so.EnrichedCount,     Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleEnriched_Accumulate()
        {
            var so = CreateSO(morphismsNeeded: 2, bonusPerEnriched: 3025);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.EnrichedCount,     Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(6050));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_EnrichedSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.EnrichedSO, Is.Null);
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
            typeof(ZoneControlCaptureEnrichedController)
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
