using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureOvertimeTests
    {
        private static ZoneControlCaptureOvertimeSO CreateSO(int bonusPerLead = 75, int maxBonus = 600)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureOvertimeSO>();
            typeof(ZoneControlCaptureOvertimeSO)
                .GetField("_bonusPerOvertimeLead", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerLead);
            typeof(ZoneControlCaptureOvertimeSO)
                .GetField("_maxOvertimeBonus", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, maxBonus);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureOvertimeController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureOvertimeController>();
        }

        [Test]
        public void SO_FreshInstance_IsActive_False()
        {
            var so = CreateSO();
            Assert.That(so.IsActive, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StartOvertime_SetsActive()
        {
            var so = CreateSO();
            so.StartOvertime();
            Assert.That(so.IsActive, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StartOvertime_Idempotent()
        {
            var so = CreateSO();
            so.StartOvertime();
            so.RecordPlayerCapture();
            so.StartOvertime();
            Assert.That(so.PlayerOTCaptures, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_WhenActive_Increments()
        {
            var so = CreateSO();
            so.StartOvertime();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.PlayerOTCaptures, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_WhenNotActive_Ignored()
        {
            var so = CreateSO();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.PlayerOTCaptures, Is.EqualTo(0));
            Assert.That(so.BotOTCaptures,    Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ResolveOvertime_WithLead_ReturnsScaledBonus()
        {
            var so = CreateSO(bonusPerLead: 75, maxBonus: 600);
            so.StartOvertime();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.ResolveOvertime();
            Assert.That(bonus, Is.EqualTo(3 * 75));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ResolveOvertime_BonusClampedToMax()
        {
            var so = CreateSO(bonusPerLead: 75, maxBonus: 100);
            so.StartOvertime();
            for (int i = 0; i < 10; i++) so.RecordPlayerCapture();
            int bonus = so.ResolveOvertime();
            Assert.That(bonus, Is.EqualTo(100));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ResolveOvertime_NoLead_ReturnsZero()
        {
            var so = CreateSO();
            so.StartOvertime();
            so.RecordBotCapture();
            int bonus = so.ResolveOvertime();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ResolveOvertime_ClearsActive()
        {
            var so = CreateSO();
            so.StartOvertime();
            so.ResolveOvertime();
            Assert.That(so.IsActive, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            so.StartOvertime();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            so.Reset();
            Assert.That(so.IsActive,         Is.False);
            Assert.That(so.PlayerOTCaptures, Is.EqualTo(0));
            Assert.That(so.BotOTCaptures,    Is.EqualTo(0));
            Assert.That(so.OvertimeBonus,    Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_OvertimeSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.OvertimeSO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(true));
            Object.DestroyImmediate(ctrl.gameObject);
        }
    }
}
