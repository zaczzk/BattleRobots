using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureHochschildCohomology", order = 477)]
    public sealed class ZoneControlCaptureHochschildCohomologySO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _deformationsNeeded  = 5;
        [SerializeField, Min(1)] private int _obstructionPerBot   = 1;
        [SerializeField, Min(0)] private int _bonusPerDeformation = 3895;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onHochschildCohomologyDeformed;

        private int _deformations;
        private int _deformCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   DeformationsNeeded  => _deformationsNeeded;
        public int   ObstructionPerBot   => _obstructionPerBot;
        public int   BonusPerDeformation => _bonusPerDeformation;
        public int   Deformations        => _deformations;
        public int   DeformCount         => _deformCount;
        public int   TotalBonusAwarded   => _totalBonusAwarded;
        public float DeformationProgress => _deformationsNeeded > 0
            ? Mathf.Clamp01(_deformations / (float)_deformationsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _deformations = Mathf.Min(_deformations + 1, _deformationsNeeded);
            if (_deformations >= _deformationsNeeded)
            {
                int bonus = _bonusPerDeformation;
                _deformCount++;
                _totalBonusAwarded += bonus;
                _deformations       = 0;
                _onHochschildCohomologyDeformed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _deformations = Mathf.Max(0, _deformations - _obstructionPerBot);
        }

        public void Reset()
        {
            _deformations      = 0;
            _deformCount       = 0;
            _totalBonusAwarded = 0;
        }
    }
}
