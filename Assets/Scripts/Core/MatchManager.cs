using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Manages a single match round: countdown timer, win-condition checking,
    /// MatchRecord writing, and economy reward distribution.
    ///
    /// ── Win conditions (checked every frame while match is running) ───────────
    ///   1. Enemy HealthSO reaches zero   → player wins.
    ///   2. Player HealthSO reaches zero  → enemy wins.
    ///   3. Timer expires                 → combatant with more health wins;
    ///                                      tie counts as a player loss.
    ///
    /// ── Scene wiring instructions ─────────────────────────────────────────────
    ///   • Add a VoidGameEventListener to the same GameObject.
    ///     Event = MatchStarted SO, Response = MatchManager.HandleMatchStarted().
    ///   • Assign _playerHealth and _enemyHealth HealthSO assets.
    ///   • Assign _playerWallet PlayerWallet SO.
    ///   • Assign _onMatchEnded and (optionally) _onTimerUpdated event channel SOs.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - No heap allocations in Update (only float/bool comparisons).
    ///   - SaveSystem called only in EndMatch (cold path).
    ///   - BattleRobots.Core only — no Physics/UI namespace references.
    ///   - Cross-component signalling via SO event channels.
    /// </summary>
    public sealed class MatchManager : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Round Settings")]
        [Tooltip("Maximum duration of one round in seconds.")]
        [SerializeField, Min(10f)] private float _roundDuration = 120f;

        [Header("Economy")]
        [Tooltip("Currency awarded to the player for winning a match.")]
        [SerializeField, Min(0)] private int _winReward = 200;

        [Tooltip("Consolation currency awarded even on a loss.")]
        [SerializeField, Min(0)] private int _lossConsolationReward = 50;

        [Header("Combatants")]
        [Tooltip("HealthSO for the local player robot.")]
        [SerializeField] private HealthSO _playerHealth;

        [Tooltip("HealthSO for the enemy robot (AI or remote).")]
        [SerializeField] private HealthSO _enemyHealth;

        [Header("Wallet")]
        [SerializeField] private PlayerWallet _playerWallet;

        [Header("Event Channels — Out")]
        [Tooltip("Raised when the match ends (win or loss).")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        [Tooltip("Raised every frame while match is running. Payload = seconds remaining.")]
        [SerializeField] private FloatGameEvent _onTimerUpdated;

        [Header("Audio")]
        [Tooltip("AudioEvent SO played when the player wins the match.")]
        [SerializeField] private AudioEvent _onWinJingle;

        [Tooltip("AudioEvent SO played when the player loses the match.")]
        [SerializeField] private AudioEvent _onLossJingle;

        // ── Runtime state ─────────────────────────────────────────────────────

        /// <summary>True while a round is active.</summary>
        public bool IsMatchRunning => _matchRunning;

        /// <summary>Seconds remaining in the current round. Zero when not running.</summary>
        public float TimeRemaining => _timeRemaining;

        private bool  _matchRunning;
        private float _timeRemaining;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Update()
        {
            if (!_matchRunning) return;

            // Decrement timer — no allocation, pure float math
            _timeRemaining -= Time.deltaTime;
            _onTimerUpdated?.Raise(_timeRemaining);

            // Check death win conditions
            if (_enemyHealth != null && _enemyHealth.IsDead)
            {
                EndMatch(playerWon: true);
                return;
            }

            if (_playerHealth != null && _playerHealth.IsDead)
            {
                EndMatch(playerWon: false);
                return;
            }

            // Timer expiry — most health wins; tie is a loss for the player
            if (_timeRemaining <= 0f)
            {
                bool playerHasMoreHealth =
                    _playerHealth != null &&
                    _enemyHealth  != null &&
                    _playerHealth.CurrentHealth > _enemyHealth.CurrentHealth;

                EndMatch(playerWon: playerHasMoreHealth);
            }
        }

        // ── Public API (called by VoidGameEventListener response) ─────────────

        /// <summary>
        /// Resets both HealthSOs and starts the round timer.
        /// Wire to a VoidGameEventListener responding to the MatchStarted SO channel.
        /// Safe to call from the Inspector Response UnityEvent.
        /// </summary>
        public void HandleMatchStarted()
        {
            if (_playerHealth == null || _enemyHealth == null)
            {
                Debug.LogError("[MatchManager] Player or enemy HealthSO not assigned — match cannot start.");
                return;
            }

            _playerHealth.Reset();
            _enemyHealth.Reset();

            _timeRemaining = _roundDuration;
            _matchRunning  = true;

            // Broadcast initial timer value so UI can display it immediately
            _onTimerUpdated?.Raise(_timeRemaining);

            Debug.Log($"[MatchManager] Match started. Round duration: {_roundDuration}s.");
        }

        // ── Internal ──────────────────────────────────────────────────────────

        private void EndMatch(bool playerWon)
        {
            // Guard against re-entry (both HealthSOs could die in the same frame)
            if (!_matchRunning) return;
            _matchRunning = false;

            float elapsed    = _roundDuration - Mathf.Max(0f, _timeRemaining);
            int   reward     = playerWon ? _winReward : _lossConsolationReward;
            float damageDone = _enemyHealth  != null
                ? _enemyHealth.MaxHealth  - _enemyHealth.CurrentHealth  : 0f;
            float damageTaken = _playerHealth != null
                ? _playerHealth.MaxHealth - _playerHealth.CurrentHealth : 0f;

            // Award currency
            if (_playerWallet != null)
                _playerWallet.AddFunds(reward);

            int walletSnapshot = _playerWallet != null ? _playerWallet.Balance : 0;

            // Build and persist match record
            var record = new MatchRecord
            {
                timestamp       = DateTime.UtcNow.ToString("o"),
                playerWon       = playerWon,
                durationSeconds = elapsed,
                damageDone      = damageDone,
                damageTaken     = damageTaken,
                currencyEarned  = reward,
                walletSnapshot  = walletSnapshot,
                equippedPartIds = new List<string>(), // populated by RobotAssembly in M2+
            };

            // Append to save file — load, mutate, save
            SaveData saveData = SaveSystem.Load();
            saveData.walletBalance = walletSnapshot;
            saveData.matchHistory.Add(record);
            SaveSystem.Save(saveData);

            // Signal other systems
            _onMatchEnded?.Raise();

            // Audio jingle
            if (playerWon) _onWinJingle?.Raise();
            else           _onLossJingle?.Raise();

            Debug.Log($"[MatchManager] Match ended. PlayerWon={playerWon}, " +
                      $"Duration={elapsed:F1}s, Reward={reward}, Wallet={walletSnapshot}.");
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_playerHealth == null)
                Debug.LogWarning("[MatchManager] _playerHealth HealthSO not assigned.");
            if (_enemyHealth == null)
                Debug.LogWarning("[MatchManager] _enemyHealth HealthSO not assigned.");
            if (_playerWallet == null)
                Debug.LogWarning("[MatchManager] _playerWallet PlayerWallet SO not assigned.");
            if (_onMatchEnded == null)
                Debug.LogWarning("[MatchManager] _onMatchEnded event channel not assigned.");
        }
#endif
    }
}
