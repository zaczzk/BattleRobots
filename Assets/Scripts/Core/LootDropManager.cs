using System;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// MonoBehaviour that awards a random post-match part drop when the player wins.
    ///
    /// ── Drop flow ─────────────────────────────────────────────────────────────
    ///   On <c>_onMatchEnded</c>:
    ///     1. Guard: PlayerWon must be true (reads <see cref="MatchResultSO"/>).
    ///     2. Drop-chance roll: <c>Random.value &lt; _lootTable.WinDropChance</c>.
    ///     3. Part selection: <see cref="LootTableSO.RollDrop"/> with a random seed.
    ///     4. Duplicate guard: if the player already owns the part, no drop occurs.
    ///     5. Unlock: part added to <see cref="PlayerInventory"/> and persisted to disk.
    ///     6. Notification: optional toast via <see cref="NotificationQueueSO"/>.
    ///
    /// ── Testable API ─────────────────────────────────────────────────────────
    ///   <see cref="AttemptDrop"/> accepts explicit <paramref name="dropRoll"/> and
    ///   <paramref name="lootSeed"/> values so EditMode tests can drive the full drop
    ///   path deterministically — pass <c>dropRoll = 0f</c> to bypass the chance
    ///   check and a fixed seed for a predictable table selection.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace — no Physics / UI references.
    ///   - All SO fields are optional and null-safe; the system degrades gracefully.
    ///   - <see cref="Action"/> delegate cached in Awake — zero alloc in callbacks.
    ///   - Uses the load → mutate → save pattern for persistence (same as ShopManager).
    ///
    /// ── Scene / SO wiring ─────────────────────────────────────────────────────
    ///   1. Add this MB to any persistent GameObject (e.g. the GameBootstrapper root).
    ///   2. Assign <c>_lootTable</c> (required for any drop to occur).
    ///   3. Assign <c>_inventory</c> and <c>_matchResult</c> (required for drop logic).
    ///   4. Assign <c>_onMatchEnded</c> — the same VoidGameEvent as MatchManager.
    ///   5. Optionally assign <c>_notificationQueue</c> for in-game toast on drop.
    /// </summary>
    public sealed class LootDropManager : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Loot")]
        [Tooltip("Weighted loot table defining which parts can drop and at what probability. " +
                 "Leave null to disable the loot-drop system entirely.")]
        [SerializeField] private LootTableSO _lootTable;

        [Tooltip("Optional rarity config that scales each entry's base weight by its rarity " +
                 "multiplier before the cumulative walk.  Leave null to use unscaled weights " +
                 "(identical to the base RollDrop overload).")]
        [SerializeField] private PartRarityConfig _rarityConfig;

        [Header("Player Data")]
        [Tooltip("Runtime inventory SO — receives the unlocked part on a successful drop. " +
                 "Leave null to skip inventory update (persistence still runs).")]
        [SerializeField] private PlayerInventory _inventory;

        [Tooltip("Blackboard written by MatchManager before _onMatchEnded fires. " +
                 "PlayerWon is read to gate drops to wins only. " +
                 "Leave null to treat every match end as a loss (no drops).")]
        [SerializeField] private MatchResultSO _matchResult;

        [Header("Notifications (optional)")]
        [Tooltip("When assigned, enqueues a 'Loot Drop!' toast on a successful drop. " +
                 "Leave null to suppress the notification.")]
        [SerializeField] private NotificationQueueSO _notificationQueue;

        [Header("Event Channels — In")]
        [Tooltip("VoidGameEvent raised by MatchManager when a match ends. " +
                 "LootDropManager evaluates and potentially awards a drop.")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        // ── Cached delegate (allocated once in Awake — zero alloc in callback) ─

        private Action _handleMatchEndedDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleMatchEndedDelegate = HandleMatchEnded;
        }

        private void OnEnable()  => _onMatchEnded?.RegisterCallback(_handleMatchEndedDelegate);
        private void OnDisable() => _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);

        // ── Internal event handler ────────────────────────────────────────────

        private void HandleMatchEnded()
        {
            AttemptDrop(
                UnityEngine.Random.value,
                UnityEngine.Random.Range(int.MinValue, int.MaxValue));
        }

        // ── Testable public API ───────────────────────────────────────────────

        /// <summary>
        /// Evaluates and potentially executes a loot drop.
        ///
        /// <para>
        /// Exposed as a public method so EditMode tests can control the random values:
        /// pass <paramref name="dropRoll"/> = 0f and <paramref name="lootSeed"/> = any
        /// fixed integer to guarantee a drop attempt and a deterministic part selection.
        /// </para>
        ///
        /// <para>Guards (all must pass for a drop to occur):</para>
        /// <list type="bullet">
        ///   <item><description><c>_lootTable</c> is not null and has valid entries.</description></item>
        ///   <item><description><c>_matchResult.PlayerWon</c> is true.</description></item>
        ///   <item><description><paramref name="dropRoll"/> &lt; <see cref="LootTableSO.WinDropChance"/>.</description></item>
        ///   <item><description><see cref="LootTableSO.RollDrop"/> returns a non-null part.</description></item>
        ///   <item><description>The player does not already own the rolled part.</description></item>
        /// </list>
        /// </summary>
        /// <param name="dropRoll">
        /// Value in [0, 1] compared against <see cref="LootTableSO.WinDropChance"/>.
        /// Values below WinDropChance allow the drop; values at or above skip it.
        /// </param>
        /// <param name="lootSeed">Seed forwarded to <see cref="LootTableSO.RollDrop"/>.</param>
        public void AttemptDrop(float dropRoll, int lootSeed)
        {
            if (_lootTable == null)                              return;
            if (_matchResult == null || !_matchResult.PlayerWon) return;
            if (dropRoll >= _lootTable.WinDropChance)            return;

            PartDefinition drop = _lootTable.RollDrop(lootSeed, _rarityConfig);
            if (drop == null) return;

            // Duplicate guard — only reward parts the player doesn't own yet.
            if (_inventory != null && _inventory.HasPart(drop.PartId)) return;

            AddToInventoryAndPersist(drop);
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void AddToInventoryAndPersist(PartDefinition drop)
        {
            // Update runtime inventory SO.
            _inventory?.UnlockPart(drop.PartId);

            // Persist via load → mutate → save (does not disturb other SaveData fields).
            SaveData save = SaveSystem.Load();
            if (!save.unlockedPartIds.Contains(drop.PartId))
                save.unlockedPartIds.Add(drop.PartId);
            SaveSystem.Save(save);

            // Optional toast notification.
            _notificationQueue?.Enqueue("Loot Drop!", drop.DisplayName + " received!");

            Debug.Log($"[LootDropManager] Loot drop awarded: '{drop.DisplayName}' (id: '{drop.PartId}').");
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_lootTable == null)
                Debug.LogWarning(
                    "[LootDropManager] _lootTable not assigned — no loot will ever drop.", this);

            if (_matchResult == null)
                Debug.LogWarning(
                    "[LootDropManager] _matchResult not assigned — match outcome cannot be read; " +
                    "drops will be suppressed.", this);

            if (_onMatchEnded == null)
                Debug.LogWarning(
                    "[LootDropManager] _onMatchEnded not assigned — drops will never be triggered.", this);
        }
#endif
    }
}
