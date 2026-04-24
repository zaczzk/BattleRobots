using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureManifold", order = 452)]
    public sealed class ZoneControlCaptureManifoldSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _chartsNeeded = 6;
        [SerializeField, Min(1)] private int _wrinklePerBot = 1;
        [SerializeField, Min(0)] private int _bonusPerAtlas = 3520;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onAtlasFormed;

        private int _charts;
        private int _atlasCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ChartsNeeded      => _chartsNeeded;
        public int   WrinklePerBot     => _wrinklePerBot;
        public int   BonusPerAtlas     => _bonusPerAtlas;
        public int   Charts            => _charts;
        public int   AtlasCount        => _atlasCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float ManifoldProgress  => _chartsNeeded > 0
            ? Mathf.Clamp01(_charts / (float)_chartsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _charts = Mathf.Min(_charts + 1, _chartsNeeded);
            if (_charts >= _chartsNeeded)
            {
                int bonus = _bonusPerAtlas;
                _atlasCount++;
                _totalBonusAwarded += bonus;
                _charts             = 0;
                _onAtlasFormed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _charts = Mathf.Max(0, _charts - _wrinklePerBot);
        }

        public void Reset()
        {
            _charts            = 0;
            _atlasCount        = 0;
            _totalBonusAwarded = 0;
        }
    }
}
