using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T405: <see cref="ZoneControlCaptureCooldownSO"/> and
    /// <see cref="ZoneControlCaptureCooldownController"/>.
    ///
    /// ZoneControlCaptureCooldownTests (12):
    ///   SO_FreshInstance_IsOnCooldown_False              x1
    ///   SO_FreshInstance_RemainingTime_Zero              x1
    ///   SO_StartCooldown_SetsIsOnCooldown                x1
    ///   SO_StartCooldown_Idempotent_ReturnsFalse         x1
    ///   SO_Tick_WhileOnCooldown_DecreasesRemaining       x1
    ///   SO_Tick_WhileNotOnCooldown_NoChange              x1
    ///   SO_Tick_ExpiresOnDepletion                       x1
    ///   SO_Reset_ClearsAll                               x1
    ///   SO_CooldownProgress_ZeroWhenNotOnCooldown        x1
    ///   SO_CooldownProgress_UpdatesWhileOnCooldown       x1
    ///   Controller_FreshInstance_CooldownSO_Null         x1
    ///   Controller_Refresh_NullSO_HidesPanel             x1
    /// </summary>
    public sealed class ZoneControlCaptureCooldownTests
    {
        private static ZoneControlCaptureCooldownSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlCaptureCooldownSO>();

        private static ZoneControlCaptureCooldownController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureCooldownController>();
        }

        [Test]
        public void SO_FreshInstance_IsOnCooldown_False()
        {
            var so = CreateSO();
            Assert.That(so.IsOnCooldown, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_RemainingTime_Zero()
        {
            var so = CreateSO();
            Assert.That(so.RemainingTime, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StartCooldown_SetsIsOnCooldown()
        {
            var so = CreateSO();
            so.StartCooldown();
            Assert.That(so.IsOnCooldown,  Is.True);
            Assert.That(so.RemainingTime, Is.EqualTo(so.CooldownDuration).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StartCooldown_Idempotent_ReturnsFalse()
        {
            var so = CreateSO();
            so.StartCooldown();
            bool result = so.StartCooldown();
            Assert.That(result, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_WhileOnCooldown_DecreasesRemaining()
        {
            var so = CreateSO();
            so.StartCooldown();
            float before = so.RemainingTime;
            so.Tick(1f);
            Assert.That(so.RemainingTime, Is.LessThan(before));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_WhileNotOnCooldown_NoChange()
        {
            var so = CreateSO();
            so.Tick(5f);
            Assert.That(so.IsOnCooldown,  Is.False);
            Assert.That(so.RemainingTime, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_ExpiresOnDepletion()
        {
            var so = CreateSO();
            so.StartCooldown();
            so.Tick(10000f);
            Assert.That(so.IsOnCooldown,  Is.False);
            Assert.That(so.RemainingTime, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            so.StartCooldown();
            so.Reset();
            Assert.That(so.IsOnCooldown,   Is.False);
            Assert.That(so.RemainingTime,  Is.EqualTo(0f));
            Assert.That(so.TotalCooldowns, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_CooldownProgress_ZeroWhenNotOnCooldown()
        {
            var so = CreateSO();
            Assert.That(so.CooldownProgress, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_CooldownProgress_UpdatesWhileOnCooldown()
        {
            var so = CreateSO();
            so.StartCooldown();
            so.Tick(so.CooldownDuration * 0.5f);
            Assert.That(so.CooldownProgress, Is.GreaterThan(0f).And.LessThan(1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_CooldownSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.CooldownSO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlCaptureCooldownController)
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
