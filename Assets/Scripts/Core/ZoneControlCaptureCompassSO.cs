using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureCompass", order = 267)]
    public sealed class ZoneControlCaptureCompassSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _bearingsNeeded      = 5;
        [SerializeField, Min(1)] private int _lostPerBot          = 1;
        [SerializeField, Min(0)] private int _bonusPerNavigation  = 745;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onCompassNavigated;

        private int _bearings;
        private int _navigationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   BearingsNeeded     => _bearingsNeeded;
        public int   LostPerBot         => _lostPerBot;
        public int   BonusPerNavigation => _bonusPerNavigation;
        public int   Bearings           => _bearings;
        public int   NavigationCount    => _navigationCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float BearingProgress    => _bearingsNeeded > 0
            ? Mathf.Clamp01(_bearings / (float)_bearingsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _bearings = Mathf.Min(_bearings + 1, _bearingsNeeded);
            if (_bearings >= _bearingsNeeded)
            {
                int bonus = _bonusPerNavigation;
                _navigationCount++;
                _totalBonusAwarded += bonus;
                _bearings           = 0;
                _onCompassNavigated?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _bearings = Mathf.Max(0, _bearings - _lostPerBot);
        }

        public void Reset()
        {
            _bearings          = 0;
            _navigationCount   = 0;
            _totalBonusAwarded = 0;
        }
    }
}
