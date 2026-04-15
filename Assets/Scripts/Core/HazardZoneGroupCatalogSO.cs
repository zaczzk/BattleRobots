using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Catalog ScriptableObject that holds an indexed collection of
    /// <see cref="HazardZoneGroupSO"/> assets.
    ///
    /// ── Design ───────────────────────────────────────────────────────────────────
    ///   • Provides <see cref="GetGroup(int)"/> — a null-safe, bounds-checked
    ///     accessor so <see cref="HazardZoneGroupCatalogController"/> can route
    ///     activate / deactivate / toggle calls to any group by index.
    ///   • This SO is immutable at runtime; all state lives inside the individual
    ///     <see cref="HazardZoneGroupSO"/> instances.
    ///   • OnValidate warns about null entries to aid scene authoring.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ HazardZoneGroupCatalog.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/HazardZoneGroupCatalog", order = 16)]
    public sealed class HazardZoneGroupCatalogSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Groups")]
        [Tooltip("Indexed collection of HazardZoneGroupSO assets. " +
                 "Null entries are skipped by GetGroup(int).")]
        [SerializeField] private HazardZoneGroupSO[] _groups;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Total number of entries in the catalog (including null slots).</summary>
        public int EntryCount => _groups?.Length ?? 0;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the <see cref="HazardZoneGroupSO"/> at <paramref name="index"/>.
        /// Returns null for a null groups array, an out-of-range index, or a null entry.
        /// </summary>
        public HazardZoneGroupSO GetGroup(int index)
        {
            if (_groups == null || index < 0 || index >= _groups.Length)
                return null;

            return _groups[index];
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_groups == null) return;

            for (int i = 0; i < _groups.Length; i++)
            {
                if (_groups[i] == null)
                    Debug.LogWarning($"[HazardZoneGroupCatalogSO] '{name}': " +
                                     $"Entry [{i}] is null — GetGroup({i}) will return null.");
            }
        }
#endif
    }
}
