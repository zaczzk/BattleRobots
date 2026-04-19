using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureReverberationTests
    {
        private static ZoneControlCaptureReverberationSO CreateSO(int baseBonus = 15, int maxMultiplier = 8)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureReverberationSO>();
            typeof(ZoneControlCaptureReverberationSO)
                .GetField("_baseBonus", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, baseBonus);
            typeof(ZoneControlCaptureReverberationSO)
                .GetField("_maxMultiplier", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, maxMultiplier);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureReverberationController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureReverberationController>();
        }

        [Test]
        public void SO_FreshInstance_CurrentMultiplier_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CurrentMultiplier, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_PlayerCapture_IncrementsMultiplier()
        {
            var so = CreateSO();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.CurrentMultiplier, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_PlayerCapture_ClampsAtMax()
        {
            var so = CreateSO(maxMultiplier: 3);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.CurrentMultiplier, Is.EqualTo(3));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_BotCapture_NoPlayerCaptures_PayoutZero()
        {
            var so     = CreateSO(baseBonus: 15);
            int payout = so.RecordBotCapture();
            Assert.That(payout, Is.EqualTo(0));
            Assert.That(so.PayoutCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_BotCapture_AfterPlayerCaptures_PaysPendingBonus()
        {
            var so = CreateSO(baseBonus: 10);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int payout = so.RecordBotCapture();
            Assert.That(payout, Is.EqualTo(30));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_BotCapture_ResetsMultiplier()
        {
            var so = CreateSO();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.CurrentMultiplier, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_BotCapture_FiresEvent_WhenPayoutPositive()
        {
            var so    = CreateSO(baseBonus: 10);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureReverberationSO)
                .GetField("_onReverberationPayout", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(baseBonus: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            so.Reset();
            Assert.That(so.CurrentMultiplier, Is.EqualTo(0));
            Assert.That(so.TotalEarned,       Is.EqualTo(0));
            Assert.That(so.PayoutCount,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ReverberationSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ReverberationSO, Is.Null);
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
            typeof(ZoneControlCaptureReverberationController)
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
