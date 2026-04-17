using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T399: <see cref="ZoneControlZoneValueSO"/> and
    /// <see cref="ZoneControlZoneValueController"/>.
    ///
    /// ZoneControlZoneValueTests (12):
    ///   SO_FreshInstance_CurrentValue_EqualsBaseValue                x1
    ///   SO_FreshInstance_IsActive_False                              x1
    ///   SO_StartAccruing_SetsActive                                  x1
    ///   SO_StartAccruing_Idempotent                                  x1
    ///   SO_Tick_WhileActive_IncreasesValue                           x1
    ///   SO_Tick_WhileInactive_NoChange                               x1
    ///   SO_Tick_CapsAtMaxValue                                       x1
    ///   SO_Harvest_ReturnsCurrentValueAndResets                      x1
    ///   SO_StopAccruing_ClearsActive                                 x1
    ///   SO_Reset_ClearsAll                                           x1
    ///   Controller_FreshInstance_ZoneValueSO_Null                    x1
    ///   Controller_Refresh_NullSO_HidesPanel                         x1
    /// </summary>
    public sealed class ZoneControlZoneValueTests
    {
        private static ZoneControlZoneValueSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlZoneValueSO>();

        private static ZoneControlZoneValueController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlZoneValueController>();
        }

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_CurrentValue_EqualsBaseValue()
        {
            var so = CreateSO();
            Assert.That(so.CurrentValue, Is.EqualTo(so.BaseValue));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_IsActive_False()
        {
            var so = CreateSO();
            Assert.That(so.IsActive, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StartAccruing_SetsActive()
        {
            var so = CreateSO();
            so.StartAccruing();
            Assert.That(so.IsActive, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StartAccruing_Idempotent()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlZoneValueSO)
                .GetField("_onValueChanged", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            so.StartAccruing();
            so.StartAccruing();
            Assert.That(so.IsActive, Is.True);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Tick_WhileActive_IncreasesValue()
        {
            var so = CreateSO();
            so.StartAccruing();
            int before = so.CurrentValue;
            so.Tick(10f);
            Assert.That(so.CurrentValue, Is.GreaterThan(before));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_WhileInactive_NoChange()
        {
            var so   = CreateSO();
            int before = so.CurrentValue;
            so.Tick(10f);
            Assert.That(so.CurrentValue, Is.EqualTo(before));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_CapsAtMaxValue()
        {
            var so = CreateSO();
            so.StartAccruing();
            so.Tick(10000f);
            Assert.That(so.CurrentValue, Is.EqualTo(so.MaxValue));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Harvest_ReturnsCurrentValueAndResets()
        {
            var so = CreateSO();
            so.StartAccruing();
            so.Tick(10f);
            int valueBeforeHarvest = so.CurrentValue;
            int harvested          = so.Harvest();
            Assert.That(harvested,       Is.EqualTo(valueBeforeHarvest));
            Assert.That(so.CurrentValue, Is.EqualTo(so.BaseValue));
            Assert.That(so.IsActive,     Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StopAccruing_ClearsActive()
        {
            var so = CreateSO();
            so.StartAccruing();
            so.StopAccruing();
            Assert.That(so.IsActive, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            so.StartAccruing();
            so.Tick(20f);
            so.Reset();
            Assert.That(so.IsActive,     Is.False);
            Assert.That(so.CurrentValue, Is.EqualTo(so.BaseValue));
            Object.DestroyImmediate(so);
        }

        // ── Controller tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_ZoneValueSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ZoneValueSO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlZoneValueController)
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
