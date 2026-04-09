using System;
using System.Collections.Generic;
using BattleRobots.Core;
using UnityEngine;

namespace BattleRobots.Physics
{
    /// <summary>
    /// Instantiates PartDefinition prefabs into their designated slot Transforms
    /// on a robot chassis at match start.
    ///
    /// ── Scene wiring instructions ─────────────────────────────────────────────
    ///   1. Add to the robot root GameObject alongside RobotLocomotionController.
    ///   2. Assign _robotDefinition — defines the expected slots and their categories.
    ///   3. Populate _slotMounts so each slotId maps to a child Transform
    ///      (the physical attachment point in the hierarchy).
    ///   4. Assign _equippedParts — the PartDefinition SOs the player has selected.
    ///      Each part is placed in the first matching-category slot that is still free.
    ///   5. Call Assemble() at match start:
    ///      - via VoidGameEventListener → Response on the MatchStarted channel, OR
    ///      - directly by MatchFlowController.HandleMatchStarted().
    ///
    /// ── Architecture rules enforced ───────────────────────────────────────────
    ///   • BattleRobots.Physics namespace. May reference BattleRobots.Core.
    ///   • No heap allocations in hot paths — all allocations are one-time in Assemble().
    ///   • Parts are instantiated via Object.Instantiate once per match (cold path).
    ///   • GetEquippedPartIds() feeds into MatchRecord.equippedPartIds for persistence.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RobotAssembler : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Robot Definition")]
        [Tooltip("SO that defines the chassis slot list and their allowed categories.")]
        [SerializeField] private RobotDefinition _robotDefinition;

        [Header("Slot Mounts")]
        [Tooltip("Maps each slotId to the Transform attachment point in the robot hierarchy. " +
                 "slotId must match an entry in the RobotDefinition.Slots list.")]
        [SerializeField] private List<SlotMount> _slotMounts = new List<SlotMount>();

        [Header("Equipped Parts")]
        [Tooltip("PartDefinition SOs currently equipped. Each part is placed into the first " +
                 "available slot whose category matches the part's Category field.")]
        [SerializeField] private List<PartDefinition> _equippedParts = new List<PartDefinition>();

        // ── Private state ─────────────────────────────────────────────────────

        private readonly List<string>     _equippedPartIds = new List<string>();
        private readonly List<GameObject> _spawnedParts    = new List<GameObject>();
        private bool                      _assembled;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>True after a successful Assemble() call.</summary>
        public bool IsAssembled => _assembled;

        /// <summary>
        /// Instantiates all equipped-part prefabs into their matching slot Transforms.
        /// Re-calling first tears down any previously spawned parts (Disassemble).
        /// Safe to call from a VoidGameEventListener Inspector Response.
        /// All allocations happen here (cold path — called once per match).
        /// </summary>
        public void Assemble()
        {
            Disassemble();

            if (_robotDefinition == null)
            {
                Debug.LogError("[RobotAssembler] RobotDefinition is not assigned.", this);
                return;
            }

            // Build slotId → Transform lookup once (cold path).
            var mountLookup = new Dictionary<string, Transform>(_slotMounts.Count, StringComparer.Ordinal);
            foreach (SlotMount mount in _slotMounts)
            {
                if (mount == null || string.IsNullOrEmpty(mount.slotId) || mount.attachPoint == null)
                    continue;
                mountLookup[mount.slotId] = mount.attachPoint;
            }

            // Build category → available-slot queue from RobotDefinition.
            var availableSlots = new Dictionary<PartCategory, Queue<PartSlot>>();
            foreach (PartSlot slot in _robotDefinition.Slots)
            {
                if (!availableSlots.TryGetValue(slot.category, out Queue<PartSlot> queue))
                {
                    queue = new Queue<PartSlot>();
                    availableSlots[slot.category] = queue;
                }
                queue.Enqueue(slot);
            }

            _equippedPartIds.Clear();

            foreach (PartDefinition partDef in _equippedParts)
            {
                if (partDef == null) continue;

                // Find the first free slot that accepts this part's category.
                if (!availableSlots.TryGetValue(partDef.Category, out Queue<PartSlot> slotQueue)
                    || slotQueue.Count == 0)
                {
                    Debug.LogWarning(
                        $"[RobotAssembler] No available '{partDef.Category}' slot for part " +
                        $"'{partDef.PartId}' on '{name}'. Part skipped.", this);
                    continue;
                }

                PartSlot slot = slotQueue.Dequeue();

                if (!mountLookup.TryGetValue(slot.slotId, out Transform attachPoint))
                {
                    Debug.LogWarning(
                        $"[RobotAssembler] No SlotMount found for slotId '{slot.slotId}' " +
                        $"on '{name}'. Part '{partDef.PartId}' skipped.", this);
                    continue;
                }

                // Record the part regardless of whether it has a prefab (stat-only parts are valid).
                _equippedPartIds.Add(partDef.PartId);

                if (partDef.Prefab == null)
                    continue;   // stat-only part — no geometry to spawn

                // Instantiate and parent to the slot attachment point.
                GameObject instance = Instantiate(partDef.Prefab, attachPoint);
                instance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                _spawnedParts.Add(instance);
            }

            _assembled = true;
            Debug.Log($"[RobotAssembler] '{name}': assembled {_spawnedParts.Count} prefab(s), " +
                      $"{_equippedPartIds.Count} part(s) total.");
        }

        /// <summary>
        /// Destroys all previously instantiated part GameObjects and resets assembly state.
        /// Called automatically at the start of Assemble(); also callable from MatchFlowController
        /// between matches.
        /// </summary>
        public void Disassemble()
        {
            foreach (GameObject go in _spawnedParts)
            {
                if (go != null) Destroy(go);
            }
            _spawnedParts.Clear();
            _equippedPartIds.Clear();
            _assembled = false;
        }

        /// <summary>
        /// Returns the immutable list of part IDs currently equipped.
        /// Pass this to MatchRecord.equippedPartIds for persistence.
        /// Returns an empty list if Assemble() has not been called.
        /// </summary>
        public IReadOnlyList<string> GetEquippedPartIds() => _equippedPartIds;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnDestroy() => Disassemble();

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_robotDefinition == null)
                Debug.LogWarning("[RobotAssembler] RobotDefinition not assigned.", this);

            if (_slotMounts == null || _slotMounts.Count == 0)
                Debug.LogWarning("[RobotAssembler] No SlotMounts configured.", this);
        }
#endif
    }

    /// <summary>
    /// Pairs a slot identifier string with its physical Transform attachment point
    /// in the robot hierarchy. Serialised as part of <see cref="RobotAssembler._slotMounts"/>.
    /// </summary>
    [Serializable]
    public sealed class SlotMount
    {
        [Tooltip("Must exactly match a PartSlot.slotId entry in the assigned RobotDefinition.")]
        public string    slotId;

        [Tooltip("Child Transform to which the part prefab will be parented on Assemble().")]
        public Transform attachPoint;
    }
}
