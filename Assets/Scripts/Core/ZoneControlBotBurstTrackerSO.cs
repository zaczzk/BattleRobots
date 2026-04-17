using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that detects "burst" moments when bot zone captures within a
    /// sliding time window reach <c>_burstThreshold</c>.
    ///
    /// Fires <c>_onBotBurstStarted</c> on false→true transition.
    /// Fires <c>_onBotBurstEnded</c> on true→false transition.
    /// <see cref="Reset"/> clears all state silently; called from <c>OnEnable</c>.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlBotBurstTracker.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlBotBurstTracker", order = 91)]
    public sealed class ZoneControlBotBurstTrackerSO : ScriptableObject
    {
        [Header("Burst Settings")]
        [Min(2)]
        [SerializeField] private int _burstThreshold = 3;

        [Min(0.5f)]
        [SerializeField] private float _burstWindow = 8f;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onBotBurstStarted;
        [SerializeField] private VoidGameEvent _onBotBurstEnded;

        private readonly List<float> _timestamps = new List<float>();
        private bool _isBotBursting;

        private void OnEnable() => Reset();

        public bool  IsBotBursting     => _isBotBursting;
        public int   BotBurstThreshold => _burstThreshold;
        public float BotBurstWindow    => _burstWindow;
        public int   BotCaptureCount   => _timestamps.Count;

        /// <summary>Records a bot capture at <paramref name="timestamp"/> and evaluates burst state.</summary>
        public void RecordBotCapture(float timestamp)
        {
            Prune(timestamp);
            _timestamps.Add(timestamp);
            EvaluateBurst();
        }

        /// <summary>Prunes stale timestamps and re-evaluates burst state.</summary>
        public void Tick(float currentTime)
        {
            Prune(currentTime);
            EvaluateBurst();
        }

        /// <summary>Clears all runtime state silently.</summary>
        public void Reset()
        {
            _timestamps.Clear();
            _isBotBursting = false;
        }

        private void Prune(float referenceTime)
        {
            float cutoff = referenceTime - _burstWindow;
            _timestamps.RemoveAll(t => t < cutoff);
        }

        private void EvaluateBurst()
        {
            bool was = _isBotBursting;
            _isBotBursting = _timestamps.Count >= _burstThreshold;
            if (!was && _isBotBursting)
                _onBotBurstStarted?.Raise();
            else if (was && !_isBotBursting)
                _onBotBurstEnded?.Raise();
        }
    }
}
