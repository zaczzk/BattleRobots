using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlTimedBonusWindow", order = 101)]
    public sealed class ZoneControlTimedBonusWindowSO : ScriptableObject
    {
        [Header("Bonus Window Settings")]
        [Min(1f)]
        [SerializeField] private float _windowDuration = 15f;

        [Range(1f, 5f)]
        [SerializeField] private float _rewardMultiplier = 2f;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onWindowOpened;
        [SerializeField] private VoidGameEvent _onWindowClosed;

        private bool  _isActive;
        private float _elapsed;
        private int   _totalWindowsOpened;

        private void OnEnable() => Reset();

        public bool  IsActive           => _isActive;
        public float WindowDuration     => _windowDuration;
        public float RewardMultiplier   => _rewardMultiplier;
        public float WindowProgress     => _isActive ? Mathf.Clamp01(_elapsed / _windowDuration) : 0f;
        public float RemainingTime      => _isActive ? Mathf.Max(0f, _windowDuration - _elapsed) : 0f;
        public int   TotalWindowsOpened => _totalWindowsOpened;

        public bool OpenWindow()
        {
            if (_isActive) return false;
            _isActive = true;
            _elapsed  = 0f;
            _totalWindowsOpened++;
            _onWindowOpened?.Raise();
            return true;
        }

        public void Tick(float dt)
        {
            if (!_isActive) return;
            _elapsed += dt;
            if (_elapsed >= _windowDuration)
                CloseWindow();
        }

        public int ApplyMultiplier(int baseReward) =>
            Mathf.RoundToInt(baseReward * (_isActive ? _rewardMultiplier : 1f));

        public void Reset()
        {
            _isActive           = false;
            _elapsed            = 0f;
            _totalWindowsOpened = 0;
        }

        private void CloseWindow()
        {
            _isActive = false;
            _elapsed  = 0f;
            _onWindowClosed?.Raise();
        }
    }
}
