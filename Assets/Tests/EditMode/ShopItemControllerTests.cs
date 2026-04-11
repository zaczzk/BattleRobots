using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="ShopItemController"/>.
    ///
    /// Covers:
    ///   • <see cref="ShopItemController.Setup"/> null-safety:
    ///       null part → early return (no throw even when manager is null);
    ///       valid part with null manager → proceeds to set labels (all null refs) then
    ///         calls Refresh() which early-returns on null manager (no throw);
    ///       valid part + valid manager + all UI refs null → no throw.
    ///   • <see cref="ShopItemController.Refresh"/> null-safety:
    ///       both internal refs null (before any Setup call) → early return;
    ///       only part ref null → early return;
    ///       only manager ref null → early return.
    ///   • <see cref="ShopItemController.OnBuyClicked"/> null-safety:
    ///       null manager → no throw;
    ///       null part → no throw;
    ///       both null → no throw.
    ///   • Setup called twice → no throw (second call overwrites data cleanly).
    ///
    /// ShopItemController is a MonoBehaviour; a headless <see cref="GameObject"/>
    /// is created per test.  Private serialised fields are injected via reflection
    /// for the granular Refresh tests (null-part / null-manager variants).
    /// ShopManager is also a MonoBehaviour, so a separate headless GO is used;
    /// no fields are set on it, so <see cref="ShopManager.IsOwned"/> returns false
    /// (null inventory path — safe and backwards-compatible).
    /// </summary>
    public class ShopItemControllerTests
    {
        // ── Reflection helper ─────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        // ── Factory helpers ───────────────────────────────────────────────────

        /// <summary>Creates a fresh headless ShopItemController on a new inactive GO.</summary>
        private static (GameObject go, ShopItemController ctrl) MakeController()
        {
            var go   = new GameObject("ShopItemController");
            go.SetActive(false); // prevent Awake until we want it
            var ctrl = go.AddComponent<ShopItemController>();
            return (go, ctrl);
        }

        /// <summary>Creates a minimal PartDefinition with no fields set.</summary>
        private static PartDefinition MakePart()
            => ScriptableObject.CreateInstance<PartDefinition>();

        /// <summary>
        /// Creates a headless ShopManager (no refs set).
        /// IsOwned() returns false safely because _inventory is null.
        /// </summary>
        private static (GameObject go, ShopManager mgr) MakeShopManager()
        {
            var go  = new GameObject("ShopManager");
            var mgr = go.AddComponent<ShopManager>();
            return (go, mgr);
        }

        // ── Setup — null part ─────────────────────────────────────────────────

        [Test]
        public void Setup_NullPart_DoesNotThrow()
        {
            var (go, ctrl) = MakeController();
            go.SetActive(true); // Awake

            Assert.DoesNotThrow(() => ctrl.Setup(null, null),
                "Setup(null, null) must early-return without throwing.");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Setup_NullPart_ValidManager_DoesNotThrow()
        {
            var (ctrlGO, ctrl) = MakeController();
            var (mgrGO,  mgr)  = MakeShopManager();
            ctrlGO.SetActive(true);

            Assert.DoesNotThrow(() => ctrl.Setup(null, mgr),
                "Setup(null, mgr) must early-return without throwing.");

            Object.DestroyImmediate(ctrlGO);
            Object.DestroyImmediate(mgrGO);
        }

        // ── Setup — valid part, null / valid manager ──────────────────────────

        [Test]
        public void Setup_ValidPart_NullManager_AllUINull_DoesNotThrow()
        {
            var (go, ctrl) = MakeController();
            var part       = MakePart();
            go.SetActive(true);

            // Refresh() will guard on null _shopManager and return early.
            Assert.DoesNotThrow(() => ctrl.Setup(part, null),
                "Setup(part, null) with all UI refs null must not throw.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(part);
        }

        [Test]
        public void Setup_ValidPart_ValidManager_AllUINull_DoesNotThrow()
        {
            var (ctrlGO, ctrl) = MakeController();
            var (mgrGO,  mgr)  = MakeShopManager();
            var part           = MakePart();
            ctrlGO.SetActive(true);

            Assert.DoesNotThrow(() => ctrl.Setup(part, mgr),
                "Setup with valid part + manager but all UI refs null must not throw.");

            Object.DestroyImmediate(ctrlGO);
            Object.DestroyImmediate(mgrGO);
            Object.DestroyImmediate(part);
        }

        [Test]
        public void Setup_CalledTwice_DoesNotThrow()
        {
            var (ctrlGO, ctrl) = MakeController();
            var (mgrGO,  mgr)  = MakeShopManager();
            var partA = MakePart();
            var partB = MakePart();
            ctrlGO.SetActive(true);

            Assert.DoesNotThrow(() =>
            {
                ctrl.Setup(partA, mgr);
                ctrl.Setup(partB, mgr);
            }, "Calling Setup twice in succession must not throw.");

            Object.DestroyImmediate(ctrlGO);
            Object.DestroyImmediate(mgrGO);
            Object.DestroyImmediate(partA);
            Object.DestroyImmediate(partB);
        }

        // ── Refresh — internal ref guards ─────────────────────────────────────

        [Test]
        public void Refresh_BothInternalRefsNull_DoesNotThrow()
        {
            // Before any Setup — _partDefinition and _shopManager are both null.
            var (go, ctrl) = MakeController();
            go.SetActive(true);

            Assert.DoesNotThrow(() => ctrl.Refresh(),
                "Refresh() before Setup (both private refs null) must early-return without throwing.");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Refresh_NullPartDef_DoesNotThrow()
        {
            var (ctrlGO, ctrl) = MakeController();
            var (mgrGO,  mgr)  = MakeShopManager();

            // Inject manager but leave part null → guard must catch it.
            SetField(ctrl, "_shopManager", mgr);
            ctrlGO.SetActive(true);

            Assert.DoesNotThrow(() => ctrl.Refresh(),
                "Refresh() with null _partDefinition must early-return without throwing.");

            Object.DestroyImmediate(ctrlGO);
            Object.DestroyImmediate(mgrGO);
        }

        [Test]
        public void Refresh_NullManager_DoesNotThrow()
        {
            var (go, ctrl) = MakeController();
            var part       = MakePart();

            // Inject part but leave manager null → guard must catch it.
            SetField(ctrl, "_partDefinition", part);
            go.SetActive(true);

            Assert.DoesNotThrow(() => ctrl.Refresh(),
                "Refresh() with null _shopManager must early-return without throwing.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(part);
        }

        // ── OnBuyClicked — null guards ────────────────────────────────────────

        [Test]
        public void OnBuyClicked_NullManager_DoesNotThrow()
        {
            var (go, ctrl) = MakeController();
            var part       = MakePart();
            SetField(ctrl, "_partDefinition", part);
            // _shopManager left null
            go.SetActive(true);

            Assert.DoesNotThrow(() => ctrl.OnBuyClicked(),
                "OnBuyClicked() with null _shopManager must guard and not throw.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(part);
        }

        [Test]
        public void OnBuyClicked_NullPart_DoesNotThrow()
        {
            var (ctrlGO, ctrl) = MakeController();
            var (mgrGO,  mgr)  = MakeShopManager();
            SetField(ctrl, "_shopManager", mgr);
            // _partDefinition left null
            ctrlGO.SetActive(true);

            Assert.DoesNotThrow(() => ctrl.OnBuyClicked(),
                "OnBuyClicked() with null _partDefinition must guard and not throw.");

            Object.DestroyImmediate(ctrlGO);
            Object.DestroyImmediate(mgrGO);
        }

        [Test]
        public void OnBuyClicked_BothNull_DoesNotThrow()
        {
            var (go, ctrl) = MakeController();
            go.SetActive(true);

            Assert.DoesNotThrow(() => ctrl.OnBuyClicked(),
                "OnBuyClicked() with both refs null must guard and not throw.");

            Object.DestroyImmediate(go);
        }
    }
}
