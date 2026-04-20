using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureFaultline", order = 226)]
    public sealed class ZoneControlCaptureFaultlineSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(2)] private int _tensionThreshold = 8;
        [SerializeField, Min(0)] private int _majorityBonus    = 400;
        [SerializeField, Min(0)] private int _minorityBonus    = 100;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onFaultRupture;

        private int _playerCaptures;
        private int _botCaptures;
        private int _ruptureCount;
        private int _majorityRuptures;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int  TensionThreshold  => _tensionThreshold;
        public int  MajorityBonus     => _majorityBonus;
        public int  MinorityBonus     => _minorityBonus;
        public int  PlayerCaptures    => _playerCaptures;
        public int  BotCaptures       => _botCaptures;
        public int  TotalTension      => _playerCaptures + _botCaptures;
        public int  RuptureCount      => _ruptureCount;
        public int  MajorityRuptures  => _majorityRuptures;
        public int  TotalBonusAwarded => _totalBonusAwarded;
        public float TensionProgress  => _tensionThreshold > 0
            ? Mathf.Clamp01(TotalTension / (float)_tensionThreshold)
            : 0f;

        public int RecordPlayerCapture()
        {
            _playerCaptures++;
            if (TotalTension >= _tensionThreshold)
                return Rupture();
            return 0;
        }

        public int RecordBotCapture()
        {
            _botCaptures++;
            if (TotalTension >= _tensionThreshold)
                return Rupture();
            return 0;
        }

        private int Rupture()
        {
            _ruptureCount++;
            int bonus = _playerCaptures > _botCaptures ? _majorityBonus : _minorityBonus;
            if (_playerCaptures > _botCaptures)
                _majorityRuptures++;
            _totalBonusAwarded += bonus;
            _playerCaptures     = 0;
            _botCaptures        = 0;
            _onFaultRupture?.Raise();
            return bonus;
        }

        public void Reset()
        {
            _playerCaptures    = 0;
            _botCaptures       = 0;
            _ruptureCount      = 0;
            _majorityRuptures  = 0;
            _totalBonusAwarded = 0;
        }
    }
}
