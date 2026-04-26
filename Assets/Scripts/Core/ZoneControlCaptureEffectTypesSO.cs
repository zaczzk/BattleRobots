using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureEffectTypes", order = 563)]
    public sealed class ZoneControlCaptureEffectTypesSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _effectsNeeded                = 6;
        [SerializeField, Min(1)] private int _sideEffectCancellationsPerBot = 1;
        [SerializeField, Min(0)] private int _bonusPerEffectApplication    = 5185;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onEffectTypesCompleted;

        private int _effects;
        private int _effectApplicationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   EffectsNeeded                => _effectsNeeded;
        public int   SideEffectCancellationsPerBot => _sideEffectCancellationsPerBot;
        public int   BonusPerEffectApplication    => _bonusPerEffectApplication;
        public int   Effects                      => _effects;
        public int   EffectApplicationCount       => _effectApplicationCount;
        public int   TotalBonusAwarded            => _totalBonusAwarded;
        public float EffectProgress => _effectsNeeded > 0
            ? Mathf.Clamp01(_effects / (float)_effectsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _effects = Mathf.Min(_effects + 1, _effectsNeeded);
            if (_effects >= _effectsNeeded)
            {
                int bonus = _bonusPerEffectApplication;
                _effectApplicationCount++;
                _totalBonusAwarded  += bonus;
                _effects             = 0;
                _onEffectTypesCompleted?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _effects = Mathf.Max(0, _effects - _sideEffectCancellationsPerBot);
        }

        public void Reset()
        {
            _effects             = 0;
            _effectApplicationCount = 0;
            _totalBonusAwarded   = 0;
        }
    }
}
