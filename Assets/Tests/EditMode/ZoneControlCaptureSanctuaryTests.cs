using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureSanctuaryTests
    {
        private static ZoneControlCaptureSanctuarySO CreateSO(
            int chargesNeeded     = 4,
            int bonusPerSanctuary = 380)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureSanctuarySO>();
            typeof(ZoneControlCaptureSanctuarySO)
                .GetField("_chargesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, chargesNeeded);
            typeof(ZoneControlCaptureSanctuarySO)
                .GetField("_bonusPerSanctuary", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerSanctuary);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureSanctuaryController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureSanctuaryController>();
        }

        [Test]
        public void SO_FreshInstance_Charges_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Charges, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_SanctuaryCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.SanctuaryCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesCharges()
        {
            var so = CreateSO(chargesNeeded: 4);
            so.RecordPlayerCapture();
            Assert.That(so.Charges, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_SealsAtThreshold()
        {
            var so    = CreateSO(chargesNeeded: 3, bonusPerSanctuary: 380);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,              Is.EqualTo(380));
            Assert.That(so.SanctuaryCount,  Is.EqualTo(1));
            Assert.That(so.Charges,         Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileBuilding()
        {
            var so    = CreateSO(chargesNeeded: 4);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_DrainsCharge()
        {
            var so = CreateSO(chargesNeeded: 4);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Charges, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(chargesNeeded: 4);
            so.RecordBotCapture();
            Assert.That(so.Charges, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ChargeProgress_Clamped()
        {
            var so = CreateSO(chargesNeeded: 4);
            so.RecordPlayerCapture();
            Assert.That(so.ChargeProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnSanctuarySealed_FiresEvent()
        {
            var so    = CreateSO(chargesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureSanctuarySO)
                .GetField("_onSanctuarySealed", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(chargesNeeded: 2, bonusPerSanctuary: 380);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Charges,           Is.EqualTo(0));
            Assert.That(so.SanctuaryCount,    Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleSanctuaries_Accumulate()
        {
            var so = CreateSO(chargesNeeded: 2, bonusPerSanctuary: 380);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.SanctuaryCount,    Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(760));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_SanctuarySO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.SanctuarySO, Is.Null);
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
            typeof(ZoneControlCaptureSanctuaryController)
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
