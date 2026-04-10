using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that tracks which parts the player has purchased.
    ///
    /// Lifecycle:
    ///   • <see cref="GameBootstrapper"/> calls <see cref="LoadSnapshot"/> on startup
    ///     to restore the persisted owned-part list from <see cref="SaveData.unlockedPartIds"/>.
    ///   • <see cref="BattleRobots.UI.ShopManager"/> calls <see cref="UnlockPart"/> after a
    ///     successful purchase and then persists the updated snapshot to disk.
    ///   • UI panels call <see cref="HasPart"/> to gate buy-button state (already-owned parts
    ///     are greyed out).
    ///
    /// Design rules:
    ///   • This SO holds runtime-only state — its inspector-serialised fields are empty.
    ///   • A private <see cref="HashSet{T}"/> mirrors <c>_unlockedPartIds</c> for O(1)
    ///     membership checks without exposing mutable state externally.
    ///   • <see cref="_onInventoryChanged"/> fires after every mutation so shop UI can refresh.
    ///   • All mutators are idempotent (calling <see cref="UnlockPart"/> twice with the same
    ///     id has no effect beyond the first call).
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Economy ▶ PlayerInventory.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Economy/PlayerInventory", order = 1)]
    public sealed class PlayerInventory : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Event Channels — Out")]
        [Tooltip("Raised after any change to the owned-part list (unlock or reset). " +
                 "Wire to a VoidGameEventListener on the shop UI root to trigger a refresh.")]
        [SerializeField] private VoidGameEvent _onInventoryChanged;

        // ── Runtime state (not SO-serialised) ────────────────────────────────

        // Ordered list — preserves insertion order for UI display.
        private readonly List<string>   _unlockedPartIds = new List<string>();
        // Shadow set — O(1) membership test; kept in sync with the list at all times.
        private readonly HashSet<string> _unlockedSet     = new HashSet<string>();

        // ── Read-only access ──────────────────────────────────────────────────

        /// <summary>
        /// Ordered, read-only view of all currently owned part IDs.
        /// Snapshot this via <c>new List&lt;string&gt;(UnlockedPartIds)</c> before persisting.
        /// </summary>
        public IReadOnlyList<string> UnlockedPartIds => _unlockedPartIds;

        // ── Mutators ──────────────────────────────────────────────────────────

        /// <summary>
        /// Adds <paramref name="partId"/> to the owned set if not already present.
        /// Fires <c>_onInventoryChanged</c> only when the list actually changes.
        /// Silently ignores null/whitespace IDs (defensive; callers should validate).
        /// </summary>
        public void UnlockPart(string partId)
        {
            if (string.IsNullOrWhiteSpace(partId)) return;

            if (_unlockedSet.Add(partId))
            {
                _unlockedPartIds.Add(partId);
                _onInventoryChanged?.Raise();
            }
        }

        /// <summary>
        /// Returns true if <paramref name="partId"/> is in the owned set.
        /// O(1). Returns false for null/whitespace IDs.
        /// </summary>
        public bool HasPart(string partId)
        {
            if (string.IsNullOrWhiteSpace(partId)) return false;
            return _unlockedSet.Contains(partId);
        }

        /// <summary>
        /// Replaces the current owned-part list with the persisted snapshot loaded
        /// from <see cref="SaveData.unlockedPartIds"/>.
        /// Duplicates within <paramref name="ids"/> are silently dropped.
        /// Fires <c>_onInventoryChanged</c> once after loading.
        /// Safe to call with a null or empty enumerable (resets to empty).
        /// </summary>
        public void LoadSnapshot(IEnumerable<string> ids)
        {
            _unlockedPartIds.Clear();
            _unlockedSet.Clear();

            if (ids != null)
            {
                foreach (string id in ids)
                {
                    if (!string.IsNullOrWhiteSpace(id) && _unlockedSet.Add(id))
                        _unlockedPartIds.Add(id);
                }
            }

            _onInventoryChanged?.Raise();
        }

        /// <summary>
        /// Clears all owned parts (e.g. new-game reset).
        /// Fires <c>_onInventoryChanged</c>.
        /// </summary>
        public void Reset()
        {
            _unlockedPartIds.Clear();
            _unlockedSet.Clear();
            _onInventoryChanged?.Raise();
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_onInventoryChanged == null)
                Debug.LogWarning("[PlayerInventory] _onInventoryChanged not assigned — " +
                                 "shop UI will not refresh automatically after purchases.", this);
        }
#endif
    }
}
