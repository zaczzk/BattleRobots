using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureLever", order = 293)]
    public sealed class ZoneControlCaptureLeverSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _liftsNeeded      = 4;
        [SerializeField, Min(1)] private int _dropPerBot       = 1;
        [SerializeField, Min(0)] private int _bonusPerFulcrum  = 1135;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onLeverFulcrumed;

        private int _lifts;
        private int _fulcrumCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   LiftsNeeded       => _liftsNeeded;
        public int   DropPerBot        => _dropPerBot;
        public int   BonusPerFulcrum   => _bonusPerFulcrum;
        public int   Lifts             => _lifts;
        public int   FulcrumCount      => _fulcrumCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float LiftProgress      => _liftsNeeded > 0
            ? Mathf.Clamp01(_lifts / (float)_liftsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _lifts = Mathf.Min(_lifts + 1, _liftsNeeded);
            if (_lifts >= _liftsNeeded)
            {
                int bonus = _bonusPerFulcrum;
                _fulcrumCount++;
                _totalBonusAwarded += bonus;
                _lifts              = 0;
                _onLeverFulcrumed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _lifts = Mathf.Max(0, _lifts - _dropPerBot);
        }

        public void Reset()
        {
            _lifts             = 0;
            _fulcrumCount      = 0;
            _totalBonusAwarded = 0;
        }
    }
}
