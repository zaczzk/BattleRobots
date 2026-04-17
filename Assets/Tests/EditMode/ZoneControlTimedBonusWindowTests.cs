using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T401: <see cref="ZoneControlTimedBonusWindowSO"/> and
    /// <see cref="ZoneControlTimedBonusWindowController"/>.
    ///
    /// ZoneControlTimedBonusWindowTests (13):
    ///   SO_FreshInstance_IsActive_False                              x1
    ///   SO_FreshInstance_TotalWindowsOpened_Zero                     x1
    ///   SO_OpenWindow_SetsActive                                     x1
    ///   SO_OpenWindow_Idempotent_ReturnsFalseIfAlreadyActive         x1
    ///   SO_OpenWindow_IncrementsTotalWindowsOpened                   x1
    ///   SO_OpenWindow_FiresOnWindowOpened                            x1
    ///   SO_Tick_WhileActive_AdvancesProgress                         x1
    ///   SO_Tick_ExpiresWindowAndFiresOnWindowClosed                  x1
    ///   SO_ApplyMultiplier_WhileActive_ScalesReward                  x1
    ///   SO_ApplyMultiplier_WhileInactive_ReturnsBase                 x1
    ///   SO_Reset_ClearsAll                                           x1
    ///   Controller_FreshInstance_BonusWindowSO_Null                  x1
    ///   Controller_Refresh_NullSO_HidesPanel                         x1
    /// </summary>
    public sealed class ZoneControlTimedBonusWindowTests
    {
        private static ZoneControlTimedBonusWindowSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlTimedBonusWindowSO>();

        private static ZoneControlTimedBonusWindowController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlTimedBonusWindowController>();
        }

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_IsActive_False()
        {
            var so = CreateSO();
            Assert.That(so.IsActive, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_TotalWindowsOpened_Zero()
        {
            var so = CreateSO();
            Assert.That(so.TotalWindowsOpened, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OpenWindow_SetsActive()
        {
            var so = CreateSO();
            so.OpenWindow();
            Assert.That(so.IsActive, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OpenWindow_Idempotent_ReturnsFalseIfAlreadyActive()
        {
            var so = CreateSO();
            bool first  = so.OpenWindow();
            bool second = so.OpenWindow();
            Assert.That(first,  Is.True);
            Assert.That(second, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OpenWindow_IncrementsTotalWindowsOpened()
        {
            var so = CreateSO();
            so.OpenWindow();
            Assert.That(so.TotalWindowsOpened, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OpenWindow_FiresOnWindowOpened()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlTimedBonusWindowSO)
                .GetField("_onWindowOpened", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.OpenWindow();

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Tick_WhileActive_AdvancesProgress()
        {
            var so = CreateSO();
            so.OpenWindow();
            so.Tick(so.WindowDuration * 0.5f);
            Assert.That(so.WindowProgress, Is.GreaterThan(0f).And.LessThanOrEqualTo(1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_ExpiresWindowAndFiresOnWindowClosed()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlTimedBonusWindowSO)
                .GetField("_onWindowClosed", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.OpenWindow();
            so.Tick(so.WindowDuration + 1f);

            Assert.That(so.IsActive, Is.False);
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_ApplyMultiplier_WhileActive_ScalesReward()
        {
            var so = CreateSO();
            so.OpenWindow();
            int result = so.ApplyMultiplier(100);
            Assert.That(result, Is.GreaterThan(100));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ApplyMultiplier_WhileInactive_ReturnsBase()
        {
            var so     = CreateSO();
            int result = so.ApplyMultiplier(100);
            Assert.That(result, Is.EqualTo(100));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            so.OpenWindow();
            so.Tick(so.WindowDuration * 0.3f);
            so.Reset();
            Assert.That(so.IsActive,           Is.False);
            Assert.That(so.WindowProgress,     Is.EqualTo(0f).Within(0.001f));
            Assert.That(so.TotalWindowsOpened, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        // ── Controller tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_BonusWindowSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.BonusWindowSO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlTimedBonusWindowController)
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
