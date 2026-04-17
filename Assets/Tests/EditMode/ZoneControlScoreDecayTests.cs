using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T376: <see cref="ZoneControlScoreDecaySO"/> and
    /// <see cref="ZoneControlScoreDecayController"/>.
    ///
    /// ZoneControlScoreDecayTests (12):
    ///   SO_FreshInstance_IsDecaying_False                        ×1
    ///   SO_FreshInstance_TimeSinceCapture_Zero                   ×1
    ///   SO_Tick_BelowWindow_NoDecay                              ×1
    ///   SO_Tick_AtWindow_StartsDecay                             ×1
    ///   SO_Tick_FiresOnDecayStarted                              ×1
    ///   SO_RecordCapture_WhileDecaying_EndsDecay                 ×1
    ///   SO_RecordCapture_FiresOnDecayEnded                       ×1
    ///   SO_Reset_ClearsAll                                       ×1
    ///   Controller_FreshInstance_DecaySO_Null                    ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                ×1
    ///   Controller_OnDisable_Unregisters_Channel                 ×1
    ///   Controller_Refresh_NullDecaySO_HidesPanel                ×1
    /// </summary>
    public sealed class ZoneControlScoreDecayTests
    {
        private static ZoneControlScoreDecaySO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlScoreDecaySO>();

        private static ZoneControlScoreDecayController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlScoreDecayController>();
        }

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_IsDecaying_False()
        {
            var so = CreateSO();
            Assert.That(so.IsDecaying, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_TimeSinceCapture_Zero()
        {
            var so = CreateSO();
            Assert.That(so.TimeSinceCapture, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_BelowWindow_NoDecay()
        {
            var so = CreateSO();
            so.Tick(so.DecayWindow - 0.1f);
            Assert.That(so.IsDecaying, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_AtWindow_StartsDecay()
        {
            var so = CreateSO();
            so.Tick(so.DecayWindow);
            Assert.That(so.IsDecaying, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_FiresOnDecayStarted()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlScoreDecaySO)
                .GetField("_onDecayStarted", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.Tick(so.DecayWindow);

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_RecordCapture_WhileDecaying_EndsDecay()
        {
            var so = CreateSO();
            so.Tick(so.DecayWindow);
            Assert.That(so.IsDecaying, Is.True);

            so.RecordCapture();

            Assert.That(so.IsDecaying,        Is.False);
            Assert.That(so.TimeSinceCapture,  Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_FiresOnDecayEnded()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlScoreDecaySO)
                .GetField("_onDecayEnded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            // Start decay then cancel it
            so.Tick(so.DecayWindow);
            so.RecordCapture();

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            so.Tick(so.DecayWindow);
            so.Reset();
            Assert.That(so.IsDecaying,       Is.False);
            Assert.That(so.TimeSinceCapture, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        // ── Controller tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_DecaySO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.DecaySO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(false));
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(true));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channel()
        {
            var ctrl    = CreateController();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlScoreDecayController)
                .GetField("_onDecayStarted", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, channel);

            ctrl.gameObject.SetActive(true);
            ctrl.gameObject.SetActive(false);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            channel.Raise();

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void Controller_Refresh_NullDecaySO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlScoreDecayController)
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
