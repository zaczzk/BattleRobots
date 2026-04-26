using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureModalLogicTests
    {
        private static ZoneControlCaptureModalLogicSO CreateSO(
            int possibleWorldsNeeded        = 6,
            int accessibilityFailuresPerBot = 1,
            int bonusPerCompletion          = 4945)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureModalLogicSO>();
            typeof(ZoneControlCaptureModalLogicSO)
                .GetField("_possibleWorldsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, possibleWorldsNeeded);
            typeof(ZoneControlCaptureModalLogicSO)
                .GetField("_accessibilityFailuresPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, accessibilityFailuresPerBot);
            typeof(ZoneControlCaptureModalLogicSO)
                .GetField("_bonusPerCompletion", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerCompletion);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureModalLogicController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureModalLogicController>();
        }

        [Test]
        public void SO_FreshInstance_PossibleWorlds_Zero()
        {
            var so = CreateSO();
            Assert.That(so.PossibleWorlds, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_CompletionCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CompletionCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesWorlds()
        {
            var so = CreateSO(possibleWorldsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.PossibleWorlds, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(possibleWorldsNeeded: 3, bonusPerCompletion: 4945);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                Is.EqualTo(4945));
            Assert.That(so.CompletionCount,   Is.EqualTo(1));
            Assert.That(so.PossibleWorlds,    Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(possibleWorldsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesAccessibilityFailures()
        {
            var so = CreateSO(possibleWorldsNeeded: 6, accessibilityFailuresPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.PossibleWorlds, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(possibleWorldsNeeded: 6, accessibilityFailuresPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.PossibleWorlds, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_PossibleWorldProgress_Clamped()
        {
            var so = CreateSO(possibleWorldsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.PossibleWorldProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnModalLogicCompleted_FiresEvent()
        {
            var so    = CreateSO(possibleWorldsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureModalLogicSO)
                .GetField("_onModalLogicCompleted", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(possibleWorldsNeeded: 2, bonusPerCompletion: 4945);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.PossibleWorlds,    Is.EqualTo(0));
            Assert.That(so.CompletionCount,   Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleCompletions_Accumulate()
        {
            var so = CreateSO(possibleWorldsNeeded: 2, bonusPerCompletion: 4945);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.CompletionCount,   Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(9890));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ModalLogicSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ModalLogicSO, Is.Null);
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
            typeof(ZoneControlCaptureModalLogicController)
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
