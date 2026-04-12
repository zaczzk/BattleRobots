using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that stores up to <see cref="MaxPresets"/> named loadout
    /// presets for the player.  Each preset is a named snapshot of equipped part IDs that
    /// the player can restore in one click from the pre-match lobby.
    ///
    /// ── Lifecycle ─────────────────────────────────────────────────────────────
    ///   1. <see cref="GameBootstrapper"/> calls <see cref="LoadSnapshot"/> (no event) to
    ///      rehydrate saved presets from <see cref="SaveData.savedLoadoutPresets"/>.
    ///   2. <see cref="BattleRobots.UI.LoadoutPresetController"/> calls
    ///      <see cref="SavePreset"/> / <see cref="DeletePreset"/> and persists the result
    ///      via the load → mutate → Save round-trip.
    ///   3. UI subscribes to <c>_onPresetsChanged</c> to refresh the preset list panel.
    ///
    /// ── Capacity ──────────────────────────────────────────────────────────────
    ///   <see cref="SavePreset"/> returns <c>false</c> when the preset list is full
    ///   (<see cref="Count"/> ≥ <see cref="MaxPresets"/>).  The player must delete an
    ///   existing preset before saving a new one.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   • BattleRobots.Core namespace; no Physics / UI references.
    ///   • <see cref="LoadSnapshot"/> and <see cref="Reset"/> do NOT fire
    ///     <c>_onPresetsChanged</c> — bootstrapper-safe.
    ///   • Part-ID lists stored in presets are independent copies — mutating the
    ///     player's live loadout does NOT retroactively alter saved presets.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Economy ▶ LoadoutPresets.
    /// </summary>
    [CreateAssetMenu(
        menuName = "BattleRobots/Economy/LoadoutPresets",
        fileName = "LoadoutPresetManagerSO",
        order    = 4)]
    public sealed class LoadoutPresetManagerSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Maximum number of named presets the player can save. " +
                 "SavePreset() returns false when this limit is reached.")]
        [SerializeField, Range(1, 10)] private int _maxPresets = 5;

        [Tooltip("Fired after SavePreset() or DeletePreset() successfully mutates the list. " +
                 "Subscribe in LoadoutPresetController to refresh the preset-list panel. " +
                 "Leave null if no UI needs to react.")]
        [SerializeField] private VoidGameEvent _onPresetsChanged;

        // ── Runtime state (not serialized — domain-reload safe via LoadSnapshot) ─

        private readonly List<SavedLoadoutPreset> _presets = new List<SavedLoadoutPreset>();

        // ── Public read-only API ──────────────────────────────────────────────

        /// <summary>
        /// Maximum number of presets that can be saved simultaneously.
        /// Configured in the Inspector (default 5, range [1, 10]).
        /// </summary>
        public int MaxPresets => _maxPresets;

        /// <summary>Number of presets currently stored.</summary>
        public int Count => _presets.Count;

        /// <summary>True when no more presets can be saved (<see cref="Count"/> ≥ <see cref="MaxPresets"/>).</summary>
        public bool IsFull => _presets.Count >= _maxPresets;

        /// <summary>
        /// Read-only view of all saved presets in insertion order.
        /// Never null; may be empty.
        /// </summary>
        public IReadOnlyList<SavedLoadoutPreset> Presets => _presets;

        // ── Mutators ──────────────────────────────────────────────────────────

        /// <summary>
        /// Saves a new named preset containing a snapshot of <paramref name="partIds"/>.
        ///
        /// Returns <c>false</c> (and does not add) when:
        ///   • <paramref name="name"/> is null or whitespace.
        ///   • <paramref name="partIds"/> is null.
        ///   • The list is already full (<see cref="IsFull"/>).
        ///
        /// On success fires <c>_onPresetsChanged</c>.
        /// An empty <paramref name="partIds"/> sequence is accepted (empty loadout preset).
        /// </summary>
        /// <param name="name">Display name for the preset. Leading/trailing whitespace trimmed.</param>
        /// <param name="partIds">Equipped part IDs to snapshot. A defensive copy is stored.</param>
        /// <returns>True when the preset was added successfully.</returns>
        public bool SavePreset(string name, IEnumerable<string> partIds)
        {
            if (string.IsNullOrWhiteSpace(name))  return false;
            if (partIds == null)                   return false;
            if (IsFull)                            return false;

            var preset = new SavedLoadoutPreset
            {
                name    = name.Trim(),
                partIds = new List<string>(partIds),
            };
            _presets.Add(preset);

            _onPresetsChanged?.Raise();
            return true;
        }

        /// <summary>
        /// Returns the part-ID list of the preset at <paramref name="index"/> as a
        /// read-only view, or <c>null</c> when the index is out of range.
        /// </summary>
        /// <param name="index">Zero-based index into <see cref="Presets"/>.</param>
        public IReadOnlyList<string> LoadPreset(int index)
        {
            if (index < 0 || index >= _presets.Count) return null;
            return _presets[index].partIds;
        }

        /// <summary>
        /// Removes the preset at <paramref name="index"/>.
        ///
        /// Returns <c>false</c> when the index is out of range (no-op).
        /// On success fires <c>_onPresetsChanged</c>.
        /// </summary>
        /// <param name="index">Zero-based index of the preset to remove.</param>
        public bool DeletePreset(int index)
        {
            if (index < 0 || index >= _presets.Count) return false;

            _presets.RemoveAt(index);
            _onPresetsChanged?.Raise();
            return true;
        }

        /// <summary>
        /// Silent rehydration from a <see cref="SaveData"/> snapshot.
        /// Does NOT fire <c>_onPresetsChanged</c> — safe to call from
        /// <see cref="GameBootstrapper"/>.
        ///
        /// A defensive copy is made so mutations to the caller's list do not affect
        /// the runtime state.  Null input clears the preset list.
        /// </summary>
        public void LoadSnapshot(List<SavedLoadoutPreset> presets)
        {
            _presets.Clear();

            if (presets == null) return;

            for (int i = 0; i < presets.Count; i++)
            {
                SavedLoadoutPreset p = presets[i];
                if (p == null) continue;

                _presets.Add(new SavedLoadoutPreset
                {
                    name    = p.name ?? "",
                    partIds = p.partIds != null
                        ? new List<string>(p.partIds)
                        : new List<string>(),
                });
            }
        }

        /// <summary>
        /// Returns a deep copy of the current preset list for persistence.
        /// Safe to serialise directly into <see cref="SaveData.savedLoadoutPresets"/>.
        /// </summary>
        public List<SavedLoadoutPreset> TakeSnapshot()
        {
            var copy = new List<SavedLoadoutPreset>(_presets.Count);
            for (int i = 0; i < _presets.Count; i++)
            {
                SavedLoadoutPreset p = _presets[i];
                copy.Add(new SavedLoadoutPreset
                {
                    name    = p.name,
                    partIds = new List<string>(p.partIds),
                });
            }
            return copy;
        }

        /// <summary>
        /// Silently clears all presets.  Does NOT fire <c>_onPresetsChanged</c>.
        /// Intended for test teardown and fresh-install resets.
        /// </summary>
        public void Reset()
        {
            _presets.Clear();
        }
    }
}
