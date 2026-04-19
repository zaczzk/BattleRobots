using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureResonance", order = 157)]
    public sealed class ZoneControlCaptureResonanceSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(0.1f)] private float _resonanceWindow  = 4f;
        [SerializeField, Min(2)]    private int   _resonanceTarget  = 4;
        [SerializeField, Min(0)]    private int   _bonusPerResonance = 175;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onResonance;

        private readonly List<float> _timestamps = new List<float>();
        private int _resonanceCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ResonanceTarget   => _resonanceTarget;
        public int   BonusPerResonance => _bonusPerResonance;
        public int   ResonanceCount    => _resonanceCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float ResonanceWindow   => _resonanceWindow;
        public int   CaptureCount      => _timestamps.Count;
        public float ResonanceProgress => Mathf.Clamp01(_timestamps.Count / (float)_resonanceTarget);

        public void RecordCapture(float t)
        {
            Prune(t);
            _timestamps.Add(t);

            if (_timestamps.Count >= _resonanceTarget)
            {
                _resonanceCount++;
                _totalBonusAwarded += _bonusPerResonance;
                _timestamps.Clear();
                _onResonance?.Raise();
            }
        }

        public void Reset()
        {
            _timestamps.Clear();
            _resonanceCount    = 0;
            _totalBonusAwarded = 0;
        }

        private void Prune(float t)
        {
            float cutoff = t - _resonanceWindow;
            for (int i = _timestamps.Count - 1; i >= 0; i--)
            {
                if (_timestamps[i] < cutoff)
                    _timestamps.RemoveAt(i);
            }
        }
    }
}
