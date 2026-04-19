using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureInverterTests
    {
        private static ZoneControlCaptureInverterSO CreateSO(int threshold = 3, int bonus = 220)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureInverterSO>();
            typeof(ZoneControlCaptureInverterSO)
                .GetField("_chargeThreshold", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, threshold);
            typeof(ZoneControlCaptureInverterSO)
                .GetField("_bonusPerInversion", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonus);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureInverterController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureInverterController>();
        }

        [Test]
        public void SO_FreshInstance_BotChargeCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.BotChargeCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_InversionCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.InversionCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IncreasesCharge()
        {
            var so = CreateSO();
            so.RecordBotCapture();
            so.RecordBotCapture();
            Assert.That(so.BotChargeCount, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_BelowThreshold_ReturnsZero()
        {
            var so = CreateSO(threshold: 3);
            so.RecordBotCapture();
            so.RecordBotCapture();
            int result = so.RecordPlayerCapture();
            Assert.That(result, Is.EqualTo(0));
            Assert.That(so.InversionCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_ReturnsBonus()
        {
            var so = CreateSO(threshold: 2, bonus: 300);
            so.RecordBotCapture();
            so.RecordBotCapture();
            int result = so.RecordPlayerCapture();
            Assert.That(result, Is.EqualTo(300));
            Assert.That(so.InversionCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_DecreasesCharge()
        {
            var so = CreateSO(threshold: 3);
            so.RecordBotCapture();
            so.RecordBotCapture();
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            Assert.That(so.BotChargeCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_FiresEvent()
        {
            var so    = CreateSO(threshold: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureInverterSO)
                .GetField("_onInversion", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordBotCapture();
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_InversionProgress_Computed()
        {
            var so = CreateSO(threshold: 4);
            Assert.That(so.InversionProgress, Is.EqualTo(0f).Within(0.001f));
            so.RecordBotCapture();
            so.RecordBotCapture();
            Assert.That(so.InversionProgress, Is.EqualTo(0.5f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(threshold: 2);
            so.RecordBotCapture();
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.BotChargeCount,    Is.EqualTo(0));
            Assert.That(so.InversionCount,    Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_InverterSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.InverterSO, Is.Null);
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
            typeof(ZoneControlCaptureInverterController)
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
