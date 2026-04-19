using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureWarpTests
    {
        private static ZoneControlCaptureWarpSO CreateSO(int maxLevel = 5, float multiplierPerLevel = 0.2f)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureWarpSO>();
            typeof(ZoneControlCaptureWarpSO)
                .GetField("_maxWarpLevel", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, maxLevel);
            typeof(ZoneControlCaptureWarpSO)
                .GetField("_warpMultiplierPerLevel", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, multiplierPerLevel);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureWarpController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureWarpController>();
        }

        [Test]
        public void SO_FreshInstance_WarpLevel_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CurrentWarpLevel, Is.EqualTo(0));
            Assert.That(so.CurrentStreak,    Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_PlayerCaptures_IncreaseStreakAndLevel()
        {
            var so = CreateSO(maxLevel: 5);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.CurrentStreak,    Is.EqualTo(2));
            Assert.That(so.CurrentWarpLevel, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_WarpLevel_CappedAtMax()
        {
            var so = CreateSO(maxLevel: 3);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.CurrentWarpLevel, Is.EqualTo(3));
            Assert.That(so.CurrentStreak,    Is.EqualTo(4));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_BotCapture_ResetsStreak()
        {
            var so = CreateSO(maxLevel: 5);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.CurrentStreak,    Is.EqualTo(0));
            Assert.That(so.CurrentWarpLevel, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_BotCapture_WhenAlreadyZero_NoEvent()
        {
            var so    = CreateSO();
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureWarpSO)
                .GetField("_onWarpLevelChanged", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordBotCapture();
            Assert.That(fired, Is.EqualTo(0));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_WarpMultiplier_ScalesWithLevel()
        {
            var so = CreateSO(maxLevel: 5, multiplierPerLevel: 0.25f);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.WarpMultiplier, Is.EqualTo(1.5f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ComputeWarpBonus_ScalesBase()
        {
            var so = CreateSO(maxLevel: 5, multiplierPerLevel: 0.5f);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.ComputeWarpBonus(100);
            Assert.That(bonus, Is.EqualTo(200));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.CurrentStreak,    Is.EqualTo(0));
            Assert.That(so.CurrentWarpLevel, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_WarpSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.WarpSO, Is.Null);
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
            typeof(ZoneControlCaptureWarpController)
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
            typeof(ZoneControlCaptureWarpController)
                .GetField("_warpSO", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, so);
            typeof(ZoneControlCaptureWarpController)
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
