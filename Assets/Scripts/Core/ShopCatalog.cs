using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Immutable ScriptableObject that holds the master list of parts available
    /// for purchase in the shop.
    ///
    /// Assign PartDefinition SO assets in the Inspector. The list is read-only at
    /// runtime — never add / remove entries from code.
    ///
    /// Create via Assets ▶ BattleRobots ▶ Shop ▶ ShopCatalog.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Shop/ShopCatalog", order = 1)]
    public sealed class ShopCatalog : ScriptableObject
    {
        [Header("Available Parts")]
        [Tooltip("All PartDefinition assets that appear in the shop. No nulls allowed.")]
        [SerializeField] private List<PartDefinition> _parts = new List<PartDefinition>();

        /// <summary>
        /// Read-only view of all parts in the catalog.
        /// Filter by PartCategory on the caller's side as needed.
        /// </summary>
        public IReadOnlyList<PartDefinition> Parts => _parts;

        /// <summary>
        /// Returns the PartDefinition whose name matches <paramref name="partName"/>,
        /// or null if not found. O(n) — call only in non-hot code paths (e.g. UI setup).
        /// </summary>
        public PartDefinition FindByName(string partName)
        {
            if (string.IsNullOrEmpty(partName)) return null;
            for (int i = 0; i < _parts.Count; i++)
            {
                if (_parts[i] != null && _parts[i].PartName == partName)
                    return _parts[i];
            }
            return null;
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_parts == null) return;
            for (int i = 0; i < _parts.Count; i++)
            {
                if (_parts[i] == null)
                    Debug.LogWarning($"[ShopCatalog] '{name}': Null entry at index {i}.");
            }
        }
#endif
    }
}
