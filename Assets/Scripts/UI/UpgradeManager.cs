using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Mediates between the player's wallet, their part-upgrade data, and the
    /// upgrade configuration SO.
    ///
    /// ── Responsibilities ──────────────────────────────────────────────────────
    ///   • <see cref="UpgradePart"/> validates eligibility, deducts credits,
    ///     advances the tier, persists to disk, and fires the completion event.
    ///   • <see cref="GetCurrentTier"/> / <see cref="GetNextUpgradeCost"/> expose
    ///     read-only state for <see cref="UpgradeController"/> display without
    ///     coupling it to the SO directly.
    ///
    /// ── Guard conditions ──────────────────────────────────────────────────────
    ///   UpgradePart returns false (no side-effect) when:
    ///     • part is null
    ///     • _wallet / _upgrades / _upgradeConfig are not assigned
    ///     • the part is already at max tier
    ///     • the player has insufficient funds
    ///
    /// ── Architecture ─────────────────────────────────────────────────────────
    ///   BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   No Update / FixedUpdate — purely event-driven.
    ///   SaveSystem pattern: Load → mutate → Save (preserves existing match history).
    ///
    /// ── Scene wiring ─────────────────────────────────────────────────────────
    ///   • _wallet       → same PlayerWallet SO as GameBootstrapper
    ///   • _upgrades     → PlayerPartUpgrades SO (one global instance)
    ///   • _upgradeConfig→ PartUpgradeConfig SO (one global instance)
    ///   • _onUpgradeCompleted → VoidGameEvent SO to notify UI/audio
    /// </summary>
    public sealed class UpgradeManager : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("The player's runtime wallet SO.")]
        [SerializeField] private PlayerWallet _wallet;

        [Tooltip("Runtime SO tracking the upgrade tier for each owned part.")]
        [SerializeField] private PlayerPartUpgrades _upgrades;

        [Tooltip("SO defining tier costs and stat multipliers.")]
        [SerializeField] private PartUpgradeConfig _upgradeConfig;

        [Header("Event Channels — Out")]
        [Tooltip("Raised after every successful upgrade. Wire to audio, analytics, UI refresh.")]
        [SerializeField] private VoidGameEvent _onUpgradeCompleted;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Attempts to upgrade <paramref name="part"/> by one tier.
        ///
        /// On success:
        ///   1. Deducts the tier cost from <c>_wallet</c>.
        ///   2. Increments the tier in <c>_upgrades</c>.
        ///   3. Persists wallet balance + tier data to disk.
        ///   4. Raises <c>_onUpgradeCompleted</c>.
        ///
        /// Returns false without any side-effects on any guard failure.
        /// </summary>
        public bool UpgradePart(PartDefinition part)
        {
            if (part == null)
            {
                Debug.LogWarning("[UpgradeManager] UpgradePart called with null PartDefinition.");
                return false;
            }

            if (_wallet == null)
            {
                Debug.LogError("[UpgradeManager] _wallet (PlayerWallet SO) not assigned.");
                return false;
            }

            if (_upgrades == null)
            {
                Debug.LogError("[UpgradeManager] _upgrades (PlayerPartUpgrades SO) not assigned.");
                return false;
            }

            if (_upgradeConfig == null)
            {
                Debug.LogError("[UpgradeManager] _upgradeConfig (PartUpgradeConfig SO) not assigned.");
                return false;
            }

            int currentTier = _upgrades.GetTier(part.PartId);

            if (currentTier >= _upgradeConfig.MaxTier)
            {
                Debug.Log($"[UpgradeManager] '{part.DisplayName}' is already at max tier " +
                          $"{currentTier} — upgrade skipped.");
                return false;
            }

            int cost = _upgradeConfig.GetUpgradeCost(currentTier);
            if (cost < 0)
            {
                Debug.LogWarning($"[UpgradeManager] Invalid upgrade cost for " +
                                 $"'{part.DisplayName}' at tier {currentTier}.");
                return false;
            }

            if (!_wallet.Deduct(cost))
            {
                Debug.Log($"[UpgradeManager] Insufficient funds to upgrade '{part.DisplayName}' " +
                          $"(cost {cost}, balance {_wallet.Balance}).");
                return false;
            }

            _upgrades.SetTier(part.PartId, currentTier + 1);
            PersistUpgrade();
            _onUpgradeCompleted?.Raise();

            Debug.Log($"[UpgradeManager] Upgraded '{part.DisplayName}' to tier {currentTier + 1}. " +
                      $"Remaining balance: {_wallet.Balance}.");
            return true;
        }

        /// <summary>
        /// Returns the current upgrade tier for <paramref name="part"/>.
        /// Returns 0 when part is null or <c>_upgrades</c> is not assigned.
        /// </summary>
        public int GetCurrentTier(PartDefinition part)
        {
            if (part == null || _upgrades == null) return 0;
            return _upgrades.GetTier(part.PartId);
        }

        /// <summary>
        /// Returns the credit cost to upgrade <paramref name="part"/> to the next tier.
        /// Returns -1 when part is null, at max tier, or required SOs are unassigned.
        /// </summary>
        public int GetNextUpgradeCost(PartDefinition part)
        {
            if (part == null || _upgrades == null || _upgradeConfig == null) return -1;
            return _upgradeConfig.GetUpgradeCost(_upgrades.GetTier(part.PartId));
        }

        // ── Private ───────────────────────────────────────────────────────────

        /// <summary>
        /// Persists the current wallet balance and part-tier data to disk.
        /// Pattern: Load existing save → mutate → Save (preserves match history).
        /// </summary>
        private void PersistUpgrade()
        {
            if (_wallet == null || _upgrades == null) return;

            SaveData save = SaveSystem.Load();
            save.walletBalance = _wallet.Balance;
            _upgrades.TakeSnapshot(out save.upgradePartIds, out save.upgradePartTierValues);
            SaveSystem.Save(save);
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_wallet == null)
                Debug.LogWarning("[UpgradeManager] _wallet (PlayerWallet SO) not assigned.");
            if (_upgrades == null)
                Debug.LogWarning("[UpgradeManager] _upgrades (PlayerPartUpgrades SO) not assigned.");
            if (_upgradeConfig == null)
                Debug.LogWarning("[UpgradeManager] _upgradeConfig (PartUpgradeConfig SO) not assigned.");
            if (_onUpgradeCompleted == null)
                Debug.LogWarning("[UpgradeManager] _onUpgradeCompleted event channel not assigned.");
        }
#endif
    }
}
