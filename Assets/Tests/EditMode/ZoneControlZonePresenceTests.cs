using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T331: <see cref="ZoneControlZoneControllerCatalogSO"/> and
    /// <see cref="ZoneControlZonePresenceController"/>.
    ///
    /// ZoneControlZonePresenceTests (12):
    ///   SO_FreshInstance_AllZones_BotOwned                        ×1
    ///   SO_SetZoneController_PlayerOwned_ReturnsTrue              ×1
    ///   SO_SetZoneController_BotOwned_ReturnsFalse                ×1
    ///   SO_SetZoneController_OutOfBounds_DoesNotThrow             ×1
    ///   SO_SetZoneController_FiresControlChanged                  ×1
    ///   SO_PlayerOwnedCount_TracksOwnership                       ×1
    ///   SO_Reset_ClearsAllOwnership                               ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                 ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow                ×1
    ///   Controller_OnDisable_Unregisters_Channel                  ×1
    ///   Controller_HandlePlayerZoneCaptured_SetsPlayerOwned       ×1
    ///   Controller_HandleMatchStarted_ResetsCatalog               ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneControlZonePresenceTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static ZoneControlZoneControllerCatalogSO CreateCatalogSO()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlZoneControllerCatalogSO>();
            so.Reset();
            return so;
        }

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_AllZones_BotOwned()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlZoneControllerCatalogSO>();
            // default zone count = 4
            for (int i = 0; i < so.ZoneCount; i++)
                Assert.IsFalse(so.GetZoneController(i),
                    $"Zone {i} must be bot-owned on a fresh instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SetZoneController_PlayerOwned_ReturnsTrue()
        {
            var so = CreateCatalogSO();
            so.SetZoneController(0, true);
            Assert.IsTrue(so.GetZoneController(0),
                "GetZoneController must return true after SetZoneController(0, true).");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SetZoneController_BotOwned_ReturnsFalse()
        {
            var so = CreateCatalogSO();
            so.SetZoneController(1, true);
            so.SetZoneController(1, false);
            Assert.IsFalse(so.GetZoneController(1),
                "GetZoneController must return false after SetZoneController(1, false).");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SetZoneController_OutOfBounds_DoesNotThrow()
        {
            var so = CreateCatalogSO();
            Assert.DoesNotThrow(() => so.SetZoneController(-1, true),
                "SetZoneController with negative index must not throw.");
            Assert.DoesNotThrow(() => so.SetZoneController(999, true),
                "SetZoneController with out-of-range index must not throw.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SetZoneController_FiresControlChanged()
        {
            var so  = CreateCatalogSO();
            var evt = CreateEvent();
            SetField(so, "_onControlChanged", evt);

            int fired = 0;
            evt.RegisterCallback(() => fired++);
            so.SetZoneController(0, true);

            Assert.AreEqual(1, fired,
                "_onControlChanged must fire when a zone changes owner.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_PlayerOwnedCount_TracksOwnership()
        {
            var so = CreateCatalogSO(); // 4 zones, all bot
            so.SetZoneController(0, true);
            so.SetZoneController(2, true);
            Assert.AreEqual(2, so.PlayerOwnedCount,
                "PlayerOwnedCount must equal the number of player-owned zones.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAllOwnership()
        {
            var so = CreateCatalogSO();
            so.SetZoneController(0, true);
            so.SetZoneController(1, true);
            so.Reset();
            Assert.AreEqual(0, so.PlayerOwnedCount,
                "PlayerOwnedCount must be 0 after Reset.");
            Object.DestroyImmediate(so);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_OnEnable_Null");
            Assert.DoesNotThrow(
                () => go.AddComponent<ZoneControlZonePresenceController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneControlZonePresenceController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channel()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneControlZonePresenceController>();
            var evt  = CreateEvent();
            SetField(ctrl, "_onMatchStarted", evt);

            go.SetActive(true);
            go.SetActive(false);

            int count = 0;
            evt.RegisterCallback(() => count++);
            evt.Raise();

            Assert.AreEqual(1, count,
                "_onMatchStarted must be unregistered after OnDisable.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void Controller_HandlePlayerZoneCaptured_SetsPlayerOwned()
        {
            var go      = new GameObject("Test_PlayerCapture");
            var ctrl    = go.AddComponent<ZoneControlZonePresenceController>();
            var catalog = CreateCatalogSO();
            SetField(ctrl, "_catalogSO", catalog);

            ctrl.HandlePlayerZoneCaptured(2);

            Assert.IsTrue(catalog.GetZoneController(2),
                "HandlePlayerZoneCaptured must mark the zone as player-owned.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void Controller_HandleMatchStarted_ResetsCatalog()
        {
            var go      = new GameObject("Test_MatchStarted");
            var ctrl    = go.AddComponent<ZoneControlZonePresenceController>();
            var catalog = CreateCatalogSO();
            SetField(ctrl, "_catalogSO", catalog);

            catalog.SetZoneController(0, true);
            catalog.SetZoneController(1, true);

            ctrl.HandleMatchStarted();

            Assert.AreEqual(0, catalog.PlayerOwnedCount,
                "HandleMatchStarted must reset all zone ownership.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(catalog);
        }
    }
}
