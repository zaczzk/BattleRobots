using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Immutable SO that encodes how an endless survival run escalates across waves.
    /// Consumed by <see cref="WaveManagerSO"/> to compute bot counts and rewards.
    ///
    /// ── Bot count formula ─────────────────────────────────────────────────────
    ///   botsInWave = Clamp(BaseBotsPerWave + BotCountIncrement × (wave − 1),
    ///                      1, MaxBotsPerWave)
    ///
    /// ── Reward formula ───────────────────────────────────────────────────────
    ///   reward = BaseWaveReward + BonusRewardPerWave × Max(0, wave − 1)
    ///
    /// ── Opponent roster (optional) ────────────────────────────────────────────
    ///   When <see cref="Roster"/> is non-null the wave manager can select the
    ///   opponent profile for each wave using (wave − 1) % Roster.Count.
    ///   When null the AI uses its own scene-configured BotDifficultyConfig.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Wave ▶ WaveConfig.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Wave/WaveConfig", order = 30)]
    public sealed class WaveConfigSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Bot Counts")]
        [Tooltip("Number of bots the player must defeat in wave 1. Minimum 1.")]
        [SerializeField, Min(1)] private int _baseBotsPerWave = 1;

        [Tooltip("Additional bots added to each subsequent wave. 0 keeps count constant.")]
        [SerializeField, Min(0)] private int _botCountIncrement = 1;

        [Tooltip("Maximum bots allowed in any single wave. Must be ≥ BaseBotsPerWave.")]
        [SerializeField, Min(1)] private int _maxBotsPerWave = 10;

        [Header("Rewards")]
        [Tooltip("Credits awarded on completing wave 1.")]
        [SerializeField, Min(0f)] private float _baseWaveReward = 50f;

        [Tooltip("Extra credits added per wave beyond wave 1 (stacks linearly).")]
        [SerializeField, Min(0f)] private float _bonusRewardPerWave = 10f;

        [Header("Opponent Roster (optional)")]
        [Tooltip("When assigned, the opponent profile for each wave is selected by " +
                 "(wave − 1) % Roster.Count. Leave null to use the AI's own config.")]
        [SerializeField] private OpponentRosterSO _roster;

        // ── Properties (read-only) ────────────────────────────────────────────

        /// <summary>Bots in wave 1 before any increment is applied.</summary>
        public int BaseBotsPerWave => _baseBotsPerWave;

        /// <summary>Extra bots added to each successive wave (0 = constant count).</summary>
        public int BotCountIncrement => _botCountIncrement;

        /// <summary>Hard cap on bots per wave regardless of formula output.</summary>
        public int MaxBotsPerWave => _maxBotsPerWave;

        /// <summary>Credits awarded on completing wave 1.</summary>
        public float BaseWaveReward => _baseWaveReward;

        /// <summary>Extra credits stacked per wave beyond wave 1.</summary>
        public float BonusRewardPerWave => _bonusRewardPerWave;

        /// <summary>Optional roster used to select per-wave opponent profiles.</summary>
        public OpponentRosterSO Roster => _roster;

        // ── Public helpers ────────────────────────────────────────────────────

        /// <summary>
        /// Returns the number of bots the player must defeat in <paramref name="waveNumber"/>.
        /// <paramref name="waveNumber"/> &lt; 1 is treated as 1.
        /// Result is clamped to [1, <see cref="MaxBotsPerWave"/>].
        /// </summary>
        public int GetBotsForWave(int waveNumber)
        {
            int wave = Mathf.Max(1, waveNumber);
            int count = _baseBotsPerWave + _botCountIncrement * (wave - 1);
            return Mathf.Clamp(count, 1, _maxBotsPerWave);
        }

        /// <summary>
        /// Returns the credit reward for completing <paramref name="waveNumber"/>.
        /// <paramref name="waveNumber"/> &lt; 1 is treated as 1 (returns base reward).
        /// Result is always ≥ <see cref="BaseWaveReward"/>.
        /// </summary>
        public float GetRewardForWave(int waveNumber)
        {
            int wave = Mathf.Max(1, waveNumber);
            return _baseWaveReward + _bonusRewardPerWave * (wave - 1);
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_baseBotsPerWave < 1)
                _baseBotsPerWave = 1;

            if (_maxBotsPerWave < _baseBotsPerWave)
            {
                _maxBotsPerWave = _baseBotsPerWave;
                Debug.LogWarning($"[WaveConfigSO] '{name}': MaxBotsPerWave must be ≥ " +
                                 $"BaseBotsPerWave ({_baseBotsPerWave}). Clamped.");
            }
        }
#endif
    }
}
