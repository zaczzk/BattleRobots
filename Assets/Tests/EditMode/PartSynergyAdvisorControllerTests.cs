using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T215:
    ///   <see cref="PartSynergyAdvisorController"/>.
    ///
    /// PartSynergyAdvisorControllerTests (14):
    ///   FreshInstance_SynergyConfigNull                                    ×1
    ///   FreshInstance_CatalogNull                                          ×1
    ///   FreshInstance_PlayerLoadoutNull                                    ×1
    ///   OnEnable_NullRefs_DoesNotThrow                                     ×1
    ///   OnDisable_NullRefs_DoesNotThrow                                    ×1
    ///   OnDisable_Unregisters                                              ×1
    ///   Refresh_NullSynergyConfig_NoThrow                                  ×1
    ///   Refresh_NullSynergyConfig_CountLabelZero                           ×1
    ///   Refresh_NullLoadout_NoActiveSynergies_ShowsNoSynergiesLabel        ×1
    ///   Refresh_ZeroActiveSynergies_ShowsNoSynergiesLabel                  ×1
    ///   Refresh_ActiveCountLabel_ShowsActiveCount                          ×1
    ///   Refresh_TotalCountLabel_ShowsTotalCount                            ×1
    ///   Refresh_NullActiveContainer_DoesNotThrow                           ×1
    ///   Refresh_NullInactiveContainer_DoesNotThrow                         ×1
    ///
    /// Total: 14 new EditMode tests.
    /// </summary>
    public class PartSynergyAdvisorControllerTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void InvokePrivate(object target, string method)
        {
            MethodInfo mi = target.GetType()
                .GetMethod(method,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(mi, $"Method '{method}' not found on {target.GetType().Name}.");
            mi.Invoke(target, null);
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static PartSynergyAdvisorController CreateController() =>
            new GameObject("PartSynergyAdvisor_Test")
                .AddComponent<PartSynergyAdvisorController>();

        private static Text AddText(GameObject parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent.transform);
            return child.AddComponent<Text>();
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_SynergyConfigNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.SynergyConfig);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void FreshInstance_CatalogNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.Catalog);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void FreshInstance_PlayerLoadoutNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.PlayerLoadout);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnEnable"));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnDisable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnDisable"));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnDisable_Unregisters()
        {
            var ctrl = CreateController();
            var ch   = CreateEvent();
            SetField(ctrl, "_onLoadoutChanged", ch);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            int count = 0;
            ch.RegisterCallback(() => count++);
            InvokePrivate(ctrl, "OnDisable");
            ch.Raise();

            Assert.AreEqual(1, count, "After OnDisable only manually registered callback fires.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void Refresh_NullSynergyConfig_NoThrow()
        {
            var ctrl = CreateController();
            // All optional refs null
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => ctrl.Refresh());
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Refresh_NullSynergyConfig_CountLabelZero()
        {
            var ctrl = CreateController();
            var lbl  = AddText(ctrl.gameObject, "activeCount");
            SetField(ctrl, "_activeSynergyCountLabel", lbl);
            // _synergyConfig left null
            InvokePrivate(ctrl, "Awake");
            ctrl.Refresh();

            Assert.AreEqual("Active: 0", lbl.text,
                "Null config must result in 'Active: 0' label.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Refresh_NullLoadout_NoActiveSynergies_ShowsNoSynergiesLabel()
        {
            var ctrl    = CreateController();
            var noLabel = new GameObject("noSynergies");
            noLabel.SetActive(false);
            SetField(ctrl, "_noActiveSynergiesLabel", noLabel);
            // _playerLoadout and _synergyConfig left null
            InvokePrivate(ctrl, "Awake");
            ctrl.Refresh();

            Assert.IsTrue(noLabel.activeSelf,
                "NoActiveSynergies label must show when loadout is null.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(noLabel);
        }

        [Test]
        public void Refresh_ZeroActiveSynergies_ShowsNoSynergiesLabel()
        {
            // Provide a PartSynergyConfig with no entries so GetActiveSynergies returns empty.
            var ctrl    = CreateController();
            var noLabel = new GameObject("noSynergies");
            noLabel.SetActive(false);
            var synCfg  = ScriptableObject.CreateInstance<PartSynergyConfig>();
            var catalog = ScriptableObject.CreateInstance<ShopCatalog>();
            var loadout = ScriptableObject.CreateInstance<PlayerLoadout>();
            loadout.SetLoadout(new List<string> { "some_part" });

            SetField(ctrl, "_noActiveSynergiesLabel", noLabel);
            SetField(ctrl, "_synergyConfig",  synCfg);
            SetField(ctrl, "_catalog",        catalog);
            SetField(ctrl, "_playerLoadout",  loadout);
            InvokePrivate(ctrl, "Awake");
            ctrl.Refresh();

            Assert.IsTrue(noLabel.activeSelf,
                "NoActiveSynergies label must show when zero synergies are active.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(noLabel);
            Object.DestroyImmediate(synCfg);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(loadout);
        }

        [Test]
        public void Refresh_ActiveCountLabel_ShowsActiveCount()
        {
            var ctrl    = CreateController();
            var lbl     = AddText(ctrl.gameObject, "activeCount");
            var synCfg  = ScriptableObject.CreateInstance<PartSynergyConfig>();
            var catalog = ScriptableObject.CreateInstance<ShopCatalog>();
            var loadout = ScriptableObject.CreateInstance<PlayerLoadout>();
            SetField(ctrl, "_activeSynergyCountLabel", lbl);
            SetField(ctrl, "_synergyConfig",  synCfg);
            SetField(ctrl, "_catalog",        catalog);
            SetField(ctrl, "_playerLoadout",  loadout);
            InvokePrivate(ctrl, "Awake");
            ctrl.Refresh();

            // Empty config → Active: 0
            Assert.AreEqual("Active: 0", lbl.text);
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(synCfg);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(loadout);
        }

        [Test]
        public void Refresh_TotalCountLabel_ShowsTotalCount()
        {
            var ctrl    = CreateController();
            var lbl     = AddText(ctrl.gameObject, "totalCount");
            var synCfg  = ScriptableObject.CreateInstance<PartSynergyConfig>();
            SetField(ctrl, "_totalSynergyCountLabel", lbl);
            SetField(ctrl, "_synergyConfig", synCfg);
            InvokePrivate(ctrl, "Awake");
            ctrl.Refresh();

            // Empty config → Total: 0
            Assert.AreEqual("Total: 0", lbl.text);
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(synCfg);
        }

        [Test]
        public void Refresh_NullActiveContainer_DoesNotThrow()
        {
            var ctrl = CreateController();
            // _activeContainer left null
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => ctrl.Refresh());
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Refresh_NullInactiveContainer_DoesNotThrow()
        {
            var ctrl = CreateController();
            // _inactiveContainer left null
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => ctrl.Refresh());
            Object.DestroyImmediate(ctrl.gameObject);
        }
    }
}
