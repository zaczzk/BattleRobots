using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureBornology", order = 446)]
    public sealed class ZoneControlCaptureBornologySO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _setsNeeded    = 6;
        [SerializeField, Min(1)] private int _unboundPerBot = 1;
        [SerializeField, Min(0)] private int _bonusPerBound = 3430;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onBornologyBounded;

        private int _sets;
        private int _boundCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   SetsNeeded        => _setsNeeded;
        public int   UnboundPerBot     => _unboundPerBot;
        public int   BonusPerBound     => _bonusPerBound;
        public int   Sets              => _sets;
        public int   BoundCount        => _boundCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float BornologyProgress => _setsNeeded > 0
            ? Mathf.Clamp01(_sets / (float)_setsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _sets = Mathf.Min(_sets + 1, _setsNeeded);
            if (_sets >= _setsNeeded)
            {
                int bonus = _bonusPerBound;
                _boundCount++;
                _totalBonusAwarded += bonus;
                _sets               = 0;
                _onBornologyBounded?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _sets = Mathf.Max(0, _sets - _unboundPerBot);
        }

        public void Reset()
        {
            _sets              = 0;
            _boundCount        = 0;
            _totalBonusAwarded = 0;
        }
    }
}
