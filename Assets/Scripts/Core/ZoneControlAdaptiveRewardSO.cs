using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlAdaptiveReward", order = 101)]
    public sealed class ZoneControlAdaptiveRewardSO : ScriptableObject
    {
        [Header("Scale Settings")]
        [Min(0.1f)]
        [SerializeField] private float _minScaleFactor = 0.5f;
        [Min(1f)]
        [SerializeField] private float _maxScaleFactor = 2f;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onScaleChanged;

        private float _currentScaleFactor = 1f;

        private void OnEnable() => Reset();

        public float CurrentScaleFactor => _currentScaleFactor;
        public float MinScaleFactor     => _minScaleFactor;
        public float MaxScaleFactor     => _maxScaleFactor;

        public void SetPerformanceRatio(float ratio)
        {
            _currentScaleFactor = Mathf.Lerp(_minScaleFactor, _maxScaleFactor, Mathf.Clamp01(ratio));
            _onScaleChanged?.Raise();
        }

        public int ApplyReward(int baseAmount) =>
            Mathf.RoundToInt(baseAmount * _currentScaleFactor);

        public void Reset()
        {
            _currentScaleFactor = 1f;
        }

        private void OnValidate()
        {
            _minScaleFactor = Mathf.Max(0.1f, _minScaleFactor);
            _maxScaleFactor = Mathf.Max(_minScaleFactor, _maxScaleFactor);
        }
    }
}
