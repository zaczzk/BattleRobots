using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureGeyserTests
    {
        private static ZoneControlCaptureGeyserSO CreateSO(
            int capturesForEruption = 6,
            int bonusPerEruption    = 220,
            int maxEruptions        = 4,
            int completionBonus     = 700)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureGeyserSO>();
            typeof(ZoneControlCaptureGeyserSO)
                .GetField("_capturesForEruption", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, capturesForEruption);
            typeof(ZoneControlCaptureGeyserSO)
                .GetField("_bonusPerEruption", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerEruption);
            typeof(ZoneControlCaptureGeyserSO)
                .GetField("_maxEruptions", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, maxEruptions);
            typeof(ZoneControlCaptureGeyserSO)
                .GetField("_completionBonus", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, completionBonus);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureGeyserController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureGeyserController>();
        }

        [Test]
        public void SO_FreshInstance_IsComplete_False()
        {
            var so = CreateSO();
            Assert.That(so.IsComplete, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_BelowThreshold_ReturnsZero()
        {
            var so    = CreateSO(capturesForEruption: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_Erupts()
        {
            var so = CreateSO(capturesForEruption: 3, maxEruptions: 4);
            for (int i = 0; i < 3; i++) so.RecordPlayerCapture();
            Assert.That(so.EruptionCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_Eruption_ReturnsBonusPerEruption()
        {
            var so = CreateSO(capturesForEruption: 2, bonusPerEruption: 220, maxEruptions: 4);
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(220));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_MaxEruptions_SetsComplete()
        {
            var so = CreateSO(capturesForEruption: 2, maxEruptions: 2);
            for (int i = 0; i < 4; i++) so.RecordPlayerCapture();
            Assert.That(so.IsComplete, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_Completion_ReturnsCompletionBonus()
        {
            var so = CreateSO(capturesForEruption: 2, maxEruptions: 2, completionBonus: 700);
            for (int i = 0; i < 3; i++) so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(700));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AfterComplete_ReturnsZero()
        {
            var so = CreateSO(capturesForEruption: 2, maxEruptions: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_DrainsBuildCount()
        {
            var so = CreateSO(capturesForEruption: 6);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.BuildCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsBuildCountAtZero()
        {
            var so = CreateSO();
            so.RecordBotCapture();
            Assert.That(so.BuildCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnEruption_FiresEvent()
        {
            var so    = CreateSO(capturesForEruption: 2, maxEruptions: 4);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureGeyserSO)
                .GetField("_onEruption", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_OnGeyserComplete_FiresEvent()
        {
            var so    = CreateSO(capturesForEruption: 2, maxEruptions: 1);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureGeyserSO)
                .GetField("_onGeyserComplete", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(capturesForEruption: 2, maxEruptions: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.BuildCount,        Is.EqualTo(0));
            Assert.That(so.EruptionCount,     Is.EqualTo(0));
            Assert.That(so.IsComplete,        Is.False);
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_GeyserSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.GeyserSO, Is.Null);
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
            typeof(ZoneControlCaptureGeyserController)
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
