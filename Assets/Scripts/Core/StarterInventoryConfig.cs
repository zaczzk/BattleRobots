using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Immutable ScriptableObject that declares which parts a brand-new player
    /// receives for free on their first launch.
    ///
    /// ── Lifecycle ─────────────────────────────────────────────────────────────
    ///   <see cref="GameBootstrapper"/> reads this SO once during <c>Awake</c>.
    ///   If the player's <see cref="PlayerInventory"/> is empty after the save-data
    ///   snapshot is loaded (indicating a new game), the bootstrapper calls
    ///   <see cref="PlayerInventory.UnlockPart"/> for each ID listed here and
    ///   immediately persists the result so the starters survive the next session.
    ///
    /// ── Design rules ──────────────────────────────────────────────────────────
    ///   • This SO is immutable at runtime — the list is only read, never written.
    ///   • IDs must match PartDefinition.PartId values exactly (case-sensitive).
    ///   • Duplicate IDs are harmless — <see cref="PlayerInventory.UnlockPart"/>
    ///     is idempotent — but the Editor will warn about them via OnValidate.
    ///   • Assigning an empty list is valid; new players simply start with no parts.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Economy ▶ StarterInventoryConfig.
    /// Assign the single global instance to <see cref="GameBootstrapper._starterConfig"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Economy/StarterInventoryConfig", order = 2)]
    public sealed class StarterInventoryConfig : ScriptableObject
    {
        [Header("Starter Parts")]
        [Tooltip("Part IDs (matching PartDefinition.PartId) given to the player on a new game. " +
                 "Starters are applied once when PlayerInventory is empty after save-data load.")]
        [SerializeField] private List<string> _starterPartIds = new List<string>();

        /// <summary>Read-only list of starter part IDs. Never mutate at runtime.</summary>
        public IReadOnlyList<string> StarterPartIds => _starterPartIds;

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            var seen = new HashSet<string>(System.StringComparer.Ordinal);
            for (int i = 0; i < _starterPartIds.Count; i++)
            {
                string id = _starterPartIds[i];
                if (string.IsNullOrWhiteSpace(id))
                {
                    Debug.LogWarning($"[StarterInventoryConfig] Entry [{i}] is null or whitespace — " +
                                     "will be silently ignored by PlayerInventory.UnlockPart.", this);
                }
                else if (!seen.Add(id))
                {
                    Debug.LogWarning($"[StarterInventoryConfig] Duplicate partId '{id}' at index [{i}] — " +
                                     "harmless but redundant.", this);
                }
            }
        }
#endif
    }
}
