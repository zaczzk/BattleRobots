using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Immutable SO listing all available difficulty presets shown to the player
    /// before entering the arena.
    ///
    /// Each <see cref="DifficultyPreset"/> pairs a human-readable display name
    /// (e.g. "Easy", "Normal", "Hard") with a <see cref="BotDifficultyConfig"/>
    /// SO that drives AI tuning parameters on match start.
    ///
    /// ── Usage ─────────────────────────────────────────────────────────────────
    ///   Assign this SO to <see cref="BattleRobots.UI.DifficultySelectionController"/>.
    ///   The controller cycles through <see cref="Presets"/> and writes the chosen
    ///   config to <see cref="SelectedDifficultySO"/>.
    ///   <see cref="BattleRobots.Physics.RobotAIController"/> reads the active SO
    ///   at Awake time to override its inspector config.
    ///
    /// Suggested preset values (for three-difficulty designs):
    ///   Easy   — detectionRange 8,  attackRange 2, damage 5,  cooldown 2.0, speed 0.7
    ///   Normal — detectionRange 15, attackRange 3, damage 10, cooldown 1.0, speed 1.0
    ///   Hard   — detectionRange 22, attackRange 4, damage 18, cooldown 0.5, speed 1.5
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ AI ▶ DifficultyPresetsConfig.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/AI/DifficultyPresetsConfig", order = 1)]
    public sealed class DifficultyPresetsConfig : ScriptableObject
    {
        // ── Nested data ───────────────────────────────────────────────────────

        /// <summary>
        /// One entry in the difficulty roster: a UI display name and the
        /// <see cref="BotDifficultyConfig"/> it maps to.
        /// </summary>
        [Serializable]
        public sealed class DifficultyPreset
        {
            [Tooltip("Name displayed in the pre-match UI, e.g. 'Easy', 'Normal', 'Hard'.")]
            public string displayName = "Normal";

            [Tooltip("BotDifficultyConfig SO applied to AI controllers when this preset is selected.")]
            public BotDifficultyConfig config;
        }

        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Ordered list of difficulty presets. Index 0 is the default selection.")]
        [SerializeField] private List<DifficultyPreset> _presets = new List<DifficultyPreset>();

        // ── Public API (immutable at runtime) ─────────────────────────────────

        /// <summary>
        /// Ordered, read-only list of available difficulty presets.
        /// Index 0 is the default selection.
        /// </summary>
        public IReadOnlyList<DifficultyPreset> Presets => _presets;

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_presets == null || _presets.Count == 0)
            {
                Debug.LogWarning($"[DifficultyPresetsConfig] '{name}': No presets defined. " +
                                 "Add at least one BotDifficultyConfig entry.");
                return;
            }

            for (int i = 0; i < _presets.Count; i++)
            {
                if (_presets[i] == null)
                {
                    Debug.LogWarning($"[DifficultyPresetsConfig] '{name}': " +
                                     $"Preset at index {i} is null — remove or replace it.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(_presets[i].displayName))
                    Debug.LogWarning($"[DifficultyPresetsConfig] '{name}': " +
                                     $"Preset at index {i} has an empty displayName.");

                if (_presets[i].config == null)
                    Debug.LogWarning($"[DifficultyPresetsConfig] '{name}': " +
                                     $"Preset '{_presets[i].displayName}' has a null BotDifficultyConfig.");
            }
        }
#endif
    }
}
