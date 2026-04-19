using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureTorrent", order = 194)]
    public sealed class ZoneControlCaptureTorrentSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(0)]          private int   _torrentBuildPerCapture = 50;
        [SerializeField, Range(0f, 1f)]   private float _payoutFraction         = 0.75f;
        [SerializeField, Min(1)]          private int   _maxPool                = 1000;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onTorrentPayout;

        private int _torrentPool;
        private int _torrentPayouts;
        private int _totalPaidOut;

        private void OnEnable() => Reset();

        public int   TorrentBuildPerCapture => _torrentBuildPerCapture;
        public float PayoutFraction         => _payoutFraction;
        public int   MaxPool                => _maxPool;
        public int   TorrentPool            => _torrentPool;
        public int   TorrentPayouts         => _torrentPayouts;
        public int   TotalPaidOut           => _totalPaidOut;
        public float TorrentProgress        => _maxPool > 0
            ? Mathf.Clamp01(_torrentPool / (float)_maxPool)
            : 0f;

        public void RecordPlayerCapture()
        {
            _torrentPool = Mathf.Min(_torrentPool + _torrentBuildPerCapture, _maxPool);
        }

        public int RecordBotCapture()
        {
            int payout   = Mathf.RoundToInt(_torrentPool * _payoutFraction);
            _torrentPool -= payout;
            if (payout > 0)
            {
                _torrentPayouts++;
                _totalPaidOut += payout;
                _onTorrentPayout?.Raise();
            }
            return payout;
        }

        public void Reset()
        {
            _torrentPool    = 0;
            _torrentPayouts = 0;
            _totalPaidOut   = 0;
        }
    }
}
