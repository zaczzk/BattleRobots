using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureGoblet", order = 270)]
    public sealed class ZoneControlCaptureGobletSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _poursNeeded     = 4;
        [SerializeField, Min(1)] private int _spiltPerBot     = 1;
        [SerializeField, Min(0)] private int _bonusPerGoblet  = 790;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onGobletFilled;

        private int _pours;
        private int _gobletCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   PoursNeeded       => _poursNeeded;
        public int   SpiltPerBot       => _spiltPerBot;
        public int   BonusPerGoblet    => _bonusPerGoblet;
        public int   Pours             => _pours;
        public int   GobletCount       => _gobletCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float PourProgress      => _poursNeeded > 0
            ? Mathf.Clamp01(_pours / (float)_poursNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _pours = Mathf.Min(_pours + 1, _poursNeeded);
            if (_pours >= _poursNeeded)
            {
                int bonus = _bonusPerGoblet;
                _gobletCount++;
                _totalBonusAwarded += bonus;
                _pours              = 0;
                _onGobletFilled?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _pours = Mathf.Max(0, _pours - _spiltPerBot);
        }

        public void Reset()
        {
            _pours             = 0;
            _gobletCount       = 0;
            _totalBonusAwarded = 0;
        }
    }
}
