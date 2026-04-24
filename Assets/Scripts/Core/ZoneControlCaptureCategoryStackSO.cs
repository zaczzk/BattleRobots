using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureCategoryStack", order = 459)]
    public sealed class ZoneControlCaptureCategoryStackSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _patchesNeeded  = 6;
        [SerializeField, Min(1)] private int _obstructPerBot = 2;
        [SerializeField, Min(0)] private int _bonusPerDescend = 3625;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onCategoryStackDescended;

        private int _patches;
        private int _descendCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   PatchesNeeded      => _patchesNeeded;
        public int   ObstructPerBot     => _obstructPerBot;
        public int   BonusPerDescend    => _bonusPerDescend;
        public int   Patches            => _patches;
        public int   DescendCount       => _descendCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float PatchProgress      => _patchesNeeded > 0
            ? Mathf.Clamp01(_patches / (float)_patchesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _patches = Mathf.Min(_patches + 1, _patchesNeeded);
            if (_patches >= _patchesNeeded)
            {
                int bonus = _bonusPerDescend;
                _descendCount++;
                _totalBonusAwarded += bonus;
                _patches            = 0;
                _onCategoryStackDescended?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _patches = Mathf.Max(0, _patches - _obstructPerBot);
        }

        public void Reset()
        {
            _patches           = 0;
            _descendCount      = 0;
            _totalBonusAwarded = 0;
        }
    }
}
