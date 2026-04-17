using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that begins decaying when no zone capture has been recorded for
    /// <see cref="_decayWindow"/> seconds.  Decay is cancelled the moment a new
    /// capture is recorded.
    ///
    /// Call <see cref="RecordCapture"/> on each zone capture.
    /// Call <see cref="Tick(float)"/> every frame (with <c>Time.deltaTime</c>).
    /// Fires <c>_onDecayStarted</c> when the window expires and decay begins.
    /// Fires <c>_onDecayEnded</c> when a capture cancels the active decay.
    /// <see cref="Reset"/> clears all state silently; called from <c>OnEnable</c>.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlScoreDecay.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlScoreDecay", order = 88)]
    public sealed class ZoneControlScoreDecaySO : ScriptableObject
    {
        [Header("Decay Settings")]
        [Min(1f)]
        [SerializeField] private float _decayWindow = 15f;

        [Min(0)]
        [SerializeField] private int _decayAmount = 25;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onDecayStarted;
        [SerializeField] private VoidGameEvent _onDecayEnded;

        private float _timeSinceCapture;
        private bool  _isDecaying;
        private int   _totalDecayApplied;

        private void OnEnable() => Reset();

        public float DecayWindow       => _decayWindow;
        public int   DecayAmount       => _decayAmount;
        public bool  IsDecaying        => _isDecaying;
        public float TimeSinceCapture  => _timeSinceCapture;
        public int   TotalDecayApplied => _totalDecayApplied;

        /// <summary>
        /// Resets the idle timer.  Ends an active decay if one is running.
        /// </summary>
        public void RecordCapture()
        {
            _timeSinceCapture = 0f;
            if (_isDecaying)
                EndDecay();
        }

        /// <summary>
        /// Advances the idle timer.  Starts decay when the window elapses; accumulates
        /// decay amount each frame while decay is active.
        /// </summary>
        public void Tick(float dt)
        {
            _timeSinceCapture += dt;

            if (!_isDecaying && _timeSinceCapture >= _decayWindow)
                StartDecay();
        }

        /// <summary>Clears all runtime state silently.</summary>
        public void Reset()
        {
            _timeSinceCapture  = 0f;
            _isDecaying        = false;
            _totalDecayApplied = 0;
        }

        private void StartDecay()
        {
            _isDecaying = true;
            _onDecayStarted?.Raise();
        }

        private void EndDecay()
        {
            _isDecaying = false;
            _onDecayEnded?.Raise();
        }
    }
}
