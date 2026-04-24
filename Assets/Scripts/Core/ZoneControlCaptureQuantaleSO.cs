using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureQuantale", order = 438)]
    public sealed class ZoneControlCaptureQuantaleSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _compositesNeeded = 5;
        [SerializeField, Min(1)] private int _decomposePerBot  = 1;
        [SerializeField, Min(0)] private int _bonusPerCompose  = 3310;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onQuantaleComposed;

        private int _composites;
        private int _composeCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   CompositesNeeded  => _compositesNeeded;
        public int   DecomposePerBot   => _decomposePerBot;
        public int   BonusPerCompose   => _bonusPerCompose;
        public int   Composites        => _composites;
        public int   ComposeCount      => _composeCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float CompositeProgress => _compositesNeeded > 0
            ? Mathf.Clamp01(_composites / (float)_compositesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _composites = Mathf.Min(_composites + 1, _compositesNeeded);
            if (_composites >= _compositesNeeded)
            {
                int bonus = _bonusPerCompose;
                _composeCount++;
                _totalBonusAwarded += bonus;
                _composites         = 0;
                _onQuantaleComposed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _composites = Mathf.Max(0, _composites - _decomposePerBot);
        }

        public void Reset()
        {
            _composites        = 0;
            _composeCount      = 0;
            _totalBonusAwarded = 0;
        }
    }
}
