using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureShard", order = 273)]
    public sealed class ZoneControlCaptureShardSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _shardsNeeded           = 5;
        [SerializeField, Min(1)] private int _shatterPerBot          = 1;
        [SerializeField, Min(0)] private int _bonusPerCrystallization = 835;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onShardCrystallized;

        private int _shards;
        private int _crystallizationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ShardsNeeded            => _shardsNeeded;
        public int   ShatterPerBot           => _shatterPerBot;
        public int   BonusPerCrystallization => _bonusPerCrystallization;
        public int   Shards                  => _shards;
        public int   CrystallizationCount    => _crystallizationCount;
        public int   TotalBonusAwarded       => _totalBonusAwarded;
        public float ShardProgress           => _shardsNeeded > 0
            ? Mathf.Clamp01(_shards / (float)_shardsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _shards = Mathf.Min(_shards + 1, _shardsNeeded);
            if (_shards >= _shardsNeeded)
            {
                int bonus = _bonusPerCrystallization;
                _crystallizationCount++;
                _totalBonusAwarded += bonus;
                _shards             = 0;
                _onShardCrystallized?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _shards = Mathf.Max(0, _shards - _shatterPerBot);
        }

        public void Reset()
        {
            _shards               = 0;
            _crystallizationCount = 0;
            _totalBonusAwarded    = 0;
        }
    }
}
