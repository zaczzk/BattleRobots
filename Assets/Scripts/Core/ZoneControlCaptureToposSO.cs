using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureTopos", order = 386)]
    public sealed class ZoneControlCaptureToposSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _pointsNeeded   = 5;
        [SerializeField, Min(1)] private int _collapsePerBot = 1;
        [SerializeField, Min(0)] private int _bonusPerSieve  = 2530;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onToposSieved;

        private int _points;
        private int _sieveCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   PointsNeeded      => _pointsNeeded;
        public int   CollapsePerBot    => _collapsePerBot;
        public int   BonusPerSieve     => _bonusPerSieve;
        public int   Points            => _points;
        public int   SieveCount        => _sieveCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float PointProgress     => _pointsNeeded > 0
            ? Mathf.Clamp01(_points / (float)_pointsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _points = Mathf.Min(_points + 1, _pointsNeeded);
            if (_points >= _pointsNeeded)
            {
                int bonus = _bonusPerSieve;
                _sieveCount++;
                _totalBonusAwarded += bonus;
                _points             = 0;
                _onToposSieved?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _points = Mathf.Max(0, _points - _collapsePerBot);
        }

        public void Reset()
        {
            _points            = 0;
            _sieveCount        = 0;
            _totalBonusAwarded = 0;
        }
    }
}
