using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureGovernor", order = 309)]
    public sealed class ZoneControlCaptureGovernorSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _flyweightsNeeded  = 5;
        [SerializeField, Min(1)] private int _lossPerBot        = 1;
        [SerializeField, Min(0)] private int _bonusPerRegulation = 1375;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onGovernorRegulated;

        private int _flyweights;
        private int _regulationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   FlyweightsNeeded   => _flyweightsNeeded;
        public int   LossPerBot         => _lossPerBot;
        public int   BonusPerRegulation => _bonusPerRegulation;
        public int   Flyweights         => _flyweights;
        public int   RegulationCount    => _regulationCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float FlyweightProgress  => _flyweightsNeeded > 0
            ? Mathf.Clamp01(_flyweights / (float)_flyweightsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _flyweights = Mathf.Min(_flyweights + 1, _flyweightsNeeded);
            if (_flyweights >= _flyweightsNeeded)
            {
                int bonus = _bonusPerRegulation;
                _regulationCount++;
                _totalBonusAwarded += bonus;
                _flyweights         = 0;
                _onGovernorRegulated?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _flyweights = Mathf.Max(0, _flyweights - _lossPerBot);
        }

        public void Reset()
        {
            _flyweights        = 0;
            _regulationCount   = 0;
            _totalBonusAwarded = 0;
        }
    }
}
