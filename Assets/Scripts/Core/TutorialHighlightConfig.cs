using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// A single mapping from a tutorial step ID to a Unity scene tag that identifies
    /// which UI <c>GameObject</c> the <see cref="BattleRobots.UI.TutorialHighlightOverlay"/>
    /// should spotlight during that step.
    /// </summary>
    [Serializable]
    public sealed class TutorialHighlightEntry
    {
        [Tooltip("Unique step ID from TutorialStepSO (must match exactly, case-sensitive).")]
        public string stepId = "";

        [Tooltip("Unity tag assigned to the UI GameObject to spotlight during this step. " +
                 "Leave empty for no highlight on this step.")]
        public string targetTag = "";
    }

    /// <summary>
    /// Data-driven mapping of tutorial step IDs to Unity scene tags, consumed by
    /// <see cref="BattleRobots.UI.TutorialHighlightOverlay"/> to spotlight a specific
    /// UI element during each step of the tutorial.
    ///
    /// ── Typical flow ─────────────────────────────────────────────────────────────
    ///   1. Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ TutorialHighlightConfig.
    ///   2. Add one <see cref="TutorialHighlightEntry"/> per step that needs a spotlight.
    ///      Steps with no entry produce no highlight frame — the overlay hides itself.
    ///   3. Set each entry's <c>targetTag</c> to the Unity tag of the UI element to frame.
    ///   4. Assign this asset to <see cref="BattleRobots.UI.TutorialHighlightOverlay._config"/>.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - SO assets are immutable at runtime — entries must be set in the Inspector.
    ///   - <see cref="GetTagForStep"/> performs a linear scan (entry count is tiny).
    ///   - OnValidate warns on null entries, empty step IDs, and duplicate step IDs.
    /// </summary>
    [CreateAssetMenu(
        menuName = "BattleRobots/Core/TutorialHighlightConfig",
        fileName = "TutorialHighlightConfig")]
    public sealed class TutorialHighlightConfig : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Per-step highlight entries. Only steps that need a UI spotlight require an " +
                 "entry; steps with no entry produce no highlight (overlay hides itself).")]
        [SerializeField] private List<TutorialHighlightEntry> _entries =
            new List<TutorialHighlightEntry>();

        // ── Public read-only API ──────────────────────────────────────────────

        /// <summary>
        /// Read-only ordered list of all highlight entries. Never null; may be empty.
        /// </summary>
        public IReadOnlyList<TutorialHighlightEntry> Entries => _entries;

        /// <summary>
        /// Returns the target tag configured for <paramref name="stepId"/>, or
        /// <see cref="string.Empty"/> when no entry matches or <paramref name="stepId"/>
        /// is null / whitespace.
        ///
        /// Null entries in the list are silently skipped.
        /// </summary>
        public string GetTagForStep(string stepId)
        {
            if (string.IsNullOrWhiteSpace(stepId)) return string.Empty;

            for (int i = 0; i < _entries.Count; i++)
            {
                TutorialHighlightEntry entry = _entries[i];
                if (entry == null) continue;
                if (entry.stepId == stepId)
                    return entry.targetTag ?? string.Empty;
            }

            return string.Empty;
        }

        // ── Validation ────────────────────────────────────────────────────────

        private void OnValidate()
        {
            var seenIds = new HashSet<string>();

            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i] == null)
                {
                    Debug.LogWarning($"[TutorialHighlightConfig] '{name}': Null entry at index {i}. " +
                                     "Remove or replace it.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(_entries[i].stepId))
                {
                    Debug.LogWarning($"[TutorialHighlightConfig] '{name}': Entry at index {i} has " +
                                     "an empty stepId. Each entry must reference a unique, non-empty step.");
                    continue;
                }

                if (!seenIds.Add(_entries[i].stepId))
                    Debug.LogWarning($"[TutorialHighlightConfig] '{name}': Duplicate stepId " +
                                     $"'{_entries[i].stepId}' at index {i}. StepIds must be unique.");
            }
        }
    }
}
