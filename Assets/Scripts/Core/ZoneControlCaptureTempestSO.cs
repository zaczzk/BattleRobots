using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureTempest", order = 212)]
    public sealed class ZoneControlCaptureTempestSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _chargeForTempest      = 3;
        [SerializeField, Min(1)] private int _capturesNeeded        = 3;
        [SerializeField, Min(0)] private int _bonusPerTempestCapture = 120;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onTempestOpened;
        [SerializeField] private VoidGameEvent _onTempestClosed;

        private int  _botCharge;
        private bool _isActive;
        private int  _capturesRemaining;
        private int  _tempestCount;
        private int  _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ChargeForTempest       => _chargeForTempest;
        public int   CapturesNeeded         => _capturesNeeded;
        public int   BonusPerTempestCapture => _bonusPerTempestCapture;
        public int   BotCharge              => _botCharge;
        public bool  IsActive               => _isActive;
        public int   CapturesRemaining      => _capturesRemaining;
        public int   TempestCount           => _tempestCount;
        public int   TotalBonusAwarded      => _totalBonusAwarded;
        public float TempestProgress
        {
            get
            {
                if (_isActive)
                    return _capturesNeeded > 0
                        ? Mathf.Clamp01(_capturesRemaining / (float)_capturesNeeded)
                        : 0f;
                return _chargeForTempest > 0
                    ? Mathf.Clamp01(_botCharge / (float)_chargeForTempest)
                    : 0f;
            }
        }

        public int RecordPlayerCapture()
        {
            if (!_isActive)
            {
                _botCharge = 0;
                return 0;
            }

            int bonus = _bonusPerTempestCapture;
            _totalBonusAwarded += bonus;
            _capturesRemaining--;
            if (_capturesRemaining <= 0)
                CloseTempest();
            return bonus;
        }

        public void RecordBotCapture()
        {
            if (_isActive)
            {
                CloseTempest();
                return;
            }

            _botCharge++;
            if (_botCharge >= _chargeForTempest)
                OpenTempest();
        }

        private void OpenTempest()
        {
            _isActive          = true;
            _botCharge         = 0;
            _capturesRemaining = _capturesNeeded;
            _onTempestOpened?.Raise();
        }

        private void CloseTempest()
        {
            _isActive  = false;
            _botCharge = 0;
            _tempestCount++;
            _onTempestClosed?.Raise();
        }

        public void Reset()
        {
            _botCharge         = 0;
            _isActive          = false;
            _capturesRemaining = 0;
            _tempestCount      = 0;
            _totalBonusAwarded = 0;
        }
    }
}
