using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T463: <see cref="ZoneControlZoneLockoutSO"/> and
    /// <see cref="ZoneControlZoneLockoutController"/>.
    ///
    /// ZoneControlZoneLockoutTests (12):
    ///   SO_FreshInstance_IsLockedOut_False                                           x1
    ///   SO_FreshInstance_TotalLockouts_Zero                                          x1
    ///   SO_StartLockout_SetsIsLockedOut                                              x1
    ///   SO_StartLockout_WhileActive_ReturnsFalse                                     x1
    ///   SO_StartLockout_IncementsTotalLockouts                                       x1
    ///   SO_Tick_BeforeExpiry_StillLocked                                             x1
    ///   SO_Tick_AfterExpiry_NotLocked                                                x1
    ///   SO_Tick_WhileNotLocked_NoOp                                                  x1
    ///   SO_LockoutProgress_WhenLocked_NonZero                                        x1
    ///   SO_Reset_ClearsAll                                                           x1
    ///   Controller_FreshInstance_LockoutSO_Null                                      x1
    ///   Controller_Refresh_NullSO_HidesPanel                                         x1
    /// </summary>
    public sealed class ZoneControlZoneLockoutTests
    {
        private static ZoneControlZoneLockoutSO CreateSO(float duration = 5f)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlZoneLockoutSO>();
            typeof(ZoneControlZoneLockoutSO)
                .GetField("_lockoutDuration", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, duration);
            so.Reset();
            return so;
        }

        private static ZoneControlZoneLockoutController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlZoneLockoutController>();
        }

        [Test]
        public void SO_FreshInstance_IsLockedOut_False()
        {
            var so = CreateSO();
            Assert.That(so.IsLockedOut, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_TotalLockouts_Zero()
        {
            var so = CreateSO();
            Assert.That(so.TotalLockouts, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StartLockout_SetsIsLockedOut()
        {
            var so = CreateSO();
            so.StartLockout();
            Assert.That(so.IsLockedOut, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StartLockout_WhileActive_ReturnsFalse()
        {
            var so = CreateSO();
            so.StartLockout();
            bool result = so.StartLockout();
            Assert.That(result, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StartLockout_IncrementsTotalLockouts()
        {
            var so = CreateSO();
            so.StartLockout();
            Assert.That(so.TotalLockouts, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_BeforeExpiry_StillLocked()
        {
            var so = CreateSO(duration: 10f);
            so.StartLockout();
            so.Tick(5f);
            Assert.That(so.IsLockedOut, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_AfterExpiry_NotLocked()
        {
            var so = CreateSO(duration: 5f);
            so.StartLockout();
            so.Tick(6f);
            Assert.That(so.IsLockedOut, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_WhileNotLocked_NoOp()
        {
            var so = CreateSO(duration: 5f);
            Assert.DoesNotThrow(() => so.Tick(10f));
            Assert.That(so.IsLockedOut, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_LockoutProgress_WhenLocked_NonZero()
        {
            var so = CreateSO(duration: 10f);
            so.StartLockout();
            so.Tick(5f);
            Assert.That(so.LockoutProgress, Is.GreaterThan(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(duration: 10f);
            so.StartLockout();
            so.Reset();
            Assert.That(so.IsLockedOut,   Is.False);
            Assert.That(so.TotalLockouts, Is.EqualTo(0));
            Assert.That(so.RemainingTime, Is.EqualTo(0f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_LockoutSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.LockoutSO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlZoneLockoutController)
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
