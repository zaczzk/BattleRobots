using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureLightningTests
    {
        private static ZoneControlCaptureLightningSO CreateSO(
            int chargesNeeded   = 6,
            int dischargePerBot = 2,
            int bonusPerStrike  = 580)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureLightningSO>();
            typeof(ZoneControlCaptureLightningSO)
                .GetField("_chargesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, chargesNeeded);
            typeof(ZoneControlCaptureLightningSO)
                .GetField("_dischargePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, dischargePerBot);
            typeof(ZoneControlCaptureLightningSO)
                .GetField("_bonusPerStrike", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerStrike);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureLightningController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureLightningController>();
        }

        [Test]
        public void SO_FreshInstance_Charges_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Charges, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_StrikeCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.StrikeCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesCharges()
        {
            var so = CreateSO(chargesNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Charges, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_StrikesAtThreshold()
        {
            var so    = CreateSO(chargesNeeded: 3, bonusPerStrike: 580);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,          Is.EqualTo(580));
            Assert.That(so.StrikeCount, Is.EqualTo(1));
            Assert.That(so.Charges,     Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(chargesNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_DischargesCharges()
        {
            var so = CreateSO(chargesNeeded: 6, dischargePerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Charges, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(chargesNeeded: 6, dischargePerBot: 5);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Charges, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ChargeProgress_Clamped()
        {
            var so = CreateSO(chargesNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.ChargeProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnLightningStruck_FiresEvent()
        {
            var so    = CreateSO(chargesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureLightningSO)
                .GetField("_onLightningStruck", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(chargesNeeded: 2, bonusPerStrike: 580);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Charges,           Is.EqualTo(0));
            Assert.That(so.StrikeCount,       Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleStrikes_Accumulate()
        {
            var so = CreateSO(chargesNeeded: 2, bonusPerStrike: 580);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.StrikeCount,       Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(1160));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_LightningSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.LightningSO, Is.Null);
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
            typeof(ZoneControlCaptureLightningController)
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
