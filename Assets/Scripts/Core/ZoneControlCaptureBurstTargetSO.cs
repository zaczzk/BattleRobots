using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that detects when the player captures <c>_burstTargetCount</c>
    /// zones within a <c>_burstWindow</c>-second sliding time window (a "burst target").
    ///
    /// When the target count is met, <c>_onBurstTargetMet</c> is fired, the
    /// burst counter increments, and the timestamp list is cleared so the next
    /// burst requires a fresh set of captures.
    /// Call <see cref="Tick"/> each frame to prune expired timestamps.
    /// <see cref="Reset"/> clears all state silently; called from <c>OnEnable</c>.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlCaptureBurstTarget.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureBurstTarget", order = 101)]
    public sealed class ZoneControlCaptureBurstTargetSO : ScriptableObject
    {
        [Header("Burst Target Settings")]
        [Min(2)]
        [SerializeField] private int _burstTargetCount = 5;

        [Min(1f)]
        [SerializeField] private float _burstWindow = 20f;

        [Min(0)]
        [SerializeField] private int _burstReward = 300;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onBurstTargetMet;

        private readonly List<float> _timestamps = new List<float>();
        private int _burstsMet;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int BurstTargetCount   => _burstTargetCount;
        public int BurstsMet          => _burstsMet;
        public int TotalBonusAwarded  => _totalBonusAwarded;
        public int BurstReward        => _burstReward;
        public int CaptureCount       => _timestamps.Count;

        /// <summary>
        /// Records a capture at <paramref name="timestamp"/>.  When in-window
        /// captures reach <c>BurstTargetCount</c>, a burst is credited, the
        /// bonus accumulated, the event fired, and the list cleared.
        /// </summary>
        public void RecordCapture(float timestamp)
        {
            Prune(timestamp);
            _timestamps.Add(timestamp);

            if (_timestamps.Count >= _burstTargetCount)
            {
                _burstsMet++;
                _totalBonusAwarded += _burstReward;
                _onBurstTargetMet?.Raise();
                _timestamps.Clear();
            }
        }

        /// <summary>Prunes expired timestamps (no burst evaluation).</summary>
        public void Tick(float currentTime)
        {
            Prune(currentTime);
        }

        /// <summary>Clears all runtime state silently.</summary>
        public void Reset()
        {
            _timestamps.Clear();
            _burstsMet         = 0;
            _totalBonusAwarded = 0;
        }

        private void Prune(float referenceTime)
        {
            float cutoff = referenceTime - _burstWindow;
            _timestamps.RemoveAll(t => t < cutoff);
        }
    }
}
