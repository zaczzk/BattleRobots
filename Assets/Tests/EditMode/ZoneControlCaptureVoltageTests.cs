using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureVoltageTests
    {
        private static ZoneControlCaptureVoltageSO CreateSO(
            float chargePerCapture = 25f, float maxVoltage = 100f,
            float decayRate = 8f, int bonusOnDischarge = 300)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureVoltageSO>();
            typeof(ZoneControlCaptureVoltageSO)
                .GetField("_chargePerCapture", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, chargePerCapture);
            typeof(ZoneControlCaptureVoltageSO)
                .GetField("_maxVoltage", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, maxVoltage);
            typeof(ZoneControlCaptureVoltageSO)
                .GetField("_decayRate", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, decayRate);
            typeof(ZoneControlCaptureVoltageSO)
                .GetField("_bonusOnDischarge", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusOnDischarge);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureVoltageController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureVoltageController>();
        }

        [Test]
        public void SO_FreshInstance_CurrentVoltage_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CurrentVoltage, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_IncreasesVoltage()
        {
            var so = CreateSO(chargePerCapture: 25f, maxVoltage: 100f);
            so.RecordCapture();
            Assert.That(so.CurrentVoltage, Is.EqualTo(25f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_AtMax_Discharges()
        {
            var so = CreateSO(chargePerCapture: 100f, maxVoltage: 100f, bonusOnDischarge: 300);
            so.RecordCapture();
            Assert.That(so.DischargeCount,    Is.EqualTo(1));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(300));
            Assert.That(so.CurrentVoltage,    Is.EqualTo(0f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Discharge_FiresEvent()
        {
            var so    = CreateSO(chargePerCapture: 100f, maxVoltage: 100f);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureVoltageSO)
                .GetField("_onDischarge", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordCapture();
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_Tick_DecaysVoltage()
        {
            var so = CreateSO(chargePerCapture: 50f, maxVoltage: 100f, decayRate: 10f);
            so.RecordCapture();
            so.Tick(2f);
            Assert.That(so.CurrentVoltage, Is.EqualTo(30f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_ClampsAtZero()
        {
            var so = CreateSO(chargePerCapture: 10f, maxVoltage: 100f, decayRate: 50f);
            so.RecordCapture();
            so.Tick(10f);
            Assert.That(so.CurrentVoltage, Is.EqualTo(0f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_VoltageProgress_Correct()
        {
            var so = CreateSO(chargePerCapture: 50f, maxVoltage: 100f);
            so.RecordCapture();
            Assert.That(so.VoltageProgress, Is.EqualTo(0.5f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(chargePerCapture: 50f, maxVoltage: 100f);
            so.RecordCapture();
            so.Reset();
            Assert.That(so.CurrentVoltage,    Is.EqualTo(0f));
            Assert.That(so.DischargeCount,    Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_VoltageSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.VoltageSO, Is.Null);
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
            typeof(ZoneControlCaptureVoltageController)
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
