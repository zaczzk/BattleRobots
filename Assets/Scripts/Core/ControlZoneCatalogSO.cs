using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Catalog ScriptableObject that holds an indexed collection of
    /// <see cref="ControlZoneSO"/> assets for an arena layout.
    ///
    /// ── Design ───────────────────────────────────────────────────────────────────
    ///   • Provides <see cref="GetZone(int)"/> — a null-safe, bounds-checked
    ///     accessor for iterating or addressing zones by index.
    ///   • <see cref="ResetAll"/> iterates every non-null entry and calls
    ///     <see cref="ControlZoneSO.Reset"/> silently — safe to call at match start
    ///     or match end.
    ///   • This SO is immutable at runtime; all zone state lives inside the
    ///     individual <see cref="ControlZoneSO"/> instances.
    ///   • OnValidate warns about null entries to aid scene authoring.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ControlZoneCatalog.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ControlZoneCatalog", order = 17)]
    public sealed class ControlZoneCatalogSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Zones")]
        [Tooltip("Indexed collection of ControlZoneSO assets. " +
                 "Null entries are skipped by GetZone(int) and ResetAll().")]
        [SerializeField] private ControlZoneSO[] _zones;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Total number of entries in the catalog (including null slots).</summary>
        public int EntryCount => _zones?.Length ?? 0;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the <see cref="ControlZoneSO"/> at <paramref name="index"/>.
        /// Returns null for a null zones array, an out-of-range index, or a null entry.
        /// </summary>
        public ControlZoneSO GetZone(int index)
        {
            if (_zones == null || index < 0 || index >= _zones.Length)
                return null;

            return _zones[index];
        }

        /// <summary>
        /// Calls <see cref="ControlZoneSO.Reset"/> on every non-null zone entry.
        /// No events are fired. Safe to call at match start or match end.
        /// Zero allocation.
        /// </summary>
        public void ResetAll()
        {
            if (_zones == null) return;

            foreach (ControlZoneSO zone in _zones)
                zone?.Reset();
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_zones == null) return;

            for (int i = 0; i < _zones.Length; i++)
            {
                if (_zones[i] == null)
                    Debug.LogWarning($"[ControlZoneCatalogSO] '{name}': " +
                                     $"Entry [{i}] is null — GetZone({i}) will return null.");
            }
        }
#endif
    }
}
