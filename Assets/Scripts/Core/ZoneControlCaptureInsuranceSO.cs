using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureInsurance", order = 167)]
    public sealed class ZoneControlCaptureInsuranceSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(0)]          private int   _fillPerCapture  = 50;
        [SerializeField, Min(1)]          private int   _maxPool         = 500;
        [SerializeField, Range(0f, 1f)]   private float _payoutFraction  = 0.5f;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onInsurancePayout;

        private int _pool;
        private int _totalPaidOut;
        private int _lastPayout;

        private void OnEnable() => Reset();

        public int   Pool            => _pool;
        public int   MaxPool         => _maxPool;
        public int   FillPerCapture  => _fillPerCapture;
        public float PayoutFraction  => _payoutFraction;
        public int   TotalPaidOut    => _totalPaidOut;
        public int   LastPayout      => _lastPayout;
        public float PoolProgress    => _maxPool > 0 ? Mathf.Clamp01((float)_pool / _maxPool) : 0f;

        public void RecordPlayerCapture()
        {
            _pool = Mathf.Min(_pool + _fillPerCapture, _maxPool);
        }

        public void RecordBotCapture()
        {
            int payout = Mathf.RoundToInt(_pool * _payoutFraction);
            _pool        -= payout;
            _totalPaidOut += payout;
            _lastPayout   = payout;
            if (payout > 0)
                _onInsurancePayout?.Raise();
        }

        public void Reset()
        {
            _pool         = 0;
            _totalPaidOut = 0;
            _lastPayout   = 0;
        }
    }
}
