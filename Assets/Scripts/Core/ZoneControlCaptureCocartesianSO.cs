using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureCocartesian", order = 427)]
    public sealed class ZoneControlCaptureCocartesianSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _injectionsNeeded     = 7;
        [SerializeField, Min(1)] private int _collapsePerBot       = 2;
        [SerializeField, Min(0)] private int _bonusPerCodiagonalize = 3145;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onCodiagonalized;

        private int _injections;
        private int _codiagonalizeCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   InjectionsNeeded      => _injectionsNeeded;
        public int   CollapsePerBot        => _collapsePerBot;
        public int   BonusPerCodiagonalize => _bonusPerCodiagonalize;
        public int   Injections            => _injections;
        public int   CodiagonalizeCount    => _codiagonalizeCount;
        public int   TotalBonusAwarded     => _totalBonusAwarded;
        public float InjectionProgress     => _injectionsNeeded > 0
            ? Mathf.Clamp01(_injections / (float)_injectionsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _injections = Mathf.Min(_injections + 1, _injectionsNeeded);
            if (_injections >= _injectionsNeeded)
            {
                int bonus = _bonusPerCodiagonalize;
                _codiagonalizeCount++;
                _totalBonusAwarded += bonus;
                _injections         = 0;
                _onCodiagonalized?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _injections = Mathf.Max(0, _injections - _collapsePerBot);
        }

        public void Reset()
        {
            _injections         = 0;
            _codiagonalizeCount = 0;
            _totalBonusAwarded  = 0;
        }
    }
}
