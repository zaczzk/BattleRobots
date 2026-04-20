using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureVolcanoTests
    {
        private static ZoneControlCaptureVolcanoSO CreateSO(
            float pressurePerCapture = 20f,
            float eruptionThreshold  = 100f,
            float coolingPerBot      = 15f,
            int   bonusPerEruption   = 425)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureVolcanoSO>();
            typeof(ZoneControlCaptureVolcanoSO)
                .GetField("_pressurePerCapture", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, pressurePerCapture);
            typeof(ZoneControlCaptureVolcanoSO)
                .GetField("_eruptionThreshold", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, eruptionThreshold);
            typeof(ZoneControlCaptureVolcanoSO)
                .GetField("_coolingPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, coolingPerBot);
            typeof(ZoneControlCaptureVolcanoSO)
                .GetField("_bonusPerEruption", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerEruption);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureVolcanoController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureVolcanoController>();
        }

        [Test]
        public void SO_FreshInstance_EruptionCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.EruptionCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_BelowThreshold_ReturnsZero()
        {
            var so    = CreateSO(pressurePerCapture: 20f, eruptionThreshold: 100f);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_Erupts()
        {
            var so = CreateSO(pressurePerCapture: 25f, eruptionThreshold: 100f);
            for (int i = 0; i < 4; i++) so.RecordPlayerCapture();
            Assert.That(so.EruptionCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Erupt_ReturnsBonus()
        {
            var so = CreateSO(pressurePerCapture: 50f, eruptionThreshold: 100f, bonusPerEruption: 425);
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(425));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Erupt_ResetsPressure()
        {
            var so = CreateSO(pressurePerCapture: 50f, eruptionThreshold: 100f);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.Pressure, Is.EqualTo(0f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ReducesPressure()
        {
            var so = CreateSO(pressurePerCapture: 40f, eruptionThreshold: 100f, coolingPerBot: 15f);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Pressure, Is.EqualTo(25f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_AtZero_ClampsToZero()
        {
            var so = CreateSO();
            so.RecordBotCapture();
            Assert.That(so.Pressure, Is.EqualTo(0f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_PressureProgress_Clamped()
        {
            var so = CreateSO(pressurePerCapture: 20f, eruptionThreshold: 100f);
            Assert.That(so.PressureProgress, Is.InRange(0f, 1f));
            so.RecordPlayerCapture();
            Assert.That(so.PressureProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnEruption_FiresEvent()
        {
            var so    = CreateSO(pressurePerCapture: 50f, eruptionThreshold: 100f);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureVolcanoSO)
                .GetField("_onEruption", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(pressurePerCapture: 50f, eruptionThreshold: 100f);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Pressure,          Is.EqualTo(0f).Within(0.001f));
            Assert.That(so.EruptionCount,     Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleEruptions_Accumulate()
        {
            var so = CreateSO(pressurePerCapture: 50f, eruptionThreshold: 100f);
            for (int i = 0; i < 6; i++) so.RecordPlayerCapture();
            Assert.That(so.EruptionCount, Is.EqualTo(3));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_TotalBonusAwarded_AccumulatesAcrossEruptions()
        {
            var so = CreateSO(pressurePerCapture: 50f, eruptionThreshold: 100f, bonusPerEruption: 100);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(200));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_VolcanoSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.VolcanoSO, Is.Null);
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
            typeof(ZoneControlCaptureVolcanoController)
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
