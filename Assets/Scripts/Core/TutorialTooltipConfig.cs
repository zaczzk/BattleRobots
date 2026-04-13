using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// A single mapping from a UI panel tag to the first-visit tooltip text that
    /// <see cref="BattleRobots.UI.TutorialTooltipController"/> displays when a player
    /// opens that panel for the first time.
    /// </summary>
    [Serializable]
    public sealed class TutorialTooltipEntry
    {
        [Tooltip("Unity tag assigned to the panel root GameObject. " +
                 "Must match exactly (case-sensitive).")]
        public string panelTag = "";

        [Tooltip("Hint text shown the first time the player opens this panel. " +
                 "Can be left empty for a panel that needs no hint text.")]
        public string tooltipText = "";
    }

    /// <summary>
    /// Data-driven mapping of UI panel tags to first-visit tooltip hint texts,
    /// consumed by <see cref="BattleRobots.UI.TutorialTooltipController"/> to present
    /// contextual hints when a player opens a UI panel for the first time.
    ///
    /// ── Typical flow ─────────────────────────────────────────────────────────────
    ///   1. Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ TutorialTooltipConfig.
    ///   2. Add one <see cref="TutorialTooltipEntry"/> per panel that needs a hint.
    ///      The entry's <c>panelTag</c> must match the Unity tag on the panel root.
    ///   3. Assign this asset to each <see cref="BattleRobots.UI.TutorialTooltipController"/>
    ///      that should read tooltip text from it.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - SO assets are immutable at runtime — entries must be set in the Inspector.
    ///   - <see cref="GetTooltipForTag"/> performs a linear scan (entry count is small).
    ///   - OnValidate warns on null entries, empty panel tags, and duplicate panel tags.
    /// </summary>
    [CreateAssetMenu(
        menuName = "BattleRobots/Core/TutorialTooltipConfig",
        fileName = "TutorialTooltipConfig")]
    public sealed class TutorialTooltipConfig : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Per-panel tooltip entries. Each entry maps a panel's Unity tag to the hint text " +
                 "that appears when the player first opens the panel. " +
                 "Panels with no entry produce no tooltip — the controller hides immediately.")]
        [SerializeField] private List<TutorialTooltipEntry> _entries =
            new List<TutorialTooltipEntry>();

        // ── Public read-only API ──────────────────────────────────────────────

        /// <summary>
        /// Read-only ordered list of all tooltip entries. Never null; may be empty.
        /// </summary>
        public IReadOnlyList<TutorialTooltipEntry> Entries => _entries;

        /// <summary>
        /// Returns the tooltip text configured for <paramref name="panelTag"/>, or
        /// <see cref="string.Empty"/> when no entry matches or <paramref name="panelTag"/>
        /// is null / whitespace.
        ///
        /// Null entries in the list are silently skipped.
        /// </summary>
        public string GetTooltipForTag(string panelTag)
        {
            if (string.IsNullOrWhiteSpace(panelTag)) return string.Empty;

            for (int i = 0; i < _entries.Count; i++)
            {
                TutorialTooltipEntry entry = _entries[i];
                if (entry == null) continue;
                if (entry.panelTag == panelTag)
                    return entry.tooltipText ?? string.Empty;
            }

            return string.Empty;
        }

        // ── Validation ────────────────────────────────────────────────────────

        private void OnValidate()
        {
            var seenTags = new HashSet<string>();

            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i] == null)
                {
                    Debug.LogWarning($"[TutorialTooltipConfig] '{name}': Null entry at index {i}. " +
                                     "Remove or replace it.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(_entries[i].panelTag))
                {
                    Debug.LogWarning($"[TutorialTooltipConfig] '{name}': Entry at index {i} has " +
                                     "an empty panelTag. Each entry must reference a unique, non-empty tag.");
                    continue;
                }

                if (!seenTags.Add(_entries[i].panelTag))
                    Debug.LogWarning($"[TutorialTooltipConfig] '{name}': Duplicate panelTag " +
                                     $"'{_entries[i].panelTag}' at index {i}. Panel tags must be unique.");
            }
        }
    }
}
