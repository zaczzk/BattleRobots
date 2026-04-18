using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T440: <see cref="ZoneControlMatchMomentumSO"/> and
    /// <see cref="ZoneControlMatchMomentumController"/>.
    ///
    /// ZoneControlMatchMomentumTests (12):
    ///   SO_FreshInstance_Momentum_Half                              x1
    ///   SO_FreshInstance_IsNeutral_True                             x1
    ///   SO_RecordPlayerCapture_RaisesAboveHalf                      x1
    ///   SO_RecordBotCapture_LowersBelowHalf                         x1
    ///   SO_Momentum_AllPlayer_IsHigh_True                           x1
    ///   SO_Momentum_AllBot_IsLow_True                               x1
    ///   SO_FiresOnMomentumHigh_WhenThresholdCrossed                 x1
    ///   SO_FiresOnMomentumLow_WhenThresholdCrossed                  x1
    ///   SO_FiresOnMomentumNeutral_WhenReturnsToBand                 x1
    ///   SO_Tick_PrunesOldCaptures                                   x1
    ///   SO_Reset_ClearsAll                                          x1
    ///   Controller_Refresh_NullSO_HidesPanel                       x1
    /// </summary>
    public sealed class ZoneControlMatchMomentumTests
    {
        private static ZoneControlMatchMomentumSO CreateSO(float window = 20f, float high = 0.65f, float low = 0.35f)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlMatchMomentumSO>();
            typeof(ZoneControlMatchMomentumSO)
                .GetField("_windowSeconds", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, window);
            typeof(ZoneControlMatchMomentumSO)
                .GetField("_highThreshold", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, high);
            typeof(ZoneControlMatchMomentumSO)
                .GetField("_lowThreshold", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, low);
            so.Reset();
            return so;
        }

        private static ZoneControlMatchMomentumController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlMatchMomentumController>();
        }

        [Test]
        public void SO_FreshInstance_Momentum_Half()
        {
            var so = CreateSO();
            Assert.That(so.Momentum, Is.EqualTo(0.5f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_IsNeutral_True()
        {
            var so = CreateSO();
            Assert.That(so.IsNeutral, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_RaisesAboveHalf()
        {
            var so = CreateSO();
            so.RecordPlayerCapture(0f);
            so.RecordPlayerCapture(1f);
            so.RecordBotCapture(2f);
            // 2 player, 1 bot → 2/3 ≈ 0.667
            Assert.That(so.Momentum, Is.GreaterThan(0.5f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_LowersBelowHalf()
        {
            var so = CreateSO();
            so.RecordPlayerCapture(0f);
            so.RecordBotCapture(1f);
            so.RecordBotCapture(2f);
            // 1 player, 2 bot → 1/3 ≈ 0.333
            Assert.That(so.Momentum, Is.LessThan(0.5f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Momentum_AllPlayer_IsHigh_True()
        {
            var so = CreateSO(high: 0.65f, low: 0.35f);
            so.RecordPlayerCapture(0f);
            so.RecordPlayerCapture(1f);
            so.RecordPlayerCapture(2f);
            Assert.That(so.IsHigh, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Momentum_AllBot_IsLow_True()
        {
            var so = CreateSO(high: 0.65f, low: 0.35f);
            so.RecordBotCapture(0f);
            so.RecordBotCapture(1f);
            so.RecordBotCapture(2f);
            Assert.That(so.IsLow, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FiresOnMomentumHigh_WhenThresholdCrossed()
        {
            var so      = CreateSO(high: 0.65f, low: 0.35f);
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlMatchMomentumSO)
                .GetField("_onMomentumHigh", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);
            so.Reset();

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            so.RecordPlayerCapture(0f);
            so.RecordPlayerCapture(1f);
            so.RecordPlayerCapture(2f); // 3/3 = 1.0 ≥ 0.65
            Assert.That(fired, Is.EqualTo(1));

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_FiresOnMomentumLow_WhenThresholdCrossed()
        {
            var so      = CreateSO(high: 0.65f, low: 0.35f);
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlMatchMomentumSO)
                .GetField("_onMomentumLow", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);
            so.Reset();

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            so.RecordBotCapture(0f);
            so.RecordBotCapture(1f);
            so.RecordBotCapture(2f); // 0/3 = 0.0 ≤ 0.35
            Assert.That(fired, Is.EqualTo(1));

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_FiresOnMomentumNeutral_WhenReturnsToBand()
        {
            var so       = CreateSO(high: 0.65f, low: 0.35f);
            var neutral  = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlMatchMomentumSO)
                .GetField("_onMomentumNeutral", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, neutral);
            so.Reset();

            int fired = 0;
            neutral.RegisterCallback(() => fired++);

            // Push to high
            so.RecordPlayerCapture(0f);
            so.RecordPlayerCapture(1f);
            so.RecordPlayerCapture(2f);
            // Return to neutral with bot captures
            so.RecordBotCapture(3f);
            so.RecordBotCapture(4f);
            // 3 player, 2 bot → 3/5 = 0.6 — still above 0.35 but below 0.65
            Assert.That(fired, Is.EqualTo(1));

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(neutral);
        }

        [Test]
        public void SO_Tick_PrunesOldCaptures()
        {
            var so = CreateSO(window: 5f);
            so.RecordBotCapture(0f);
            so.RecordBotCapture(1f);
            so.RecordBotCapture(2f);
            // Advance time beyond window — all bot entries should prune
            so.Tick(10f);
            // After pruning, no entries: Momentum returns to 0.5
            Assert.That(so.Momentum, Is.EqualTo(0.5f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            so.RecordPlayerCapture(0f);
            so.RecordPlayerCapture(1f);
            so.RecordPlayerCapture(2f);
            so.Reset();
            Assert.That(so.IsHigh,    Is.False);
            Assert.That(so.IsLow,     Is.False);
            Assert.That(so.IsNeutral, Is.True);
            Assert.That(so.Momentum,  Is.EqualTo(0.5f).Within(0.001f));
            Object.DestroyImmediate(so);
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
            typeof(ZoneControlMatchMomentumController)
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
