using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that detects "capture rush" moments when the player captures
    /// <c>_requiredCaptures</c> or more zones within a <c>_rushWindow</c>-second
    /// sliding time window.
    ///
    /// Fires <c>_onRushCompleted</c> on the false→true rush transition.
    /// The rush ends when the window expires and the in-window count falls below
    /// the threshold.
    /// <see cref="Reset"/> clears all state silently; called from <c>OnEnable</c>.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlCaptureRush.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureRush", order = 83)]
    public sealed class ZoneControlCaptureRushSO : ScriptableObject
    {
        [Header("Rush Settings")]
        [Min(2)]
        [SerializeField] private int _requiredCaptures = 3;

        [Min(1f)]
        [SerializeField] private float _rushWindow = 8f;

        [Min(0)]
        [SerializeField] private int _rushBonus = 200;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onRushCompleted;

        private readonly List<float> _timestamps = new List<float>();
        private bool _isRushing;
        private int  _totalRushCount;
        private int  _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   RequiredCaptures  => _requiredCaptures;
        public float RushWindow        => _rushWindow;
        public int   RushBonus         => _rushBonus;
        public bool  IsRushing         => _isRushing;
        public int   TotalRushCount    => _totalRushCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public int   CaptureCount      => _timestamps.Count;

        /// <summary>Records a capture at <paramref name="timestamp"/> and evaluates rush state.</summary>
        public void RecordCapture(float timestamp)
        {
            Prune(timestamp);
            _timestamps.Add(timestamp);
            EvaluateRush();
        }

        /// <summary>Prunes stale timestamps and re-evaluates rush state.</summary>
        public void Tick(float currentTime)
        {
            Prune(currentTime);
            EvaluateRush();
        }

        /// <summary>Clears all runtime state silently.</summary>
        public void Reset()
        {
            _timestamps.Clear();
            _isRushing         = false;
            _totalRushCount    = 0;
            _totalBonusAwarded = 0;
        }

        private void Prune(float referenceTime)
        {
            float cutoff = referenceTime - _rushWindow;
            _timestamps.RemoveAll(t => t < cutoff);
        }

        private void EvaluateRush()
        {
            bool wasRushing = _isRushing;
            _isRushing = _timestamps.Count >= _requiredCaptures;
            if (!wasRushing && _isRushing)
            {
                _totalRushCount++;
                _totalBonusAwarded += _rushBonus;
                _onRushCompleted?.Raise();
            }
        }
    }
}
