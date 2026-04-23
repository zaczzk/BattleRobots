using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureClosed", order = 428)]
    public sealed class ZoneControlCaptureClosedSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _homTermsNeeded = 6;
        [SerializeField, Min(1)] private int _openPerBot     = 2;
        [SerializeField, Min(0)] private int _bonusPerClose  = 3160;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onClosed;

        private int _homTerms;
        private int _closeCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   HomTermsNeeded    => _homTermsNeeded;
        public int   OpenPerBot        => _openPerBot;
        public int   BonusPerClose     => _bonusPerClose;
        public int   HomTerms          => _homTerms;
        public int   CloseCount        => _closeCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float HomTermProgress   => _homTermsNeeded > 0
            ? Mathf.Clamp01(_homTerms / (float)_homTermsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _homTerms = Mathf.Min(_homTerms + 1, _homTermsNeeded);
            if (_homTerms >= _homTermsNeeded)
            {
                int bonus = _bonusPerClose;
                _closeCount++;
                _totalBonusAwarded += bonus;
                _homTerms           = 0;
                _onClosed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _homTerms = Mathf.Max(0, _homTerms - _openPerBot);
        }

        public void Reset()
        {
            _homTerms          = 0;
            _closeCount        = 0;
            _totalBonusAwarded = 0;
        }
    }
}
