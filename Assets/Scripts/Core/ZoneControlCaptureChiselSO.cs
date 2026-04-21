using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureChisel", order = 285)]
    public sealed class ZoneControlCaptureChiselSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _carvingsNeeded  = 5;
        [SerializeField, Min(1)] private int _erosionPerBot   = 1;
        [SerializeField, Min(0)] private int _bonusPerSculpt  = 1015;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onChiselSculpted;

        private int _carvings;
        private int _sculptCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   CarvingsNeeded    => _carvingsNeeded;
        public int   ErosionPerBot     => _erosionPerBot;
        public int   BonusPerSculpt    => _bonusPerSculpt;
        public int   Carvings          => _carvings;
        public int   SculptCount       => _sculptCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float CarvingProgress   => _carvingsNeeded > 0
            ? Mathf.Clamp01(_carvings / (float)_carvingsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _carvings = Mathf.Min(_carvings + 1, _carvingsNeeded);
            if (_carvings >= _carvingsNeeded)
            {
                int bonus = _bonusPerSculpt;
                _sculptCount++;
                _totalBonusAwarded += bonus;
                _carvings           = 0;
                _onChiselSculpted?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _carvings = Mathf.Max(0, _carvings - _erosionPerBot);
        }

        public void Reset()
        {
            _carvings          = 0;
            _sculptCount       = 0;
            _totalBonusAwarded = 0;
        }
    }
}
