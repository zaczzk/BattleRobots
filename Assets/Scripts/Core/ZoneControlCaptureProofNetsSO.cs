using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureProofNets", order = 557)]
    public sealed class ZoneControlCaptureProofNetsSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _netLinksNeeded           = 6;
        [SerializeField, Min(1)] private int _cyclicObstructionsPerBot = 1;
        [SerializeField, Min(0)] private int _bonusPerNetLink          = 5095;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onProofNetsCompleted;

        private int _netLinks;
        private int _netLinkCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   NetLinksNeeded           => _netLinksNeeded;
        public int   CyclicObstructionsPerBot => _cyclicObstructionsPerBot;
        public int   BonusPerNetLink          => _bonusPerNetLink;
        public int   NetLinks                 => _netLinks;
        public int   NetLinkCount             => _netLinkCount;
        public int   TotalBonusAwarded        => _totalBonusAwarded;
        public float NetLinkProgress => _netLinksNeeded > 0
            ? Mathf.Clamp01(_netLinks / (float)_netLinksNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _netLinks = Mathf.Min(_netLinks + 1, _netLinksNeeded);
            if (_netLinks >= _netLinksNeeded)
            {
                int bonus = _bonusPerNetLink;
                _netLinkCount++;
                _totalBonusAwarded += bonus;
                _netLinks           = 0;
                _onProofNetsCompleted?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _netLinks = Mathf.Max(0, _netLinks - _cyclicObstructionsPerBot);
        }

        public void Reset()
        {
            _netLinks          = 0;
            _netLinkCount      = 0;
            _totalBonusAwarded = 0;
        }
    }
}
