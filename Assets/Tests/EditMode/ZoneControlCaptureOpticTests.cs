using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureOpticTests
    {
        private static ZoneControlCaptureOpticSO CreateSO(
            int focusNeeded   = 7,
            int blurPerBot    = 2,
            int bonusPerFocus = 2350)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureOpticSO>();
            typeof(ZoneControlCaptureOpticSO)
                .GetField("_focusNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, focusNeeded);
            typeof(ZoneControlCaptureOpticSO)
                .GetField("_blurPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, blurPerBot);
            typeof(ZoneControlCaptureOpticSO)
                .GetField("_bonusPerFocus", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerFocus);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureOpticController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureOpticController>();
        }

        [Test]
        public void SO_FreshInstance_Focus_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Focus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_FocusCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.FocusCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesFocus()
        {
            var so = CreateSO(focusNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.Focus, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(focusNeeded: 3, bonusPerFocus: 2350);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,          Is.EqualTo(2350));
            Assert.That(so.FocusCount,  Is.EqualTo(1));
            Assert.That(so.Focus,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(focusNeeded: 7);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesFocus()
        {
            var so = CreateSO(focusNeeded: 7, blurPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Focus, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(focusNeeded: 7, blurPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Focus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FocusProgress_Clamped()
        {
            var so = CreateSO(focusNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.FocusProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnOpticFocused_FiresEvent()
        {
            var so    = CreateSO(focusNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureOpticSO)
                .GetField("_onOpticFocused", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(focusNeeded: 2, bonusPerFocus: 2350);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Focus,             Is.EqualTo(0));
            Assert.That(so.FocusCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleFocuses_Accumulate()
        {
            var so = CreateSO(focusNeeded: 2, bonusPerFocus: 2350);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.FocusCount,        Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(4700));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_OpticSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.OpticSO, Is.Null);
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
            typeof(ZoneControlCaptureOpticController)
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
