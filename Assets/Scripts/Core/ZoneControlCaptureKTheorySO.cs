using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureKTheory", order = 489)]
    public sealed class ZoneControlCaptureKTheorySO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _bundlesNeeded        = 7;
        [SerializeField, Min(1)] private int _exactSeqPerBot       = 2;
        [SerializeField, Min(0)] private int _bonusPerClassification = 4075;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onKTheoryClassified;

        private int _bundles;
        private int _classificationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   BundlesNeeded         => _bundlesNeeded;
        public int   ExactSeqPerBot        => _exactSeqPerBot;
        public int   BonusPerClassification => _bonusPerClassification;
        public int   Bundles               => _bundles;
        public int   ClassificationCount   => _classificationCount;
        public int   TotalBonusAwarded     => _totalBonusAwarded;
        public float BundleProgress        => _bundlesNeeded > 0
            ? Mathf.Clamp01(_bundles / (float)_bundlesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _bundles = Mathf.Min(_bundles + 1, _bundlesNeeded);
            if (_bundles >= _bundlesNeeded)
            {
                int bonus = _bonusPerClassification;
                _classificationCount++;
                _totalBonusAwarded += bonus;
                _bundles            = 0;
                _onKTheoryClassified?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _bundles = Mathf.Max(0, _bundles - _exactSeqPerBot);
        }

        public void Reset()
        {
            _bundles             = 0;
            _classificationCount = 0;
            _totalBonusAwarded   = 0;
        }
    }
}
