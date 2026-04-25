using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureRussellParadoxTests
    {
        private static ZoneControlCaptureRussellParadoxSO CreateSO(
            int selfContainingSetsNeeded = 5,
            int paradoxLoopsPerBot       = 1,
            int bonusPerResolution        = 4825)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureRussellParadoxSO>();
            typeof(ZoneControlCaptureRussellParadoxSO)
                .GetField("_selfContainingSetsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, selfContainingSetsNeeded);
            typeof(ZoneControlCaptureRussellParadoxSO)
                .GetField("_paradoxLoopsPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, paradoxLoopsPerBot);
            typeof(ZoneControlCaptureRussellParadoxSO)
                .GetField("_bonusPerResolution", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerResolution);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureRussellParadoxController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureRussellParadoxController>();
        }

        [Test]
        public void SO_FreshInstance_SelfContainingSets_Zero()
        {
            var so = CreateSO();
            Assert.That(so.SelfContainingSets, Is.EqualTo(0));
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
        public void SO_RecordPlayerCapture_AccumulatesSets()
        {
            var so = CreateSO(selfContainingSetsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.SelfContainingSets, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(selfContainingSetsNeeded: 3, bonusPerResolution: 4825);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                    Is.EqualTo(4825));
            Assert.That(so.ResolutionCount,       Is.EqualTo(1));
            Assert.That(so.SelfContainingSets,    Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(selfContainingSetsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesParadoxLoops()
        {
            var so = CreateSO(selfContainingSetsNeeded: 5, paradoxLoopsPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.SelfContainingSets, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(selfContainingSetsNeeded: 5, paradoxLoopsPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.SelfContainingSets, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SelfContainingSetProgress_Clamped()
        {
            var so = CreateSO(selfContainingSetsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.SelfContainingSetProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnRussellParadoxResolved_FiresEvent()
        {
            var so    = CreateSO(selfContainingSetsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureRussellParadoxSO)
                .GetField("_onRussellParadoxResolved", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(selfContainingSetsNeeded: 2, bonusPerResolution: 4825);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.SelfContainingSets, Is.EqualTo(0));
            Assert.That(so.ResolutionCount,    Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleResolutions_Accumulate()
        {
            var so = CreateSO(selfContainingSetsNeeded: 2, bonusPerResolution: 4825);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ResolutionCount,   Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(9650));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_RussellParadoxSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.RussellParadoxSO, Is.Null);
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
            typeof(ZoneControlCaptureRussellParadoxController)
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
