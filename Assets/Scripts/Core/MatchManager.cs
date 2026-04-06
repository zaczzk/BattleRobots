using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Manages a single match: initialises robots, runs the round countdown,
    /// detects the win condition, writes a <see cref="MatchRecord"/>, and persists.
    ///
    /// Architecture constraints:
    ///   • <c>BattleRobots.Core</c> namespace — no Physics or UI references.
    ///   • No heap allocations in Update (value-type locals, pre-cached array).
    ///   • Cross-component communication via SO event channels.
    ///   • PlayerWallet mutated only via AddFunds — never written directly.
    ///
    /// Wire-up (Inspector):
    ///   1. Assign ArenaConfig, HealthSO[2] (index 0 = player), PlayerWallet.
    ///   2. Optionally assign PlayerRobotDefinition to capture equippedPartIds.
    ///   3. Assign event channel SOs (_onMatchEnd, _onPlayerWon, _onPlayerLost).
    ///   4. Call StartMatch() after robots are spawned and ready.
    /// </summary>
    public sealed class MatchManager : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Arena")]
        [Tooltip("Describes the arena: time limit, win bonus, spawn layout. " +
                 "If ArenaSelectionSO is also assigned, its SelectedArena takes priority.")]
        [SerializeField] private ArenaConfig _arenaConfig;

        [Tooltip("Optional runtime SO set by the ArenaSelector screen. " +
                 "When assigned and HasSelection is true, overrides _arenaConfig.")]
        [SerializeField] private ArenaSelectionSO _arenaSelection;

        [Header("Combatants  (index 0 = player · index 1 = opponent)")]
        [Tooltip("One HealthSO per robot in match order. Must have at least 2 entries.")]
        [SerializeField] private HealthSO[] _robotHealthSOs;

        [Tooltip("RobotDefinition for the player robot. Used to populate MatchRecord.equippedPartIds.")]
        [SerializeField] private RobotDefinition _playerRobotDefinition;

        [Header("Economy")]
        [Tooltip("PlayerWallet SO. AddFunds is called on a player win.")]
        [SerializeField] private PlayerWallet _wallet;

        [Tooltip("Flat currency award on any win, before ArenaConfig.WinBonusCurrency is added.")]
        [SerializeField, Min(0)] private int _baseWinReward = 200;

        [Header("Event Channels")]
        [Tooltip("Raised after every match ends (win, loss, or time-out draw).")]
        [SerializeField] private VoidGameEvent _onMatchEnd;

        [Tooltip("Raised only when the player wins. Payload = total currency earned this match.")]
        [SerializeField] private IntGameEvent _onPlayerWon;

        [Tooltip("Raised only when the player loses or the match ends in a draw.")]
        [SerializeField] private VoidGameEvent _onPlayerLost;

        // ── Runtime State ─────────────────────────────────────────────────────

        /// <summary>
        /// The arena to use for this match.
        /// Prefers <see cref="_arenaSelection"/>.SelectedArena when available;
        /// falls back to the Inspector-wired <see cref="_arenaConfig"/>.
        /// </summary>
        private ArenaConfig ActiveArena =>
            (_arenaSelection != null && _arenaSelection.HasSelection)
                ? _arenaSelection.SelectedArena
                : _arenaConfig;

        /// <summary>True while a match is in progress.</summary>
        public bool IsMatchActive => _matchActive;

        /// <summary>Seconds elapsed since <see cref="StartMatch"/> was last called.</summary>
        public float ElapsedSeconds => _elapsedSeconds;

        private float _elapsedSeconds;
        private bool  _matchActive;

        // Max-HP snapshots taken at StartMatch — used to compute damage totals
        // at match end without subscribing to per-frame damage events.
        private float _playerMaxHp;
        private float _opponentMaxHp;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Starts the match: validates dependencies, initialises all HealthSOs,
        /// and begins the timer.  Safe to call from any other MonoBehaviour after
        /// robots are placed at their spawn points.
        /// </summary>
        public void StartMatch()
        {
            if (_matchActive)
            {
                Debug.LogWarning("[MatchManager] StartMatch called while a match is already active.", this);
                return;
            }

            if (!ValidateDependencies())
                return;

            for (int i = 0; i < _robotHealthSOs.Length; i++)
                _robotHealthSOs[i].Initialize();

            // Use EffectiveMaxHp to account for any part-bonus applied by RobotSpawner.
            _playerMaxHp   = _robotHealthSOs[0].EffectiveMaxHp;
            _opponentMaxHp = _robotHealthSOs[1].EffectiveMaxHp;
            _elapsedSeconds = 0f;
            _matchActive    = true;

            Debug.Log($"[MatchManager] Match started. Arena: '{ActiveArena.ArenaName}' · " +
                      $"Time limit: {ActiveArena.TimeLimitSeconds}s.");
        }

        /// <summary>
        /// Immediately aborts the current match without writing a MatchRecord.
        /// Use for disconnects, editor stops, etc.
        /// </summary>
        public void AbortMatch()
        {
            if (!_matchActive) return;
            _matchActive = false;
            Debug.Log("[MatchManager] Match aborted (no record written).");
        }

        // ── Unity Lifecycle ───────────────────────────────────────────────────

        private void Update()
        {
            if (!_matchActive) return;

            // Advance timer — float add only, no allocation.
            _elapsedSeconds += Time.deltaTime;

            // Win condition: check both HealthSOs directly (array index, no LINQ).
            bool playerAlive   = _robotHealthSOs[0].IsAlive;
            bool opponentAlive = _robotHealthSOs[1].IsAlive;

            if (!playerAlive)
            {
                EndMatch(playerWon: false);
                return;
            }

            if (!opponentAlive)
            {
                EndMatch(playerWon: true);
                return;
            }

            // Time-limit expiry (0 means no limit).
            if (ActiveArena.TimeLimitSeconds > 0f &&
                _elapsedSeconds >= ActiveArena.TimeLimitSeconds)
            {
                EndMatch(playerWon: false); // draw → treated as loss for economy
            }
        }

        // ── Private Helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Called exactly once per match.  Allocations here (List, string) are
        /// intentional — EndMatch is never called from FixedUpdate hot path.
        /// </summary>
        private void EndMatch(bool playerWon)
        {
            // Deactivate immediately so Update skips on re-entry.
            _matchActive = false;

            // Damage totals derived from HP delta (no event subscription required).
            float damageTaken = _playerMaxHp   - _robotHealthSOs[0].CurrentHp;
            float damageDone  = _opponentMaxHp - _robotHealthSOs[1].CurrentHp;

            // Economy payout.
            int currencyEarned = 0;
            if (playerWon)
            {
                currencyEarned = _baseWinReward + ActiveArena.WinBonusCurrency;
                _wallet?.AddFunds(currencyEarned);
                _onPlayerWon?.Raise(currencyEarned);
            }
            else
            {
                _onPlayerLost?.Raise();
            }

            // Build MatchRecord.
            var record = new MatchRecord
            {
                timestamp       = DateTime.UtcNow.ToString("o"),
                arenaIndex      = ActiveArena.ArenaIndex,
                playerWon       = playerWon,
                durationSeconds = _elapsedSeconds,
                damageDone      = damageDone,
                damageTaken     = damageTaken,
                currencyEarned  = currencyEarned,
                walletSnapshot  = _wallet != null ? _wallet.Balance : 0,
                equippedPartIds = BuildEquippedPartIds(),
            };

            // Append to persistent save data.
            SaveData saveData    = SaveSystem.Load();
            saveData.walletBalance = _wallet != null ? _wallet.Balance : 0;
            saveData.matchHistory.Add(record);
            SaveSystem.Save(saveData);

            // Notify subscribers.
            _onMatchEnd?.Raise();

            Debug.Log($"[MatchManager] Match over. Result: {(playerWon ? "WIN" : "LOSS/DRAW")} · " +
                      $"Duration: {_elapsedSeconds:F1}s · Currency earned: {currencyEarned}.");
        }

        /// <summary>
        /// Builds the equippedPartIds list from the player's RobotDefinition.
        /// Returns an empty list if no definition is assigned.
        /// Called only from EndMatch — allocation is acceptable.
        /// </summary>
        private List<string> BuildEquippedPartIds()
        {
            if (_playerRobotDefinition == null)
                return new List<string>(0);

            IReadOnlyList<PartSlot> slots = _playerRobotDefinition.PartSlots;
            var ids = new List<string>(slots.Count);
            for (int i = 0; i < slots.Count; i++)
                ids.Add(slots[i].SlotId);
            return ids;
        }

        private bool ValidateDependencies()
        {
            if (ActiveArena == null)
            {
                Debug.LogError("[MatchManager] No ArenaConfig available. Assign _arenaConfig or " +
                               "ensure _arenaSelection has a valid selection before StartMatch.", this);
                return false;
            }

            if (_robotHealthSOs == null || _robotHealthSOs.Length < 2)
            {
                Debug.LogError("[MatchManager] _robotHealthSOs must have at least 2 entries " +
                               "(index 0 = player, index 1 = opponent).", this);
                return false;
            }

            for (int i = 0; i < _robotHealthSOs.Length; i++)
            {
                if (_robotHealthSOs[i] == null)
                {
                    Debug.LogError($"[MatchManager] _robotHealthSOs[{i}] is null.", this);
                    return false;
                }
            }

            if (_wallet == null)
                Debug.LogWarning("[MatchManager] PlayerWallet not assigned — win rewards will not be awarded.", this);

            return true;
        }
    }
}
