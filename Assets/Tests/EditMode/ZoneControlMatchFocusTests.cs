using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T431: <see cref="ZoneControlMatchFocusSO"/> and
    /// <see cref="ZoneControlMatchFocusController"/>.
    ///
    /// ZoneControlMatchFocusTests (12):
    ///   SO_FreshInstance_IsFocusLost_False                         x1
    ///   SO_FreshInstance_IdleTime_Zero                             x1
    ///   SO_Tick_BelowThreshold_FocusNotLost                        x1
    ///   SO_Tick_AboveThreshold_FocusLost                           x1
    ///   SO_Tick_FiresFocusLostEvent                                x1
    ///   SO_RecordCapture_ResetIdleTime                             x1
    ///   SO_RecordCapture_WhenFocusLost_RegainsAndFiresEvent        x1
    ///   SO_Tick_AfterFocusLost_NoFurtherTick                       x1
    ///   SO_Reset_ClearsAll                                         x1
    ///   SO_FocusTimeoutSeconds_DefaultPositive                     x1
    ///   Controller_FreshInstance_FocusSO_Null                      x1
    ///   Controller_Refresh_NullSO_HidesPanel                       x1
    /// </summary>
    public sealed class ZoneControlMatchFocusTests
    {
        private static ZoneControlMatchFocusSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlMatchFocusSO>();

        private static ZoneControlMatchFocusController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlMatchFocusController>();
        }

        [Test]
        public void SO_FreshInstance_IsFocusLost_False()
        {
            var so = CreateSO();
            Assert.That(so.IsFocusLost, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_IdleTime_Zero()
        {
            var so = CreateSO();
            Assert.That(so.IdleTime, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_BelowThreshold_FocusNotLost()
        {
            var so = CreateSO();
            so.Tick(so.FocusTimeoutSeconds * 0.5f);
            Assert.That(so.IsFocusLost, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_AboveThreshold_FocusLost()
        {
            var so = CreateSO();
            so.Tick(so.FocusTimeoutSeconds + 1f);
            Assert.That(so.IsFocusLost, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_FiresFocusLostEvent()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlMatchFocusSO)
                .GetField("_onFocusLost", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.Tick(so.FocusTimeoutSeconds + 1f);

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_RecordCapture_ResetIdleTime()
        {
            var so = CreateSO();
            so.Tick(3f);
            so.RecordCapture();
            Assert.That(so.IdleTime, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordCapture_WhenFocusLost_RegainsAndFiresEvent()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlMatchFocusSO)
                .GetField("_onFocusRegained", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            so.Tick(so.FocusTimeoutSeconds + 1f);
            Assert.That(so.IsFocusLost, Is.True);

            so.RecordCapture();
            Assert.That(so.IsFocusLost, Is.False);
            Assert.That(fired,          Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Tick_AfterFocusLost_NoFurtherTick()
        {
            var so = CreateSO();
            so.Tick(so.FocusTimeoutSeconds + 1f);
            float idleAfterLost = so.IdleTime;
            so.Tick(5f); // should not advance since focus is already lost
            Assert.That(so.IdleTime, Is.EqualTo(idleAfterLost));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            so.Tick(so.FocusTimeoutSeconds + 1f);
            so.Reset();
            Assert.That(so.IsFocusLost, Is.False);
            Assert.That(so.IdleTime,    Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FocusTimeoutSeconds_DefaultPositive()
        {
            var so = CreateSO();
            Assert.That(so.FocusTimeoutSeconds, Is.GreaterThan(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_FocusSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.FocusSO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlMatchFocusController)
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
