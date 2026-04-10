using System.Collections.Generic;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Mediates between the shop catalog, the player's wallet, the player's inventory,
    /// and the shop UI.
    ///
    /// ── Responsibilities ──────────────────────────────────────────────────────
    ///   • Exposes BuyPart(PartDefinition) for UI buttons to call.
    ///   • Rejects purchases for parts the player already owns.
    ///   • Delegates fund deduction to PlayerWallet (SO mutator).
    ///   • Records the newly owned part in PlayerInventory SO.
    ///   • Persists wallet + inventory to disk after every successful purchase.
    ///   • Fires _onPurchaseCompleted (VoidGameEvent) so other systems can react
    ///     (e.g. UI refresh, audio, analytics) without a direct dependency.
    ///
    /// ── Scene wiring instructions ─────────────────────────────────────────────
    ///   • Assign _catalog (ShopCatalog SO), _wallet (PlayerWallet SO), and
    ///     _inventory (PlayerInventory SO — same asset used by GameBootstrapper).
    ///   • Assign _onPurchaseCompleted (VoidGameEvent SO channel).
    ///   • Wire PlayerWallet._onBalanceChanged → IntGameEventListener → UI label.
    ///   • Wire PlayerInventory._onInventoryChanged → VoidGameEventListener →
    ///     shop panel refresh to grey-out already-owned parts.
    ///   • Wire individual buy buttons to ShopManager.BuyPart via UnityEvent.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   - No Update / FixedUpdate — purely event-driven.
    ///   - BuyPart is the only mutation entry-point; no direct wallet field access.
    ///   - _inventory is optional (null = backwards-compatible; no ownership tracking).
    ///   - SaveSystem.Load → mutate → Save pattern preserves existing match history.
    /// </summary>
    public sealed class ShopManager : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("The full catalog of parts available in the shop.")]
        [SerializeField] private ShopCatalog _catalog;

        [Tooltip("The player's runtime wallet SO.")]
        [SerializeField] private PlayerWallet _wallet;

        [Tooltip("The player's runtime inventory SO. Assign to enable ownership tracking " +
                 "and 'already-owned' purchase gating. Leave null to skip (backwards-compatible).")]
        [SerializeField] private PlayerInventory _inventory;

        [Header("Event Channels — Out")]
        [Tooltip("Raised after a successful purchase. Listeners refresh UI, play audio, etc.")]
        [SerializeField] private VoidGameEvent _onPurchaseCompleted;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Attempts to purchase <paramref name="part"/> by deducting its cost
        /// from the player's wallet.
        ///
        /// Returns <c>false</c> without deducting funds if:
        ///   • <paramref name="part"/> is null.
        ///   • The player already owns the part (requires <c>_inventory</c> assigned).
        ///   • The wallet has insufficient funds.
        ///   • <c>_wallet</c> is not assigned.
        /// </summary>
        /// <param name="part">The PartDefinition to buy. Must not be null.</param>
        /// <returns>True if the purchase succeeded; false otherwise.</returns>
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

            // Prevent re-purchasing an already-owned part.
            if (_inventory != null && _inventory.HasPart(part.PartId))
            {
                Debug.Log($"[ShopManager] '{part.DisplayName}' is already owned — purchase skipped.");
                return false;
            }

            bool success = _wallet.Deduct(part.Cost);

            if (success)
            {
                // Record ownership and persist wallet + inventory snapshot to disk.
                _inventory?.UnlockPart(part.PartId);
                PersistPurchase();

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
        /// Returns true if the player already owns <paramref name="part"/>.
        /// Always returns false when <c>_inventory</c> is not assigned.
        /// </summary>
        public bool IsOwned(PartDefinition part) =>
            part != null && _inventory != null && _inventory.HasPart(part.PartId);

        /// <summary>
        /// Returns the catalog assigned to this ShopManager.
        /// UI panels read this once during initialisation to build the part list.
        /// </summary>
        public ShopCatalog Catalog => _catalog;

        // ── Private ───────────────────────────────────────────────────────────

        /// <summary>
        /// Persists the current wallet balance and owned-part list to disk.
        ///
        /// Pattern: Load existing save → update wallet + inventory fields → Save.
        /// This preserves match history that was already in the file.
        /// Called only on the cold path (after a successful purchase).
        /// </summary>
        private void PersistPurchase()
        {
            if (_wallet == null) return;

            SaveData save = SaveSystem.Load();
            save.walletBalance = _wallet.Balance;

            if (_inventory != null)
            {
                // Replace the persisted list with the current runtime state.
                save.unlockedPartIds.Clear();
                IReadOnlyList<string> owned = _inventory.UnlockedPartIds;
                for (int i = 0; i < owned.Count; i++)
                    save.unlockedPartIds.Add(owned[i]);
            }

            SaveSystem.Save(save);
        }

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
            if (_inventory == null)
                Debug.LogWarning("[ShopManager] _inventory PlayerInventory not assigned — " +
                                 "ownership tracking disabled (already-owned parts can be re-purchased).");
        }
#endif
    }
}
