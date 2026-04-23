using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureEqualizer", order = 403)]
    public sealed class ZoneControlCaptureEqualizerSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _morphismsNeeded      = 6;
        [SerializeField, Min(1)] private int _splitPerBot          = 2;
        [SerializeField, Min(0)] private int _bonusPerEqualization = 2785;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onEqualizerFormed;

        private int _morphisms;
        private int _equalizationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   MorphismsNeeded      => _morphismsNeeded;
        public int   SplitPerBot          => _splitPerBot;
        public int   BonusPerEqualization => _bonusPerEqualization;
        public int   Morphisms            => _morphisms;
        public int   EqualizationCount    => _equalizationCount;
        public int   TotalBonusAwarded    => _totalBonusAwarded;
        public float MorphismProgress     => _morphismsNeeded > 0
            ? Mathf.Clamp01(_morphisms / (float)_morphismsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _morphisms = Mathf.Min(_morphisms + 1, _morphismsNeeded);
            if (_morphisms >= _morphismsNeeded)
            {
                int bonus = _bonusPerEqualization;
                _equalizationCount++;
                _totalBonusAwarded += bonus;
                _morphisms          = 0;
                _onEqualizerFormed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _morphisms = Mathf.Max(0, _morphisms - _splitPerBot);
        }

        public void Reset()
        {
            _morphisms         = 0;
            _equalizationCount = 0;
            _totalBonusAwarded = 0;
        }
    }
}
