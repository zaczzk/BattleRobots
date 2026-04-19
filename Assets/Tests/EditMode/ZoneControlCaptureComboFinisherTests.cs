using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureComboFinisherTests
    {
        private static ZoneControlCaptureComboFinisherSO CreateSO(int comboTarget = 4, int comboBonus = 175)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureComboFinisherSO>();
            typeof(ZoneControlCaptureComboFinisherSO)
                .GetField("_comboTarget", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, comboTarget);
            typeof(ZoneControlCaptureComboFinisherSO)
                .GetField("_comboBonus", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, comboBonus);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureComboFinisherController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureComboFinisherController>();
        }

        [Test]
        public void SO_FreshInstance_CurrentCombo_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CurrentCombo, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_PlayerCaptures_BelowTarget_NoComboCompleted()
        {
            var so = CreateSO(comboTarget: 4);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.CompletedCombos, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_PlayerCaptures_AtTarget_CompletesCombo()
        {
            var so = CreateSO(comboTarget: 3);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.CompletedCombos, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ComboComplete_AccumulatesBonus()
        {
            var so = CreateSO(comboTarget: 2, comboBonus: 100);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(100));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_BotCapture_ResetsCurrentCombo()
        {
            var so = CreateSO(comboTarget: 5);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.CurrentCombo, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ComboProgress_Correct()
        {
            var so = CreateSO(comboTarget: 4);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.ComboProgress, Is.EqualTo(0.5f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ComboComplete_FiresEvent()
        {
            var so    = CreateSO(comboTarget: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureComboFinisherSO)
                .GetField("_onComboFinished", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(comboTarget: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.CurrentCombo,      Is.EqualTo(0));
            Assert.That(so.CompletedCombos,   Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ComboFinisherSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ComboFinisherSO, Is.Null);
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
            typeof(ZoneControlCaptureComboFinisherController)
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
            typeof(ZoneControlCaptureComboFinisherController)
                .GetField("_comboFinisherSO", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, so);
            typeof(ZoneControlCaptureComboFinisherController)
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
