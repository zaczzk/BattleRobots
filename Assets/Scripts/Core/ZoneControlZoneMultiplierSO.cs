using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that maintains a per-capture score multiplier that ramps up with
    /// consecutive captures and resets to the base value after <see cref="_resetWindow"/>
    /// seconds without a capture.
    ///
    /// Call <see cref="RecordCapture"/> on each zone capture.
    /// Call <see cref="Tick(float)"/> every frame (with <c>Time.deltaTime</c>).
    /// Use <see cref="ComputeBonus(int)"/> to scale a base score amount by the
    /// current multiplier.
    /// Fires <c>_onMultiplierChanged</c> when the multiplier value changes.
    /// <see cref="Reset"/> restores the multiplier to its base value silently;
    /// called from <c>OnEnable</c>.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlZoneMultiplier.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlZoneMultiplier", order = 89)]
    public sealed class ZoneControlZoneMultiplierSO : ScriptableObject
    {
        [Header("Multiplier Settings")]
        [Min(1f)]
        [SerializeField] private float _baseMultiplier = 1f;

        [Min(0.1f)]
        [SerializeField] private float _incrementPerCapture = 0.25f;

        [Min(1f)]
        [SerializeField] private float _maxMultiplier = 4f;

        [Min(1f)]
        [SerializeField] private float _resetWindow = 10f;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onMultiplierChanged;

        private float _currentMultiplier;
        private float _timeSinceLastCapture;
        private bool  _isActive;

        private void OnEnable() => Reset();

        public float BaseMultiplier       => _baseMultiplier;
        public float MaxMultiplier        => _maxMultiplier;
        public float IncrementPerCapture  => _incrementPerCapture;
        public float ResetWindow          => _resetWindow;
        public float CurrentMultiplier    => _currentMultiplier;
        public float TimeSinceLastCapture => _timeSinceLastCapture;
        public bool  IsActive             => _isActive;

        /// <summary>
        /// Increments the multiplier by <see cref="_incrementPerCapture"/> (clamped
        /// to <see cref="_maxMultiplier"/>), resets the idle timer, and fires
        /// <c>_onMultiplierChanged</c>.
        /// </summary>
        public void RecordCapture()
        {
            _currentMultiplier    = Mathf.Min(_currentMultiplier + _incrementPerCapture, _maxMultiplier);
            _timeSinceLastCapture = 0f;
            _isActive             = true;
            _onMultiplierChanged?.Raise();
        }

        /// <summary>
        /// Advances the idle timer.  When the timer exceeds <see cref="_resetWindow"/>
        /// the multiplier resets to <see cref="_baseMultiplier"/> and the SO becomes
        /// inactive until the next capture.
        /// </summary>
        public void Tick(float dt)
        {
            if (!_isActive) return;

            _timeSinceLastCapture += dt;
            if (_timeSinceLastCapture >= _resetWindow)
            {
                _currentMultiplier    = _baseMultiplier;
                _timeSinceLastCapture = 0f;
                _isActive             = false;
                _onMultiplierChanged?.Raise();
            }
        }

        /// <summary>
        /// Returns <paramref name="baseAmount"/> scaled by <see cref="CurrentMultiplier"/>,
        /// rounded to the nearest integer.
        /// </summary>
        public int ComputeBonus(int baseAmount) =>
            Mathf.RoundToInt(baseAmount * _currentMultiplier);

        /// <summary>Resets the multiplier to its base value silently.</summary>
        public void Reset()
        {
            _currentMultiplier    = _baseMultiplier;
            _timeSinceLastCapture = 0f;
            _isActive             = false;
        }
    }
}
