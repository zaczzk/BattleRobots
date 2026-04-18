using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlMatchVolatility", order = 137)]
    public sealed class ZoneControlMatchVolatilitySO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _volatilityThreshold = 5;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onHighVolatility;
        [SerializeField] private VoidGameEvent _onVolatilityUpdated;

        private int  _leadChanges;
        private bool _isHighVolatility;
        private bool _lastWasPlayerLeading;
        private bool _hasFirstLead;

        private void OnEnable() => Reset();

        public int  VolatilityThreshold => _volatilityThreshold;
        public int  LeadChanges         => _leadChanges;
        public bool IsHighVolatility    => _isHighVolatility;

        public void RecordLeadChange(bool isPlayerLeading)
        {
            if (!_hasFirstLead)
            {
                _hasFirstLead         = true;
                _lastWasPlayerLeading = isPlayerLeading;
                return;
            }

            if (isPlayerLeading == _lastWasPlayerLeading) return;

            _leadChanges++;
            _lastWasPlayerLeading = isPlayerLeading;
            _onVolatilityUpdated?.Raise();
            EvaluateVolatility();
        }

        public void Reset()
        {
            _leadChanges          = 0;
            _isHighVolatility     = false;
            _lastWasPlayerLeading = false;
            _hasFirstLead         = false;
        }

        private void EvaluateVolatility()
        {
            if (_leadChanges >= _volatilityThreshold && !_isHighVolatility)
            {
                _isHighVolatility = true;
                _onHighVolatility?.Raise();
            }
        }
    }
}
