using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that linearly ramps a normalised difficulty value from
    /// <c>_startDifficulty</c> to <c>_endDifficulty</c> over <c>_rampDuration</c>
    /// seconds.  Consumers (e.g. AI spawn interval controllers) can read
    /// <see cref="CurrentDifficulty"/> each frame.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   <see cref="StartRamp"/> arms the ramp and fires <c>_onDifficultyChanged</c>.
    ///   <see cref="Tick"/> advances <c>_elapsedTime</c> and recomputes the lerp;
    ///   fires <c>_onDifficultyChanged</c> each tick while active.
    ///   <see cref="Reset"/> disarms the ramp silently; called from <c>OnEnable</c>.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlDifficultyRamp.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlDifficultyRamp", order = 77)]
    public sealed class ZoneControlDifficultyRampSO : ScriptableObject
    {
        [Header("Ramp Settings")]
        [Min(1f)]
        [SerializeField] private float _rampDuration = 120f;

        [Range(0f, 1f)]
        [SerializeField] private float _startDifficulty = 0f;

        [Range(0f, 1f)]
        [SerializeField] private float _endDifficulty = 1f;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onDifficultyChanged;

        private float _elapsedTime;
        private bool  _isActive;
        private float _currentDifficulty;

        private void OnEnable() => Reset();

        public float CurrentDifficulty => _currentDifficulty;
        public float ElapsedTime       => _elapsedTime;
        public bool  IsActive          => _isActive;
        public float RampDuration      => _rampDuration;
        public float StartDifficulty   => _startDifficulty;
        public float EndDifficulty     => _endDifficulty;

        /// <summary>Arms the ramp from the beginning and fires <c>_onDifficultyChanged</c>.</summary>
        public void StartRamp()
        {
            _isActive          = true;
            _elapsedTime       = 0f;
            _currentDifficulty = _startDifficulty;
            _onDifficultyChanged?.Raise();
        }

        /// <summary>
        /// Advances the ramp by <paramref name="dt"/> seconds.
        /// No-op when not active.  Fires <c>_onDifficultyChanged</c> each call.
        /// </summary>
        public void Tick(float dt)
        {
            if (!_isActive) return;
            _elapsedTime = Mathf.Min(_elapsedTime + dt, _rampDuration);
            float t = _rampDuration > 0f ? _elapsedTime / _rampDuration : 1f;
            _currentDifficulty = Mathf.Lerp(_startDifficulty, _endDifficulty, t);
            _onDifficultyChanged?.Raise();
        }

        /// <summary>Disarms the ramp and resets all runtime state silently.</summary>
        public void Reset()
        {
            _isActive          = false;
            _elapsedTime       = 0f;
            _currentDifficulty = _startDifficulty;
        }

        private void OnValidate()
        {
            _startDifficulty = Mathf.Clamp01(_startDifficulty);
            _endDifficulty   = Mathf.Clamp01(_endDifficulty);
        }
    }
}
