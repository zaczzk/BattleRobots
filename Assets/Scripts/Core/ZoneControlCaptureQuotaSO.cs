using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that tracks a single per-match capture quota.
    /// <see cref="RecordCapture"/> increments the count; when it reaches
    /// <c>_quotaTarget</c>, <c>_onQuotaMet</c> is fired once and
    /// <see cref="QuotaMet"/> becomes true (idempotent thereafter).
    /// <see cref="Reset"/> clears all state silently; called from <c>OnEnable</c>.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlCaptureQuota.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureQuota", order = 102)]
    public sealed class ZoneControlCaptureQuotaSO : ScriptableObject
    {
        [Header("Quota Settings")]
        [Min(1)]
        [SerializeField] private int _quotaTarget = 10;

        [Min(0)]
        [SerializeField] private int _bonusOnCompletion = 500;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onQuotaMet;

        private int  _captureCount;
        private bool _quotaMet;

        private void OnEnable() => Reset();

        public int   QuotaTarget        => _quotaTarget;
        public int   BonusOnCompletion  => _bonusOnCompletion;
        public int   CaptureCount       => _captureCount;
        public bool  QuotaMet           => _quotaMet;
        public float QuotaProgress      => Mathf.Clamp01((float)_captureCount / _quotaTarget);

        /// <summary>Records a player zone capture toward the quota.</summary>
        public void RecordCapture()
        {
            if (_quotaMet) return;
            _captureCount++;
            if (_captureCount >= _quotaTarget)
            {
                _quotaMet = true;
                _onQuotaMet?.Raise();
            }
        }

        /// <summary>Clears all runtime state silently.</summary>
        public void Reset()
        {
            _captureCount = 0;
            _quotaMet     = false;
        }
    }
}
