using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Enum of all recognised part-slot categories.
    /// Used to enforce a single slot per category (validated in Editor drawer).
    /// </summary>
    public enum PartSlotType
    {
        Body    = 0,
        Head    = 1,
        LeftArm = 2,
        RightArm= 3,
        LeftLeg = 4,
        RightLeg= 5,
        Weapon  = 6,
    }

    /// <summary>
    /// Describes one attachment point on a robot chassis.
    /// SlotId must be unique within a RobotDefinition (validated by Editor drawer).
    /// </summary>
    [Serializable]
    public sealed class PartSlot
    {
        [Tooltip("Unique identifier used at runtime to look up the equipped part. E.g. 'slot_body_main'.")]
        [SerializeField] private string _slotId;

        [Tooltip("Functional category — determines which part types can be fitted here.")]
        [SerializeField] private PartSlotType _slotType;

        public string SlotId  => _slotId;
        public PartSlotType SlotType => _slotType;
    }

    /// <summary>
    /// ScriptableObject that describes a robot chassis: its base stats and
    /// the list of part slots available for equipment.
    ///
    /// Assets are immutable at runtime — all fields exposed as read-only properties.
    /// Validate slot integrity via the custom Editor inspector before shipping assets.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Robots/RobotDefinition", order = 0)]
    public sealed class RobotDefinition : ScriptableObject
    {
        // ── Identity ─────────────────────────────────────────────────────────

        [Header("Identity")]
        [Tooltip("Display name shown in the shop and HUD.")]
        [SerializeField] private string _robotName = "Unnamed Robot";

        // ── Base Stats ────────────────────────────────────────────────────────

        [Header("Base Stats")]
        [Tooltip("Maximum hit-points before destruction.")]
        [SerializeField, Min(1f)] private float _baseHp = 100f;

        [Tooltip("Base linear movement speed (m/s) before any part bonuses.")]
        [SerializeField, Min(0f)] private float _baseSpeed = 5f;

        [Tooltip("Scalar applied to all joint torque drives on this chassis (>1 = stronger).")]
        [SerializeField, Min(0f)] private float _torqueMultiplier = 1f;

        // ── Part Slots ────────────────────────────────────────────────────────

        [Header("Part Slots")]
        [Tooltip("Ordered list of attachment points. Each SlotId must be unique. " +
                 "A Body slot is required. Validated by the Editor drawer.")]
        [SerializeField] private List<PartSlot> _partSlots = new List<PartSlot>();

        // ── Public API ────────────────────────────────────────────────────────

        public string RobotName         => _robotName;
        public float  BaseHp            => _baseHp;
        public float  BaseSpeed         => _baseSpeed;
        public float  TorqueMultiplier  => _torqueMultiplier;

        /// <summary>Read-only view of part slots. Modify only via the Editor inspector.</summary>
        public IReadOnlyList<PartSlot> PartSlots => _partSlots;

        // ── Validation helper (shared with Editor drawer) ──────────────────────

        /// <summary>
        /// Validates the slot list and populates <paramref name="errors"/> with any
        /// problems found. Returns true if the asset is valid (no errors).
        /// </summary>
        public bool Validate(out List<string> errors)
        {
            errors = new List<string>();

            if (string.IsNullOrWhiteSpace(_robotName))
                errors.Add("RobotName must not be empty.");

            if (_baseHp <= 0f)
                errors.Add("BaseHp must be > 0.");

            var seenIds   = new HashSet<string>(StringComparer.Ordinal);
            var seenTypes = new HashSet<PartSlotType>();
            bool hasBody  = false;

            for (int i = 0; i < _partSlots.Count; i++)
            {
                PartSlot slot = _partSlots[i];

                if (string.IsNullOrWhiteSpace(slot.SlotId))
                {
                    errors.Add($"Slot [{i}]: SlotId is empty.");
                }
                else if (!seenIds.Add(slot.SlotId))
                {
                    errors.Add($"Slot [{i}]: duplicate SlotId '{slot.SlotId}'.");
                }

                if (!seenTypes.Add(slot.SlotType))
                    errors.Add($"Slot [{i}]: duplicate SlotType '{slot.SlotType}'. Each type may appear at most once.");

                if (slot.SlotType == PartSlotType.Body)
                    hasBody = true;
            }

            if (_partSlots.Count == 0)
                errors.Add("PartSlots list is empty — add at least a Body slot.");
            else if (!hasBody)
                errors.Add("Missing mandatory Body slot.");

            if (_partSlots.Count > 8)
                errors.Add($"Slot count ({_partSlots.Count}) exceeds the maximum of 8.");

            return errors.Count == 0;
        }
    }
}
