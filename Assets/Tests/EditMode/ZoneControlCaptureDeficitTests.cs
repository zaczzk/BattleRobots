using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T438: <see cref="ZoneControlCaptureDeficitSO"/> and
    /// <see cref="ZoneControlCaptureDeficitController"/>.
    ///
    /// ZoneControlCaptureDeficitTests (12):
    ///   SO_FreshInstance_Deficit_Zero                               x1
    ///   SO_FreshInstance_IsHighDeficit_False                        x1
    ///   SO_RecordBotCapture_IncrementsDeficit                       x1
    ///   SO_RecordPlayerCapture_DecrementsDeficit                    x1
    ///   SO_Deficit_BelowThreshold_IsHighDeficit_False               x1
    ///   SO_Deficit_AtThreshold_IsHighDeficit_True                   x1
    ///   SO_Deficit_FiresOnHighDeficit_AtThreshold                   x1
    ///   SO_Deficit_FiresOnDeficitCleared_WhenDropsToZero            x1
    ///   SO_IsHighDeficit_Idempotent                                 x1
    ///   SO_Reset_ClearsAll                                          x1
    ///   Controller_FreshInstance_DeficitSO_Null                    x1
    ///   Controller_Refresh_NullSO_HidesPanel                       x1
    /// </summary>
    public sealed class ZoneControlCaptureDeficitTests
    {
        private static ZoneControlCaptureDeficitSO CreateSO(int threshold = 3)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureDeficitSO>();
            typeof(ZoneControlCaptureDeficitSO)
                .GetField("_highDeficitThreshold", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, threshold);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureDeficitController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureDeficitController>();
        }

        [Test]
        public void SO_FreshInstance_Deficit_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Deficit, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_IsHighDeficit_False()
        {
            var so = CreateSO();
            Assert.That(so.IsHighDeficit, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IncrementsDeficit()
        {
            var so = CreateSO();
            so.RecordBotCapture();
            Assert.That(so.Deficit, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_DecrementsDeficit()
        {
            var so = CreateSO();
            so.RecordBotCapture();
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            Assert.That(so.Deficit, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Deficit_BelowThreshold_IsHighDeficit_False()
        {
            var so = CreateSO(threshold: 3);
            so.RecordBotCapture();
            so.RecordBotCapture();
            Assert.That(so.IsHighDeficit, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Deficit_AtThreshold_IsHighDeficit_True()
        {
            var so = CreateSO(threshold: 3);
            so.RecordBotCapture();
            so.RecordBotCapture();
            so.RecordBotCapture();
            Assert.That(so.IsHighDeficit, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Deficit_FiresOnHighDeficit_AtThreshold()
        {
            var so      = CreateSO(threshold: 2);
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureDeficitSO)
                .GetField("_onHighDeficit", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);
            so.Reset();

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            so.RecordBotCapture();
            Assert.That(fired, Is.EqualTo(0));
            so.RecordBotCapture();
            Assert.That(fired, Is.EqualTo(1));

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Deficit_FiresOnDeficitCleared_WhenDropsToZero()
        {
            var so      = CreateSO(threshold: 2);
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureDeficitSO)
                .GetField("_onDeficitCleared", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);
            so.Reset();

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            so.RecordBotCapture();
            so.RecordBotCapture(); // triggers high
            so.RecordPlayerCapture();
            so.RecordPlayerCapture(); // deficit → 0
            Assert.That(fired, Is.EqualTo(1));

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_IsHighDeficit_Idempotent()
        {
            var so      = CreateSO(threshold: 2);
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureDeficitSO)
                .GetField("_onHighDeficit", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);
            so.Reset();

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            so.RecordBotCapture();
            so.RecordBotCapture(); // first trigger
            so.RecordBotCapture(); // already high — no re-fire
            Assert.That(fired, Is.EqualTo(1));

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(threshold: 2);
            so.RecordBotCapture();
            so.RecordBotCapture();
            so.Reset();
            Assert.That(so.Deficit,       Is.EqualTo(0));
            Assert.That(so.IsHighDeficit, Is.False);
            Assert.That(so.PlayerCaptures,Is.EqualTo(0));
            Assert.That(so.BotCaptures,   Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_DeficitSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.DeficitSO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(false));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlCaptureDeficitController)
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
