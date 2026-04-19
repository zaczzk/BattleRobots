using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureStorm", order = 183)]
    public sealed class ZoneControlCaptureStormSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)]    private int   _chargesRequired       = 6;
        [SerializeField, Min(1f)]   private float _stormDurationSeconds  = 15f;
        [SerializeField, Min(0)]    private int   _bonusPerStormCapture  = 100;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onStormActivated;
        [SerializeField] private VoidGameEvent _onStormEnded;

        private int   _stormCharges;
        private bool  _isStormActive;
        private float _stormElapsed;
        private int   _stormCount;
        private int   _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ChargesRequired      => _chargesRequired;
        public float StormDurationSeconds => _stormDurationSeconds;
        public int   BonusPerStormCapture => _bonusPerStormCapture;
        public int   StormCharges         => _stormCharges;
        public bool  IsStormActive        => _isStormActive;
        public float StormElapsed         => _stormElapsed;
        public int   StormCount           => _stormCount;
        public int   TotalBonusAwarded    => _totalBonusAwarded;
        public float StormTimeRemaining   => _isStormActive ? Mathf.Max(0f, _stormDurationSeconds - _stormElapsed) : 0f;

        public float ChargeProgress
        {
            get
            {
                if (_isStormActive)
                    return _stormDurationSeconds > 0f
                        ? Mathf.Clamp01(1f - _stormElapsed / _stormDurationSeconds)
                        : 0f;
                return _chargesRequired > 0
                    ? Mathf.Clamp01((float)_stormCharges / _chargesRequired)
                    : 0f;
            }
        }

        public int RecordCapture()
        {
            if (_isStormActive)
            {
                _totalBonusAwarded += _bonusPerStormCapture;
                return _bonusPerStormCapture;
            }

            _stormCharges++;
            if (_stormCharges >= _chargesRequired)
                ActivateStorm();

            return 0;
        }

        public void Tick(float dt)
        {
            if (!_isStormActive) return;
            _stormElapsed += dt;
            if (_stormElapsed >= _stormDurationSeconds)
                EndStorm();
        }

        private void ActivateStorm()
        {
            _isStormActive = true;
            _stormCharges  = 0;
            _stormCount++;
            _onStormActivated?.Raise();
        }

        private void EndStorm()
        {
            _isStormActive = false;
            _stormElapsed  = 0f;
            _onStormEnded?.Raise();
        }

        public void Reset()
        {
            _stormCharges      = 0;
            _isStormActive     = false;
            _stormElapsed      = 0f;
            _stormCount        = 0;
            _totalBonusAwarded = 0;
        }
    }
}
