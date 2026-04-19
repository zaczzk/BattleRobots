using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureOverflow", order = 170)]
    public sealed class ZoneControlCaptureOverflowSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _overflowTarget   = 10;
        [SerializeField, Min(0)] private int _bonusPerOverflow  = 30;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onOverflow;

        private int  _totalCaptures;
        private int  _overflowCaptures;
        private int  _totalBonusAwarded;
        private bool _hasOverflowed;

        private void OnEnable() => Reset();

        public int  OverflowTarget    => _overflowTarget;
        public int  BonusPerOverflow  => _bonusPerOverflow;
        public int  TotalCaptures     => _totalCaptures;
        public int  OverflowCaptures  => _overflowCaptures;
        public int  TotalBonusAwarded => _totalBonusAwarded;
        public bool HasOverflowed     => _hasOverflowed;

        public int RecordCapture()
        {
            _totalCaptures++;
            if (_totalCaptures > _overflowTarget)
            {
                _overflowCaptures++;
                _totalBonusAwarded += _bonusPerOverflow;
                if (!_hasOverflowed)
                {
                    _hasOverflowed = true;
                    _onOverflow?.Raise();
                }
                return _bonusPerOverflow;
            }
            return 0;
        }

        public void Reset()
        {
            _totalCaptures    = 0;
            _overflowCaptures = 0;
            _totalBonusAwarded = 0;
            _hasOverflowed    = false;
        }
    }
}
