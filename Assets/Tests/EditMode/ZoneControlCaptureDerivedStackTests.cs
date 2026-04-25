using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureDerivedStackTests
    {
        private static ZoneControlCaptureDerivedStackSO CreateSO(
            int derivedThickeningsNeeded  = 6,
            int obstructionTheoriesPerBot = 2,
            int bonusPerResolution        = 4180)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureDerivedStackSO>();
            typeof(ZoneControlCaptureDerivedStackSO)
                .GetField("_derivedThickeningsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, derivedThickeningsNeeded);
            typeof(ZoneControlCaptureDerivedStackSO)
                .GetField("_obstructionTheoriesPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, obstructionTheoriesPerBot);
            typeof(ZoneControlCaptureDerivedStackSO)
                .GetField("_bonusPerResolution", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerResolution);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureDerivedStackController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureDerivedStackController>();
        }

        [Test]
        public void SO_FreshInstance_DerivedThickenings_Zero()
        {
            var so = CreateSO();
            Assert.That(so.DerivedThickenings, Is.EqualTo(0));
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
        public void SO_RecordPlayerCapture_AccumulatesDerivedThickenings()
        {
            var so = CreateSO(derivedThickeningsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.DerivedThickenings, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(derivedThickeningsNeeded: 3, bonusPerResolution: 4180);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                 Is.EqualTo(4180));
            Assert.That(so.ResolutionCount,    Is.EqualTo(1));
            Assert.That(so.DerivedThickenings, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(derivedThickeningsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesObstructionTheories()
        {
            var so = CreateSO(derivedThickeningsNeeded: 6, obstructionTheoriesPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.DerivedThickenings, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(derivedThickeningsNeeded: 6, obstructionTheoriesPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.DerivedThickenings, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_DerivedThickeningProgress_Clamped()
        {
            var so = CreateSO(derivedThickeningsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.DerivedThickeningProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnDerivedStackResolved_FiresEvent()
        {
            var so    = CreateSO(derivedThickeningsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureDerivedStackSO)
                .GetField("_onDerivedStackResolved", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(derivedThickeningsNeeded: 2, bonusPerResolution: 4180);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.DerivedThickenings, Is.EqualTo(0));
            Assert.That(so.ResolutionCount,    Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleResolutions_Accumulate()
        {
            var so = CreateSO(derivedThickeningsNeeded: 2, bonusPerResolution: 4180);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ResolutionCount,   Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(8360));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_DerivedStackSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.DerivedStackSO, Is.Null);
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
            typeof(ZoneControlCaptureDerivedStackController)
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
