using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureSpace", order = 451)]
    public sealed class ZoneControlCaptureSpaceSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _pointsNeeded = 5;
        [SerializeField, Min(1)] private int _contractPerBot = 1;
        [SerializeField, Min(0)] private int _bonusPerOpen = 3505;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onSpaceOpened;

        private int _points;
        private int _openCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   PointsNeeded      => _pointsNeeded;
        public int   ContractPerBot    => _contractPerBot;
        public int   BonusPerOpen      => _bonusPerOpen;
        public int   Points            => _points;
        public int   OpenCount         => _openCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float SpaceProgress     => _pointsNeeded > 0
            ? Mathf.Clamp01(_points / (float)_pointsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _points = Mathf.Min(_points + 1, _pointsNeeded);
            if (_points >= _pointsNeeded)
            {
                int bonus = _bonusPerOpen;
                _openCount++;
                _totalBonusAwarded += bonus;
                _points             = 0;
                _onSpaceOpened?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _points = Mathf.Max(0, _points - _contractPerBot);
        }

        public void Reset()
        {
            _points            = 0;
            _openCount         = 0;
            _totalBonusAwarded = 0;
        }
    }
}
