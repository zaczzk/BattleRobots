using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Category of part that fits a given slot on a robot chassis.
    /// </summary>
    public enum PartCategory
    {
        Chassis,
        Weapon,
        Leg,
        Wheel,
        Armor,
        Sensor
    }

    /// <summary>
    /// Describes a single attachment slot on a robot.
    /// slotId must be unique within a RobotDefinition.
    /// </summary>
    [Serializable]
    public sealed class PartSlot
    {
        /// <summary>Unique identifier for this slot, e.g. "weapon_left", "leg_front_right".</summary>
        public string slotId;

        /// <summary>Which category of part may be attached here.</summary>
        public PartCategory category;
    }

    /// <summary>
    /// Immutable ScriptableObject describing a robot chassis:
    /// its identity, base combat stats, and the list of part attachment slots.
    ///
    /// Create via Assets ▶ BattleRobots ▶ Robots ▶ RobotDefinition.
    /// Mutating fields at runtime violates the SO-immutability rule;
    /// runtime state lives in HealthSO / PlayerWallet.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Robots/RobotDefinition", order = 0)]
    public sealed class RobotDefinition : ScriptableObject
    {
        // ── Identity ──────────────────────────────────────────────────────────

        [Header("Identity")]
        [SerializeField] private string _robotName = "Unnamed Robot";
        [SerializeField] private Sprite _thumbnail;

        // ── Base Stats ────────────────────────────────────────────────────────

        [Header("Base Stats")]
        [SerializeField, Min(1f)] private float _maxHitPoints = 100f;

        /// <summary>Base linear move speed (units / second). Scaled by wheel/leg parts.</summary>
        [SerializeField, Min(0.1f)] private float _moveSpeed = 5f;

        /// <summary>
        /// Multiplier applied to all ArticulationBody joint drives on this robot.
        /// 1.0 = standard; larger values increase torque proportionally.
        /// </summary>
        [SerializeField, Min(0.1f)] private float _torqueMultiplier = 1f;

        // ── Part Slots ────────────────────────────────────────────────────────

        [Header("Part Slots")]
        [SerializeField] private List<PartSlot> _slots = new List<PartSlot>();

        // ── Public API ────────────────────────────────────────────────────────

        public string RobotName        => _robotName;
        public Sprite Thumbnail        => _thumbnail;
        public float  MaxHitPoints     => _maxHitPoints;
        public float  MoveSpeed        => _moveSpeed;
        public float  TorqueMultiplier => _torqueMultiplier;

        /// <summary>Read-only view of the slot list. Never mutate at runtime.</summary>
        public IReadOnlyList<PartSlot> Slots => _slots;

        // ── Validation (Editor + Runtime guard) ───────────────────────────────

        /// <summary>
        /// Validates slot list integrity.
        /// Returns true if valid; sets <paramref name="error"/> to null.
        /// Returns false with a human-readable <paramref name="error"/> message otherwise.
        /// </summary>
        public bool ValidateSlots(out string error)
        {
            if (_slots == null || _slots.Count == 0)
            {
                error = "Robot must have at least one part slot.";
                return false;
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var slot in _slots)
            {
                if (slot == null)
                {
                    error = "Slot list contains a null entry.";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(slot.slotId))
                {
                    error = "All slots must have a non-empty slotId.";
                    return false;
                }
                if (!seen.Add(slot.slotId))
                {
                    error = $"Duplicate slotId: '{slot.slotId}'.";
                    return false;
                }
            }

            error = null;
            return true;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!ValidateSlots(out string err))
                Debug.LogWarning($"[RobotDefinition] '{name}': {err}");
        }
#endif
    }
}
