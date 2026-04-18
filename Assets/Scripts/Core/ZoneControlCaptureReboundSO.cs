using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Awards a bonus when the player recaptures a zone within
    /// <c>_reboundWindowSeconds</c> of losing it.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlCaptureRebound.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureRebound", order = 130)]
    public sealed class ZoneControlCaptureReboundSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(0.1f)] private float _reboundWindowSeconds = 10f;
        [SerializeField, Min(0)]    private int   _bonusPerRebound      = 150;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onRebound;

        private float _zoneLostTime = -1f;
        private int   _reboundCount;
        private int   _totalBonusAwarded;

        private void OnEnable() => Reset();

        public float ReboundWindowSeconds => _reboundWindowSeconds;
        public int   BonusPerRebound      => _bonusPerRebound;
        public int   ReboundCount         => _reboundCount;
        public int   TotalBonusAwarded    => _totalBonusAwarded;
        public bool  HasPendingLoss       => _zoneLostTime >= 0f;

        public void RecordZoneLost(float time)
        {
            _zoneLostTime = time;
        }

        public void RecordRecapture(float time)
        {
            if (!HasPendingLoss) return;

            float gap = time - _zoneLostTime;
            _zoneLostTime = -1f;

            if (gap <= _reboundWindowSeconds)
            {
                _reboundCount++;
                _totalBonusAwarded += _bonusPerRebound;
                _onRebound?.Raise();
            }
        }

        public void Reset()
        {
            _zoneLostTime      = -1f;
            _reboundCount      = 0;
            _totalBonusAwarded = 0;
        }
    }
}
