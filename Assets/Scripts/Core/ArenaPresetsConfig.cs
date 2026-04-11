using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Immutable SO listing all available arena presets shown to the player
    /// before entering a match.
    ///
    /// Each <see cref="ArenaPreset"/> pairs a human-readable display name
    /// (e.g. "Factory", "Wasteland", "Colosseum") with an <see cref="ArenaConfig"/>
    /// SO that defines the spawn points, ground dimensions, and wall layout.
    ///
    /// ── Usage ──────────────────────────────────────────────────────────────────
    ///   Assign this SO to <see cref="BattleRobots.UI.ArenaSelectionController"/>.
    ///   The controller cycles through <see cref="Presets"/> and writes the chosen
    ///   config to <see cref="SelectedArenaSO"/>.
    ///   <see cref="ArenaManager"/> reads the active SO at match-start time to
    ///   override its inspector <c>_arenaConfig</c> field.
    ///   <see cref="MatchManager"/> writes the selected <see cref="ArenaConfig.ArenaIndex"/>
    ///   value into the <see cref="MatchRecord"/> for per-match analytics.
    ///
    /// ── Suggested arena naming ─────────────────────────────────────────────────
    ///   Factory   — compact 20×20, tight corridors, 2 spawns face-to-face
    ///   Wasteland — large 40×40, distant spawns, exposure-heavy
    ///   Colosseum — circular 30×30 with pillar obstacles, diagonal spawns
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ArenaPresetsConfig.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ArenaPresetsConfig", order = 1)]
    public sealed class ArenaPresetsConfig : ScriptableObject
    {
        // ── Nested data ───────────────────────────────────────────────────────

        /// <summary>
        /// One entry in the arena roster: a UI display name and the
        /// <see cref="ArenaConfig"/> it maps to.
        /// </summary>
        [Serializable]
        public sealed class ArenaPreset
        {
            [Tooltip("Name displayed in the pre-match UI, e.g. 'Factory', 'Wasteland', 'Colosseum'.")]
            public string displayName = "Arena";

            [Tooltip("ArenaConfig SO that defines spawn points, ground dimensions, and wall layout.")]
            public ArenaConfig config;
        }

        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Ordered list of arena presets. Index 0 is the default selection.")]
        [SerializeField] private List<ArenaPreset> _presets = new List<ArenaPreset>();

        // ── Public API (immutable at runtime) ─────────────────────────────────

        /// <summary>
        /// Ordered, read-only list of available arena presets.
        /// Index 0 is the default selection.
        /// </summary>
        public IReadOnlyList<ArenaPreset> Presets => _presets;

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_presets == null || _presets.Count == 0)
            {
                Debug.LogWarning($"[ArenaPresetsConfig] '{name}': No arena presets defined. " +
                                 "Add at least one ArenaConfig entry.");
                return;
            }

            for (int i = 0; i < _presets.Count; i++)
            {
                if (_presets[i] == null)
                {
                    Debug.LogWarning($"[ArenaPresetsConfig] '{name}': " +
                                     $"Preset at index {i} is null — remove or replace it.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(_presets[i].displayName))
                    Debug.LogWarning($"[ArenaPresetsConfig] '{name}': " +
                                     $"Preset at index {i} has an empty displayName.");

                if (_presets[i].config == null)
                    Debug.LogWarning($"[ArenaPresetsConfig] '{name}': " +
                                     $"Preset '{_presets[i].displayName}' has a null ArenaConfig.");
            }
        }
#endif
    }
}
