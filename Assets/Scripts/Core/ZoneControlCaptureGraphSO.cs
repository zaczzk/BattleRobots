using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureGraph", order = 352)]
    public sealed class ZoneControlCaptureGraphSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _edgesNeeded     = 5;
        [SerializeField, Min(1)] private int _removePerBot    = 1;
        [SerializeField, Min(0)] private int _bonusPerConnect = 2020;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onGraphConnected;

        private int _edges;
        private int _connectCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   EdgesNeeded       => _edgesNeeded;
        public int   RemovePerBot      => _removePerBot;
        public int   BonusPerConnect   => _bonusPerConnect;
        public int   Edges             => _edges;
        public int   ConnectCount      => _connectCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float EdgeProgress      => _edgesNeeded > 0
            ? Mathf.Clamp01(_edges / (float)_edgesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _edges = Mathf.Min(_edges + 1, _edgesNeeded);
            if (_edges >= _edgesNeeded)
            {
                int bonus = _bonusPerConnect;
                _connectCount++;
                _totalBonusAwarded += bonus;
                _edges              = 0;
                _onGraphConnected?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _edges = Mathf.Max(0, _edges - _removePerBot);
        }

        public void Reset()
        {
            _edges             = 0;
            _connectCount      = 0;
            _totalBonusAwarded = 0;
        }
    }
}
