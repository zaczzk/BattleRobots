using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureDragon", order = 258)]
    public sealed class ZoneControlCaptureDragonSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _hoardNeeded        = 7;
        [SerializeField, Min(1)] private int _plunderPerBot      = 2;
        [SerializeField, Min(0)] private int _bonusPerHoard      = 610;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onHoardFilled;

        private int _gold;
        private int _hoardCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   HoardNeeded        => _hoardNeeded;
        public int   PlunderPerBot      => _plunderPerBot;
        public int   BonusPerHoard      => _bonusPerHoard;
        public int   Gold               => _gold;
        public int   HoardCount         => _hoardCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float GoldProgress       => _hoardNeeded > 0
            ? Mathf.Clamp01(_gold / (float)_hoardNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _gold = Mathf.Min(_gold + 1, _hoardNeeded);
            if (_gold >= _hoardNeeded)
            {
                int bonus = _bonusPerHoard;
                _hoardCount++;
                _totalBonusAwarded += bonus;
                _gold               = 0;
                _onHoardFilled?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _gold = Mathf.Max(0, _gold - _plunderPerBot);
        }

        public void Reset()
        {
            _gold              = 0;
            _hoardCount        = 0;
            _totalBonusAwarded = 0;
        }
    }
}
