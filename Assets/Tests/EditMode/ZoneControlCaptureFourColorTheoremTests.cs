using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureFourColorTheoremTests
    {
        private static ZoneControlCaptureFourColorTheoremSO CreateSO(
            int reducibleConfigsNeeded = 5,
            int unavoidableSetsPerBot  = 1,
            int bonusPerColoring       = 4690)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureFourColorTheoremSO>();
            typeof(ZoneControlCaptureFourColorTheoremSO)
                .GetField("_reducibleConfigsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, reducibleConfigsNeeded);
            typeof(ZoneControlCaptureFourColorTheoremSO)
                .GetField("_unavoidableSetsPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, unavoidableSetsPerBot);
            typeof(ZoneControlCaptureFourColorTheoremSO)
                .GetField("_bonusPerColoring", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerColoring);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureFourColorTheoremController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureFourColorTheoremController>();
        }

        [Test]
        public void SO_FreshInstance_ReducibleConfigs_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ReducibleConfigs, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ColoringCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ColoringCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesReducibleConfigs()
        {
            var so = CreateSO(reducibleConfigsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.ReducibleConfigs, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(reducibleConfigsNeeded: 3, bonusPerColoring: 4690);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                Is.EqualTo(4690));
            Assert.That(so.ColoringCount,     Is.EqualTo(1));
            Assert.That(so.ReducibleConfigs,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(reducibleConfigsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesUnavoidableSets()
        {
            var so = CreateSO(reducibleConfigsNeeded: 5, unavoidableSetsPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.ReducibleConfigs, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(reducibleConfigsNeeded: 5, unavoidableSetsPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.ReducibleConfigs, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ReducibleConfigProgress_Clamped()
        {
            var so = CreateSO(reducibleConfigsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.ReducibleConfigProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnFourColorTheoremColored_FiresEvent()
        {
            var so    = CreateSO(reducibleConfigsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureFourColorTheoremSO)
                .GetField("_onFourColorTheoremColored", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(reducibleConfigsNeeded: 2, bonusPerColoring: 4690);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.ReducibleConfigs,  Is.EqualTo(0));
            Assert.That(so.ColoringCount,     Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleColorings_Accumulate()
        {
            var so = CreateSO(reducibleConfigsNeeded: 2, bonusPerColoring: 4690);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ColoringCount,     Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(9380));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_FourColorSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.FourColorSO, Is.Null);
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
            typeof(ZoneControlCaptureFourColorTheoremController)
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
