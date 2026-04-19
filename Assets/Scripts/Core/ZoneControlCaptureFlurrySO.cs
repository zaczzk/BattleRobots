using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureFlurry", order = 165)]
    public sealed class ZoneControlCaptureFlurrySO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(2)]    private int   _flurryTarget        = 4;
        [SerializeField, Min(0.1f)] private float _flurryWindowSeconds = 10f;
        [SerializeField, Min(0)]    private int   _bonusPerFlurry      = 200;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onFlurry;

        private readonly List<float> _timestamps = new List<float>();
        private int _flurryCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   FlurryCount         => _flurryCount;
        public int   TotalBonusAwarded   => _totalBonusAwarded;
        public int   FlurryTarget        => _flurryTarget;
        public float FlurryWindowSeconds => _flurryWindowSeconds;
        public int   BonusPerFlurry      => _bonusPerFlurry;
        public int   CaptureCount        => _timestamps.Count;
        public float FlurryProgress      => Mathf.Clamp01(_timestamps.Count / (float)_flurryTarget);

        public void RecordCapture(float t)
        {
            Prune(t);
            _timestamps.Add(t);

            if (_timestamps.Count >= _flurryTarget)
            {
                _flurryCount++;
                _totalBonusAwarded += _bonusPerFlurry;
                _timestamps.Clear();
                _onFlurry?.Raise();
            }
        }

        private void Prune(float t)
        {
            _timestamps.RemoveAll(ts => ts < t - _flurryWindowSeconds);
        }

        public void Reset()
        {
            _timestamps.Clear();
            _flurryCount       = 0;
            _totalBonusAwarded = 0;
        }
    }
}
