using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureHomotopy", order = 454)]
    public sealed class ZoneControlCaptureHomotopySO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _deformationsNeeded = 6;
        [SerializeField, Min(1)] private int _retractPerBot = 1;
        [SerializeField, Min(0)] private int _bonusPerDeformation = 3550;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onHomotopyComplete;

        private int _deformations;
        private int _homotopyCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   DeformationsNeeded  => _deformationsNeeded;
        public int   RetractPerBot       => _retractPerBot;
        public int   BonusPerDeformation => _bonusPerDeformation;
        public int   Deformations        => _deformations;
        public int   HomotopyCount       => _homotopyCount;
        public int   TotalBonusAwarded   => _totalBonusAwarded;
        public float HomotopyProgress    => _deformationsNeeded > 0
            ? Mathf.Clamp01(_deformations / (float)_deformationsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _deformations = Mathf.Min(_deformations + 1, _deformationsNeeded);
            if (_deformations >= _deformationsNeeded)
            {
                int bonus = _bonusPerDeformation;
                _homotopyCount++;
                _totalBonusAwarded += bonus;
                _deformations       = 0;
                _onHomotopyComplete?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _deformations = Mathf.Max(0, _deformations - _retractPerBot);
        }

        public void Reset()
        {
            _deformations      = 0;
            _homotopyCount     = 0;
            _totalBonusAwarded = 0;
        }
    }
}
