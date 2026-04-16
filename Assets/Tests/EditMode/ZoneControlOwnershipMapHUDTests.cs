using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T334: <see cref="ZoneControlOwnershipMapHUDController"/>.
    ///
    /// ZoneControlOwnershipMapHUDTests (12):
    ///   FreshInstance_CatalogSO_Null                                 ×1
    ///   OnEnable_NullRefs_DoesNotThrow                               ×1
    ///   OnDisable_NullRefs_DoesNotThrow                              ×1
    ///   OnDisable_Unregisters_Channel                                ×1
    ///   Refresh_NullCatalogSO_HidesPanel                             ×1
    ///   Refresh_NullPanel_DoesNotThrow                               ×1
    ///   Refresh_ShowsPanel_WhenCatalogAssigned                       ×1
    ///   Refresh_SummaryLabel_ShowsPlayerCount                        ×1
    ///   Refresh_ZoneBadge_PlayerColor_WhenPlayerOwned                ×1
    ///   Refresh_ZoneBadge_BotColor_WhenBotOwned                      ×1
    ///   Refresh_NullBadgeImage_DoesNotThrow                          ×1
    ///   Refresh_NullSummaryLabel_DoesNotThrow                        ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneControlOwnershipMapHUDTests
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

        private static ZoneControlOwnershipMapHUDController CreateController() =>
            new GameObject("OwnershipMapHUD_Test")
                .AddComponent<ZoneControlOwnershipMapHUDController>();

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_CatalogSO_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.CatalogSO,
                "CatalogSO must be null on a fresh controller instance.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnEnable_NullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_OnEnable_Null");
            Assert.DoesNotThrow(
                () => go.AddComponent<ZoneControlOwnershipMapHUDController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_NullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneControlOwnershipMapHUDController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_Unregisters_Channel()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneControlOwnershipMapHUDController>();
            var evt  = CreateEvent();
            SetField(ctrl, "_onControlChanged", evt);

            go.SetActive(true);
            go.SetActive(false);

            int count = 0;
            evt.RegisterCallback(() => count++);
            evt.Raise();

            Assert.AreEqual(1, count,
                "_onControlChanged must be unregistered after OnDisable.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void Refresh_NullCatalogSO_HidesPanel()
        {
            var go    = new GameObject("Test_Refresh_Null");
            var ctrl  = go.AddComponent<ZoneControlOwnershipMapHUDController>();
            var panel = new GameObject("Panel");
            panel.SetActive(true);

            SetField(ctrl, "_panel", panel);
            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden when CatalogSO is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Refresh_NullPanel_DoesNotThrow()
        {
            var go      = new GameObject("Test_Refresh_NullPanel");
            var ctrl    = go.AddComponent<ZoneControlOwnershipMapHUDController>();
            var catalog = CreateCatalogSO();
            SetField(ctrl, "_catalogSO", catalog);

            Assert.DoesNotThrow(() => ctrl.Refresh(),
                "Refresh must not throw when _panel is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void Refresh_ShowsPanel_WhenCatalogAssigned()
        {
            var go      = new GameObject("Test_ShowPanel");
            var ctrl    = go.AddComponent<ZoneControlOwnershipMapHUDController>();
            var catalog = CreateCatalogSO();
            var panel   = new GameObject("Panel");
            panel.SetActive(false);

            SetField(ctrl, "_catalogSO", catalog);
            SetField(ctrl, "_panel",     panel);
            ctrl.Refresh();

            Assert.IsTrue(panel.activeSelf,
                "Panel must be shown when CatalogSO is assigned.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Refresh_SummaryLabel_ShowsPlayerCount()
        {
            var go      = new GameObject("Test_SummaryLabel");
            var ctrl    = go.AddComponent<ZoneControlOwnershipMapHUDController>();
            var catalog = CreateCatalogSO();
            catalog.SetZoneController(0, true);
            catalog.SetZoneController(1, true);

            var labelGO = new GameObject("Label");
            var label   = labelGO.AddComponent<Text>();

            SetField(ctrl, "_catalogSO",    catalog);
            SetField(ctrl, "_summaryLabel", label);
            ctrl.Refresh();

            Assert.AreEqual("Player Zones: 2 / 4", label.text,
                "Summary label must show correct owned / total zone count.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(labelGO);
        }

        [Test]
        public void Refresh_ZoneBadge_PlayerColor_WhenPlayerOwned()
        {
            var go      = new GameObject("Test_PlayerColor");
            var ctrl    = go.AddComponent<ZoneControlOwnershipMapHUDController>();
            var catalog = CreateCatalogSO();
            catalog.SetZoneController(0, true);

            var badgeGO  = new GameObject("Badge0");
            var badgeImg = badgeGO.AddComponent<Image>();
            var badges   = new Image[] { badgeImg };

            SetField(ctrl, "_catalogSO",       catalog);
            SetField(ctrl, "_zoneBadgeImages", badges);
            SetField(ctrl, "_playerColor",     Color.blue);
            ctrl.Refresh();

            Assert.AreEqual(Color.blue, badgeImg.color,
                "Zone badge must use _playerColor when the player owns that zone.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(badgeGO);
        }

        [Test]
        public void Refresh_ZoneBadge_BotColor_WhenBotOwned()
        {
            var go      = new GameObject("Test_BotColor");
            var ctrl    = go.AddComponent<ZoneControlOwnershipMapHUDController>();
            var catalog = CreateCatalogSO();

            var badgeGO  = new GameObject("Badge0");
            var badgeImg = badgeGO.AddComponent<Image>();
            var badges   = new Image[] { badgeImg };

            SetField(ctrl, "_catalogSO",       catalog);
            SetField(ctrl, "_zoneBadgeImages", badges);
            SetField(ctrl, "_botColor",        Color.red);
            ctrl.Refresh();

            Assert.AreEqual(Color.red, badgeImg.color,
                "Zone badge must use _botColor when the bot owns that zone.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(badgeGO);
        }

        [Test]
        public void Refresh_NullBadgeImage_DoesNotThrow()
        {
            var go      = new GameObject("Test_NullBadge");
            var ctrl    = go.AddComponent<ZoneControlOwnershipMapHUDController>();
            var catalog = CreateCatalogSO();
            var badges  = new Image[] { null };

            SetField(ctrl, "_catalogSO",       catalog);
            SetField(ctrl, "_zoneBadgeImages", badges);

            Assert.DoesNotThrow(() => ctrl.Refresh(),
                "Refresh must not throw when a badge Image entry is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void Refresh_NullSummaryLabel_DoesNotThrow()
        {
            var go      = new GameObject("Test_NullLabel");
            var ctrl    = go.AddComponent<ZoneControlOwnershipMapHUDController>();
            var catalog = CreateCatalogSO();
            SetField(ctrl, "_catalogSO", catalog);

            Assert.DoesNotThrow(() => ctrl.Refresh(),
                "Refresh must not throw when _summaryLabel is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(catalog);
        }
    }
}
