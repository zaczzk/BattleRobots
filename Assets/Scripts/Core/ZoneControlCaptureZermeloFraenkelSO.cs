using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureZermeloFraenkel", order = 537)]
    public sealed class ZoneControlCaptureZermeloFraenkelSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _verifiedAxiomsNeeded           = 7;
        [SerializeField, Min(1)] private int _paradoxInconsistenciesPerBot   = 2;
        [SerializeField, Min(0)] private int _bonusPerAxiomSet               = 4795;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onZermeloFraenkelVerified;

        private int _verifiedAxioms;
        private int _axiomSetCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   VerifiedAxiomsNeeded         => _verifiedAxiomsNeeded;
        public int   ParadoxInconsistenciesPerBot => _paradoxInconsistenciesPerBot;
        public int   BonusPerAxiomSet             => _bonusPerAxiomSet;
        public int   VerifiedAxioms               => _verifiedAxioms;
        public int   AxiomSetCount                => _axiomSetCount;
        public int   TotalBonusAwarded            => _totalBonusAwarded;
        public float VerifiedAxiomProgress        => _verifiedAxiomsNeeded > 0
            ? Mathf.Clamp01(_verifiedAxioms / (float)_verifiedAxiomsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _verifiedAxioms = Mathf.Min(_verifiedAxioms + 1, _verifiedAxiomsNeeded);
            if (_verifiedAxioms >= _verifiedAxiomsNeeded)
            {
                int bonus = _bonusPerAxiomSet;
                _axiomSetCount++;
                _totalBonusAwarded += bonus;
                _verifiedAxioms     = 0;
                _onZermeloFraenkelVerified?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _verifiedAxioms = Mathf.Max(0, _verifiedAxioms - _paradoxInconsistenciesPerBot);
        }

        public void Reset()
        {
            _verifiedAxioms    = 0;
            _axiomSetCount     = 0;
            _totalBonusAwarded = 0;
        }
    }
}
