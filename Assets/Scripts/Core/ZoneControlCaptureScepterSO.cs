using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureScepter", order = 259)]
    public sealed class ZoneControlCaptureScepterSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _gemsNeeded          = 5;
        [SerializeField, Min(1)] private int _lossPerBot          = 1;
        [SerializeField, Min(0)] private int _bonusPerInvestiture = 625;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onScepterInvested;

        private int _gems;
        private int _investitureCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   GemsNeeded          => _gemsNeeded;
        public int   LossPerBot          => _lossPerBot;
        public int   BonusPerInvestiture => _bonusPerInvestiture;
        public int   Gems                => _gems;
        public int   InvestitureCount    => _investitureCount;
        public int   TotalBonusAwarded   => _totalBonusAwarded;
        public float GemProgress         => _gemsNeeded > 0
            ? Mathf.Clamp01(_gems / (float)_gemsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _gems = Mathf.Min(_gems + 1, _gemsNeeded);
            if (_gems >= _gemsNeeded)
            {
                int bonus = _bonusPerInvestiture;
                _investitureCount++;
                _totalBonusAwarded += bonus;
                _gems               = 0;
                _onScepterInvested?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _gems = Mathf.Max(0, _gems - _lossPerBot);
        }

        public void Reset()
        {
            _gems              = 0;
            _investitureCount  = 0;
            _totalBonusAwarded = 0;
        }
    }
}
