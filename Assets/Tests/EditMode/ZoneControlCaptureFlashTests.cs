using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureFlashTests
    {
        private static ZoneControlCaptureFlashSO CreateSO(float windowSeconds = 4f, int bonusPerFlash = 175)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureFlashSO>();
            typeof(ZoneControlCaptureFlashSO)
                .GetField("_flashWindowSeconds", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, windowSeconds);
            typeof(ZoneControlCaptureFlashSO)
                .GetField("_bonusPerFlash", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerFlash);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureFlashController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureFlashController>();
        }

        [Test]
        public void SO_FreshInstance_FlashCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.FlashCount, Is.EqualTo(0));
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
        public void SO_FreshInstance_FlashWindowActive_False()
        {
            var so = CreateSO();
            Assert.That(so.FlashWindowActive, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_SetsWindowActive()
        {
            var so = CreateSO();
            so.RecordBotCapture(0f);
            Assert.That(so.FlashWindowActive, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_WithinWindow_ReturnsBonus()
        {
            var so    = CreateSO(windowSeconds: 4f, bonusPerFlash: 175);
            so.RecordBotCapture(0f);
            int bonus = so.RecordPlayerCapture(2f);
            Assert.That(bonus, Is.EqualTo(175));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_OutsideWindow_ReturnsZero()
        {
            var so    = CreateSO(windowSeconds: 4f);
            so.RecordBotCapture(0f);
            int bonus = so.RecordPlayerCapture(5f);
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_WithinWindow_FiresEvent()
        {
            var so    = CreateSO();
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureFlashSO)
                .GetField("_onFlash", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordBotCapture(0f);
            so.RecordPlayerCapture(1f);
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_RecordPlayerCapture_ConsumesWindow()
        {
            var so = CreateSO(windowSeconds: 4f);
            so.RecordBotCapture(0f);
            so.RecordPlayerCapture(1f);
            Assert.That(so.FlashWindowActive, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_NoWindow_ReturnsZero()
        {
            var so    = CreateSO();
            int bonus = so.RecordPlayerCapture(0f);
            Assert.That(bonus, Is.EqualTo(0));
            Assert.That(so.FlashCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(bonusPerFlash: 100);
            so.RecordBotCapture(0f);
            so.RecordPlayerCapture(1f);
            so.Reset();
            Assert.That(so.FlashCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Assert.That(so.FlashWindowActive, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_FlashSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.FlashSO, Is.Null);
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
            typeof(ZoneControlCaptureFlashController)
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
