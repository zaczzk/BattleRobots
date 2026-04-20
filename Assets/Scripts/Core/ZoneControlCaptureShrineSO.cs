using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureShrine", order = 246)]
    public sealed class ZoneControlCaptureShrineSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _candlesNeeded        = 5;
        [SerializeField, Min(1)] private int _snuffPerBot          = 1;
        [SerializeField, Min(0)] private int _bonusPerPurification = 480;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onShrinePurified;

        private int _candles;
        private int _purificationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   CandlesNeeded        => _candlesNeeded;
        public int   SnuffPerBot          => _snuffPerBot;
        public int   BonusPerPurification => _bonusPerPurification;
        public int   Candles              => _candles;
        public int   PurificationCount    => _purificationCount;
        public int   TotalBonusAwarded    => _totalBonusAwarded;
        public float CandleProgress       => _candlesNeeded > 0
            ? Mathf.Clamp01(_candles / (float)_candlesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _candles = Mathf.Min(_candles + 1, _candlesNeeded);
            if (_candles >= _candlesNeeded)
            {
                int bonus = _bonusPerPurification;
                _purificationCount++;
                _totalBonusAwarded += bonus;
                _candles            = 0;
                _onShrinePurified?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _candles = Mathf.Max(0, _candles - _snuffPerBot);
        }

        public void Reset()
        {
            _candles           = 0;
            _purificationCount = 0;
            _totalBonusAwarded = 0;
        }
    }
}
