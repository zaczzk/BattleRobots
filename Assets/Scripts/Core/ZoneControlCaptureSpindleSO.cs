using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureSpindle", order = 289)]
    public sealed class ZoneControlCaptureSpindleSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _windsNeeded     = 7;
        [SerializeField, Min(1)] private int _unravelPerBot   = 2;
        [SerializeField, Min(0)] private int _bonusPerBolt    = 1075;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onSpindleWound;

        private int _winds;
        private int _boltCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   WindsNeeded       => _windsNeeded;
        public int   UnravelPerBot     => _unravelPerBot;
        public int   BonusPerBolt      => _bonusPerBolt;
        public int   Winds             => _winds;
        public int   BoltCount         => _boltCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float WindProgress      => _windsNeeded > 0
            ? Mathf.Clamp01(_winds / (float)_windsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _winds = Mathf.Min(_winds + 1, _windsNeeded);
            if (_winds >= _windsNeeded)
            {
                int bonus = _bonusPerBolt;
                _boltCount++;
                _totalBonusAwarded += bonus;
                _winds              = 0;
                _onSpindleWound?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _winds = Mathf.Max(0, _winds - _unravelPerBot);
        }

        public void Reset()
        {
            _winds             = 0;
            _boltCount         = 0;
            _totalBonusAwarded = 0;
        }
    }
}
