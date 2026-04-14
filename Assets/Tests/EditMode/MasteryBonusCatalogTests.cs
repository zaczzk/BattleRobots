using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T185:
    ///   <see cref="MasteryBonusCatalogSO"/> and
    ///   <see cref="MasteryBonusCatalogController"/>.
    ///
    /// MasteryBonusCatalogSOTests (8):
    ///   FreshInstance_CountIsZero ×1
    ///   Count_WithEntries_ReturnsCorrect ×1
    ///   IsActive_NullMastery_ReturnsFalse ×1
    ///   IsActive_TypeNotMastered_ReturnsFalse ×1
    ///   IsActive_TypeMastered_ReturnsTrue ×1
    ///   GetTotalMultiplier_NullMastery_ReturnsOne ×1
    ///   GetTotalMultiplier_NoActive_ReturnsOne ×1
    ///   GetTotalMultiplier_TwoActive_ReturnsProduct ×1
    ///
    /// MasteryBonusCatalogControllerTests (8):
    ///   FreshInstance_CatalogIsNull ×1
    ///   FreshInstance_MasteryIsNull ×1
    ///   OnEnable_NullRefs_DoesNotThrow ×1
    ///   OnDisable_NullRefs_DoesNotThrow ×1
    ///   Refresh_NullCatalog_HidesPanel ×1
    ///   Refresh_EmptyCatalog_ShowsPanel ×1
    ///   Refresh_ActiveEntry_SetsStatusLabel ×1
    ///   OnDisable_Unregisters ×1
    ///
    /// Total: 16 new EditMode tests.
    /// </summary>
    public class MasteryBonusCatalogTests
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

        private static MasteryBonusCatalogSO CreateCatalog(MasteryBonusEntry[] entries)
        {
            var so = ScriptableObject.CreateInstance<MasteryBonusCatalogSO>();
            SetField(so, "_entries", entries);
            return so;
        }

        private static DamageTypeMasteryConfig CreateConfig(float threshold = 100f)
        {
            var cfg = ScriptableObject.CreateInstance<DamageTypeMasteryConfig>();
            SetField(cfg, "_physicalThreshold", threshold);
            SetField(cfg, "_energyThreshold",   threshold);
            SetField(cfg, "_thermalThreshold",  threshold);
            SetField(cfg, "_shockThreshold",    threshold);
            return cfg;
        }

        private static DamageTypeMasterySO CreateMastery(DamageTypeMasteryConfig cfg = null)
        {
            var so = ScriptableObject.CreateInstance<DamageTypeMasterySO>();
            if (cfg != null) SetField(so, "_config", cfg);
            return so;
        }

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static MasteryBonusCatalogController CreateController()
        {
            var go = new GameObject("MasteryBonusCatalogCtrl_Test");
            return go.AddComponent<MasteryBonusCatalogController>();
        }

        // ══════════════════════════════════════════════════════════════════════
        // MasteryBonusCatalogSO Tests
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void Catalog_FreshInstance_CountIsZero()
        {
            var so = ScriptableObject.CreateInstance<MasteryBonusCatalogSO>();
            Assert.AreEqual(0, so.Count);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Catalog_Count_WithEntries_ReturnsCorrect()
        {
            var so = CreateCatalog(new[]
            {
                new MasteryBonusEntry { requiredType = DamageType.Physical, label = "A", bonusMultiplier = 1.1f },
                new MasteryBonusEntry { requiredType = DamageType.Energy,   label = "B", bonusMultiplier = 1.2f },
            });
            Assert.AreEqual(2, so.Count);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Catalog_IsActive_NullMastery_ReturnsFalse()
        {
            var entry = new MasteryBonusEntry { requiredType = DamageType.Physical, bonusMultiplier = 1.1f };
            var so    = CreateCatalog(new[] { entry });
            Assert.IsFalse(so.IsActive(entry, null),
                "IsActive must return false when mastery SO is null.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Catalog_IsActive_TypeNotMastered_ReturnsFalse()
        {
            var entry   = new MasteryBonusEntry { requiredType = DamageType.Physical, bonusMultiplier = 1.1f };
            var so      = CreateCatalog(new[] { entry });
            var mastery = CreateMastery(); // no config → never mastered
            Assert.IsFalse(so.IsActive(entry, mastery),
                "IsActive must return false when the required type is not mastered.");
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(mastery);
        }

        [Test]
        public void Catalog_IsActive_TypeMastered_ReturnsTrue()
        {
            var cfg     = CreateConfig(50f);
            var mastery = CreateMastery(cfg);
            mastery.AddDealt(50f, DamageType.Energy); // masters Energy

            var entry = new MasteryBonusEntry { requiredType = DamageType.Energy, bonusMultiplier = 1.2f };
            var so    = CreateCatalog(new[] { entry });

            Assert.IsTrue(so.IsActive(entry, mastery),
                "IsActive must return true when the required type is mastered.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(mastery);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void Catalog_GetTotalMultiplier_NullMastery_ReturnsOne()
        {
            var so = CreateCatalog(new[]
            {
                new MasteryBonusEntry { requiredType = DamageType.Physical, bonusMultiplier = 1.5f },
            });
            Assert.AreEqual(1f, so.GetTotalMultiplier(null), 0.001f,
                "GetTotalMultiplier with null mastery must return 1.0 (no active bonuses).");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Catalog_GetTotalMultiplier_NoActive_ReturnsOne()
        {
            var mastery = CreateMastery(); // no config → nothing mastered
            var so      = CreateCatalog(new[]
            {
                new MasteryBonusEntry { requiredType = DamageType.Shock, bonusMultiplier = 2f },
            });
            Assert.AreEqual(1f, so.GetTotalMultiplier(mastery), 0.001f,
                "GetTotalMultiplier with no active entries must return 1.0.");
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(mastery);
        }

        [Test]
        public void Catalog_GetTotalMultiplier_TwoActive_ReturnsProduct()
        {
            var cfg     = CreateConfig(10f);
            var mastery = CreateMastery(cfg);
            mastery.AddDealt(10f, DamageType.Physical);
            mastery.AddDealt(10f, DamageType.Energy);

            var so = CreateCatalog(new[]
            {
                new MasteryBonusEntry { requiredType = DamageType.Physical, bonusMultiplier = 1.25f },
                new MasteryBonusEntry { requiredType = DamageType.Energy,   bonusMultiplier = 1.5f  },
            });

            float expected = 1.25f * 1.5f;
            Assert.AreEqual(expected, so.GetTotalMultiplier(mastery), 0.001f,
                "GetTotalMultiplier must return the product of all active bonus multipliers.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(mastery);
            Object.DestroyImmediate(cfg);
        }

        // ══════════════════════════════════════════════════════════════════════
        // MasteryBonusCatalogController Tests
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void Ctrl_FreshInstance_CatalogIsNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.Catalog);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_FreshInstance_MasteryIsNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.Mastery);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnEnable"));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_OnDisable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnDisable"));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_Refresh_NullCatalog_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("Panel");
            panel.SetActive(true);
            SetField(ctrl, "_panel", panel);
            // _catalog is null

            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Refresh must hide the panel when catalog is null.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Ctrl_Refresh_EmptyCatalog_ShowsPanel()
        {
            var ctrl    = CreateController();
            var panel   = new GameObject("Panel");
            panel.SetActive(false);
            var catalog = CreateCatalog(new MasteryBonusEntry[0]);
            SetField(ctrl, "_catalog", catalog);
            SetField(ctrl, "_panel",   panel);

            ctrl.Refresh();

            Assert.IsTrue(panel.activeSelf,
                "Refresh with an empty catalog must show the panel.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void Ctrl_Refresh_ActiveEntry_SetsStatusActiveLabel()
        {
            var ctrl = CreateController();

            var cfg     = CreateConfig(10f);
            var mastery = CreateMastery(cfg);
            mastery.AddDealt(10f, DamageType.Thermal); // masters Thermal

            var catalog = CreateCatalog(new[]
            {
                new MasteryBonusEntry { requiredType = DamageType.Thermal, label = "Thermal Master", bonusMultiplier = 1.3f },
            });

            // Create row prefab with two Text components.
            var prefab       = new GameObject("RowPrefab");
            var labelChild   = new GameObject("Label");
            var statusChild  = new GameObject("Status");
            labelChild.transform.SetParent(prefab.transform);
            statusChild.transform.SetParent(prefab.transform);
            labelChild.AddComponent<UnityEngine.UI.Text>();
            statusChild.AddComponent<UnityEngine.UI.Text>();

            var container = new GameObject("Container");

            SetField(ctrl, "_catalog",       catalog);
            SetField(ctrl, "_mastery",       mastery);
            SetField(ctrl, "_listContainer", container.transform);
            SetField(ctrl, "_rowPrefab",     prefab);

            ctrl.Refresh();

            // Check that at least one child was instantiated in the container.
            Assert.Greater(container.transform.childCount, 0,
                "Refresh must instantiate at least one row for the active entry.");

            var row   = container.transform.GetChild(0).gameObject;
            var texts = row.GetComponentsInChildren<UnityEngine.UI.Text>(true);
            Assert.Greater(texts.Length, 1,
                "Each row must have at least 2 Text children.");
            Assert.AreEqual("Active", texts[1].text,
                "The status text must read 'Active' for a mastered type.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(container);
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(mastery);
            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void Ctrl_OnDisable_Unregisters()
        {
            var ctrl = CreateController();
            var ch   = CreateEvent();
            SetField(ctrl, "_onMasteryUnlocked", ch);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            int callCount = 0;
            ch.RegisterCallback(() => callCount++);
            InvokePrivate(ctrl, "OnDisable");
            ch.Raise();

            Assert.AreEqual(1, callCount,
                "After OnDisable the controller's handler must be unregistered.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(ch);
        }
    }
}
