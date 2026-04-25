using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureMordellWeil", order = 515)]
    public sealed class ZoneControlCaptureMordellWeilSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _generatorsNeeded   = 6;
        [SerializeField, Min(1)] private int _torsionPerBot      = 1;
        [SerializeField, Min(0)] private int _bonusPerGeneration = 4465;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onMordellWeilGenerated;

        private int _generators;
        private int _generationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   GeneratorsNeeded   => _generatorsNeeded;
        public int   TorsionPerBot      => _torsionPerBot;
        public int   BonusPerGeneration => _bonusPerGeneration;
        public int   Generators         => _generators;
        public int   GenerationCount    => _generationCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float GeneratorProgress  => _generatorsNeeded > 0
            ? Mathf.Clamp01(_generators / (float)_generatorsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _generators = Mathf.Min(_generators + 1, _generatorsNeeded);
            if (_generators >= _generatorsNeeded)
            {
                int bonus = _bonusPerGeneration;
                _generationCount++;
                _totalBonusAwarded += bonus;
                _generators         = 0;
                _onMordellWeilGenerated?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _generators = Mathf.Max(0, _generators - _torsionPerBot);
        }

        public void Reset()
        {
            _generators        = 0;
            _generationCount   = 0;
            _totalBonusAwarded = 0;
        }
    }
}
