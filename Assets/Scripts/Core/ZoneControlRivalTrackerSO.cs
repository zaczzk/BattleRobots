using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlRivalTracker", order = 100)]
    public sealed class ZoneControlRivalTrackerSO : ScriptableObject
    {
        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onRivalLeading;
        [SerializeField] private VoidGameEvent _onPlayerLeading;
        [SerializeField] private VoidGameEvent _onCapturesUpdated;

        private int  _playerCaptures;
        private int  _rivalCaptures;
        private bool _wasRivalLeading;

        private void OnEnable() => Reset();

        public int  PlayerCaptures => _playerCaptures;
        public int  RivalCaptures  => _rivalCaptures;
        public bool IsRivalLeading => _rivalCaptures > _playerCaptures;
        public int  LeadDelta      => _playerCaptures - _rivalCaptures;

        public void RecordPlayerCapture()
        {
            _playerCaptures++;
            _onCapturesUpdated?.Raise();
            EvaluateLead();
        }

        public void RecordRivalCapture()
        {
            _rivalCaptures++;
            _onCapturesUpdated?.Raise();
            EvaluateLead();
        }

        public void Reset()
        {
            _playerCaptures  = 0;
            _rivalCaptures   = 0;
            _wasRivalLeading = false;
        }

        private void EvaluateLead()
        {
            bool rivalLeadingNow = IsRivalLeading;
            if (rivalLeadingNow && !_wasRivalLeading)
            {
                _wasRivalLeading = true;
                _onRivalLeading?.Raise();
            }
            else if (!rivalLeadingNow && _wasRivalLeading)
            {
                _wasRivalLeading = false;
                _onPlayerLeading?.Raise();
            }
        }
    }
}
