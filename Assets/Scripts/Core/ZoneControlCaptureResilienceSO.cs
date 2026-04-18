using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that measures player capture resilience — how quickly the player
    /// recaptures a zone after losing it. <c>RecordZoneLost(t)</c> arms the timer;
    /// <c>RecordRecapture(t)</c> computes the response delta and fires
    /// <c>_onResilienceUpdated</c>. <c>AverageResponseTime</c> is the running mean.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlCaptureResilience.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureResilience", order = 116)]
    public sealed class ZoneControlCaptureResilienceSO : ScriptableObject
    {
        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onResilienceUpdated;

        private float _lostTime;
        private bool  _hasPendingLoss;
        private float _totalResponseTime;
        private int   _recaptureCount;

        private void OnEnable() => Reset();

        public int   RecaptureCount      => _recaptureCount;
        public float TotalResponseTime   => _totalResponseTime;
        public bool  HasPendingLoss      => _hasPendingLoss;
        public float AverageResponseTime => _recaptureCount > 0
            ? _totalResponseTime / _recaptureCount
            : 0f;

        /// <summary>Arms the response timer. Replaces any previous pending loss.</summary>
        public void RecordZoneLost(float t)
        {
            _lostTime       = t;
            _hasPendingLoss = true;
        }

        /// <summary>
        /// Computes response time since the last <c>RecordZoneLost</c> call.
        /// No-ops when no pending loss exists.
        /// </summary>
        public void RecordRecapture(float t)
        {
            if (!_hasPendingLoss) return;
            _totalResponseTime += Mathf.Max(0f, t - _lostTime);
            _recaptureCount++;
            _hasPendingLoss = false;
            _onResilienceUpdated?.Raise();
        }

        /// <summary>Clears all runtime state silently.</summary>
        public void Reset()
        {
            _lostTime          = 0f;
            _hasPendingLoss    = false;
            _totalResponseTime = 0f;
            _recaptureCount    = 0;
        }
    }
}
