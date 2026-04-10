using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that persists the player's preferred equipped-part configuration
    /// between sessions.
    ///
    /// ── Lifecycle ────────────────────────────────────────────────────────────
    ///   1. GameBootstrapper calls LoadSnapshot(SaveData.loadoutPartIds) on startup.
    ///   2. The pre-match assembly UI calls SetLoadout() when the player confirms
    ///      their build.
    ///   3. MatchFlowController (or RobotAssembler) reads EquippedPartIds and
    ///      passes them to RobotAssembler.Assemble().
    ///   4. On match end, ShopManager / PostMatchController may call SetLoadout()
    ///      to snapshot the final build for the next match.
    ///
    /// ── Persistence ───────────────────────────────────────────────────────────
    ///   Written into SaveData.loadoutPartIds by any system that calls SetLoadout().
    ///   The caller is responsible for the Load → mutate → Save round-trip via
    ///   <see cref="SaveSystem"/> (same pattern as <see cref="PlayerInventory"/>).
    ///
    /// ── Architecture ────────────────────────────────────────────────────────
    ///   BattleRobots.Core namespace; no Physics/UI references.
    ///   All mutations go through designated mutators so <c>_onLoadoutChanged</c>
    ///   fires consistently. Zero alloc after Awake.
    ///
    /// Create via Assets ▶ BattleRobots ▶ Economy ▶ PlayerLoadout.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Economy/PlayerLoadout", order = 3)]
    public sealed class PlayerLoadout : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Fired whenever the loadout changes (SetLoadout or Reset). " +
                 "Subscribe in UI to refresh the pre-match build display.")]
        [SerializeField] private VoidGameEvent _onLoadoutChanged;

        // ── Runtime state ─────────────────────────────────────────────────────

        private readonly List<string> _equippedPartIds = new List<string>();

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Read-only view of the currently equipped part IDs.
        /// Order matches the sequence passed to the last <see cref="SetLoadout"/> call.
        /// </summary>
        public IReadOnlyList<string> EquippedPartIds => _equippedPartIds;

        /// <summary>Number of equipped parts.</summary>
        public int Count => _equippedPartIds.Count;

        // ── Mutators ──────────────────────────────────────────────────────────

        /// <summary>
        /// Replaces the current loadout with the supplied part IDs.
        /// Null or empty collections are accepted (results in an empty loadout).
        /// Null or whitespace-only entries are skipped.
        /// Fires <c>_onLoadoutChanged</c>.
        /// </summary>
        public void SetLoadout(IEnumerable<string> partIds)
        {
            _equippedPartIds.Clear();

            if (partIds != null)
            {
                foreach (string id in partIds)
                {
                    if (!string.IsNullOrWhiteSpace(id))
                        _equippedPartIds.Add(id);
                }
            }

            _onLoadoutChanged?.Raise();
        }

        /// <summary>
        /// Restores the loadout from a persisted snapshot (called by GameBootstrapper).
        /// Null or whitespace-only entries in the snapshot are skipped.
        /// Does NOT raise <c>_onLoadoutChanged</c> — bootstrapper calls this before
        /// UI listeners are registered.
        /// </summary>
        public void LoadSnapshot(List<string> partIds)
        {
            _equippedPartIds.Clear();

            if (partIds == null) return;

            for (int i = 0; i < partIds.Count; i++)
            {
                string id = partIds[i];
                if (!string.IsNullOrWhiteSpace(id))
                    _equippedPartIds.Add(id);
            }
        }

        /// <summary>
        /// Clears the loadout and fires <c>_onLoadoutChanged</c>.
        /// </summary>
        public void Reset()
        {
            _equippedPartIds.Clear();
            _onLoadoutChanged?.Raise();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_onLoadoutChanged == null)
                Debug.LogWarning("[PlayerLoadout] _onLoadoutChanged is not assigned — " +
                                 "UI will not auto-refresh when the loadout changes.");
        }
#endif
    }
}
