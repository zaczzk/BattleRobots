using System;
using BattleRobots.Core;
using UnityEngine;

namespace BattleRobots.Physics
{
    /// <summary>
    /// MonoBehaviour that fires a typed <see cref="DamageInfo"/> on a
    /// <see cref="DamageGameEvent"/> channel whenever a <see cref="VoidGameEvent"/>
    /// fire trigger is raised.
    ///
    /// ── Data flow ───────────────────────────────────────────────────────────────
    ///   Awake     → caches _fireDelegate (Action).
    ///   OnEnable  → subscribes _onFireEvent → HandleFire.
    ///   OnDisable → unsubscribes (zero-alloc after Awake).
    ///   HandleFire → builds DamageInfo(_weaponPart.BaseDamage + _damageBonus, _sourceId,
    ///                Vector3.zero, null, _weaponPart.WeaponDamageType) and raises
    ///                _outDamageEvent.  No-ops when either field is null.
    ///
    /// ── Null-safety ─────────────────────────────────────────────────────────────
    ///   All inspector fields are optional.  HandleFire silently no-ops when
    ///   _weaponPart or _outDamageEvent is null — no silent NREs at runtime.
    ///
    /// ── Architecture rules ───────────────────────────────────────────────────────
    ///   • BattleRobots.Physics namespace.
    ///   • No Rigidbody — ArticulationBody only.
    ///   • No new allocations in FixedUpdate / Update (fire is event-driven).
    ///   • DisallowMultipleComponent — one weapon controller per robot.
    ///   • BattleRobots.UI must NOT reference this class.
    ///
    /// Assign <c>_weaponPart</c>, <c>_onFireEvent</c>, and <c>_outDamageEvent</c>
    /// in the Inspector; optionally wire <c>_sourceId</c> to the robot's identifier.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WeaponAttachmentController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Weapon Config")]
        [Tooltip("Weapon part defining the outgoing DamageType and base damage amount. " +
                 "Leave null to no-op on fire.")]
        [SerializeField] private WeaponPartSO _weaponPart;

        [Header("Event Channels")]
        [Tooltip("VoidGameEvent raised externally (e.g. by AbilityController or input) to trigger a fire. " +
                 "Leave null to disable event-driven firing.")]
        [SerializeField] private VoidGameEvent _onFireEvent;

        [Tooltip("DamageGameEvent raised with the typed DamageInfo payload on each successful fire. " +
                 "Leave null to suppress outgoing damage.")]
        [SerializeField] private DamageGameEvent _outDamageEvent;

        [Header("Source")]
        [Tooltip("String identifier stamped into DamageInfo.sourceId on each hit " +
                 "(e.g. the robot's display name or prefab tag).")]
        [SerializeField] private string _sourceId = "";

        // ── Private state ─────────────────────────────────────────────────────

        private Action _fireDelegate;

        /// <summary>
        /// Runtime flat damage bonus accumulated by <see cref="AddDamageBonus"/>.
        /// Added to <see cref="WeaponPartSO.BaseDamage"/> on each fire.
        /// Not serialised — starts at zero and is cleared by <see cref="ResetDamageBonus"/>.
        /// </summary>
        private float _damageBonus;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _fireDelegate = HandleFire;
        }

        private void OnEnable()
        {
            _onFireEvent?.RegisterCallback(_fireDelegate);
        }

        private void OnDisable()
        {
            _onFireEvent?.UnregisterCallback(_fireDelegate);
        }

        // ── Logic ─────────────────────────────────────────────────────────────

        private void HandleFire()
        {
            if (_weaponPart == null || _outDamageEvent == null) return;

            _outDamageEvent.Raise(new DamageInfo(
                _weaponPart.BaseDamage + _damageBonus,
                _sourceId,
                Vector3.zero,
                null,
                _weaponPart.WeaponDamageType));
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Replaces the active weapon part at runtime (e.g. from a loadout builder or
        /// power-up pickup).  Allows re-wiring the weapon's elemental type without a
        /// scene reload.  Null clears the weapon — HandleFire will silently no-op.
        /// </summary>
        public void SetWeaponPart(WeaponPartSO part)
        {
            _weaponPart = part;
        }

        /// <summary>
        /// The <see cref="DamageType"/> this controller will stamp on outgoing
        /// <see cref="DamageInfo"/> payloads.
        /// Returns <see cref="DamageType.Physical"/> when no weapon part is assigned.
        /// </summary>
        public DamageType CurrentDamageType =>
            _weaponPart != null ? _weaponPart.WeaponDamageType : DamageType.Physical;

        /// <summary>The currently assigned weapon part. May be null.</summary>
        public WeaponPartSO WeaponPart => _weaponPart;

        /// <summary>
        /// Current accumulated runtime damage bonus (not serialised).
        /// Added to <see cref="WeaponPartSO.BaseDamage"/> on each fire.
        /// </summary>
        public float DamageBonus => _damageBonus;

        /// <summary>
        /// Accumulates a flat damage bonus applied to every subsequent fire.
        /// Negative values are ignored (clamped to zero contribution).
        /// Zero-allocation — arithmetic only.
        /// </summary>
        public void AddDamageBonus(float bonus)
        {
            if (bonus > 0f)
                _damageBonus += bonus;
        }

        /// <summary>
        /// Clears the accumulated runtime damage bonus back to zero.
        /// Call at match end (or match start before re-applying bonuses) to prevent stacking.
        /// </summary>
        public void ResetDamageBonus()
        {
            _damageBonus = 0f;
        }
    }
}
