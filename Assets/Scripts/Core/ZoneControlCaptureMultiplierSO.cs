using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureMultiplier", order = 147)]
    public sealed class ZoneControlCaptureMultiplierSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(0.01f)] private float _multiplierStep = 0.1f;
        [SerializeField, Min(1f)]    private float _maxMultiplier  = 3f;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onMultiplierUpdated;

        private float _currentMultiplier = 1f;

        private void OnEnable() => Reset();

        public float CurrentMultiplier => _currentMultiplier;
        public float MultiplierStep    => _multiplierStep;
        public float MaxMultiplier     => _maxMultiplier;

        public void RecordCapture()
        {
            _currentMultiplier = Mathf.Min(_currentMultiplier + _multiplierStep, _maxMultiplier);
            _onMultiplierUpdated?.Raise();
        }

        public int RewardForCapture(int baseReward)
        {
            return Mathf.RoundToInt(baseReward * _currentMultiplier);
        }

        public void Reset()
        {
            _currentMultiplier = 1f;
        }
    }
}
