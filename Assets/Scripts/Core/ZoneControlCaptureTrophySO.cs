using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureTrophy", order = 263)]
    public sealed class ZoneControlCaptureTrophySO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _medalsNeeded     = 5;
        [SerializeField, Min(1)] private int _losePerBot       = 1;
        [SerializeField, Min(0)] private int _bonusPerTrophy   = 685;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onTrophyAwarded;

        private int _medals;
        private int _trophyCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   MedalsNeeded      => _medalsNeeded;
        public int   LosePerBot        => _losePerBot;
        public int   BonusPerTrophy    => _bonusPerTrophy;
        public int   Medals            => _medals;
        public int   TrophyCount       => _trophyCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float MedalProgress     => _medalsNeeded > 0
            ? Mathf.Clamp01(_medals / (float)_medalsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _medals = Mathf.Min(_medals + 1, _medalsNeeded);
            if (_medals >= _medalsNeeded)
            {
                int bonus = _bonusPerTrophy;
                _trophyCount++;
                _totalBonusAwarded += bonus;
                _medals             = 0;
                _onTrophyAwarded?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _medals = Mathf.Max(0, _medals - _losePerBot);
        }

        public void Reset()
        {
            _medals            = 0;
            _trophyCount       = 0;
            _totalBonusAwarded = 0;
        }
    }
}
