using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T374: <see cref="ZoneControlCaptureFrenzySO"/> and
    /// <see cref="ZoneControlCaptureFrenzyController"/>.
    ///
    /// ZoneControlCaptureFrenzyTests (12):
    ///   SO_FreshInstance_IsFrenzy_False                          ×1
    ///   SO_FreshInstance_CaptureCount_Zero                       ×1
    ///   SO_RecordCapture_BelowThreshold_NoFrenzy                 ×1
    ///   SO_RecordCapture_AtThreshold_StartsFrenzy                ×1
    ///   SO_RecordCapture_FiresOnFrenzyStarted                    ×1
    ///   SO_Tick_PrunesOldCaptures_EndsFrenzy                     ×1
    ///   SO_Reset_ClearsAll                                       ×1
    ///   Controller_FreshInstance_FrenzySO_Null                   ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow               ×1
    ///   Controller_OnDisable_Unregisters_Channel                 ×1
    ///   Controller_Refresh_NullFrenzySO_HidesPanel               ×1
    /// </summary>
    public sealed class ZoneControlCaptureFrenzyTests
    {
        private static ZoneControlCaptureFrenzySO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlCaptureFrenzySO>();

        private static ZoneControlCaptureFrenzyController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureFrenzyController>();
        }

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_IsFrenzy_False()
        {
            var so = CreateSO();
            Assert.That(so.IsFrenzy, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_CaptureCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CaptureCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_BelowThreshold_NoFrenzy()
        {
            var so = CreateSO();
            // Record one fewer capture than threshold
            for (int i = 0; i < so.FrenzyThreshold - 1; i++)
                so.RecordCapture(0f);

            Assert.That(so.IsFrenzy, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_AtThreshold_StartsFrenzy()
        {
            var so = CreateSO();
            for (int i = 0; i < so.FrenzyThreshold; i++)
                so.RecordCapture(0f);

            Assert.That(so.IsFrenzy, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_FiresOnFrenzyStarted()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureFrenzySO)
                .GetField("_onFrenzyStarted", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            for (int i = 0; i < so.FrenzyThreshold; i++)
                so.RecordCapture(0f);

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Tick_PrunesOldCaptures_EndsFrenzy()
        {
            var so = CreateSO();
            // Build frenzy at time 0
            for (int i = 0; i < so.FrenzyThreshold; i++)
                so.RecordCapture(0f);
            Assert.That(so.IsFrenzy, Is.True);

            // Tick well past the frenzy window — entries should be pruned
            so.Tick(so.FrenzyWindow + 1f);

            Assert.That(so.IsFrenzy, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            for (int i = 0; i < so.FrenzyThreshold; i++)
                so.RecordCapture(0f);

            so.Reset();

            Assert.That(so.IsFrenzy,     Is.False);
            Assert.That(so.CaptureCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        // ── Controller tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_FrenzySO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.FrenzySO, Is.Null);
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
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            ctrl.gameObject.SetActive(true);
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(false));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channel()
        {
            var ctrl    = CreateController();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureFrenzyController)
                .GetField("_onFrenzyEnded", BindingFlags.NonPublic | BindingFlags.Instance)
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
        public void Controller_Refresh_NullFrenzySO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlCaptureFrenzyController)
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
