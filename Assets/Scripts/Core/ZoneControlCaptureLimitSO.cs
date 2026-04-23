using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureLimit", order = 407)]
    public sealed class ZoneControlCaptureLimitSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _componentsNeeded = 6;
        [SerializeField, Min(1)] private int _dissolvePerBot   = 2;
        [SerializeField, Min(0)] private int _bonusPerLimit    = 2845;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onLimitComputed;

        private int _components;
        private int _limitCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ComponentsNeeded  => _componentsNeeded;
        public int   DissolvePerBot    => _dissolvePerBot;
        public int   BonusPerLimit     => _bonusPerLimit;
        public int   Components        => _components;
        public int   LimitCount        => _limitCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float ComponentProgress => _componentsNeeded > 0
            ? Mathf.Clamp01(_components / (float)_componentsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _components = Mathf.Min(_components + 1, _componentsNeeded);
            if (_components >= _componentsNeeded)
            {
                int bonus = _bonusPerLimit;
                _limitCount++;
                _totalBonusAwarded += bonus;
                _components         = 0;
                _onLimitComputed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _components = Mathf.Max(0, _components - _dissolvePerBot);
        }

        public void Reset()
        {
            _components        = 0;
            _limitCount        = 0;
            _totalBonusAwarded = 0;
        }
    }
}
