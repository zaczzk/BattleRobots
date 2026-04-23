using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureAlgebra", order = 384)]
    public sealed class ZoneControlCaptureAlgebraSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _termsNeeded  = 6;
        [SerializeField, Min(1)] private int _breakPerBot  = 2;
        [SerializeField, Min(0)] private int _bonusPerFold = 2500;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onAlgebraFolded;

        private int _terms;
        private int _foldCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   TermsNeeded       => _termsNeeded;
        public int   BreakPerBot       => _breakPerBot;
        public int   BonusPerFold      => _bonusPerFold;
        public int   Terms             => _terms;
        public int   FoldCount         => _foldCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float TermProgress      => _termsNeeded > 0
            ? Mathf.Clamp01(_terms / (float)_termsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _terms = Mathf.Min(_terms + 1, _termsNeeded);
            if (_terms >= _termsNeeded)
            {
                int bonus = _bonusPerFold;
                _foldCount++;
                _totalBonusAwarded += bonus;
                _terms              = 0;
                _onAlgebraFolded?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _terms = Mathf.Max(0, _terms - _breakPerBot);
        }

        public void Reset()
        {
            _terms             = 0;
            _foldCount         = 0;
            _totalBonusAwarded = 0;
        }
    }
}
