using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureFusion", order = 199)]
    public sealed class ZoneControlCaptureFusionSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _chargeThreshold = 4;
        [SerializeField, Min(0)] private int _bonusPerFusion  = 325;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onFusion;

        private int _botChargeCount;
        private int _fusionCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ChargeThreshold    => _chargeThreshold;
        public int   BonusPerFusion     => _bonusPerFusion;
        public int   BotChargeCount     => _botChargeCount;
        public int   FusionCount        => _fusionCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float ChargeProgress     => _chargeThreshold > 0
            ? Mathf.Clamp01(_botChargeCount / (float)_chargeThreshold)
            : 0f;

        public void RecordBotCapture()
        {
            _botChargeCount++;
        }

        public int RecordPlayerCapture()
        {
            bool fused = _botChargeCount >= _chargeThreshold;
            _botChargeCount = 0;
            if (!fused) return 0;
            _fusionCount++;
            _totalBonusAwarded += _bonusPerFusion;
            _onFusion?.Raise();
            return _bonusPerFusion;
        }

        public void Reset()
        {
            _botChargeCount    = 0;
            _fusionCount       = 0;
            _totalBonusAwarded = 0;
        }
    }
}
