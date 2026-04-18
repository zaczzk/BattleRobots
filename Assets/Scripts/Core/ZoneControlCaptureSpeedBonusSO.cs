using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Awards a time-sensitive bonus for the player's first zone capture after match start.
    /// <c>StartMatch(float)</c> records the start timestamp. <c>RecordFirstCapture(float)</c>
    /// computes a speed bonus proportional to how much of <c>_windowSeconds</c> remains,
    /// fires <c>_onSpeedBonusAwarded</c>, and becomes idempotent.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlCaptureSpeedBonus.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureSpeedBonus", order = 121)]
    public sealed class ZoneControlCaptureSpeedBonusSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1f)] private float _windowSeconds = 15f;
        [SerializeField, Min(0)]  private int   _maxBonus      = 500;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onSpeedBonusAwarded;

        private float _matchStartTime = -1f;
        private bool  _hasAwarded;
        private int   _lastBonusAmount;
        private float _timeToFirstCapture;

        private void OnEnable() => Reset();

        public float WindowSeconds       => _windowSeconds;
        public int   MaxBonus            => _maxBonus;
        public bool  HasAwarded          => _hasAwarded;
        public bool  MatchStarted        => _matchStartTime >= 0f;
        public int   LastBonusAmount     => _lastBonusAmount;
        public float TimeToFirstCapture  => _timeToFirstCapture;

        /// <summary>Records the match start timestamp and resets award state.</summary>
        public void StartMatch(float gameTime)
        {
            _matchStartTime     = gameTime;
            _hasAwarded         = false;
            _lastBonusAmount    = 0;
            _timeToFirstCapture = 0f;
        }

        /// <summary>
        /// Records the first capture. Computes and caches the speed bonus based on time elapsed
        /// since match start. Fires <c>_onSpeedBonusAwarded</c> once; idempotent thereafter.
        /// Returns the bonus amount awarded (0 if outside window or already awarded).
        /// </summary>
        public int RecordFirstCapture(float gameTime)
        {
            if (_hasAwarded || !MatchStarted) return 0;

            float elapsed = gameTime - _matchStartTime;
            _timeToFirstCapture = elapsed;
            _hasAwarded         = true;

            if (elapsed > _windowSeconds)
            {
                _lastBonusAmount = 0;
                return 0;
            }

            float ratio          = 1f - Mathf.Clamp01(elapsed / _windowSeconds);
            _lastBonusAmount     = Mathf.RoundToInt(_maxBonus * ratio);
            if (_lastBonusAmount > 0)
                _onSpeedBonusAwarded?.Raise();

            return _lastBonusAmount;
        }

        /// <summary>Clears all runtime state silently.</summary>
        public void Reset()
        {
            _matchStartTime     = -1f;
            _hasAwarded         = false;
            _lastBonusAmount    = 0;
            _timeToFirstCapture = 0f;
        }
    }
}
