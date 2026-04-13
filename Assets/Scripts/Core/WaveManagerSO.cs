using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that tracks the state of an endless survival run.
    /// Producers (game logic) call <see cref="StartSurvival"/>,
    /// <see cref="StartNextWave"/>, <see cref="RecordBotDefeated"/>, and
    /// <see cref="EndSurvival"/> to drive the wave lifecycle.
    /// Consumers (<see cref="BattleRobots.UI.WaveController"/>) subscribe to the
    /// optional event channels and query properties to update the HUD.
    ///
    /// ── Wave lifecycle ────────────────────────────────────────────────────────
    ///   StartSurvival(config) → [combat: bots spawn] → RecordBotDefeated() × N
    ///     → _onWaveCompleted fired → StartNextWave(config) → …
    ///     → EndSurvival() [player defeated] → _onSurvivalEnded fired
    ///
    /// ── Persistence ───────────────────────────────────────────────────────────
    ///   <see cref="BestWave"/> is the only field that survives sessions.
    ///   <see cref="GameBootstrapper"/> calls <see cref="LoadSnapshot"/> with
    ///   <c>SaveData.survivalBestWave</c> on startup.
    ///   All other runtime fields are reset each time <see cref="StartSurvival"/>
    ///   is called — no explicit Reset() call needed between sessions.
    ///
    /// ── ARCHITECTURE RULES ────────────────────────────────────────────────────
    ///   • BattleRobots.Core namespace — no UI / Physics references.
    ///   • Runtime state (except BestWave) is not serialized — clears on reload.
    ///   • LoadSnapshot must NOT raise any event (bootstrapper context).
    ///   • All event raises are optional — null-guarded throughout.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Wave ▶ WaveManagerSO.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Wave/WaveManagerSO", order = 31)]
    public sealed class WaveManagerSO : ScriptableObject
    {
        // ── Inspector — event channels out ───────────────────────────────────

        [Header("Event Channels — Out")]
        [Tooltip("Raised when a new wave begins (StartSurvival or StartNextWave).")]
        [SerializeField] private VoidGameEvent _onWaveStarted;

        [Tooltip("Raised when the last bot in the current wave is defeated.")]
        [SerializeField] private VoidGameEvent _onWaveCompleted;

        [Tooltip("Raised when the survival run ends (EndSurvival called).")]
        [SerializeField] private VoidGameEvent _onSurvivalEnded;

        // ── Runtime state (not serialized) ────────────────────────────────────

        private int  _currentWave;
        private int  _botsRemainingInWave;
        private int  _totalBotsDefeated;
        private bool _isActive;

        // BestWave is seeded by LoadSnapshot and updated live — kept as runtime
        // field so SO asset files are never dirtied during play.
        private int _bestWave;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>1-based wave index. 0 when no run is active.</summary>
        public int CurrentWave => _currentWave;

        /// <summary>Bots still to be defeated in the current wave. 0 when run not active.</summary>
        public int BotsRemainingInWave => _botsRemainingInWave;

        /// <summary>Total bots defeated across all waves in the current run.</summary>
        public int TotalBotsDefeated => _totalBotsDefeated;

        /// <summary>True while a survival run is ongoing.</summary>
        public bool IsActive => _isActive;

        /// <summary>Highest wave reached across all sessions (persisted via SaveData).</summary>
        public int BestWave => _bestWave;

        // ── Producer API ──────────────────────────────────────────────────────

        /// <summary>
        /// Begins a new survival run from wave 1.
        /// Resets current-wave counters and raises <c>_onWaveStarted</c>.
        /// No-op when <paramref name="config"/> is null.
        /// </summary>
        public void StartSurvival(WaveConfigSO config)
        {
            if (config == null) return;

            _currentWave         = 1;
            _botsRemainingInWave = config.GetBotsForWave(1);
            _totalBotsDefeated   = 0;
            _isActive            = true;

            _onWaveStarted?.Raise();
        }

        /// <summary>
        /// Advances to the next wave.
        /// Increments <see cref="CurrentWave"/>, resets bot count for the new wave,
        /// and raises <c>_onWaveStarted</c>.
        /// No-op when <see cref="IsActive"/> is false or <paramref name="config"/> is null.
        /// </summary>
        public void StartNextWave(WaveConfigSO config)
        {
            if (!_isActive || config == null) return;

            _currentWave++;
            _botsRemainingInWave = config.GetBotsForWave(_currentWave);

            _onWaveStarted?.Raise();
        }

        /// <summary>
        /// Records one bot as defeated.
        /// Decrements <see cref="BotsRemainingInWave"/> (floor 0).
        /// Increments <see cref="TotalBotsDefeated"/>.
        /// When the last bot in the wave is defeated, updates <see cref="BestWave"/>
        /// and raises <c>_onWaveCompleted</c>.
        /// No-op when <see cref="IsActive"/> is false.
        /// </summary>
        public void RecordBotDefeated()
        {
            if (!_isActive) return;

            _botsRemainingInWave = Mathf.Max(0, _botsRemainingInWave - 1);
            _totalBotsDefeated++;

            if (_botsRemainingInWave == 0)
            {
                _bestWave = Mathf.Max(_bestWave, _currentWave);
                _onWaveCompleted?.Raise();
            }
        }

        /// <summary>
        /// Ends the survival run (player defeated or quit).
        /// Sets <see cref="IsActive"/> to false, updates <see cref="BestWave"/>,
        /// and raises <c>_onSurvivalEnded</c>.
        /// No-op when <see cref="IsActive"/> is already false.
        /// </summary>
        public void EndSurvival()
        {
            if (!_isActive) return;

            _isActive = false;
            _bestWave = Mathf.Max(_bestWave, _currentWave);
            _onSurvivalEnded?.Raise();
        }

        // ── Snapshot API ──────────────────────────────────────────────────────

        /// <summary>
        /// Restores the all-time best wave from the save file.
        /// Must NOT raise any event (called by <see cref="GameBootstrapper"/> at startup).
        /// Negative values are clamped to 0.
        /// </summary>
        public void LoadSnapshot(int bestWave)
        {
            _bestWave = Mathf.Max(0, bestWave);
        }

        /// <summary>
        /// Clears all runtime state silently (no events, BestWave unchanged).
        /// Intended for test teardown and explicit "reset session" scenarios.
        /// </summary>
        public void Reset()
        {
            _currentWave         = 0;
            _botsRemainingInWave = 0;
            _totalBotsDefeated   = 0;
            _isActive            = false;
        }
    }
}
