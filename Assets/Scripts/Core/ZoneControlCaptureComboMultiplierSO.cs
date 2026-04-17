using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that implements a time-gated capture combo multiplier.  Each
    /// consecutive zone capture within <see cref="ComboWindow"/> seconds increases
    /// the multiplier by <see cref="IncrementPerCapture"/>, up to
    /// <see cref="MaxMultiplier"/>.  A capture that arrives after the window has
    /// lapsed resets the multiplier to the base value and starts a fresh combo.
    ///
    /// Call <see cref="RecordCapture(float)"/> on every zone capture and
    /// <see cref="Tick(float)"/> each frame to drive the expiry window.
    /// <see cref="ComputeBonus(int)"/> returns the scaled bonus for a given base amount.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlCaptureComboMultiplier.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureComboMultiplier", order = 96)]
    public sealed class ZoneControlCaptureComboMultiplierSO : ScriptableObject
    {
        [Header("Combo Multiplier Settings")]
        [Min(1f)]
        [SerializeField] private float _baseMultiplier = 1f;

        [Min(0.1f)]
        [SerializeField] private float _incrementPerCapture = 0.5f;

        [Min(1f)]
        [SerializeField] private float _maxMultiplier = 4f;

        [Min(0.5f)]
        [SerializeField] private float _comboWindow = 5f;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onComboMultiplierChanged;

        private float _currentMultiplier;
        private float _timeSinceLastCapture;
        private bool  _hasCapture;

        private void OnEnable() => Reset();

        public float CurrentMultiplier  => _currentMultiplier;
        public float BaseMultiplier     => _baseMultiplier;
        public float IncrementPerCapture => _incrementPerCapture;
        public float MaxMultiplier       => _maxMultiplier;
        public float ComboWindow         => _comboWindow;

        /// <summary>
        /// Records a zone capture at the given timestamp.
        /// If the combo window has lapsed the multiplier resets before incrementing.
        /// </summary>
        public void RecordCapture(float currentTime)
        {
            if (_hasCapture && _timeSinceLastCapture >= _comboWindow)
                ResetMultiplier();

            _currentMultiplier     = Mathf.Min(_currentMultiplier + _incrementPerCapture, _maxMultiplier);
            _timeSinceLastCapture  = 0f;
            _hasCapture            = true;
            _onComboMultiplierChanged?.Raise();
        }

        /// <summary>Advances the combo window timer.  Resets multiplier when the window expires.</summary>
        public void Tick(float dt)
        {
            if (!_hasCapture)
                return;

            _timeSinceLastCapture += dt;

            if (_timeSinceLastCapture >= _comboWindow)
            {
                ResetMultiplier();
                _onComboMultiplierChanged?.Raise();
            }
        }

        /// <summary>Returns the scaled bonus for a given base amount using the current multiplier.</summary>
        public int ComputeBonus(int baseAmount) =>
            Mathf.RoundToInt(baseAmount * _currentMultiplier);

        /// <summary>Clears all runtime state silently.</summary>
        public void Reset()
        {
            ResetMultiplier();
            _hasCapture = false;
        }

        private void ResetMultiplier()
        {
            _currentMultiplier    = _baseMultiplier;
            _timeSinceLastCapture = 0f;
            _hasCapture           = false;
        }

        private void OnValidate()
        {
            _baseMultiplier      = Mathf.Max(1f, _baseMultiplier);
            _incrementPerCapture = Mathf.Max(0.1f, _incrementPerCapture);
            _maxMultiplier       = Mathf.Max(_baseMultiplier, _maxMultiplier);
            _comboWindow         = Mathf.Max(0.5f, _comboWindow);
        }
    }
}
