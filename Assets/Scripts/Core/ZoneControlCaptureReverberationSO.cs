using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureReverberation", order = 178)]
    public sealed class ZoneControlCaptureReverberationSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(0)]  private int _baseBonus      = 15;
        [SerializeField, Min(1)]  private int _maxMultiplier  = 8;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onReverberationPayout;

        private int _currentMultiplier;
        private int _totalEarned;
        private int _payoutCount;

        private void OnEnable() => Reset();

        public int BaseBonus            => _baseBonus;
        public int MaxMultiplier        => _maxMultiplier;
        public int CurrentMultiplier    => _currentMultiplier;
        public int TotalEarned          => _totalEarned;
        public int PayoutCount          => _payoutCount;
        public int PendingBonus         => Mathf.RoundToInt(_baseBonus * _currentMultiplier);

        public void RecordPlayerCapture()
        {
            _currentMultiplier = Mathf.Min(_currentMultiplier + 1, _maxMultiplier);
        }

        public int RecordBotCapture()
        {
            int payout = PendingBonus;
            if (payout > 0)
            {
                _totalEarned += payout;
                _payoutCount++;
                _onReverberationPayout?.Raise();
            }
            _currentMultiplier = 0;
            return payout;
        }

        public void Reset()
        {
            _currentMultiplier = 0;
            _totalEarned       = 0;
            _payoutCount       = 0;
        }
    }
}
