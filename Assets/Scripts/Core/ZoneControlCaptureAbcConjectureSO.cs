using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureAbcConjecture", order = 519)]
    public sealed class ZoneControlCaptureAbcConjectureSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _coprimeTriplesNeeded    = 6;
        [SerializeField, Min(1)] private int _radicalReductionsPerBot = 1;
        [SerializeField, Min(0)] private int _bonusPerConjecture      = 4525;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onAbcConjectured;

        private int _coprimeTriples;
        private int _conjectureCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   CoprimeTriplesNeeded    => _coprimeTriplesNeeded;
        public int   RadicalReductionsPerBot => _radicalReductionsPerBot;
        public int   BonusPerConjecture      => _bonusPerConjecture;
        public int   CoprimeTriples          => _coprimeTriples;
        public int   ConjectureCount         => _conjectureCount;
        public int   TotalBonusAwarded       => _totalBonusAwarded;
        public float CoprimeTripleProgress   => _coprimeTriplesNeeded > 0
            ? Mathf.Clamp01(_coprimeTriples / (float)_coprimeTriplesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _coprimeTriples = Mathf.Min(_coprimeTriples + 1, _coprimeTriplesNeeded);
            if (_coprimeTriples >= _coprimeTriplesNeeded)
            {
                int bonus = _bonusPerConjecture;
                _conjectureCount++;
                _totalBonusAwarded += bonus;
                _coprimeTriples     = 0;
                _onAbcConjectured?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _coprimeTriples = Mathf.Max(0, _coprimeTriples - _radicalReductionsPerBot);
        }

        public void Reset()
        {
            _coprimeTriples    = 0;
            _conjectureCount   = 0;
            _totalBonusAwarded = 0;
        }
    }
}
