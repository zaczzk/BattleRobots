using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlThreatCaptureBonusTests
    {
        private static ZoneControlThreatCaptureBonusSO CreateSO(int bonus = 200)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlThreatCaptureBonusSO>();
            typeof(ZoneControlThreatCaptureBonusSO)
                .GetField("_bonusPerThreatCapture", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonus);
            so.Reset();
            return so;
        }

        private static ZoneControlThreatCaptureBonusController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlThreatCaptureBonusController>();
        }

        [Test]
        public void SO_FreshInstance_ThreatCaptureCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ThreatCaptureCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordThreatCapture_IncrementsCount()
        {
            var so = CreateSO();
            so.RecordThreatCapture();
            Assert.That(so.ThreatCaptureCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordThreatCapture_AccumulatesBonus()
        {
            var so = CreateSO(bonus: 200);
            so.RecordThreatCapture();
            so.RecordThreatCapture();
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(400));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsState()
        {
            var so = CreateSO();
            so.RecordThreatCapture();
            so.Reset();
            Assert.That(so.ThreatCaptureCount, Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleCaptures_CountAndBonus()
        {
            var so = CreateSO(bonus: 100);
            so.RecordThreatCapture();
            so.RecordThreatCapture();
            so.RecordThreatCapture();
            Assert.That(so.ThreatCaptureCount, Is.EqualTo(3));
            Assert.That(so.TotalBonusAwarded,  Is.EqualTo(300));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_BonusPerThreatCapture_ReturnsConfigValue()
        {
            var so = CreateSO(bonus: 150);
            Assert.That(so.BonusPerThreatCapture, Is.EqualTo(150));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AfterReset_NewCapture_StartsFromZero()
        {
            var so = CreateSO(bonus: 100);
            so.RecordThreatCapture();
            so.Reset();
            so.RecordThreatCapture();
            Assert.That(so.ThreatCaptureCount, Is.EqualTo(1));
            Assert.That(so.TotalBonusAwarded,  Is.EqualTo(100));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ZeroBonus_CaptureStillIncrements()
        {
            var so = CreateSO(bonus: 0);
            so.RecordThreatCapture();
            Assert.That(so.ThreatCaptureCount, Is.EqualTo(1));
            Assert.That(so.TotalBonusAwarded,  Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ThreatBonusSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ThreatBonusSO, Is.Null);
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
            typeof(ZoneControlThreatCaptureBonusController)
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
