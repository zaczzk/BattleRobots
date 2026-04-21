using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureCrown", order = 262)]
    public sealed class ZoneControlCaptureCrownSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _jewelsNeeded       = 7;
        [SerializeField, Min(1)] private int _removePerBot       = 2;
        [SerializeField, Min(0)] private int _bonusPerCoronation = 670;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onCrownCoronated;

        private int _jewels;
        private int _coronationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   JewelsNeeded       => _jewelsNeeded;
        public int   RemovePerBot       => _removePerBot;
        public int   BonusPerCoronation => _bonusPerCoronation;
        public int   Jewels             => _jewels;
        public int   CoronationCount    => _coronationCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float JewelProgress      => _jewelsNeeded > 0
            ? Mathf.Clamp01(_jewels / (float)_jewelsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _jewels = Mathf.Min(_jewels + 1, _jewelsNeeded);
            if (_jewels >= _jewelsNeeded)
            {
                int bonus = _bonusPerCoronation;
                _coronationCount++;
                _totalBonusAwarded += bonus;
                _jewels             = 0;
                _onCrownCoronated?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _jewels = Mathf.Max(0, _jewels - _removePerBot);
        }

        public void Reset()
        {
            _jewels            = 0;
            _coronationCount   = 0;
            _totalBonusAwarded = 0;
        }
    }
}
