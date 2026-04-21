using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureTrowel", order = 288)]
    public sealed class ZoneControlCaptureTrowelSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _layersNeeded    = 6;
        [SerializeField, Min(1)] private int _seepagePerBot   = 2;
        [SerializeField, Min(0)] private int _bonusPerSet     = 1060;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onTrowelSet;

        private int _layers;
        private int _setCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   LayersNeeded      => _layersNeeded;
        public int   SeepagePerBot     => _seepagePerBot;
        public int   BonusPerSet       => _bonusPerSet;
        public int   Layers            => _layers;
        public int   SetCount          => _setCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float LayerProgress     => _layersNeeded > 0
            ? Mathf.Clamp01(_layers / (float)_layersNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _layers = Mathf.Min(_layers + 1, _layersNeeded);
            if (_layers >= _layersNeeded)
            {
                int bonus = _bonusPerSet;
                _setCount++;
                _totalBonusAwarded += bonus;
                _layers             = 0;
                _onTrowelSet?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _layers = Mathf.Max(0, _layers - _seepagePerBot);
        }

        public void Reset()
        {
            _layers            = 0;
            _setCount          = 0;
            _totalBonusAwarded = 0;
        }
    }
}
