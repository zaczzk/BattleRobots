using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureBounty", order = 164)]
    public sealed class ZoneControlCaptureBountySO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(0)]  private int   _baseBounty      = 50;
        [SerializeField, Min(1)]  private int   _maxBounty       = 500;
        [SerializeField, Min(0f)] private float _growthPerSecond = 20f;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onBountyClaimed;

        private float _currentBountyF;
        private int   _totalBountyEarned;

        private void OnEnable() => Reset();

        public int   CurrentBounty    => Mathf.RoundToInt(_currentBountyF);
        public int   BaseBounty       => _baseBounty;
        public int   MaxBounty        => _maxBounty;
        public float GrowthPerSecond  => _growthPerSecond;
        public int   TotalBountyEarned => _totalBountyEarned;

        public float BountyProgress
        {
            get
            {
                int range = _maxBounty - _baseBounty;
                if (range <= 0) return 0f;
                return Mathf.Clamp01((_currentBountyF - _baseBounty) / range);
            }
        }

        public void Tick(float dt)
        {
            if (dt <= 0f) return;
            _currentBountyF = Mathf.Min(_currentBountyF + _growthPerSecond * dt, _maxBounty);
        }

        public int ClaimPlayerCapture()
        {
            int earned = Mathf.RoundToInt(_currentBountyF);
            _totalBountyEarned += earned;
            _currentBountyF = _baseBounty;
            _onBountyClaimed?.Raise();
            return earned;
        }

        public void ClaimBotCapture()
        {
            _currentBountyF = _baseBounty;
        }

        public void Reset()
        {
            _currentBountyF    = _baseBounty;
            _totalBountyEarned = 0;
        }
    }
}
