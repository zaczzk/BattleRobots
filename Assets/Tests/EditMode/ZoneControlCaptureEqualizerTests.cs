using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureEqualizerTests
    {
        private static ZoneControlCaptureEqualizerSO CreateSO(
            int morphismsNeeded      = 6,
            int splitPerBot          = 2,
            int bonusPerEqualization = 2785)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureEqualizerSO>();
            typeof(ZoneControlCaptureEqualizerSO)
                .GetField("_morphismsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, morphismsNeeded);
            typeof(ZoneControlCaptureEqualizerSO)
                .GetField("_splitPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, splitPerBot);
            typeof(ZoneControlCaptureEqualizerSO)
                .GetField("_bonusPerEqualization", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerEqualization);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureEqualizerController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureEqualizerController>();
        }

        [Test]
        public void SO_FreshInstance_Morphisms_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Morphisms, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_EqualizationCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.EqualizationCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesMorphisms()
        {
            var so = CreateSO(morphismsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Morphisms, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(morphismsNeeded: 3, bonusPerEqualization: 2785);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                  Is.EqualTo(2785));
            Assert.That(so.EqualizationCount,   Is.EqualTo(1));
            Assert.That(so.Morphisms,           Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(morphismsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesMorphisms()
        {
            var so = CreateSO(morphismsNeeded: 6, splitPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Morphisms, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(morphismsNeeded: 6, splitPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Morphisms, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MorphismProgress_Clamped()
        {
            var so = CreateSO(morphismsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.MorphismProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnEqualizerFormed_FiresEvent()
        {
            var so    = CreateSO(morphismsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureEqualizerSO)
                .GetField("_onEqualizerFormed", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(morphismsNeeded: 2, bonusPerEqualization: 2785);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Morphisms,         Is.EqualTo(0));
            Assert.That(so.EqualizationCount, Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleEqualizations_Accumulate()
        {
            var so = CreateSO(morphismsNeeded: 2, bonusPerEqualization: 2785);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.EqualizationCount, Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(5570));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_EqualizerSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.EqualizerSO, Is.Null);
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
            typeof(ZoneControlCaptureEqualizerController)
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
