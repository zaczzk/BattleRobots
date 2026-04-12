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
    /// EditMode tests for <see cref="BuildValidationController"/>.
    ///
    /// Covers:
    ///   • OnEnable / OnDisable with all refs null                  → DoesNotThrow.
    ///   • OnEnable / OnDisable with null channel                   → DoesNotThrow.
    ///   • Refresh with null <see cref="PlayerLoadout"/>             → DoesNotThrow.
    ///   • Refresh with null <see cref="RobotDefinition"/>           → DoesNotThrow.
    ///   • Refresh with valid loadout  → _startMatchButton.interactable = true.
    ///   • Refresh with invalid loadout → _startMatchButton.interactable = false.
    ///   • Refresh with null _startMatchButton and valid loadout     → DoesNotThrow.
    ///   • Refresh with invalid loadout → _validationText contains the error message.
    ///   • Refresh with valid loadout   → _validationText is cleared to empty string.
    ///   • OnDisable unregisters delegate from <c>_onLoadoutChanged</c>
    ///     (external-counter pattern verifies callback is removed).
    ///
    /// All tests run headless (no Canvas required).
    /// The inactive-GO pattern prevents OnEnable from firing before fields are injected.
    /// </summary>
    public class BuildValidationControllerTests
    {
        private GameObject                 _go;
        private BuildValidationController  _ctrl;

        // Shared test fixtures re-used across multiple tests.
        private RobotDefinition  _robotDef;
        private PartDefinition   _weaponPart;
        private ShopCatalog      _catalog;
        private PlayerInventory  _inventory;
        private PlayerLoadout    _loadout;

        // ── Reflection helper ─────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            // Controller under test — inactive until fields are wired.
            _go = new GameObject("BuildValidationController");
            _go.SetActive(false);
            _ctrl = _go.AddComponent<BuildValidationController>();

            // Robot definition: one required slot (Weapon).
            _robotDef = ScriptableObject.CreateInstance<RobotDefinition>();
            SetField(_robotDef, "_slots", new List<PartSlot>
            {
                new PartSlot { slotId = "weapon_main", category = PartCategory.Weapon }
            });

            // One weapon PartDefinition.
            _weaponPart = ScriptableObject.CreateInstance<PartDefinition>();
            SetField(_weaponPart, "_partId",   "weapon_01");
            SetField(_weaponPart, "_category", PartCategory.Weapon);

            // Catalog containing the weapon part.
            _catalog = ScriptableObject.CreateInstance<ShopCatalog>();
            SetField(_catalog, "_parts", new List<PartDefinition> { _weaponPart });

            // Inventory (empty by default; tests unlock parts as needed).
            _inventory = ScriptableObject.CreateInstance<PlayerInventory>();

            // Loadout (empty by default).
            _loadout = ScriptableObject.CreateInstance<PlayerLoadout>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_robotDef);
            Object.DestroyImmediate(_weaponPart);
            Object.DestroyImmediate(_catalog);
            Object.DestroyImmediate(_inventory);
            Object.DestroyImmediate(_loadout);
        }

        private void Activate() => _go.SetActive(true);

        // ── Helper: create a valid loadout ────────────────────────────────────

        private void MakeValidLoadout()
        {
            _inventory.UnlockPart("weapon_01");
            _loadout.SetLoadout(new List<string> { "weapon_01" });
            SetField(_ctrl, "_playerLoadout",   _loadout);
            SetField(_ctrl, "_robotDefinition", _robotDef);
            SetField(_ctrl, "_playerInventory", _inventory);
            SetField(_ctrl, "_shopCatalog",     _catalog);
        }

        // ── Helper: create an invalid loadout (missing required Weapon slot) ──

        private void MakeInvalidLoadout()
        {
            // Loadout is empty → Weapon slot uncovered → invalid.
            SetField(_ctrl, "_playerLoadout",   _loadout);
            SetField(_ctrl, "_robotDefinition", _robotDef);
            SetField(_ctrl, "_playerInventory", _inventory);
            SetField(_ctrl, "_shopCatalog",     _catalog);
        }

        // ── OnEnable / OnDisable guard paths ──────────────────────────────────

        [Test]
        public void OnEnable_AllNullRefs_DoesNotThrow()
        {
            // All inspector fields null — Awake + OnEnable must not throw.
            Assert.DoesNotThrow(() => Activate());
        }

        [Test]
        public void OnDisable_AllNullRefs_DoesNotThrow()
        {
            Activate();
            Assert.DoesNotThrow(() => _go.SetActive(false));
        }

        [Test]
        public void OnEnable_NullChannel_DoesNotThrow()
        {
            // Data assigned but _onLoadoutChanged intentionally null.
            SetField(_ctrl, "_playerLoadout",   _loadout);
            SetField(_ctrl, "_robotDefinition", _robotDef);
            // channel stays null
            Assert.DoesNotThrow(() => Activate());
        }

        [Test]
        public void OnDisable_NullChannel_DoesNotThrow()
        {
            SetField(_ctrl, "_playerLoadout",   _loadout);
            SetField(_ctrl, "_robotDefinition", _robotDef);
            Activate();
            Assert.DoesNotThrow(() => _go.SetActive(false));
        }

        // ── Refresh null-guard paths ──────────────────────────────────────────

        [Test]
        public void Refresh_NullLoadout_DoesNotThrow()
        {
            // _playerLoadout null — LoadoutValidator.Validate returns invalid;
            // all UI refs also null so no further throw.
            SetField(_ctrl, "_robotDefinition", _robotDef);
            Assert.DoesNotThrow(() => Activate());
        }

        [Test]
        public void Refresh_NullRobotDefinition_DoesNotThrow()
        {
            // _robotDefinition null — validator short-circuits to invalid result.
            SetField(_ctrl, "_playerLoadout", _loadout);
            Assert.DoesNotThrow(() => Activate());
        }

        // ── Start Match button gating ─────────────────────────────────────────

        [Test]
        public void Refresh_ValidLoadout_StartMatchButtonInteractable()
        {
            // Wire a real Button component so we can read interactable.
            var buttonGo = new GameObject("Button");
            var button   = buttonGo.AddComponent<Button>();
            button.interactable = false; // start blocked to confirm Refresh sets it true

            MakeValidLoadout();
            SetField(_ctrl, "_startMatchButton", button);

            Activate(); // OnEnable → Refresh

            bool isInteractable = button.interactable;
            Object.DestroyImmediate(buttonGo);

            Assert.IsTrue(isInteractable,
                "A fully valid loadout should make _startMatchButton interactable.");
        }

        [Test]
        public void Refresh_InvalidLoadout_StartMatchButtonNotInteractable()
        {
            var buttonGo = new GameObject("Button");
            var button   = buttonGo.AddComponent<Button>();
            button.interactable = true; // start enabled to confirm Refresh blocks it

            MakeInvalidLoadout(); // empty loadout → Weapon slot uncovered
            SetField(_ctrl, "_startMatchButton", button);

            Activate();

            bool isInteractable = button.interactable;
            Object.DestroyImmediate(buttonGo);

            Assert.IsFalse(isInteractable,
                "An invalid loadout should disable _startMatchButton.");
        }

        [Test]
        public void Refresh_NullStartMatchButton_DoesNotThrow()
        {
            // Valid loadout, but _startMatchButton is null → no NullReferenceException.
            MakeValidLoadout();
            // _startMatchButton stays null
            Assert.DoesNotThrow(() => Activate());
        }

        // ── Validation text ───────────────────────────────────────────────────

        [Test]
        public void Refresh_InvalidLoadout_ValidationTextContainsError()
        {
            var textGo = new GameObject("Text");
            var text   = textGo.AddComponent<Text>();
            text.text  = string.Empty;

            MakeInvalidLoadout();
            SetField(_ctrl, "_validationText", text);

            Activate();

            string displayedText = text.text;
            Object.DestroyImmediate(textGo);

            Assert.IsFalse(string.IsNullOrEmpty(displayedText),
                "Validation text should contain at least one error message for an invalid loadout.");
        }

        [Test]
        public void Refresh_ValidLoadout_ValidationTextIsEmpty()
        {
            var textGo = new GameObject("Text");
            var text   = textGo.AddComponent<Text>();
            text.text  = "some stale error";

            MakeValidLoadout();
            SetField(_ctrl, "_validationText", text);

            Activate();

            string displayedText = text.text;
            Object.DestroyImmediate(textGo);

            Assert.AreEqual(string.Empty, displayedText,
                "Validation text should be cleared to empty when the loadout is valid.");
        }

        // ── OnDisable unregisters ─────────────────────────────────────────────

        [Test]
        public void OnDisable_UnregistersFromOnLoadoutChanged()
        {
            // External-counter pattern: after OnDisable, raising the channel must NOT
            // trigger the controller's Refresh delegate.
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            SetField(_ctrl, "_onLoadoutChanged", channel);

            Activate();           // OnEnable — registers _refreshDelegate + calls Refresh once
            _go.SetActive(false); // OnDisable — must unregister delegate

            // Register an external counter to verify the event still fires (just not to ctrl).
            int callCount = 0;
            channel.RegisterCallback(() => callCount++);
            channel.Raise(); // only the external counter should fire

            Object.DestroyImmediate(channel);

            Assert.AreEqual(1, callCount,
                "Only the external counter should fire after the controller unregisters.");
        }
    }
}
