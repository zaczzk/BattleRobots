using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Tracks player focus by measuring idle time between captures.
    /// <c>RecordCapture()</c> resets the idle timer and fires <c>_onFocusRegained</c> when
    /// focus was previously lost. <c>Tick(dt)</c> advances the timer and fires
    /// <c>_onFocusLost</c> when <c>_focusTimeoutSeconds</c> elapses without a capture.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlMatchFocus.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlMatchFocus", order = 119)]
    public sealed class ZoneControlMatchFocusSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1f)] private float _focusTimeoutSeconds = 8f;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onFocusLost;
        [SerializeField] private VoidGameEvent _onFocusRegained;

        private float _idleTime;
        private bool  _isFocusLost;

        private void OnEnable() => Reset();

        public float FocusTimeoutSeconds => _focusTimeoutSeconds;
        public float IdleTime            => _idleTime;
        public bool  IsFocusLost         => _isFocusLost;

        /// <summary>
        /// Records a capture. Resets the idle timer and fires <c>_onFocusRegained</c> if focus
        /// was previously lost.
        /// </summary>
        public void RecordCapture()
        {
            _idleTime = 0f;
            if (_isFocusLost)
            {
                _isFocusLost = false;
                _onFocusRegained?.Raise();
            }
        }

        /// <summary>
        /// Advances the idle timer. Fires <c>_onFocusLost</c> when the threshold is first reached.
        /// </summary>
        public void Tick(float dt)
        {
            if (_isFocusLost) return;
            _idleTime += dt;
            if (_idleTime >= _focusTimeoutSeconds)
            {
                _isFocusLost = true;
                _onFocusLost?.Raise();
            }
        }

        /// <summary>Clears all runtime state silently.</summary>
        public void Reset()
        {
            _idleTime    = 0f;
            _isFocusLost = false;
        }
    }
}
