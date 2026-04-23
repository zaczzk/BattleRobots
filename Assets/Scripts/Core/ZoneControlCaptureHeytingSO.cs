using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureHeyting", order = 433)]
    public sealed class ZoneControlCaptureHeytingSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _implicationsNeeded  = 7;
        [SerializeField, Min(1)] private int _retractPerBot       = 2;
        [SerializeField, Min(0)] private int _bonusPerImplication = 3235;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onImplicationFormed;

        private int _implications;
        private int _implicationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ImplicationsNeeded  => _implicationsNeeded;
        public int   RetractPerBot       => _retractPerBot;
        public int   BonusPerImplication => _bonusPerImplication;
        public int   Implications        => _implications;
        public int   ImplicationCount    => _implicationCount;
        public int   TotalBonusAwarded   => _totalBonusAwarded;
        public float ImplicationProgress => _implicationsNeeded > 0
            ? Mathf.Clamp01(_implications / (float)_implicationsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _implications = Mathf.Min(_implications + 1, _implicationsNeeded);
            if (_implications >= _implicationsNeeded)
            {
                int bonus = _bonusPerImplication;
                _implicationCount++;
                _totalBonusAwarded += bonus;
                _implications       = 0;
                _onImplicationFormed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _implications = Mathf.Max(0, _implications - _retractPerBot);
        }

        public void Reset()
        {
            _implications      = 0;
            _implicationCount  = 0;
            _totalBonusAwarded = 0;
        }
    }
}
