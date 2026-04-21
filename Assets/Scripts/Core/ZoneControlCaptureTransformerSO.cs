using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureTransformer", order = 313)]
    public sealed class ZoneControlCaptureTransformerSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _turnsNeeded      = 7;
        [SerializeField, Min(1)] private int _fluxPerBot       = 2;
        [SerializeField, Min(0)] private int _bonusPerInduction = 1435;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onTransformerInduced;

        private int _turns;
        private int _inductionCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   TurnsNeeded       => _turnsNeeded;
        public int   FluxPerBot        => _fluxPerBot;
        public int   BonusPerInduction => _bonusPerInduction;
        public int   Turns             => _turns;
        public int   InductionCount    => _inductionCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float TurnProgress      => _turnsNeeded > 0
            ? Mathf.Clamp01(_turns / (float)_turnsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _turns = Mathf.Min(_turns + 1, _turnsNeeded);
            if (_turns >= _turnsNeeded)
            {
                int bonus = _bonusPerInduction;
                _inductionCount++;
                _totalBonusAwarded += bonus;
                _turns              = 0;
                _onTransformerInduced?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _turns = Mathf.Max(0, _turns - _fluxPerBot);
        }

        public void Reset()
        {
            _turns             = 0;
            _inductionCount    = 0;
            _totalBonusAwarded = 0;
        }
    }
}
