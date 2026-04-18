using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T402: <see cref="ZoneControlZoneHeatshieldSO"/> and
    /// <see cref="ZoneControlZoneHeatshieldController"/>.
    ///
    /// ZoneControlZoneHeatshieldTests (12):
    ///   SO_FreshInstance_IsActive_False                  x1
    ///   SO_FreshInstance_CurrentShield_Zero              x1
    ///   SO_Activate_SetsIsActiveAndMaxShield             x1
    ///   SO_Activate_Idempotent                           x1
    ///   SO_Tick_WhileActive_DecreasesShield              x1
    ///   SO_Tick_WhileInactive_NoChange                   x1
    ///   SO_Tick_DepletesWhenExhausted                    x1
    ///   SO_Reset_ClearsAll                               x1
    ///   SO_ShieldProgress_IsZeroWhenInactive             x1
    ///   SO_ShieldProgress_IsOneOnActivate                x1
    ///   Controller_FreshInstance_HeatshieldSO_Null       x1
    ///   Controller_Refresh_NullSO_HidesPanel             x1
    /// </summary>
    public sealed class ZoneControlZoneHeatshieldTests
    {
        private static ZoneControlZoneHeatshieldSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlZoneHeatshieldSO>();

        private static ZoneControlZoneHeatshieldController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlZoneHeatshieldController>();
        }

        [Test]
        public void SO_FreshInstance_IsActive_False()
        {
            var so = CreateSO();
            Assert.That(so.IsActive, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_CurrentShield_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CurrentShield, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Activate_SetsIsActiveAndMaxShield()
        {
            var so = CreateSO();
            so.Activate();
            Assert.That(so.IsActive,      Is.True);
            Assert.That(so.CurrentShield, Is.EqualTo(so.MaxShield));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Activate_Idempotent()
        {
            var so = CreateSO();
            so.Activate();
            so.Tick(1f);
            float partialShield = so.CurrentShield;
            so.Activate();
            Assert.That(so.CurrentShield, Is.EqualTo(partialShield).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_WhileActive_DecreasesShield()
        {
            var so = CreateSO();
            so.Activate();
            float before = so.CurrentShield;
            so.Tick(1f);
            Assert.That(so.CurrentShield, Is.LessThan(before));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_WhileInactive_NoChange()
        {
            var so     = CreateSO();
            float before = so.CurrentShield;
            so.Tick(5f);
            Assert.That(so.CurrentShield, Is.EqualTo(before));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_DepletesWhenExhausted()
        {
            var so = CreateSO();
            so.Activate();
            so.Tick(10000f);
            Assert.That(so.IsActive,      Is.False);
            Assert.That(so.CurrentShield, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            so.Activate();
            so.Tick(1f);
            so.Reset();
            Assert.That(so.IsActive,      Is.False);
            Assert.That(so.CurrentShield, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ShieldProgress_IsZeroWhenInactive()
        {
            var so = CreateSO();
            Assert.That(so.ShieldProgress, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ShieldProgress_IsOneOnActivate()
        {
            var so = CreateSO();
            so.Activate();
            Assert.That(so.ShieldProgress, Is.EqualTo(1f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_HeatshieldSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.HeatshieldSO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlZoneHeatshieldController)
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
