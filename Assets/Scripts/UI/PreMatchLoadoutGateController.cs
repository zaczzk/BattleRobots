using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Pre-match UI gate that runs a full <see cref="LoadoutValidator.Validate"/>
    /// (rules 1–6) every time the player's loadout changes and blocks the
    /// Start Match button until the loadout is valid.
    ///
    /// ── Data flow ────────────────────────────────────────────────────────────────
    ///   Awake     → caches _validateDelegate (zero-alloc after Awake).
    ///   OnEnable  → subscribes _onLoadoutChanged → Validate(); calls Validate().
    ///   Validate()→ calls LoadoutValidator.Validate(7-param) with all optional refs;
    ///               sets _startMatchButton.interactable = IsValid;
    ///               destroys old error rows and instantiates one row per error;
    ///               raises _onValidationChanged.
    ///   OnDisable → unsubscribes _onLoadoutChanged.
    ///
    /// ── Architecture rules ────────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   • DisallowMultipleComponent — one gate panel per pre-match canvas.
    ///   • All UI fields optional — assign only those present in the scene.
    ///   • No Update / FixedUpdate loop.
    ///   • Delegate cached in Awake; zero heap allocations after initialisation.
    ///
    /// Scene wiring:
    ///   _loadout         → shared PlayerLoadout SO.
    ///   _robotDef        → robot's RobotDefinition SO (defines required slots).
    ///   _inventory       → shared PlayerInventory SO (ownership check).
    ///   _catalog         → shared ShopCatalog SO (catalog membership + slot coverage).
    ///   _unlockConfig    → WeaponTypeUnlockConfig SO (prestige-gated weapon types).
    ///   _prestigeSystem  → shared PrestigeSystemSO (current prestige count for rule 6).
    ///   _weaponCatalog   → WeaponPartCatalogSO (maps part IDs to DamageType for rule 6).
    ///   _onLoadoutChanged→ same VoidGameEvent as PlayerLoadout._onLoadoutChanged.
    ///   _startMatchButton→ the button that launches the match (disabled when invalid).
    ///   _errorListContainer→ ScrollRect Content Transform for error rows.
    ///   _errorRowPrefab  → prefab with ≥ 1 Text child (receives the error message).
    ///   _gatePanel       → optional root panel; shown/hidden by callers as needed.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PreMatchLoadoutGateController : MonoBehaviour
    {
        // ── Inspector — Data ──────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("The player's equipped-part loadout SO. Leave null to always show an error.")]
        [SerializeField] private PlayerLoadout _loadout;

        [Tooltip("Robot definition that specifies required part-slot categories.")]
        [SerializeField] private RobotDefinition _robotDef;

        [Tooltip("Optional — player inventory SO for ownership checks (rule 3).")]
        [SerializeField] private PlayerInventory _inventory;

        [Tooltip("Optional — shop catalog SO for membership and slot-coverage checks (rules 2 & 4).")]
        [SerializeField] private ShopCatalog _catalog;

        [Tooltip("Optional — prestige-gate config per DamageType (rule 6). " +
                 "Leave null to skip weapon-type unlock checking.")]
        [SerializeField] private WeaponTypeUnlockConfig _unlockConfig;

        [Tooltip("Optional — current prestige SO used in rule 6. " +
                 "Null is treated as prestige count 0 by the validator.")]
        [SerializeField] private PrestigeSystemSO _prestigeSystem;

        [Tooltip("Optional — weapon-part catalog needed to resolve DamageType for rule 6. " +
                 "Leave null to skip weapon-type unlock checking.")]
        [SerializeField] private WeaponPartCatalogSO _weaponCatalog;

        // ── Inspector — Event Channels ────────────────────────────────────────

        [Header("Event Channel — In")]
        [Tooltip("Raised each time the player modifies their loadout. " +
                 "Triggers Validate() to re-evaluate the current build.")]
        [SerializeField] private VoidGameEvent _onLoadoutChanged;

        [Header("Event Channel — Out")]
        [Tooltip("Raised after every Validate() call regardless of result. " +
                 "Useful for other panels that want to react to validation state changes.")]
        [SerializeField] private VoidGameEvent _onValidationChanged;

        // ── Inspector — UI ────────────────────────────────────────────────────

        [Header("UI (optional)")]
        [Tooltip("The Start Match button. interactable = IsValid after each Validate().")]
        [SerializeField] private Button _startMatchButton;

        [Tooltip("Parent Transform for error message rows. " +
                 "Requires _errorRowPrefab to instantiate rows. Leave null to skip row generation.")]
        [SerializeField] private Transform _errorListContainer;

        [Tooltip("Row prefab instantiated once per validation error. " +
                 "The first Text child receives the error message string.")]
        [SerializeField] private GameObject _errorRowPrefab;

        [Tooltip("Optional gate panel root. Not managed by this controller — " +
                 "provided as a convenience reference for scene callers.")]
        [SerializeField] private GameObject _gatePanel;

        // ── Private state ─────────────────────────────────────────────────────

        private Action _validateDelegate;
        private bool   _isValid;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _validateDelegate = Validate;
        }

        private void OnEnable()
        {
            _onLoadoutChanged?.RegisterCallback(_validateDelegate);
            Validate();
        }

        private void OnDisable()
        {
            _onLoadoutChanged?.UnregisterCallback(_validateDelegate);
        }

        // ── Logic ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Runs the full 7-parameter <see cref="LoadoutValidator.Validate"/> against the
        /// currently assigned data SOs, updates <see cref="IsValid"/>,
        /// toggles <c>_startMatchButton.interactable</c>, rebuilds the error row list,
        /// and raises <c>_onValidationChanged</c>.
        ///
        /// <para>Safe to call at any time — fully null-safe; all data fields are optional.</para>
        /// </summary>
        public void Validate()
        {
            // Destroy stale error rows.
            if (_errorListContainer != null)
            {
                for (int i = _errorListContainer.childCount - 1; i >= 0; i--)
                    Destroy(_errorListContainer.GetChild(i).gameObject);
            }

            LoadoutValidationResult result = LoadoutValidator.Validate(
                _loadout, _robotDef, _inventory, _catalog,
                _unlockConfig, _prestigeSystem, _weaponCatalog);

            _isValid = result.IsValid;

            if (_startMatchButton != null)
                _startMatchButton.interactable = _isValid;

            // Instantiate one error row per validation message.
            if (!_isValid && _errorListContainer != null && _errorRowPrefab != null)
            {
                IReadOnlyList<string> errors = result.Errors;
                for (int i = 0; i < errors.Count; i++)
                {
                    GameObject row   = Instantiate(_errorRowPrefab, _errorListContainer);
                    Text[]     texts = row.GetComponentsInChildren<Text>(true);
                    if (texts.Length > 0) texts[0].text = errors[i];
                }
            }

            _onValidationChanged?.Raise();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>True when the last <see cref="Validate"/> call found no violations.</summary>
        public bool IsValid => _isValid;

        /// <summary>The currently assigned <see cref="PlayerLoadout"/>. May be null.</summary>
        public PlayerLoadout Loadout => _loadout;

        /// <summary>The currently assigned <see cref="RobotDefinition"/>. May be null.</summary>
        public RobotDefinition RobotDef => _robotDef;

        /// <summary>The currently assigned <see cref="WeaponTypeUnlockConfig"/>. May be null.</summary>
        public WeaponTypeUnlockConfig UnlockConfig => _unlockConfig;

        /// <summary>The currently assigned <see cref="PrestigeSystemSO"/>. May be null.</summary>
        public PrestigeSystemSO PrestigeSystem => _prestigeSystem;

        /// <summary>The currently assigned <see cref="WeaponPartCatalogSO"/>. May be null.</summary>
        public WeaponPartCatalogSO WeaponCatalog => _weaponCatalog;
    }
}
