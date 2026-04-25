using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureGödelsCompletenessTheorem", order = 543)]
    public sealed class ZoneControlCaptureGödelsCompletenessTheoremSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _consistentExtensionsNeeded        = 6;
        [SerializeField, Min(1)] private int _incompleteAxiomatizationsPerBot   = 1;
        [SerializeField, Min(0)] private int _bonusPerCompletion                = 4885;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onGödelsCompletenessTheoremCompleted;

        private int _consistentExtensions;
        private int _completionCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ConsistentExtensionsNeeded      => _consistentExtensionsNeeded;
        public int   IncompleteAxiomatizationsPerBot => _incompleteAxiomatizationsPerBot;
        public int   BonusPerCompletion              => _bonusPerCompletion;
        public int   ConsistentExtensions            => _consistentExtensions;
        public int   CompletionCount                 => _completionCount;
        public int   TotalBonusAwarded               => _totalBonusAwarded;
        public float ConsistentExtensionProgress     => _consistentExtensionsNeeded > 0
            ? Mathf.Clamp01(_consistentExtensions / (float)_consistentExtensionsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _consistentExtensions = Mathf.Min(_consistentExtensions + 1, _consistentExtensionsNeeded);
            if (_consistentExtensions >= _consistentExtensionsNeeded)
            {
                int bonus = _bonusPerCompletion;
                _completionCount++;
                _totalBonusAwarded   += bonus;
                _consistentExtensions = 0;
                _onGödelsCompletenessTheoremCompleted?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _consistentExtensions = Mathf.Max(0, _consistentExtensions - _incompleteAxiomatizationsPerBot);
        }

        public void Reset()
        {
            _consistentExtensions = 0;
            _completionCount      = 0;
            _totalBonusAwarded    = 0;
        }
    }
}
