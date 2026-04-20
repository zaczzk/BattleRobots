using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureFaultlineTests
    {
        private static ZoneControlCaptureFaultlineSO CreateSO(
            int tensionThreshold = 8,
            int majorityBonus    = 400,
            int minorityBonus    = 100)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureFaultlineSO>();
            typeof(ZoneControlCaptureFaultlineSO)
                .GetField("_tensionThreshold", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, tensionThreshold);
            typeof(ZoneControlCaptureFaultlineSO)
                .GetField("_majorityBonus", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, majorityBonus);
            typeof(ZoneControlCaptureFaultlineSO)
                .GetField("_minorityBonus", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, minorityBonus);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureFaultlineController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureFaultlineController>();
        }

        [Test]
        public void SO_FreshInstance_RuptureCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.RuptureCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_BelowThreshold_ReturnsZero()
        {
            var so    = CreateSO(tensionThreshold: 8);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_Ruptures()
        {
            var so = CreateSO(tensionThreshold: 4);
            for (int i = 0; i < 4; i++) so.RecordPlayerCapture();
            Assert.That(so.RuptureCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Rupture_PlayerMajority_ReturnsMajorityBonus()
        {
            var so = CreateSO(tensionThreshold: 4, majorityBonus: 400, minorityBonus: 100);
            for (int i = 0; i < 3; i++) so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(400));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Rupture_BotMajority_ReturnsMinorityBonus()
        {
            var so = CreateSO(tensionThreshold: 4, majorityBonus: 400, minorityBonus: 100);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            so.RecordBotCapture();
            int bonus = so.RecordBotCapture();
            Assert.That(bonus, Is.EqualTo(100));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Rupture_ResetsCaptureCounters()
        {
            var so = CreateSO(tensionThreshold: 3);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.PlayerCaptures, Is.EqualTo(0));
            Assert.That(so.BotCaptures,    Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_AtThreshold_Ruptures()
        {
            var so = CreateSO(tensionThreshold: 4);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            so.RecordBotCapture();
            Assert.That(so.RuptureCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_TensionProgress_Clamped()
        {
            var so = CreateSO(tensionThreshold: 4);
            Assert.That(so.TensionProgress, Is.InRange(0f, 1f));
            so.RecordPlayerCapture();
            Assert.That(so.TensionProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnFaultRupture_FiresEvent()
        {
            var so    = CreateSO(tensionThreshold: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureFaultlineSO)
                .GetField("_onFaultRupture", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(tensionThreshold: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.PlayerCaptures,    Is.EqualTo(0));
            Assert.That(so.BotCaptures,       Is.EqualTo(0));
            Assert.That(so.RuptureCount,      Is.EqualTo(0));
            Assert.That(so.MajorityRuptures,  Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleRuptures_Accumulate()
        {
            var so = CreateSO(tensionThreshold: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.RuptureCount, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_FaultlineSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.FaultlineSO, Is.Null);
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
            typeof(ZoneControlCaptureFaultlineController)
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
