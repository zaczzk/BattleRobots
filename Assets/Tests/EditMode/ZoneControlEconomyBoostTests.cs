using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for <see cref="ZoneControlEconomyBoostSO"/> and
    /// <see cref="ZoneControlEconomyBoostController"/>.
    /// </summary>
    public sealed class ZoneControlEconomyBoostTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static ZoneControlEconomyBoostSO CreateBoostSO() =>
            ScriptableObject.CreateInstance<ZoneControlEconomyBoostSO>();

        private static ZoneControlEconomyBoostController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlEconomyBoostController>();
        }

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_IsActive_False()
        {
            var so = CreateBoostSO();
            Assert.That(so.IsActive, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_BoostProgress_Zero()
        {
            var so = CreateBoostSO();
            Assert.That(so.BoostProgress, Is.EqualTo(0f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ActivateBoost_SetsIsActive()
        {
            var so = CreateBoostSO();
            so.ActivateBoost();
            Assert.That(so.IsActive, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ActivateBoost_SetsRemainingTime_EqualsDuration()
        {
            var so = CreateBoostSO();
            so.ActivateBoost();
            Assert.That(so.RemainingTime, Is.EqualTo(so.BoostDuration).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_WhenInactive_DoesNothing()
        {
            var so = CreateBoostSO();
            so.Tick(10f);
            Assert.That(so.IsActive, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_ReducesRemainingTime()
        {
            var so = CreateBoostSO();
            so.ActivateBoost();
            float before = so.RemainingTime;
            so.Tick(5f);
            Assert.That(so.RemainingTime, Is.LessThan(before));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_OnExpiry_SetsIsActiveToFalse()
        {
            var so = CreateBoostSO();
            so.ActivateBoost();
            so.Tick(so.BoostDuration + 1f);
            Assert.That(so.IsActive, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_OnExpiry_FiresBoostExpired()
        {
            var so      = CreateBoostSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlEconomyBoostSO)
                .GetField("_onBoostExpired",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.ActivateBoost();
            so.Tick(so.BoostDuration + 1f);

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_ApplyBoost_WhenActive_MultipliesAmount()
        {
            var so = CreateBoostSO();
            so.ActivateBoost();
            int result = so.ApplyBoost(100);
            Assert.That(result, Is.EqualTo(Mathf.RoundToInt(100 * so.BoostMultiplier)));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ApplyBoost_WhenInactive_PassesThrough()
        {
            var so = CreateBoostSO();
            int result = so.ApplyBoost(100);
            Assert.That(result, Is.EqualTo(100));
            Object.DestroyImmediate(so);
        }

        // ── Controller tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(false));
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(true));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullBoostSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            panel.SetActive(true);
            typeof(ZoneControlEconomyBoostController)
                .GetField("_panel",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(ctrl, panel);

            ctrl.Refresh();
            Assert.That(panel.activeSelf, Is.False);

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }
    }
}
