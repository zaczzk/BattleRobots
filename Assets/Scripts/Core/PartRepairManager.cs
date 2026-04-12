using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// MonoBehaviour that allows players to spend credits repairing damaged parts
    /// between matches.
    ///
    /// ── Purpose ─────────────────────────────────────────────────────────────
    ///   After each match, parts may be below full HP (tracked by <see cref="PartConditionSO"/>
    ///   and persisted via <see cref="PartConditionRegistry"/>). This manager provides
    ///   <see cref="RepairPart"/> and <see cref="RepairAll"/> APIs that:
    ///     1. Deduct the repair cost from <see cref="PlayerWallet"/>.
    ///     2. Heal the part fully via <see cref="PartConditionSO.Repair"/>.
    ///     3. Persist the updated HP ratios and wallet balance via the
    ///        load → mutate → save round-trip.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - All inspector fields are optional — null dependencies cause early-returns,
    ///     never exceptions.
    ///   - No Update / FixedUpdate. No heap allocations in RepairPart.
    ///   - Persistence follows the standard XOR SaveSystem load→mutate→save round-trip.
    ///
    /// ── Scene wiring ────────────────────────────────────────────────────────
    ///   1. Add to the Bootstrap / Workshop scene (DisallowMultipleComponent).
    ///   2. Assign _registry     → PartConditionRegistry SO.
    ///   3. Assign _wallet       → PlayerWallet SO.
    ///   4. Assign _repairConfig → PartRepairConfig SO.
    ///   5. Optionally assign _onRepairApplied → VoidGameEvent for UI/audio refresh.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PartRepairManager : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Dependencies")]
        [Tooltip("Registry mapping part IDs to their PartConditionSO assets.")]
        [SerializeField] private PartConditionRegistry _registry;

        [Tooltip("Runtime wallet SO. Credits are deducted here and persisted.")]
        [SerializeField] private PlayerWallet _wallet;

        [Tooltip("Config SO defining credits charged per missing HP point.")]
        [SerializeField] private PartRepairConfig _repairConfig;

        [Header("Event Channels — Out (optional)")]
        [Tooltip("Raised after each successful repair or RepairAll batch. " +
                 "Wire to the repair UI or audio for feedback.")]
        [SerializeField] private VoidGameEvent _onRepairApplied;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the credit cost to fully repair the part identified by
        /// <paramref name="partId"/>, or 0 when any dependency is null or
        /// the part is unknown / already at full HP.
        /// </summary>
        public int GetRepairCost(string partId)
        {
            if (_registry     == null) return 0;
            if (_repairConfig == null) return 0;
            var condition = _registry.GetCondition(partId);
            return condition == null ? 0 : _repairConfig.GetRepairCost(condition);
        }

        /// <summary>
        /// Attempts to fully repair the part identified by <paramref name="partId"/>.
        /// Deducts the repair cost from the wallet and persists the updated state.
        /// <br/>
        /// Returns <c>false</c> when:
        ///   • <c>_registry</c>, <c>_wallet</c>, or <c>_repairConfig</c> is null,
        ///   • the part ID is unknown in the registry,
        ///   • the part is already at full HP (cost = 0 → nothing to repair),
        ///   • the wallet balance is insufficient.
        /// </summary>
        public bool RepairPart(string partId)
        {
            if (_registry     == null) return false;
            if (_wallet       == null) return false;
            if (_repairConfig == null) return false;

            var condition = _registry.GetCondition(partId);
            if (condition == null) return false;

            int cost = _repairConfig.GetRepairCost(condition);
            if (cost == 0) return false;              // already at full HP

            if (_wallet.Balance < cost) return false; // insufficient funds

            // Deduct credits (we already verified balance is sufficient).
            _wallet.Deduct(cost);

            // Heal the part to full HP.
            condition.Repair(condition.MaxHP - condition.CurrentHP);

            // Persist updated conditions and wallet balance.
            PersistConditions();

            // Notify subscribers.
            _onRepairApplied?.Raise();

            return true;
        }

        /// <summary>
        /// Repairs all damaged parts the wallet can currently afford, in registry order.
        /// Parts whose repair cost exceeds the remaining balance are skipped (not cancelled).
        /// Fires <see cref="_onRepairApplied"/> once if any part was repaired.
        /// Returns the total credits spent. Returns 0 when any required dependency is null.
        /// </summary>
        public int RepairAll()
        {
            if (_registry     == null) return 0;
            if (_wallet       == null) return 0;
            if (_repairConfig == null) return 0;

            var damaged    = _registry.GetDamagedParts();
            int totalSpent = 0;

            for (int i = 0; i < damaged.Count; i++)
            {
                var entry = damaged[i];
                if (entry.condition == null) continue;

                int cost = _repairConfig.GetRepairCost(entry.condition);
                if (cost == 0) continue;
                if (_wallet.Balance < cost) continue; // can't afford — skip, try next

                _wallet.Deduct(cost);
                entry.condition.Repair(entry.condition.MaxHP - entry.condition.CurrentHP);
                totalSpent += cost;
            }

            if (totalSpent > 0)
            {
                PersistConditions();
                _onRepairApplied?.Raise();
            }

            return totalSpent;
        }

        // ── Internal helpers ──────────────────────────────────────────────────

        /// <summary>
        /// Persists the current part-condition HP ratios and wallet balance in a single
        /// load → mutate → save round-trip so no other SaveData field is disturbed.
        /// </summary>
        private void PersistConditions()
        {
            SaveData save = SaveSystem.Load();
            save.savedPartConditions = _registry.TakeSnapshot();
            save.walletBalance       = _wallet.Balance;
            SaveSystem.Save(save);
        }
    }
}
