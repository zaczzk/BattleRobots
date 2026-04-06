using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that tracks which part is currently equipped in
    /// each slot on the player's robot.
    ///
    /// Architecture rules:
    ///   - <c>BattleRobots.Core</c> namespace only — no UI or Physics references.
    ///   - Asset is immutable at edit time; all mutation happens through API methods.
    ///   - All state is transient (not serialized to the asset). <see cref="LoadFromData"/>
    ///     populates the runtime state from a <see cref="RobotLoadoutData"/> POCO that was
    ///     loaded by <see cref="SaveSystem"/>; <see cref="BuildData"/> snapshots it back.
    ///   - Internal Dictionary provides O(1) slot lookups with zero heap allocation in the
    ///     hot path. The List keeps insertion order for deterministic <see cref="BuildData"/>
    ///     output and serialization.
    ///   - Any change fires <see cref="_onLoadoutChanged"/> so UI listeners can refresh.
    ///
    /// Lifecycle (mirrors SettingsSO):
    ///   1. <see cref="GameBootstrapper"/> calls <see cref="LoadFromData"/> at startup.
    ///   2. Assembly / Shop UI calls <see cref="EquipPart"/> / <see cref="UnequipPart"/>.
    ///   3. <see cref="GameBootstrapper.RecordMatchAndSave"/> calls <see cref="BuildData"/>
    ///      and stores the result in <see cref="SaveData.robotLoadout"/> before saving.
    ///
    /// Create via:  Assets ▶ Create ▶ BattleRobots ▶ Robots ▶ RobotLoadout
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Robots/RobotLoadout", order = 1)]
    public sealed class RobotLoadoutSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Events")]
        [Tooltip("Raised after any equip, unequip, or clear operation.")]
        [SerializeField] private VoidGameEvent _onLoadoutChanged;

        // ── Runtime state (transient — not serialized to the SO asset) ────────

        // Source of truth for BuildData; preserves insertion order.
        private readonly List<LoadoutEntry> _entries = new List<LoadoutEntry>();

        // Fast lookup by slotId — rebuilt from _entries in LoadFromData.
        private readonly Dictionary<string, string> _lookup =
            new Dictionary<string, string>(StringComparer.Ordinal);

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Equips <paramref name="partId"/> into <paramref name="slotId"/>,
        /// replacing any previously equipped part.
        /// Passing a null/empty <paramref name="partId"/> is equivalent to
        /// calling <see cref="UnequipPart"/>.
        /// </summary>
        public void EquipPart(string slotId, string partId)
        {
            if (string.IsNullOrEmpty(slotId))
            {
                Debug.LogWarning("[RobotLoadoutSO] EquipPart: slotId is null or empty.");
                return;
            }

            if (string.IsNullOrEmpty(partId))
            {
                UnequipPart(slotId);
                return;
            }

            // Update existing entry if the slot is already occupied.
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].slotId == slotId)
                {
                    _entries[i].partId = partId;
                    _lookup[slotId] = partId;
                    _onLoadoutChanged?.Raise();
                    return;
                }
            }

            // New slot assignment.
            _entries.Add(new LoadoutEntry { slotId = slotId, partId = partId });
            _lookup[slotId] = partId;
            _onLoadoutChanged?.Raise();
        }

        /// <summary>
        /// Removes the part equipped in <paramref name="slotId"/>.
        /// Does nothing if the slot is already empty.
        /// </summary>
        public void UnequipPart(string slotId)
        {
            if (string.IsNullOrEmpty(slotId)) return;

            // Iterate in reverse so removal doesn't disturb earlier indices.
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                if (_entries[i].slotId != slotId) continue;

                _entries.RemoveAt(i);
                _lookup.Remove(slotId);
                _onLoadoutChanged?.Raise();
                return;
            }
        }

        /// <summary>
        /// Returns the part ID equipped in <paramref name="slotId"/>,
        /// or <c>null</c> if the slot is empty. O(1) — no allocation.
        /// </summary>
        public string GetEquippedPartId(string slotId)
        {
            if (string.IsNullOrEmpty(slotId)) return null;
            return _lookup.TryGetValue(slotId, out string partId) ? partId : null;
        }

        /// <summary>
        /// Returns <c>true</c> if <paramref name="slotId"/> currently has a part equipped.
        /// O(1) — no allocation.
        /// </summary>
        public bool IsEquipped(string slotId) =>
            !string.IsNullOrEmpty(slotId) && _lookup.ContainsKey(slotId);

        /// <summary>Total number of slots that currently have a part equipped.</summary>
        public int EquippedCount => _entries.Count;

        /// <summary>
        /// Removes all equipped parts and raises the change event.
        /// </summary>
        public void Clear()
        {
            _entries.Clear();
            _lookup.Clear();
            _onLoadoutChanged?.Raise();
        }

        // ── Save / Load bridge (mirrors SettingsSO pattern) ───────────────────

        /// <summary>
        /// Populates runtime state from a deserialized <see cref="RobotLoadoutData"/> POCO.
        /// Call this immediately after <see cref="SaveSystem.Load"/> (from GameBootstrapper).
        /// Silently no-ops on null data (treats it as an empty loadout).
        /// </summary>
        public void LoadFromData(RobotLoadoutData data)
        {
            _entries.Clear();
            _lookup.Clear();

            if (data?.entries == null) return;

            for (int i = 0; i < data.entries.Count; i++)
            {
                LoadoutEntry src = data.entries[i];
                if (string.IsNullOrEmpty(src.slotId) || string.IsNullOrEmpty(src.partId))
                    continue;

                _entries.Add(new LoadoutEntry { slotId = src.slotId, partId = src.partId });
                _lookup[src.slotId] = src.partId;
            }

            // Do NOT raise _onLoadoutChanged here — listeners may not be registered yet
            // during GameBootstrapper Awake. Callers can raise manually if needed.
        }

        /// <summary>
        /// Snapshots the current runtime loadout into a <see cref="RobotLoadoutData"/> POCO
        /// ready to be written into <see cref="SaveData.robotLoadout"/>.
        /// Allocates a new list — only call from save paths, never from the hot path.
        /// </summary>
        public RobotLoadoutData BuildData()
        {
            var data = new RobotLoadoutData();
            for (int i = 0; i < _entries.Count; i++)
            {
                data.entries.Add(new LoadoutEntry
                {
                    slotId = _entries[i].slotId,
                    partId = _entries[i].partId,
                });
            }
            return data;
        }
    }
}
