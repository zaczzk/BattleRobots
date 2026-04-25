using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCapturePerfectComplexTests
    {
        private static ZoneControlCapturePerfectComplexSO CreateSO(
            int perfectModulesNeeded   = 6,
            int quasiIsoFailuresPerBot = 2,
            int bonusPerResolution     = 4285)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCapturePerfectComplexSO>();
            typeof(ZoneControlCapturePerfectComplexSO)
                .GetField("_perfectModulesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, perfectModulesNeeded);
            typeof(ZoneControlCapturePerfectComplexSO)
                .GetField("_quasiIsoFailuresPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, quasiIsoFailuresPerBot);
            typeof(ZoneControlCapturePerfectComplexSO)
                .GetField("_bonusPerResolution", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerResolution);
            so.Reset();
            return so;
        }

        private static ZoneControlCapturePerfectComplexController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCapturePerfectComplexController>();
        }

        [Test]
        public void SO_FreshInstance_PerfectModules_Zero()
        {
            var so = CreateSO();
            Assert.That(so.PerfectModules, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ResolutionCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ResolutionCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesPerfectModules()
        {
            var so = CreateSO(perfectModulesNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.PerfectModules, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(perfectModulesNeeded: 3, bonusPerResolution: 4285);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,               Is.EqualTo(4285));
            Assert.That(so.ResolutionCount,  Is.EqualTo(1));
            Assert.That(so.PerfectModules,   Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(perfectModulesNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesQuasiIsoFailures()
        {
            var so = CreateSO(perfectModulesNeeded: 6, quasiIsoFailuresPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.PerfectModules, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(perfectModulesNeeded: 6, quasiIsoFailuresPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.PerfectModules, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_PerfectModuleProgress_Clamped()
        {
            var so = CreateSO(perfectModulesNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.PerfectModuleProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnPerfectComplexResolved_FiresEvent()
        {
            var so    = CreateSO(perfectModulesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCapturePerfectComplexSO)
                .GetField("_onPerfectComplexResolved", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(perfectModulesNeeded: 2, bonusPerResolution: 4285);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.PerfectModules,    Is.EqualTo(0));
            Assert.That(so.ResolutionCount,   Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleResolutions_Accumulate()
        {
            var so = CreateSO(perfectModulesNeeded: 2, bonusPerResolution: 4285);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ResolutionCount,   Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(8570));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_PerfectComplexSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.PerfectComplexSO, Is.Null);
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
            typeof(ZoneControlCapturePerfectComplexController)
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
