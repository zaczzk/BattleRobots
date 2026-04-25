using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureResolutionOfSingularitiesTests
    {
        private static ZoneControlCaptureResolutionOfSingularitiesSO CreateSO(
            int blowupsNeeded              = 7,
            int exceptionalDivisorsPerBot  = 2,
            int bonusPerResolution         = 4495)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureResolutionOfSingularitiesSO>();
            typeof(ZoneControlCaptureResolutionOfSingularitiesSO)
                .GetField("_blowupsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, blowupsNeeded);
            typeof(ZoneControlCaptureResolutionOfSingularitiesSO)
                .GetField("_exceptionalDivisorsPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, exceptionalDivisorsPerBot);
            typeof(ZoneControlCaptureResolutionOfSingularitiesSO)
                .GetField("_bonusPerResolution", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerResolution);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureResolutionOfSingularitiesController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureResolutionOfSingularitiesController>();
        }

        [Test]
        public void SO_FreshInstance_Blowups_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Blowups, Is.EqualTo(0));
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
        public void SO_RecordPlayerCapture_AccumulatesBlowups()
        {
            var so = CreateSO(blowupsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.Blowups, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(blowupsNeeded: 3, bonusPerResolution: 4495);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,              Is.EqualTo(4495));
            Assert.That(so.ResolutionCount, Is.EqualTo(1));
            Assert.That(so.Blowups,         Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(blowupsNeeded: 7);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ReducesBlowups()
        {
            var so = CreateSO(blowupsNeeded: 7, exceptionalDivisorsPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Blowups, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(blowupsNeeded: 7, exceptionalDivisorsPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Blowups, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_BlowupProgress_Clamped()
        {
            var so = CreateSO(blowupsNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.BlowupProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnResolutionAchieved_FiresEvent()
        {
            var so    = CreateSO(blowupsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureResolutionOfSingularitiesSO)
                .GetField("_onResolutionAchieved", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(blowupsNeeded: 2, bonusPerResolution: 4495);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Blowups,           Is.EqualTo(0));
            Assert.That(so.ResolutionCount,   Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleResolutions_Accumulate()
        {
            var so = CreateSO(blowupsNeeded: 2, bonusPerResolution: 4495);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ResolutionCount,   Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(8990));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ResolutionSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ResolutionSO, Is.Null);
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
            typeof(ZoneControlCaptureResolutionOfSingularitiesController)
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
