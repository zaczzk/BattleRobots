using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Immutable ScriptableObject holding the complete list of parts
    /// available for purchase in the shop.
    ///
    /// Assign PartDefinition SOs in the Inspector. Never mutate at runtime.
    ///
    /// Create via Assets ▶ BattleRobots ▶ Shop ▶ ShopCatalog.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Shop/ShopCatalog", order = 1)]
    public sealed class ShopCatalog : ScriptableObject
    {
        [Header("Available Parts")]
        [Tooltip("All PartDefinition SOs the player can browse and buy.")]
        [SerializeField] private List<PartDefinition> _parts = new List<PartDefinition>();

        /// <summary>Read-only view of the purchasable part list. Never mutate at runtime.</summary>
        public IReadOnlyList<PartDefinition> Parts => _parts;

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_parts == null) return;

            var seen = new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
            for (int i = 0; i < _parts.Count; i++)
            {
                var part = _parts[i];
                if (part == null)
                {
                    Debug.LogWarning($"[ShopCatalog] '{name}': entry [{i}] is null.");
                    continue;
                }
                if (!seen.Add(part.PartId))
                {
                    Debug.LogWarning($"[ShopCatalog] '{name}': duplicate partId '{part.PartId}' at index [{i}].");
                }
            }
        }
#endif
    }
}
