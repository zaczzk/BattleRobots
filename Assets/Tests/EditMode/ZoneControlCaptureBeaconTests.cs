using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureBeaconTests
    {
        private static ZoneControlCaptureBeaconSO CreateSO(
            int capturesForBeacon = 4, int bonusPerBeaconCapture = 90, int durabilityMax = 3)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureBeaconSO>();
            typeof(ZoneControlCaptureBeaconSO)
                .GetField("_capturesForBeacon", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, capturesForBeacon);
            typeof(ZoneControlCaptureBeaconSO)
                .GetField("_bonusPerBeaconCapture", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerBeaconCapture);
            typeof(ZoneControlCaptureBeaconSO)
                .GetField("_durabilityMax", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, durabilityMax);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureBeaconController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureBeaconController>();
        }

        [Test]
        public void SO_FreshInstance_IsLit_False()
        {
            var so = CreateSO();
            Assert.That(so.IsLit, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_BeaconLitCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.BeaconLitCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_BeforeFullCharge_ReturnsZero()
        {
            var so    = CreateSO(capturesForBeacon: 3);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ChargesBeacon_IncreasesChargeProgress()
        {
            var so = CreateSO(capturesForBeacon: 4);
            so.RecordPlayerCapture();
            Assert.That(so.ChargeProgress, Is.EqualTo(0.25f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_FullCharge_LightsBeacon()
        {
            var so = CreateSO(capturesForBeacon: 3, durabilityMax: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.IsLit,          Is.True);
            Assert.That(so.BeaconLitCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_WhenLit_ReturnsBonus()
        {
            var so = CreateSO(capturesForBeacon: 1, bonusPerBeaconCapture: 90, durabilityMax: 5);
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(90));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_FullCharge_FiresBeaconLitEvent()
        {
            var so    = CreateSO(capturesForBeacon: 2, durabilityMax: 3);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureBeaconSO)
                .GetField("_onBeaconLit", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_RecordBotCapture_WhenNotLit_NoOp()
        {
            var so = CreateSO(capturesForBeacon: 4, durabilityMax: 3);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.IsLit,          Is.False);
            Assert.That(so.ChargeProgress, Is.GreaterThan(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_WhenLit_ReducesDurability()
        {
            var so = CreateSO(capturesForBeacon: 1, durabilityMax: 3);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.CurrentDurability, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_DrainsDurabilityToZero_Extinguishes()
        {
            var so = CreateSO(capturesForBeacon: 1, durabilityMax: 1);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.IsLit, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(capturesForBeacon: 1, bonusPerBeaconCapture: 90, durabilityMax: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.IsLit,             Is.False);
            Assert.That(so.BeaconLitCount,    Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Assert.That(so.ChargeCount,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_BeaconSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.BeaconSO, Is.Null);
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
            typeof(ZoneControlCaptureBeaconController)
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
