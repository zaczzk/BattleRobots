using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureGradualTypesTests
    {
        private static ZoneControlCaptureGradualTypesSO CreateSO(
            int consistentTypingsNeeded = 6,
            int castFailuresPerBot      = 1,
            int bonusPerGradualStep     = 5230)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureGradualTypesSO>();
            typeof(ZoneControlCaptureGradualTypesSO)
                .GetField("_consistentTypingsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, consistentTypingsNeeded);
            typeof(ZoneControlCaptureGradualTypesSO)
                .GetField("_castFailuresPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, castFailuresPerBot);
            typeof(ZoneControlCaptureGradualTypesSO)
                .GetField("_bonusPerGradualStep", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerGradualStep);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureGradualTypesController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureGradualTypesController>();
        }

        [Test]
        public void SO_FreshInstance_ConsistentTypings_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ConsistentTypings, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_GradualStepCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.GradualStepCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesConsistentTypings()
        {
            var so = CreateSO(consistentTypingsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.ConsistentTypings, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(consistentTypingsNeeded: 3, bonusPerGradualStep: 5230);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,             Is.EqualTo(5230));
            Assert.That(so.GradualStepCount, Is.EqualTo(1));
            Assert.That(so.ConsistentTypings, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(consistentTypingsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesCastFailures()
        {
            var so = CreateSO(consistentTypingsNeeded: 6, castFailuresPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.ConsistentTypings, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(consistentTypingsNeeded: 6, castFailuresPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.ConsistentTypings, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ConsistentTypingProgress_Clamped()
        {
            var so = CreateSO(consistentTypingsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.ConsistentTypingProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnGradualTypesCompleted_FiresEvent()
        {
            var so    = CreateSO(consistentTypingsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureGradualTypesSO)
                .GetField("_onGradualTypesCompleted", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(consistentTypingsNeeded: 2, bonusPerGradualStep: 5230);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.ConsistentTypings, Is.EqualTo(0));
            Assert.That(so.GradualStepCount,  Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleGradualSteps_Accumulate()
        {
            var so = CreateSO(consistentTypingsNeeded: 2, bonusPerGradualStep: 5230);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.GradualStepCount,  Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(10460));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_GradualTypesSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.GradualTypesSO, Is.Null);
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
            typeof(ZoneControlCaptureGradualTypesController)
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
