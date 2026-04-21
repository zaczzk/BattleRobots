using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureAstrolabe", order = 283)]
    public sealed class ZoneControlCaptureAstrolabeSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _chartingsNeeded    = 5;
        [SerializeField, Min(1)] private int _driftPerBot        = 1;
        [SerializeField, Min(0)] private int _bonusPerAlignment  = 985;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onAstrolabeAligned;

        private int _chartings;
        private int _alignmentCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ChartingsNeeded    => _chartingsNeeded;
        public int   DriftPerBot        => _driftPerBot;
        public int   BonusPerAlignment  => _bonusPerAlignment;
        public int   Chartings          => _chartings;
        public int   AlignmentCount     => _alignmentCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float ChartingProgress   => _chartingsNeeded > 0
            ? Mathf.Clamp01(_chartings / (float)_chartingsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _chartings = Mathf.Min(_chartings + 1, _chartingsNeeded);
            if (_chartings >= _chartingsNeeded)
            {
                int bonus = _bonusPerAlignment;
                _alignmentCount++;
                _totalBonusAwarded += bonus;
                _chartings          = 0;
                _onAstrolabeAligned?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _chartings = Mathf.Max(0, _chartings - _driftPerBot);
        }

        public void Reset()
        {
            _chartings         = 0;
            _alignmentCount    = 0;
            _totalBonusAwarded = 0;
        }
    }
}
