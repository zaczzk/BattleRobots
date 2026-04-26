using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureResolutionPrinciple", order = 549)]
    public sealed class ZoneControlCaptureResolutionPrincipleSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _resolvedClausesNeeded = 6;
        [SerializeField, Min(1)] private int _tautologiesPerBot     = 1;
        [SerializeField, Min(0)] private int _bonusPerResolution    = 4975;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onResolutionPrincipleApplied;

        private int _resolvedClauses;
        private int _resolutionCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ResolvedClausesNeeded => _resolvedClausesNeeded;
        public int   TautologiesPerBot     => _tautologiesPerBot;
        public int   BonusPerResolution    => _bonusPerResolution;
        public int   ResolvedClauses       => _resolvedClauses;
        public int   ResolutionCount       => _resolutionCount;
        public int   TotalBonusAwarded     => _totalBonusAwarded;
        public float ResolvedClauseProgress => _resolvedClausesNeeded > 0
            ? Mathf.Clamp01(_resolvedClauses / (float)_resolvedClausesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _resolvedClauses = Mathf.Min(_resolvedClauses + 1, _resolvedClausesNeeded);
            if (_resolvedClauses >= _resolvedClausesNeeded)
            {
                int bonus = _bonusPerResolution;
                _resolutionCount++;
                _totalBonusAwarded += bonus;
                _resolvedClauses    = 0;
                _onResolutionPrincipleApplied?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _resolvedClauses = Mathf.Max(0, _resolvedClauses - _tautologiesPerBot);
        }

        public void Reset()
        {
            _resolvedClauses   = 0;
            _resolutionCount   = 0;
            _totalBonusAwarded = 0;
        }
    }
}
