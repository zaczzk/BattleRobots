using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCapturePyramid", order = 217)]
    public sealed class ZoneControlCapturePyramidSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _capturesPerTier  = 3;
        [SerializeField, Min(1)] private int _maxTiers         = 3;
        [SerializeField, Min(0)] private int _bonusPerPyramid  = 400;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onPyramidComplete;

        private int _currentTier;
        private int _tierCaptures;
        private int _pyramidCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   CapturesPerTier  => _capturesPerTier;
        public int   MaxTiers         => _maxTiers;
        public int   BonusPerPyramid  => _bonusPerPyramid;
        public int   CurrentTier      => _currentTier;
        public int   TierCaptures     => _tierCaptures;
        public int   PyramidCount     => _pyramidCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float TierProgress     => _capturesPerTier > 0
            ? Mathf.Clamp01(_tierCaptures / (float)_capturesPerTier)
            : 0f;
        public float PyramidProgress  => _maxTiers > 0
            ? Mathf.Clamp01(_currentTier / (float)_maxTiers)
            : 0f;

        public int RecordPlayerCapture()
        {
            _tierCaptures++;
            if (_tierCaptures < _capturesPerTier)
                return 0;

            _tierCaptures = 0;
            _currentTier++;

            if (_currentTier < _maxTiers)
                return 0;

            Complete();
            return _bonusPerPyramid;
        }

        private void Complete()
        {
            _pyramidCount++;
            _totalBonusAwarded += _bonusPerPyramid;
            _currentTier        = 0;
            _tierCaptures       = 0;
            _onPyramidComplete?.Raise();
        }

        public void RecordBotCapture()
        {
            if (_currentTier > 0)
            {
                _currentTier--;
                _tierCaptures = 0;
            }
            else
            {
                _tierCaptures = Mathf.Max(0, _tierCaptures - 1);
            }
        }

        public void Reset()
        {
            _currentTier       = 0;
            _tierCaptures      = 0;
            _pyramidCount      = 0;
            _totalBonusAwarded = 0;
        }
    }
}
