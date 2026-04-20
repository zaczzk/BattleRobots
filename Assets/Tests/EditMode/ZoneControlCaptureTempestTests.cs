using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureTempestTests
    {
        private static ZoneControlCaptureTempestSO CreateSO(
            int chargeForTempest      = 3,
            int capturesNeeded        = 3,
            int bonusPerTempestCapture = 120)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureTempestSO>();
            typeof(ZoneControlCaptureTempestSO)
                .GetField("_chargeForTempest",       BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, chargeForTempest);
            typeof(ZoneControlCaptureTempestSO)
                .GetField("_capturesNeeded",          BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, capturesNeeded);
            typeof(ZoneControlCaptureTempestSO)
                .GetField("_bonusPerTempestCapture",  BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerTempestCapture);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureTempestController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureTempestController>();
        }

        [Test]
        public void SO_FreshInstance_IsActive_False()
        {
            var so = CreateSO();
            Assert.That(so.IsActive, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_TempestCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.TempestCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_BelowThreshold_NoTempest()
        {
            var so = CreateSO(chargeForTempest: 3);
            so.RecordBotCapture();
            Assert.That(so.IsActive, Is.False);
            Assert.That(so.BotCharge, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ReachesThreshold_ActivatesTempest()
        {
            var so = CreateSO(chargeForTempest: 2, capturesNeeded: 2);
            so.RecordBotCapture();
            so.RecordBotCapture();
            Assert.That(so.IsActive, Is.True);
            Assert.That(so.CapturesRemaining, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_WhileActive_ClosedTempest()
        {
            var so = CreateSO(chargeForTempest: 1, capturesNeeded: 3);
            so.RecordBotCapture();
            so.RecordBotCapture();
            Assert.That(so.IsActive, Is.False);
            Assert.That(so.TempestCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_WhenInactive_ReturnsZero_ResetsCharge()
        {
            var so = CreateSO(chargeForTempest: 3);
            so.RecordBotCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Assert.That(so.BotCharge, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_WhenActive_ReturnsBonus()
        {
            var so = CreateSO(chargeForTempest: 1, capturesNeeded: 3, bonusPerTempestCapture: 120);
            so.RecordBotCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(120));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ConsumesAllSlots_ClosedTempest()
        {
            var so = CreateSO(chargeForTempest: 1, capturesNeeded: 2);
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.IsActive, Is.False);
            Assert.That(so.TempestCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ThresholdReached_FiresOpenEvent()
        {
            var so    = CreateSO(chargeForTempest: 1, capturesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureTempestSO)
                .GetField("_onTempestOpened", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordBotCapture();
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_TempestProgress_WhenInactive_ReflectsChargeRatio()
        {
            var so = CreateSO(chargeForTempest: 4);
            so.RecordBotCapture();
            so.RecordBotCapture();
            Assert.That(so.TempestProgress, Is.EqualTo(0.5f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_TempestProgress_WhenActive_ReflectsRemainingRatio()
        {
            var so = CreateSO(chargeForTempest: 1, capturesNeeded: 4);
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            Assert.That(so.TempestProgress, Is.EqualTo(0.75f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(chargeForTempest: 1, capturesNeeded: 3);
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.IsActive,          Is.False);
            Assert.That(so.BotCharge,          Is.EqualTo(0));
            Assert.That(so.TempestCount,       Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_TempestSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.TempestSO, Is.Null);
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
            typeof(ZoneControlCaptureTempestController)
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
