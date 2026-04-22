using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureHeap", order = 348)]
    public sealed class ZoneControlCaptureHeapSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _allocsNeeded   = 6;
        [SerializeField, Min(1)] private int _gcPerBot       = 2;
        [SerializeField, Min(0)] private int _bonusPerAlloc  = 1960;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onHeapAllocated;

        private int _allocs;
        private int _allocCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   AllocsNeeded      => _allocsNeeded;
        public int   GcPerBot          => _gcPerBot;
        public int   BonusPerAlloc     => _bonusPerAlloc;
        public int   Allocs            => _allocs;
        public int   AllocCount        => _allocCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float AllocProgress     => _allocsNeeded > 0
            ? Mathf.Clamp01(_allocs / (float)_allocsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _allocs = Mathf.Min(_allocs + 1, _allocsNeeded);
            if (_allocs >= _allocsNeeded)
            {
                int bonus = _bonusPerAlloc;
                _allocCount++;
                _totalBonusAwarded += bonus;
                _allocs             = 0;
                _onHeapAllocated?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _allocs = Mathf.Max(0, _allocs - _gcPerBot);
        }

        public void Reset()
        {
            _allocs            = 0;
            _allocCount        = 0;
            _totalBonusAwarded = 0;
        }
    }
}
