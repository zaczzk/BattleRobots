using System;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// MonoBehaviour that bridges combat events into the wave-survival lifecycle.
    ///
    /// ── Responsibilities ──────────────────────────────────────────────────────
    ///   • Start a survival run via <see cref="StartSurvival"/> (wire to a UI button
    ///     or GameEvent listener).
    ///   • When the last bot in a wave is defeated (<c>_onWaveCompleted</c> fires):
    ///       – Awards credits for the completed wave via <see cref="WaveConfigSO.GetRewardForWave"/>.
    ///       – Persists <see cref="WaveManagerSO.BestWave"/> to <see cref="SaveData"/>.
    ///       – Advances to the next wave via <see cref="WaveManagerSO.StartNextWave"/>.
    ///   • When the player's robot is destroyed (<c>_onPlayerDeath</c> fires):
    ///       – Calls <see cref="WaveManagerSO.EndSurvival"/>.
    ///       – Persists <see cref="WaveManagerSO.BestWave"/> to <see cref="SaveData"/>.
    ///
    /// ── Scene wiring instructions ─────────────────────────────────────────────
    ///   1. Add this component to any persistent manager GameObject.
    ///   2. Assign <c>_waveConfig</c> and <c>_waveManager</c> SO assets.
    ///   3. Assign <c>_playerWallet</c> SO for per-wave credit rewards.
    ///   4. Assign <c>_onWaveCompleted</c> → the same VoidGameEvent as
    ///      <see cref="WaveManagerSO._onWaveCompleted"/> (so this MB reacts to it).
    ///   5. Assign <c>_onPlayerDeath</c> → a VoidGameEvent raised by the player's
    ///      robot death handler (e.g. a VoidGameEventListener on the HealthSO's
    ///      _onDeath channel, or directly from your robot AI script).
    ///   6. Call <see cref="StartSurvival"/> from a button or GameEvent at run start.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   • BattleRobots.Core namespace — no UI / Physics namespace references.
    ///   • Delegates cached in Awake; zero heap alloc after initialisation.
    ///   • No Update / FixedUpdate — entirely event-driven.
    ///   • All fields optional and null-guarded.
    ///   • SaveSystem called only on cold paths (wave end / survival end).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SurvivalMatchManager : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("Escalation config: bot counts per wave and credit rewards. " +
                 "Assign the same asset used by WaveManagerSO.")]
        [SerializeField] private WaveConfigSO _waveConfig;

        [Tooltip("Runtime SO that tracks wave state and fires lifecycle events. " +
                 "Assign the same asset used by WaveController.")]
        [SerializeField] private WaveManagerSO _waveManager;

        [Header("Economy")]
        [Tooltip("Player wallet SO. Credits for completing each wave are deposited here. " +
                 "Leave null to skip credit awards (backwards-compatible).")]
        [SerializeField] private PlayerWallet _playerWallet;

        [Header("Event Channels — In")]
        [Tooltip("Raised by WaveManagerSO when the last bot in the current wave is defeated. " +
                 "→ Awards credits, persists BestWave, and advances to the next wave.")]
        [SerializeField] private VoidGameEvent _onWaveCompleted;

        [Tooltip("Raised when the player's robot is destroyed (route HealthSO._onDeath here " +
                 "via a VoidGameEventListener). " +
                 "→ Ends the survival run and persists BestWave.")]
        [SerializeField] private VoidGameEvent _onPlayerDeath;

        // ── Cached delegates (allocated once in Awake) ─────────────────────────

        private Action _handleWaveCompletedDelegate;
        private Action _handlePlayerDeathDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleWaveCompletedDelegate = HandleWaveCompleted;
            _handlePlayerDeathDelegate   = HandlePlayerDeath;
        }

        private void OnEnable()
        {
            _onWaveCompleted?.RegisterCallback(_handleWaveCompletedDelegate);
            _onPlayerDeath?.RegisterCallback(_handlePlayerDeathDelegate);
        }

        private void OnDisable()
        {
            _onWaveCompleted?.UnregisterCallback(_handleWaveCompletedDelegate);
            _onPlayerDeath?.UnregisterCallback(_handlePlayerDeathDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Begins a new survival run from wave 1.
        /// Calls <see cref="WaveManagerSO.StartSurvival"/> with the configured
        /// <see cref="WaveConfigSO"/>.
        /// No-op when <see cref="_waveManager"/> or <see cref="_waveConfig"/> is null.
        /// Wire to a UI button or a GameEvent listener on the survival-start event.
        /// </summary>
        public void StartSurvival()
        {
            if (_waveManager == null || _waveConfig == null) return;

            _waveManager.StartSurvival(_waveConfig);
        }

        /// <summary>
        /// Called when the last bot in the current wave is defeated (via
        /// <c>_onWaveCompleted</c> subscription).
        /// Awards per-wave credits, persists BestWave, then advances to the next wave.
        /// No-op when <see cref="_waveManager"/> or <see cref="_waveConfig"/> is null.
        /// </summary>
        public void HandleWaveCompleted()
        {
            if (_waveManager == null || _waveConfig == null) return;

            // Credit reward for the wave just completed.
            // CurrentWave still reflects the wave that was just finished
            // (it only increments in StartNextWave).
            int   completedWave = _waveManager.CurrentWave;
            float rewardFloat   = _waveConfig.GetRewardForWave(completedWave);
            int   reward        = Mathf.RoundToInt(rewardFloat);

            if (_playerWallet != null && reward > 0)
                _playerWallet.AddFunds(reward);

            // Persist the best-wave record before advancing so the save always
            // reflects the highest wave the player has successfully cleared.
            PersistBestWave();

            // Advance to the next wave — raises _onWaveStarted internally.
            _waveManager.StartNextWave(_waveConfig);
        }

        /// <summary>
        /// Called when the player's robot is destroyed (via <c>_onPlayerDeath</c>
        /// subscription).
        /// Ends the survival run via <see cref="WaveManagerSO.EndSurvival"/> and
        /// persists BestWave to <see cref="SaveData"/>.
        /// No-op when <see cref="_waveManager"/> is null or the run is not active.
        /// </summary>
        public void HandlePlayerDeath()
        {
            if (_waveManager == null || !_waveManager.IsActive) return;

            _waveManager.EndSurvival();
            PersistBestWave();
        }

        // ── Internal ──────────────────────────────────────────────────────────

        /// <summary>
        /// Writes <see cref="WaveManagerSO.BestWave"/> into <see cref="SaveData"/>
        /// using the standard load → mutate → save round-trip.
        /// No-op when <see cref="_waveManager"/> is null.
        /// </summary>
        private void PersistBestWave()
        {
            if (_waveManager == null) return;

            SaveData save = SaveSystem.Load();
            save.survivalBestWave = _waveManager.BestWave;
            SaveSystem.Save(save);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_waveConfig == null)
                Debug.LogWarning("[SurvivalMatchManager] _waveConfig not assigned — " +
                                 "StartSurvival and HandleWaveCompleted will be no-ops.");
            if (_waveManager == null)
                Debug.LogWarning("[SurvivalMatchManager] _waveManager not assigned — " +
                                 "all survival operations will be no-ops.");
        }
#endif
    }
}
