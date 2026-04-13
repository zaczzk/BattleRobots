using System;
using System.Collections.Generic;
using BattleRobots.Core;
using UnityEngine;

namespace BattleRobots.Physics
{
    /// <summary>
    /// MonoBehaviour that resolves the player's equipped weapon from a
    /// <see cref="WeaponPartCatalogSO"/> and applies it to a
    /// <see cref="WeaponAttachmentController"/> whenever the loadout is confirmed.
    ///
    /// ── Data flow ───────────────────────────────────────────────────────────────
    ///   Awake     → caches _applyDelegate (zero-alloc after Awake).
    ///   OnEnable  → subscribes _onLoadoutConfirmed → ApplyWeapon; calls ApplyWeapon once.
    ///   OnDisable → unsubscribes (zero-alloc after Awake).
    ///   ApplyWeapon → iterates <see cref="PlayerLoadout.EquippedPartIds"/>; resolves
    ///                 the first matching <see cref="WeaponPartSO"/> via
    ///                 <see cref="WeaponPartCatalogSO.Lookup"/>; calls
    ///                 <see cref="WeaponAttachmentController.SetWeaponPart"/> with the result.
    ///                 Clears the weapon (null) when no match is found.
    ///
    /// ── Null-safety ─────────────────────────────────────────────────────────────
    ///   ApplyWeapon is a no-op when _playerLoadout, _catalog, or _weaponController
    ///   is null — no silent NREs at runtime.
    ///
    /// ── Architecture notes ───────────────────────────────────────────────────────
    ///   • BattleRobots.Physics namespace (legal reference to WeaponAttachmentController).
    ///   • No Rigidbody — ArticulationBody only.
    ///   • No new allocations in ApplyWeapon — IReadOnlyList scan only.
    ///   • DisallowMultipleComponent — one applicator per robot.
    ///   • BattleRobots.UI must NOT reference this class.
    ///
    /// Assign <c>_playerLoadout</c>, <c>_catalog</c>, and <c>_weaponController</c>
    /// in the Inspector; optionally wire <c>_onLoadoutConfirmed</c> to the same
    /// VoidGameEvent raised by LoadoutBuilderController when the player confirms.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WeaponLoadoutApplicator : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("Runtime loadout SO. EquippedPartIds are iterated by ApplyWeapon to find " +
                 "the weapon part. Leave null to disable — ApplyWeapon silently no-ops.")]
        [SerializeField] private PlayerLoadout _playerLoadout;

        [Tooltip("Catalog that maps PartIds to WeaponPartSO assets. " +
                 "Leave null to disable — ApplyWeapon silently no-ops.")]
        [SerializeField] private WeaponPartCatalogSO _catalog;

        [Header("Target")]
        [Tooltip("WeaponAttachmentController on the player robot. " +
                 "Receives SetWeaponPart(resolved) on each apply. " +
                 "Leave null to disable — ApplyWeapon silently no-ops.")]
        [SerializeField] private WeaponAttachmentController _weaponController;

        [Header("Event Channels — In")]
        [Tooltip("VoidGameEvent raised by LoadoutBuilderController when the player confirms " +
                 "a loadout change. Triggers ApplyWeapon so the in-combat weapon stays in sync. " +
                 "Leave null to apply only at OnEnable time.")]
        [SerializeField] private VoidGameEvent _onLoadoutConfirmed;

        // ── Private state ─────────────────────────────────────────────────────

        private Action _applyDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _applyDelegate = ApplyWeapon;
        }

        private void OnEnable()
        {
            _onLoadoutConfirmed?.RegisterCallback(_applyDelegate);
            ApplyWeapon();
        }

        private void OnDisable()
        {
            _onLoadoutConfirmed?.UnregisterCallback(_applyDelegate);
        }

        // ── Logic ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Iterates <see cref="PlayerLoadout.EquippedPartIds"/> and applies the first
        /// matching <see cref="WeaponPartSO"/> found in the catalog to the weapon controller.
        ///
        /// If no equipped part resolves to a catalog entry, the weapon controller is
        /// cleared via <see cref="WeaponAttachmentController.SetWeaponPart(null)"/>.
        /// No-op when any of _playerLoadout, _catalog, or _weaponController is null.
        /// No allocation — iterates IReadOnlyList by index.
        /// </summary>
        public void ApplyWeapon()
        {
            if (_playerLoadout == null || _catalog == null || _weaponController == null)
                return;

            IReadOnlyList<string> ids = _playerLoadout.EquippedPartIds;
            for (int i = 0; i < ids.Count; i++)
            {
                WeaponPartSO found = _catalog.Lookup(ids[i]);
                if (found != null)
                {
                    _weaponController.SetWeaponPart(found);
                    return;
                }
            }

            // No weapon part in the loadout matched the catalog — clear the weapon.
            _weaponController.SetWeaponPart(null);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>The currently assigned WeaponPartCatalogSO. May be null.</summary>
        public WeaponPartCatalogSO Catalog => _catalog;

        /// <summary>The currently assigned PlayerLoadout SO. May be null.</summary>
        public PlayerLoadout PlayerLoadout => _playerLoadout;

        /// <summary>The currently assigned WeaponAttachmentController. May be null.</summary>
        public WeaponAttachmentController WeaponController => _weaponController;
    }
}
