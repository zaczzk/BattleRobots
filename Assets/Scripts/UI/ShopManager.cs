using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Mediates between the shop catalog, the player's wallet, and the shop UI.
    ///
    /// ── Responsibilities ──────────────────────────────────────────────────────
    ///   • Exposes BuyPart(PartDefinition) for UI buttons to call.
    ///   • Delegates fund deduction to PlayerWallet (SO mutator).
    ///   • Fires _onPurchaseCompleted (VoidGameEvent) so other systems can react
    ///     (e.g. UI refresh, audio, analytics) without a direct dependency.
    ///
    /// ── Scene wiring instructions ─────────────────────────────────────────────
    ///   • Assign _catalog (ShopCatalog SO) and _wallet (PlayerWallet SO).
    ///   • Assign _onPurchaseCompleted (VoidGameEvent SO channel).
    ///   • Wire PlayerWallet._onBalanceChanged → IntGameEventListener → UI label
    ///     to keep the balance display reactive without polling.
    ///   • Wire individual buy buttons to ShopManager.BuyPart via UnityEvent
    ///     (pass the PartDefinition SO reference as the argument).
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   - No Update / FixedUpdate — purely event-driven.
    ///   - BuyPart is the only mutation entry-point; no direct wallet field access.
    /// </summary>
    public sealed class ShopManager : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("The full catalog of parts available in the shop.")]
        [SerializeField] private ShopCatalog _catalog;

        [Tooltip("The player's runtime wallet SO.")]
        [SerializeField] private PlayerWallet _wallet;

        [Header("Event Channels — Out")]
        [Tooltip("Raised after a successful purchase. Listeners refresh UI, play audio, etc.")]
        [SerializeField] private VoidGameEvent _onPurchaseCompleted;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Attempts to purchase <paramref name="part"/> by deducting its cost
        /// from the player's wallet.
        /// </summary>
        /// <param name="part">The PartDefinition to buy. Must not be null.</param>
        /// <returns>
        /// <c>true</c> if the purchase succeeded (sufficient funds, valid part);
        /// <c>false</c> otherwise (logged as a warning).
        /// </returns>
        public bool BuyPart(PartDefinition part)
        {
            if (part == null)
            {
                Debug.LogWarning("[ShopManager] BuyPart called with null PartDefinition.");
                return false;
            }

            if (_wallet == null)
            {
                Debug.LogError("[ShopManager] PlayerWallet SO not assigned — cannot process purchase.");
                return false;
            }

            bool success = _wallet.Deduct(part.Cost);

            if (success)
            {
                _onPurchaseCompleted?.Raise();
                Debug.Log($"[ShopManager] Purchased '{part.DisplayName}' for {part.Cost}. " +
                          $"New balance: {_wallet.Balance}.");
            }
            else
            {
                Debug.Log($"[ShopManager] Insufficient funds for '{part.DisplayName}' " +
                          $"(cost {part.Cost}, balance {_wallet.Balance}).");
            }

            return success;
        }

        /// <summary>
        /// Returns the catalog assigned to this ShopManager.
        /// UI panels read this once during initialisation to build the part list.
        /// </summary>
        public ShopCatalog Catalog => _catalog;

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_catalog == null)
                Debug.LogWarning("[ShopManager] _catalog ShopCatalog not assigned.");
            if (_wallet == null)
                Debug.LogWarning("[ShopManager] _wallet PlayerWallet SO not assigned.");
            if (_onPurchaseCompleted == null)
                Debug.LogWarning("[ShopManager] _onPurchaseCompleted event channel not assigned.");
        }
#endif
    }
}
