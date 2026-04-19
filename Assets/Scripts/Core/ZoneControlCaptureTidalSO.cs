using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureTidal", order = 173)]
    public sealed class ZoneControlCaptureTidalSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(0)] private int _bonusPerCycle = 250;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onTidalCycle;

        // 0 = none, 1 = player leading, 2 = bot leading
        private int _phase;
        private int _cycleCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int BonusPerCycle    => _bonusPerCycle;
        public int CycleCount       => _cycleCount;
        public int TotalBonusAwarded => _totalBonusAwarded;
        public int Phase            => _phase;

        public void RecordLeadState(bool playerLeading)
        {
            if (_phase == 0)
            {
                _phase = playerLeading ? 1 : 2;
                return;
            }

            if (playerLeading && _phase == 2)
            {
                _cycleCount++;
                _totalBonusAwarded += _bonusPerCycle;
                _onTidalCycle?.Raise();
                _phase = 1;
                return;
            }

            if (!playerLeading && _phase == 1)
            {
                _phase = 2;
            }
            // same state: no-op
        }

        public void Reset()
        {
            _phase             = 0;
            _cycleCount        = 0;
            _totalBonusAwarded = 0;
        }
    }
}
