using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureTitanTests
    {
        private static ZoneControlCaptureTitanSO CreateSO(
            int capturesForRise    = 4,
            int maxTitans          = 3,
            int bonusPerRise       = 150,
            int completionBonus    = 600,
            int drainPerBotCapture = 1)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureTitanSO>();
            typeof(ZoneControlCaptureTitanSO)
                .GetField("_capturesForRise",    BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, capturesForRise);
            typeof(ZoneControlCaptureTitanSO)
                .GetField("_maxTitans",          BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, maxTitans);
            typeof(ZoneControlCaptureTitanSO)
                .GetField("_bonusPerRise",       BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerRise);
            typeof(ZoneControlCaptureTitanSO)
                .GetField("_completionBonus",    BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, completionBonus);
            typeof(ZoneControlCaptureTitanSO)
                .GetField("_drainPerBotCapture", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, drainPerBotCapture);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureTitanController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureTitanController>();
        }

        [Test]
        public void SO_FreshInstance_TitanCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.TitanCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_BelowRiseThreshold_ReturnsZero()
        {
            var so    = CreateSO(capturesForRise: 4);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtRiseThreshold_AddsTitan()
        {
            var so = CreateSO(capturesForRise: 2, maxTitans: 3);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.TitanCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtRiseThreshold_ReturnsBonusPerRise()
        {
            var so = CreateSO(capturesForRise: 2, maxTitans: 3, bonusPerRise: 150);
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(150));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtMaxTitans_TriggersCompletion()
        {
            var so = CreateSO(capturesForRise: 2, maxTitans: 2);
            for (int i = 0; i < 4; i++)
                so.RecordPlayerCapture();
            Assert.That(so.CompletionCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_Completion_ReturnsCompletionBonus()
        {
            var so = CreateSO(capturesForRise: 2, maxTitans: 2, completionBonus: 600);
            for (int i = 0; i < 3; i++)
                so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(600));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_Completion_ResetsTitanCount()
        {
            var so = CreateSO(capturesForRise: 2, maxTitans: 2);
            for (int i = 0; i < 4; i++)
                so.RecordPlayerCapture();
            Assert.That(so.TitanCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_DrainsBuildCount()
        {
            var so = CreateSO(capturesForRise: 4, drainPerBotCapture: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.BuildCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsBuildCountAtZero()
        {
            var so = CreateSO(drainPerBotCapture: 1);
            so.RecordBotCapture();
            Assert.That(so.BuildCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Completion_FiresEvent()
        {
            var so    = CreateSO(capturesForRise: 2, maxTitans: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureTitanSO)
                .GetField("_onTitanComplete", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            for (int i = 0; i < 4; i++)
                so.RecordPlayerCapture();
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(capturesForRise: 2, maxTitans: 2);
            for (int i = 0; i < 4; i++)
                so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.BuildCount,        Is.EqualTo(0));
            Assert.That(so.TitanCount,        Is.EqualTo(0));
            Assert.That(so.CompletionCount,   Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_TitanSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.TitanSO, Is.Null);
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
            typeof(ZoneControlCaptureTitanController)
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
