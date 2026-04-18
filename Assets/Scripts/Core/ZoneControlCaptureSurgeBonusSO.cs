using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Awards a bonus when the player captures <c>_surgeCount</c> zones
    /// within <c>_surgeWindowSeconds</c>. Uses a rolling timestamp list;
    /// when the surge target is reached the timestamps are cleared so the
    /// next surge can accumulate independently.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlCaptureSurgeBonus.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureSurgeBonus", order = 129)]
    public sealed class ZoneControlCaptureSurgeBonusSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(2)]    private int   _surgeCount         = 3;
        [SerializeField, Min(0.5f)] private float _surgeWindowSeconds = 8f;
        [SerializeField, Min(0)]    private int   _bonusPerSurge      = 200;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onSurgeBonus;

        private readonly List<float> _timestamps = new List<float>();
        private int _surgeTotal;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   SurgeCount         => _surgeCount;
        public float SurgeWindowSeconds => _surgeWindowSeconds;
        public int   BonusPerSurge      => _bonusPerSurge;
        public int   SurgeTotal         => _surgeTotal;
        public int   TotalBonusAwarded  => _totalBonusAwarded;

        /// <summary>Progress toward next surge [0,1].</summary>
        public float SurgeProgress => Mathf.Clamp01((float)_timestamps.Count / Mathf.Max(1, _surgeCount));

        public void RecordCapture(float time)
        {
            Prune(time);
            _timestamps.Add(time);

            if (_timestamps.Count >= _surgeCount)
            {
                _surgeTotal++;
                _totalBonusAwarded += _bonusPerSurge;
                _timestamps.Clear();
                _onSurgeBonus?.Raise();
            }
        }

        private void Prune(float now)
        {
            float cutoff = now - _surgeWindowSeconds;
            _timestamps.RemoveAll(t => t < cutoff);
        }

        public void Reset()
        {
            _timestamps.Clear();
            _surgeTotal        = 0;
            _totalBonusAwarded = 0;
        }
    }
}
