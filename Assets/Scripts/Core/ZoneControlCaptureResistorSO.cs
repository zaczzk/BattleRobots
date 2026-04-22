using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureResistor", order = 321)]
    public sealed class ZoneControlCaptureResistorSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _ohmsNeeded   = 7;
        [SerializeField, Min(1)] private int _shuntPerBot  = 2;
        [SerializeField, Min(0)] private int _bonusPerBlock = 1555;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onResistorBlocked;

        private int _ohms;
        private int _blockCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   OhmsNeeded       => _ohmsNeeded;
        public int   ShuntPerBot      => _shuntPerBot;
        public int   BonusPerBlock    => _bonusPerBlock;
        public int   Ohms             => _ohms;
        public int   BlockCount       => _blockCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float OhmProgress      => _ohmsNeeded > 0
            ? Mathf.Clamp01(_ohms / (float)_ohmsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _ohms = Mathf.Min(_ohms + 1, _ohmsNeeded);
            if (_ohms >= _ohmsNeeded)
            {
                int bonus = _bonusPerBlock;
                _blockCount++;
                _totalBonusAwarded += bonus;
                _ohms               = 0;
                _onResistorBlocked?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _ohms = Mathf.Max(0, _ohms - _shuntPerBot);
        }

        public void Reset()
        {
            _ohms              = 0;
            _blockCount        = 0;
            _totalBonusAwarded = 0;
        }
    }
}
