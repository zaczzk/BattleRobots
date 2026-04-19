using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCapturePulseTests
    {
        private static ZoneControlCapturePulseSO CreateSO(int threshold = 5, int bonusPerPulse = 200)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCapturePulseSO>();
            typeof(ZoneControlCapturePulseSO)
                .GetField("_pulseThreshold", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, threshold);
            typeof(ZoneControlCapturePulseSO)
                .GetField("_bonusPerPulse", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerPulse);
            so.Reset();
            return so;
        }

        private static ZoneControlCapturePulseController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCapturePulseController>();
        }

        [Test]
        public void SO_FreshInstance_ChargeCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ChargeCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_BelowThreshold_DoesNotPulse()
        {
            var so = CreateSO(threshold: 5);
            so.RecordCapture();
            so.RecordCapture();
            Assert.That(so.PulseCount,  Is.EqualTo(0));
            Assert.That(so.ChargeCount, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_AtThreshold_FiresPulse()
        {
            var so    = CreateSO(threshold: 3);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCapturePulseSO)
                .GetField("_onPulse", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordCapture();
            so.RecordCapture();
            so.RecordCapture();
            Assert.That(fired,         Is.EqualTo(1));
            Assert.That(so.PulseCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_RecordCapture_AtThreshold_ResetsCharge()
        {
            var so = CreateSO(threshold: 3);
            so.RecordCapture();
            so.RecordCapture();
            so.RecordCapture();
            Assert.That(so.ChargeCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_AtThreshold_AccumulatesBonus()
        {
            var so = CreateSO(threshold: 2, bonusPerPulse: 100);
            so.RecordCapture();
            so.RecordCapture();
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(100));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultiplePulses_CountsCorrectly()
        {
            var so = CreateSO(threshold: 2, bonusPerPulse: 50);
            for (int i = 0; i < 6; i++) so.RecordCapture();
            Assert.That(so.PulseCount,        Is.EqualTo(3));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(150));
            Assert.That(so.ChargeCount,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ChargeProgress_BelowThreshold_InRange()
        {
            var so = CreateSO(threshold: 5);
            so.RecordCapture();
            so.RecordCapture();
            Assert.That(so.ChargeProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(threshold: 2, bonusPerPulse: 100);
            so.RecordCapture();
            so.RecordCapture();
            so.RecordCapture();
            so.Reset();
            Assert.That(so.ChargeCount,       Is.EqualTo(0));
            Assert.That(so.PulseCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_PulseSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.PulseSO, Is.Null);
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
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            ctrl.gameObject.SetActive(true);
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(false));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlCapturePulseController)
                .GetField("_panel", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, panel);
            panel.SetActive(true);
            ctrl.Refresh();
            Assert.That(panel.activeSelf, Is.False);
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Controller_Refresh_WithSO_ShowsPanel()
        {
            var ctrl  = CreateController();
            var so    = CreateSO();
            var panel = new GameObject();
            typeof(ZoneControlCapturePulseController)
                .GetField("_pulseSO", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, so);
            typeof(ZoneControlCapturePulseController)
                .GetField("_panel", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, panel);
            panel.SetActive(false);
            ctrl.Refresh();
            Assert.That(panel.activeSelf, Is.True);
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(so);
        }
    }
}
