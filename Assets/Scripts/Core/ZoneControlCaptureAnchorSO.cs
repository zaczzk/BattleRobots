using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureAnchor", order = 197)]
    public sealed class ZoneControlCaptureAnchorSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(0)] private int _anchorBonus = 80;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onAnchorBonus;
        [SerializeField] private VoidGameEvent _onAnchorBroken;

        private bool _isAnchored;
        private int  _anchorChainLength;
        private int  _totalAnchorBonus;
        private int  _anchorsSet;

        private void OnEnable() => Reset();

        public int  AnchorBonus       => _anchorBonus;
        public bool IsAnchored        => _isAnchored;
        public int  AnchorChainLength => _anchorChainLength;
        public int  TotalAnchorBonus  => _totalAnchorBonus;
        public int  AnchorsSet        => _anchorsSet;

        public int RecordPlayerCapture()
        {
            if (!_isAnchored)
            {
                _isAnchored = true;
                _anchorsSet++;
                return 0;
            }
            _anchorChainLength++;
            _totalAnchorBonus += _anchorBonus;
            _onAnchorBonus?.Raise();
            return _anchorBonus;
        }

        public void RecordBotCapture()
        {
            if (!_isAnchored) return;
            _isAnchored        = false;
            _anchorChainLength = 0;
            _onAnchorBroken?.Raise();
        }

        public void Reset()
        {
            _isAnchored        = false;
            _anchorChainLength = 0;
            _totalAnchorBonus  = 0;
            _anchorsSet        = 0;
        }
    }
}
