using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureDoldKan", order = 470)]
    public sealed class ZoneControlCaptureDoldKanSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _degeneraciesNeeded    = 7;
        [SerializeField, Min(1)] private int _breakPerBot           = 2;
        [SerializeField, Min(0)] private int _bonusPerCorrespondence = 3790;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onDoldKanCorresponded;

        private int _degeneracies;
        private int _correspondCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   DegeneraciesNeeded    => _degeneraciesNeeded;
        public int   BreakPerBot           => _breakPerBot;
        public int   BonusPerCorrespondence => _bonusPerCorrespondence;
        public int   Degeneracies          => _degeneracies;
        public int   CorrespondCount       => _correspondCount;
        public int   TotalBonusAwarded     => _totalBonusAwarded;
        public float DegeneracyProgress    => _degeneraciesNeeded > 0
            ? Mathf.Clamp01(_degeneracies / (float)_degeneraciesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _degeneracies = Mathf.Min(_degeneracies + 1, _degeneraciesNeeded);
            if (_degeneracies >= _degeneraciesNeeded)
            {
                int bonus = _bonusPerCorrespondence;
                _correspondCount++;
                _totalBonusAwarded += bonus;
                _degeneracies       = 0;
                _onDoldKanCorresponded?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _degeneracies = Mathf.Max(0, _degeneracies - _breakPerBot);
        }

        public void Reset()
        {
            _degeneracies      = 0;
            _correspondCount   = 0;
            _totalBonusAwarded = 0;
        }
    }
}
