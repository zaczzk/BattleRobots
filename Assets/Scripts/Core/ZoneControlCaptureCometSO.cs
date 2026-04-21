using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureComet", order = 274)]
    public sealed class ZoneControlCaptureCometSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _tailsNeeded    = 4;
        [SerializeField, Min(1)] private int _dissipatePerBot = 1;
        [SerializeField, Min(0)] private int _bonusPerBlaze  = 850;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onCometBlazed;

        private int _tails;
        private int _blazeCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   TailsNeeded       => _tailsNeeded;
        public int   DissipatePerBot   => _dissipatePerBot;
        public int   BonusPerBlaze     => _bonusPerBlaze;
        public int   Tails             => _tails;
        public int   BlazeCount        => _blazeCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float TailProgress      => _tailsNeeded > 0
            ? Mathf.Clamp01(_tails / (float)_tailsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _tails = Mathf.Min(_tails + 1, _tailsNeeded);
            if (_tails >= _tailsNeeded)
            {
                int bonus = _bonusPerBlaze;
                _blazeCount++;
                _totalBonusAwarded += bonus;
                _tails              = 0;
                _onCometBlazed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _tails = Mathf.Max(0, _tails - _dissipatePerBot);
        }

        public void Reset()
        {
            _tails             = 0;
            _blazeCount        = 0;
            _totalBonusAwarded = 0;
        }
    }
}
