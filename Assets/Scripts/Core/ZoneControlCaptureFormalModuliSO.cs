using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureFormalModuli", order = 497)]
    public sealed class ZoneControlCaptureFormalModuliSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _deformationsNeeded   = 5;
        [SerializeField, Min(1)] private int _obstructionsPerBot   = 1;
        [SerializeField, Min(0)] private int _bonusPerClassification = 4195;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onFormalModuliClassified;

        private int _deformations;
        private int _classificationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   DeformationsNeeded    => _deformationsNeeded;
        public int   ObstructionsPerBot    => _obstructionsPerBot;
        public int   BonusPerClassification => _bonusPerClassification;
        public int   Deformations          => _deformations;
        public int   ClassificationCount   => _classificationCount;
        public int   TotalBonusAwarded     => _totalBonusAwarded;
        public float DeformationProgress => _deformationsNeeded > 0
            ? Mathf.Clamp01(_deformations / (float)_deformationsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _deformations = Mathf.Min(_deformations + 1, _deformationsNeeded);
            if (_deformations >= _deformationsNeeded)
            {
                int bonus = _bonusPerClassification;
                _classificationCount++;
                _totalBonusAwarded += bonus;
                _deformations       = 0;
                _onFormalModuliClassified?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _deformations = Mathf.Max(0, _deformations - _obstructionsPerBot);
        }

        public void Reset()
        {
            _deformations        = 0;
            _classificationCount = 0;
            _totalBonusAwarded   = 0;
        }
    }
}
