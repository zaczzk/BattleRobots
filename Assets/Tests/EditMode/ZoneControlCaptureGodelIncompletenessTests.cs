using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureGodelIncompletenessTests
    {
        private static ZoneControlCaptureGodelIncompletenessSO CreateSO(
            int consistentExtensionsNeeded = 5,
            int godelSentencesPerBot       = 1,
            int bonusPerExtension          = 4630)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureGodelIncompletenessSO>();
            typeof(ZoneControlCaptureGodelIncompletenessSO)
                .GetField("_consistentExtensionsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, consistentExtensionsNeeded);
            typeof(ZoneControlCaptureGodelIncompletenessSO)
                .GetField("_godelSentencesPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, godelSentencesPerBot);
            typeof(ZoneControlCaptureGodelIncompletenessSO)
                .GetField("_bonusPerExtension", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerExtension);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureGodelIncompletenessController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureGodelIncompletenessController>();
        }

        [Test]
        public void SO_FreshInstance_ConsistentExtensions_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ConsistentExtensions, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ExtensionCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ExtensionCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesExtensions()
        {
            var so = CreateSO(consistentExtensionsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.ConsistentExtensions, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(consistentExtensionsNeeded: 3, bonusPerExtension: 4630);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                   Is.EqualTo(4630));
            Assert.That(so.ExtensionCount,       Is.EqualTo(1));
            Assert.That(so.ConsistentExtensions, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(consistentExtensionsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesGodelSentences()
        {
            var so = CreateSO(consistentExtensionsNeeded: 5, godelSentencesPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.ConsistentExtensions, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(consistentExtensionsNeeded: 5, godelSentencesPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.ConsistentExtensions, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ExtensionProgress_Clamped()
        {
            var so = CreateSO(consistentExtensionsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.ExtensionProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnExtensionCompleted_FiresEvent()
        {
            var so    = CreateSO(consistentExtensionsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureGodelIncompletenessSO)
                .GetField("_onExtensionCompleted", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(consistentExtensionsNeeded: 2, bonusPerExtension: 4630);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.ConsistentExtensions, Is.EqualTo(0));
            Assert.That(so.ExtensionCount,       Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,    Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleExtensions_Accumulate()
        {
            var so = CreateSO(consistentExtensionsNeeded: 2, bonusPerExtension: 4630);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ExtensionCount,    Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(9260));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_GodelSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.GodelSO, Is.Null);
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
            typeof(ZoneControlCaptureGodelIncompletenessController)
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
