using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCapturePeriodDomain", order = 509)]
    public sealed class ZoneControlCapturePeriodDomainSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _hodgeFiltrationsNeeded     = 7;
        [SerializeField, Min(1)] private int _monodromyObstructionsPerBot = 2;
        [SerializeField, Min(0)] private int _bonusPerPolarization        = 4375;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onPeriodDomainPolarized;

        private int _hodgeFiltrations;
        private int _polarizationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   HodgeFiltrationsNeeded     => _hodgeFiltrationsNeeded;
        public int   MonodromyObstructionsPerBot => _monodromyObstructionsPerBot;
        public int   BonusPerPolarization        => _bonusPerPolarization;
        public int   HodgeFiltrations            => _hodgeFiltrations;
        public int   PolarizationCount           => _polarizationCount;
        public int   TotalBonusAwarded           => _totalBonusAwarded;
        public float HodgeFiltrationProgress => _hodgeFiltrationsNeeded > 0
            ? Mathf.Clamp01(_hodgeFiltrations / (float)_hodgeFiltrationsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _hodgeFiltrations = Mathf.Min(_hodgeFiltrations + 1, _hodgeFiltrationsNeeded);
            if (_hodgeFiltrations >= _hodgeFiltrationsNeeded)
            {
                int bonus = _bonusPerPolarization;
                _polarizationCount++;
                _totalBonusAwarded += bonus;
                _hodgeFiltrations   = 0;
                _onPeriodDomainPolarized?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _hodgeFiltrations = Mathf.Max(0, _hodgeFiltrations - _monodromyObstructionsPerBot);
        }

        public void Reset()
        {
            _hodgeFiltrations  = 0;
            _polarizationCount = 0;
            _totalBonusAwarded = 0;
        }
    }
}
