using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureRussellParadox", order = 539)]
    public sealed class ZoneControlCaptureRussellParadoxSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _selfContainingSetsNeeded = 5;
        [SerializeField, Min(1)] private int _paradoxLoopsPerBot       = 1;
        [SerializeField, Min(0)] private int _bonusPerResolution        = 4825;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onRussellParadoxResolved;

        private int _selfContainingSets;
        private int _resolutionCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   SelfContainingSetsNeeded => _selfContainingSetsNeeded;
        public int   ParadoxLoopsPerBot       => _paradoxLoopsPerBot;
        public int   BonusPerResolution        => _bonusPerResolution;
        public int   SelfContainingSets        => _selfContainingSets;
        public int   ResolutionCount           => _resolutionCount;
        public int   TotalBonusAwarded         => _totalBonusAwarded;
        public float SelfContainingSetProgress => _selfContainingSetsNeeded > 0
            ? Mathf.Clamp01(_selfContainingSets / (float)_selfContainingSetsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _selfContainingSets = Mathf.Min(_selfContainingSets + 1, _selfContainingSetsNeeded);
            if (_selfContainingSets >= _selfContainingSetsNeeded)
            {
                int bonus = _bonusPerResolution;
                _resolutionCount++;
                _totalBonusAwarded  += bonus;
                _selfContainingSets  = 0;
                _onRussellParadoxResolved?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _selfContainingSets = Mathf.Max(0, _selfContainingSets - _paradoxLoopsPerBot);
        }

        public void Reset()
        {
            _selfContainingSets = 0;
            _resolutionCount    = 0;
            _totalBonusAwarded  = 0;
        }
    }
}
