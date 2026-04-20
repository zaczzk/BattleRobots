using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureBridge", order = 241)]
    public sealed class ZoneControlCaptureBridgeSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _planksNeeded     = 6;
        [SerializeField, Min(1)] private int _removalPerBot    = 2;
        [SerializeField, Min(0)] private int _bonusPerBridge   = 460;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onBridgeComplete;

        private int _planks;
        private int _bridgeCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   PlanksNeeded      => _planksNeeded;
        public int   RemovalPerBot     => _removalPerBot;
        public int   BonusPerBridge    => _bonusPerBridge;
        public int   Planks            => _planks;
        public int   BridgeCount       => _bridgeCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float PlankProgress     => _planksNeeded > 0
            ? Mathf.Clamp01(_planks / (float)_planksNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _planks = Mathf.Min(_planks + 1, _planksNeeded);
            if (_planks >= _planksNeeded)
            {
                int bonus = _bonusPerBridge;
                _bridgeCount++;
                _totalBonusAwarded += bonus;
                _planks             = 0;
                _onBridgeComplete?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _planks = Mathf.Max(0, _planks - _removalPerBot);
        }

        public void Reset()
        {
            _planks            = 0;
            _bridgeCount       = 0;
            _totalBonusAwarded = 0;
        }
    }
}
