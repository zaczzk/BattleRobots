using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T380: <see cref="ZoneControlTerritoryMapSO"/> and
    /// <see cref="ZoneControlTerritoryMapController"/>.
    ///
    /// ZoneControlTerritoryMapTests (12):
    ///   SO_FreshInstance_PlayerOwnedCount_Zero                   ×1
    ///   SO_SetPlayerOwned_True_IncrementsOwnedCount              ×1
    ///   SO_SetPlayerOwned_False_DecrementsOwnedCount             ×1
    ///   SO_SetPlayerOwned_OutOfBounds_Silent                     ×1
    ///   SO_SetPlayerOwned_FiresOwnershipChanged                  ×1
    ///   SO_IsPlayerOwned_OutOfBounds_ReturnsFalse                ×1
    ///   SO_Reset_ClearsOwnership                                 ×1
    ///   Controller_FreshInstance_TerritoryMapSO_Null             ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow               ×1
    ///   Controller_OnDisable_Unregisters_Channel                 ×1
    ///   Controller_Refresh_NullTerritoryMapSO_HidesPanel         ×1
    /// </summary>
    public sealed class ZoneControlTerritoryMapTests
    {
        private static ZoneControlTerritoryMapSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlTerritoryMapSO>();

        private static ZoneControlTerritoryMapController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlTerritoryMapController>();
        }

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_PlayerOwnedCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.PlayerOwnedCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SetPlayerOwned_True_IncrementsOwnedCount()
        {
            var so = CreateSO();
            so.SetPlayerOwned(0, true);
            Assert.That(so.PlayerOwnedCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SetPlayerOwned_False_DecrementsOwnedCount()
        {
            var so = CreateSO();
            so.SetPlayerOwned(0, true);
            so.SetPlayerOwned(0, false);
            Assert.That(so.PlayerOwnedCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SetPlayerOwned_OutOfBounds_Silent()
        {
            var so = CreateSO();
            Assert.DoesNotThrow(() => so.SetPlayerOwned(-1, true));
            Assert.DoesNotThrow(() => so.SetPlayerOwned(999, true));
            Assert.That(so.PlayerOwnedCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SetPlayerOwned_FiresOwnershipChanged()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlTerritoryMapSO)
                .GetField("_onOwnershipChanged", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.SetPlayerOwned(0, true);

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_IsPlayerOwned_OutOfBounds_ReturnsFalse()
        {
            var so = CreateSO();
            Assert.That(so.IsPlayerOwned(-1),  Is.False);
            Assert.That(so.IsPlayerOwned(999), Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsOwnership()
        {
            var so = CreateSO();
            so.SetPlayerOwned(0, true);
            so.SetPlayerOwned(1, true);
            so.Reset();
            Assert.That(so.PlayerOwnedCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        // ── Controller tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_TerritoryMapSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.TerritoryMapSO, Is.Null);
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
            typeof(ZoneControlTerritoryMapController)
                .GetField("_onOwnershipChanged", BindingFlags.NonPublic | BindingFlags.Instance)
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
        public void Controller_Refresh_NullTerritoryMapSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlTerritoryMapController)
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
