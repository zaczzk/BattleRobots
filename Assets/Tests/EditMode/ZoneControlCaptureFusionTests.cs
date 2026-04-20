using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureFusionTests
    {
        private static ZoneControlCaptureFusionSO CreateSO(int chargeThreshold = 4, int bonusPerFusion = 325)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureFusionSO>();
            typeof(ZoneControlCaptureFusionSO)
                .GetField("_chargeThreshold", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, chargeThreshold);
            typeof(ZoneControlCaptureFusionSO)
                .GetField("_bonusPerFusion", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerFusion);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureFusionController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureFusionController>();
        }

        [Test]
        public void SO_FreshInstance_FusionCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.FusionCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_TotalBonusAwarded_Zero()
        {
            var so = CreateSO();
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ChargeProgress_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ChargeProgress, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IncrementsCharge()
        {
            var so = CreateSO(chargeThreshold: 4);
            so.RecordBotCapture();
            Assert.That(so.BotChargeCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_BelowThreshold_ReturnsZero()
        {
            var so = CreateSO(chargeThreshold: 4);
            so.RecordBotCapture();
            so.RecordBotCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_BelowThreshold_ResetsCharge()
        {
            var so = CreateSO(chargeThreshold: 4);
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            Assert.That(so.BotChargeCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_ReturnsBonus()
        {
            var so = CreateSO(chargeThreshold: 2, bonusPerFusion: 325);
            so.RecordBotCapture();
            so.RecordBotCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(325));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_FiresEvent()
        {
            var so    = CreateSO(chargeThreshold: 1);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureFusionSO)
                .GetField("_onFusion", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_IncrementsFusionCount()
        {
            var so = CreateSO(chargeThreshold: 2);
            so.RecordBotCapture();
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            Assert.That(so.FusionCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_ResetsCharge()
        {
            var so = CreateSO(chargeThreshold: 2);
            so.RecordBotCapture();
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            Assert.That(so.BotChargeCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(chargeThreshold: 1, bonusPerFusion: 100);
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.FusionCount,       Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Assert.That(so.BotChargeCount,    Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_FusionSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.FusionSO, Is.Null);
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
            typeof(ZoneControlCaptureFusionController)
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
