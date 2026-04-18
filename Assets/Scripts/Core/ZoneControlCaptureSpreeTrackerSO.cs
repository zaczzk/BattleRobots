using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Tracks a consecutive-capture spree.  Each player capture within
    /// <c>_spreeWindowSeconds</c> of the previous one extends the spree.
    /// A gap larger than the window or a bot capture breaks the spree.
    /// Fires <c>_onSpreeStarted</c> when the streak reaches
    /// <c>_spreeThreshold</c> and <c>_onSpreeEnded</c> when it breaks.
    /// Awards <c>_bonusPerSpreeCapture</c> for every capture made while
    /// the spree is active.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlCaptureSpreeTracker.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureSpreeTracker", order = 133)]
    public sealed class ZoneControlCaptureSpreeTrackerSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(2)]    private int   _spreeThreshold      = 3;
        [SerializeField, Min(0.1f)] private float _spreeWindowSeconds  = 8f;
        [SerializeField, Min(0)]    private int   _bonusPerSpreeCapture = 40;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onSpreeStarted;
        [SerializeField] private VoidGameEvent _onSpreeEnded;

        private int   _currentStreak;
        private float _lastCaptureTime = -1f;
        private bool  _isSpreeActive;
        private int   _spreeCount;
        private int   _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   SpreeThreshold      => _spreeThreshold;
        public float SpreeWindowSeconds  => _spreeWindowSeconds;
        public int   BonusPerSpreeCapture => _bonusPerSpreeCapture;
        public int   CurrentStreak       => _currentStreak;
        public bool  IsSpreeActive       => _isSpreeActive;
        public int   SpreeCount          => _spreeCount;
        public int   TotalBonusAwarded   => _totalBonusAwarded;

        /// <summary>Returns bonus earned if spree is active, else 0.</summary>
        public int RecordPlayerCapture(float currentTime)
        {
            bool withinWindow = _lastCaptureTime >= 0f &&
                                (currentTime - _lastCaptureTime) <= _spreeWindowSeconds;

            if (withinWindow)
                _currentStreak++;
            else
                _currentStreak = 1;

            _lastCaptureTime = currentTime;

            if (_currentStreak >= _spreeThreshold && !_isSpreeActive)
            {
                _isSpreeActive = true;
                _spreeCount++;
                _onSpreeStarted?.Raise();
            }

            if (_isSpreeActive)
            {
                _totalBonusAwarded += _bonusPerSpreeCapture;
                return _bonusPerSpreeCapture;
            }

            return 0;
        }

        public void BreakSpree()
        {
            _currentStreak  = 0;
            _lastCaptureTime = -1f;
            if (_isSpreeActive)
            {
                _isSpreeActive = false;
                _onSpreeEnded?.Raise();
            }
        }

        public void Reset()
        {
            _currentStreak    = 0;
            _lastCaptureTime  = -1f;
            _isSpreeActive    = false;
            _spreeCount       = 0;
            _totalBonusAwarded = 0;
        }
    }
}
