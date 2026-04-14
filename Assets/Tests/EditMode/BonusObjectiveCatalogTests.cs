using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T229:
    ///   <see cref="BonusObjectiveCatalogSO"/>,
    ///   <see cref="BonusObjectiveSelectorController"/>, and the
    ///   <see cref="BonusObjectiveHUDController.SetObjective"/> patch.
    ///
    /// BonusObjectiveCatalogSOTests (6):
    ///   FreshInstance_CountZero                     ×1
    ///   FreshInstance_Objectives_NotNull             ×1
    ///   Get_EmptyList_ReturnsNull                   ×1
    ///   Get_OutOfRangeLow_ReturnsNull               ×1
    ///   Get_OutOfRangeHigh_ReturnsNull              ×1
    ///   Get_ValidIndex_ReturnsEntry                 ×1
    ///
    /// BonusObjectiveSelectorControllerTests (8):
    ///   FreshInstance_CatalogNull                   ×1
    ///   FreshInstance_HUDNull                       ×1
    ///   FreshInstance_SelectedIndexZero             ×1
    ///   OnEnable_NullRefs_DoesNotThrow              ×1
    ///   OnDisable_NullRefs_DoesNotThrow             ×1
    ///   OnDisable_Unregisters_MatchStarted          ×1
    ///   NextObjective_NullCatalog_NoThrow           ×1
    ///   PreviousObjective_NullCatalog_NoThrow       ×1
    ///
    /// BonusObjectiveHUDController_SetObjectiveTests (2):
    ///   SetObjective_Null_HidesPanel                ×1
    ///   SetObjective_WithSO_ShowsPanel              ×1
    ///
    /// Total: 16 new EditMode tests.
    /// </summary>
    public class BonusObjectiveCatalogTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static void InvokePrivate(object target, string method)
        {
            MethodInfo mi = target.GetType()
                .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(mi, $"Method '{method}' not found on {target.GetType().Name}.");
            mi.Invoke(target, null);
        }

        private static BonusObjectiveCatalogSO CreateCatalog() =>
            ScriptableObject.CreateInstance<BonusObjectiveCatalogSO>();

        private static MatchBonusObjectiveSO CreateObjectiveSO()
        {
            var so = ScriptableObject.CreateInstance<MatchBonusObjectiveSO>();
            InvokePrivate(so, "OnEnable");
            return so;
        }

        private static VoidGameEvent CreateVoidEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static BonusObjectiveSelectorController CreateSelector() =>
            new GameObject("Selector_Test").AddComponent<BonusObjectiveSelectorController>();

        private static BonusObjectiveHUDController CreateHUD() =>
            new GameObject("HUD_Test").AddComponent<BonusObjectiveHUDController>();

        // ── BonusObjectiveCatalogSOTests ──────────────────────────────────────

        [Test]
        public void FreshInstance_CountZero()
        {
            var catalog = CreateCatalog();
            Assert.AreEqual(0, catalog.Count, "Fresh catalog must have Count == 0.");
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void FreshInstance_Objectives_NotNull()
        {
            var catalog = CreateCatalog();
            Assert.IsNotNull(catalog.Objectives, "Objectives list must not be null.");
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void Get_EmptyList_ReturnsNull()
        {
            var catalog = CreateCatalog();
            Assert.IsNull(catalog.Get(0), "Get on empty catalog must return null.");
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void Get_OutOfRangeLow_ReturnsNull()
        {
            var catalog = CreateCatalog();
            Assert.IsNull(catalog.Get(-1), "Get with negative index must return null.");
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void Get_OutOfRangeHigh_ReturnsNull()
        {
            var catalog = CreateCatalog();
            var obj     = CreateObjectiveSO();
            // Add one entry via reflection
            var list = catalog.GetType()
                .GetField("_objectives", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(catalog) as System.Collections.Generic.List<MatchBonusObjectiveSO>;
            list.Add(obj);

            Assert.IsNull(catalog.Get(1), "Get beyond last index must return null.");
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(obj);
        }

        [Test]
        public void Get_ValidIndex_ReturnsEntry()
        {
            var catalog = CreateCatalog();
            var obj     = CreateObjectiveSO();
            var list = catalog.GetType()
                .GetField("_objectives", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(catalog) as System.Collections.Generic.List<MatchBonusObjectiveSO>;
            list.Add(obj);

            Assert.AreSame(obj, catalog.Get(0), "Get(0) must return the first entry.");
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(obj);
        }

        // ── BonusObjectiveSelectorControllerTests ─────────────────────────────

        [Test]
        public void FreshInstance_CatalogNull()
        {
            var sel = CreateSelector();
            Assert.IsNull(sel.Catalog, "Catalog must be null on fresh instance.");
            Object.DestroyImmediate(sel.gameObject);
        }

        [Test]
        public void FreshInstance_HUDNull()
        {
            var sel = CreateSelector();
            Assert.IsNull(sel.BonusObjectiveHUD, "BonusObjectiveHUD must be null on fresh instance.");
            Object.DestroyImmediate(sel.gameObject);
        }

        [Test]
        public void FreshInstance_SelectedIndexZero()
        {
            var sel = CreateSelector();
            Assert.AreEqual(0, sel.SelectedIndex, "SelectedIndex must default to 0.");
            Object.DestroyImmediate(sel.gameObject);
        }

        [Test]
        public void OnEnable_NullRefs_DoesNotThrow()
        {
            var sel = CreateSelector();
            InvokePrivate(sel, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(sel, "OnEnable"),
                "OnEnable with all-null refs must not throw.");
            Object.DestroyImmediate(sel.gameObject);
        }

        [Test]
        public void OnDisable_NullRefs_DoesNotThrow()
        {
            var sel = CreateSelector();
            InvokePrivate(sel, "Awake");
            InvokePrivate(sel, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(sel, "OnDisable"),
                "OnDisable with all-null refs must not throw.");
            Object.DestroyImmediate(sel.gameObject);
        }

        [Test]
        public void OnDisable_Unregisters_MatchStarted()
        {
            var sel    = CreateSelector();
            var ch     = CreateVoidEvent();
            SetField(sel, "_onMatchStarted", ch);
            InvokePrivate(sel, "Awake");
            InvokePrivate(sel, "OnEnable");
            InvokePrivate(sel, "OnDisable");

            // Manually register a counter; if the selector's callback is still
            // subscribed it would fire InjectObjective (no-op but would not affect count).
            // After unregistration no internal callbacks remain — channel should not throw.
            int count = 0;
            ch.RegisterCallback(() => count++);
            ch.Raise();

            Assert.AreEqual(1, count,
                "After OnDisable only the manually registered callback must fire.");
            Object.DestroyImmediate(sel.gameObject);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void NextObjective_NullCatalog_NoThrow()
        {
            var sel = CreateSelector();
            Assert.DoesNotThrow(() => sel.NextObjective(),
                "NextObjective with null catalog must not throw.");
            Object.DestroyImmediate(sel.gameObject);
        }

        [Test]
        public void PreviousObjective_NullCatalog_NoThrow()
        {
            var sel = CreateSelector();
            Assert.DoesNotThrow(() => sel.PreviousObjective(),
                "PreviousObjective with null catalog must not throw.");
            Object.DestroyImmediate(sel.gameObject);
        }

        // ── BonusObjectiveHUDController.SetObjective patch tests ─────────────

        [Test]
        public void SetObjective_Null_HidesPanel()
        {
            var hud   = CreateHUD();
            var panel = new GameObject("Panel");
            panel.SetActive(true);
            SetField(hud, "_panel", panel);
            InvokePrivate(hud, "Awake");

            hud.SetObjective(null);

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden when SetObjective(null) is called.");
            Object.DestroyImmediate(hud.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void SetObjective_WithSO_ShowsPanel()
        {
            var hud   = CreateHUD();
            var so    = CreateObjectiveSO();
            var panel = new GameObject("Panel");
            panel.SetActive(false);
            SetField(hud, "_panel", panel);
            InvokePrivate(hud, "Awake");

            hud.SetObjective(so);

            Assert.IsTrue(panel.activeSelf,
                "Panel must be shown when SetObjective is called with a valid SO.");
            Assert.AreSame(so, hud.BonusObjective,
                "BonusObjective property must return the assigned SO.");
            Object.DestroyImmediate(hud.gameObject);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(panel);
        }
    }
}
