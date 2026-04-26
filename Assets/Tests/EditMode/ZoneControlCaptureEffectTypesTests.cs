using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureEffectTypesTests
    {
        private static ZoneControlCaptureEffectTypesSO CreateSO(
            int effectsNeeded                = 6,
            int sideEffectCancellationsPerBot = 1,
            int bonusPerEffectApplication    = 5185)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureEffectTypesSO>();
            typeof(ZoneControlCaptureEffectTypesSO)
                .GetField("_effectsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, effectsNeeded);
            typeof(ZoneControlCaptureEffectTypesSO)
                .GetField("_sideEffectCancellationsPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, sideEffectCancellationsPerBot);
            typeof(ZoneControlCaptureEffectTypesSO)
                .GetField("_bonusPerEffectApplication", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerEffectApplication);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureEffectTypesController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureEffectTypesController>();
        }

        [Test]
        public void SO_FreshInstance_Effects_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Effects, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_EffectApplicationCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.EffectApplicationCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesEffects()
        {
            var so = CreateSO(effectsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Effects, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(effectsNeeded: 3, bonusPerEffectApplication: 5185);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                    Is.EqualTo(5185));
            Assert.That(so.EffectApplicationCount, Is.EqualTo(1));
            Assert.That(so.Effects,               Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(effectsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesSideEffectCancellations()
        {
            var so = CreateSO(effectsNeeded: 6, sideEffectCancellationsPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Effects, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(effectsNeeded: 6, sideEffectCancellationsPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Effects, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EffectProgress_Clamped()
        {
            var so = CreateSO(effectsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.EffectProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnEffectTypesCompleted_FiresEvent()
        {
            var so    = CreateSO(effectsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureEffectTypesSO)
                .GetField("_onEffectTypesCompleted", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(effectsNeeded: 2, bonusPerEffectApplication: 5185);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Effects,               Is.EqualTo(0));
            Assert.That(so.EffectApplicationCount, Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,     Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleApplications_Accumulate()
        {
            var so = CreateSO(effectsNeeded: 2, bonusPerEffectApplication: 5185);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.EffectApplicationCount, Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded,      Is.EqualTo(10370));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_EffectTypesSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.EffectTypesSO, Is.Null);
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
            typeof(ZoneControlCaptureEffectTypesController)
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
