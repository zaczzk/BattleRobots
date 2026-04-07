using System.Collections.Generic;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// MonoBehaviour that mediates between the ShopCatalog, PlayerWallet, and the
    /// shop UI layer.
    ///
    /// ── Responsibilities ────────────────────────────────────────────────────────
    ///   • Expose the catalog to UI (GetAllParts, GetPartsByCategory).
    ///   • Validate and execute purchases via PlayerWallet.Deduct.
    ///   • Raise _onPurchaseSucceeded (VoidGameEvent) so other systems react.
    ///   • Track which parts the player owns this session (runtime list only;
    ///     persistence is handled separately when SaveSystem / MatchRecord is extended).
    ///
    /// ── Architecture rules ───────────────────────────────────────────────────────
    ///   • Namespace BattleRobots.UI — must NOT reference BattleRobots.Physics.
    ///   • No heap allocations in Update (Update is not used at all here).
    ///   • Cross-system signalling via SO VoidGameEvent channel only.
    ///   • PlayerWallet and ShopCatalog assigned via Inspector SO references.
    ///
    /// ── Scene wiring instructions ────────────────────────────────────────────────
    ///   1. Add ShopManager MonoBehaviour to a persistent UI/Manager GameObject.
    ///   2. Assign _catalog (ShopCatalog SO), _wallet (PlayerWallet SO),
    ///      and _onPurchaseSucceeded (VoidGameEvent SO) in the Inspector.
    ///   3. Wire PlayerWallet._onBalanceChanged → IntGameEventListener on the wallet
    ///      display label's GameObject; Response = TextMeshProUGUI.SetText (or similar).
    ///   4. For each shop item UI element call ShopManager.BuyPart(partDefinition)
    ///      from a UnityEvent or direct reference.
    /// </summary>
    public sealed class ShopManager : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("The master catalog of purchasable parts.")]
        [SerializeField] private ShopCatalog _catalog;

        [Tooltip("The player's runtime wallet SO.")]
        [SerializeField] private PlayerWallet _wallet;

        [Header("Event Channels — Out")]
        [Tooltip("Raised after a successful purchase. Listeners may refresh UI.")]
        [SerializeField] private VoidGameEvent _onPurchaseSucceeded;

        [Tooltip("Raised when a purchase attempt is rejected (insufficient funds or already owned).")]
        [SerializeField] private VoidGameEvent _onPurchaseFailed;

        // ── Runtime state ─────────────────────────────────────────────────────

        /// <summary>
        /// Parts the player has purchased during this session.
        /// Persisted externally (via MatchRecord / SaveSystem extension) in a later milestone.
        /// </summary>
        private readonly List<PartDefinition> _ownedParts = new List<PartDefinition>();

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns a read-only view of all parts in the catalog.
        /// Call once during UI initialisation to populate the browser panel.
        /// </summary>
        public IReadOnlyList<PartDefinition> GetAllParts()
        {
            if (_catalog == null)
            {
                Debug.LogWarning("[ShopManager] ShopCatalog not assigned.");
                return System.Array.Empty<PartDefinition>();
            }
            return _catalog.Parts;
        }

        /// <summary>
        /// Returns parts from the catalog filtered by <paramref name="category"/>.
        /// Allocates a new list — call only during UI setup, not every frame.
        /// </summary>
        public List<PartDefinition> GetPartsByCategory(PartCategory category)
        {
            var result = new List<PartDefinition>();
            if (_catalog == null) return result;

            foreach (var part in _catalog.Parts)
            {
                if (part != null && part.Category == category)
                    result.Add(part);
            }
            return result;
        }

        /// <summary>
        /// Returns true if the player currently owns <paramref name="part"/>.
        /// </summary>
        public bool IsOwned(PartDefinition part) => part != null && _ownedParts.Contains(part);

        /// <summary>
        /// Attempts to purchase <paramref name="part"/>.
        ///
        /// Succeeds when:
        ///   • <paramref name="part"/> is not null and belongs to the catalog.
        ///   • The player does not already own it.
        ///   • PlayerWallet.Deduct succeeds (sufficient balance).
        ///
        /// Raises _onPurchaseSucceeded on success; _onPurchaseFailed on rejection.
        /// </summary>
        /// <param name="part">The PartDefinition to purchase.</param>
        /// <returns>True if the purchase succeeded.</returns>
        public bool BuyPart(PartDefinition part)
        {
            if (part == null)
            {
                Debug.LogWarning("[ShopManager] BuyPart called with null PartDefinition.");
                _onPurchaseFailed?.Raise();
                return false;
            }

            if (_wallet == null)
            {
                Debug.LogError("[ShopManager] PlayerWallet SO not assigned — cannot purchase.");
                _onPurchaseFailed?.Raise();
                return false;
            }

            if (IsOwned(part))
            {
                Debug.Log($"[ShopManager] '{part.PartName}' is already owned.");
                _onPurchaseFailed?.Raise();
                return false;
            }

            if (!_wallet.Deduct(part.Cost))
            {
                Debug.Log($"[ShopManager] Insufficient funds for '{part.PartName}' " +
                          $"(cost={part.Cost}, balance={_wallet.Balance}).");
                _onPurchaseFailed?.Raise();
                return false;
            }

            _ownedParts.Add(part);
            _onPurchaseSucceeded?.Raise();

            Debug.Log($"[ShopManager] Purchased '{part.PartName}' for {part.Cost}. " +
                      $"New balance: {_wallet.Balance}.");
            return true;
        }

        /// <summary>
        /// Clears the owned-parts list. Call at game-start / before loading a saved state.
        /// </summary>
        public void ClearOwnedParts() => _ownedParts.Clear();

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_catalog == null)
                Debug.LogWarning("[ShopManager] _catalog (ShopCatalog) not assigned.");
            if (_wallet == null)
                Debug.LogWarning("[ShopManager] _wallet (PlayerWallet) not assigned.");
            if (_onPurchaseSucceeded == null)
                Debug.LogWarning("[ShopManager] _onPurchaseSucceeded event channel not assigned.");
        }
#endif
    }
}
