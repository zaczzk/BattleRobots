using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureNet", order = 444)]
    public sealed class ZoneControlCaptureNetSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _termsNeeded        = 6;
        [SerializeField, Min(1)] private int _scatterPerBot      = 1;
        [SerializeField, Min(0)] private int _bonusPerConvergence = 3400;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onNetConverged;

        private int _terms;
        private int _convergenceCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   TermsNeeded        => _termsNeeded;
        public int   ScatterPerBot      => _scatterPerBot;
        public int   BonusPerConvergence => _bonusPerConvergence;
        public int   Terms              => _terms;
        public int   ConvergenceCount   => _convergenceCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float NetProgress        => _termsNeeded > 0
            ? Mathf.Clamp01(_terms / (float)_termsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _terms = Mathf.Min(_terms + 1, _termsNeeded);
            if (_terms >= _termsNeeded)
            {
                int bonus = _bonusPerConvergence;
                _convergenceCount++;
                _totalBonusAwarded += bonus;
                _terms              = 0;
                _onNetConverged?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _terms = Mathf.Max(0, _terms - _scatterPerBot);
        }

        public void Reset()
        {
            _terms             = 0;
            _convergenceCount  = 0;
            _totalBonusAwarded = 0;
        }
    }
}
