using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCapturePhoenix", order = 257)]
    public sealed class ZoneControlCapturePhoenixSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _ashesNeeded        = 5;
        [SerializeField, Min(1)] private int _scatterPerBot      = 1;
        [SerializeField, Min(0)] private int _bonusPerRebirth    = 595;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onPhoenixReborn;

        private int _ashes;
        private int _rebirthCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   AshesNeeded        => _ashesNeeded;
        public int   ScatterPerBot      => _scatterPerBot;
        public int   BonusPerRebirth    => _bonusPerRebirth;
        public int   Ashes              => _ashes;
        public int   RebirthCount       => _rebirthCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float AshProgress        => _ashesNeeded > 0
            ? Mathf.Clamp01(_ashes / (float)_ashesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _ashes = Mathf.Min(_ashes + 1, _ashesNeeded);
            if (_ashes >= _ashesNeeded)
            {
                int bonus = _bonusPerRebirth;
                _rebirthCount++;
                _totalBonusAwarded += bonus;
                _ashes              = 0;
                _onPhoenixReborn?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _ashes = Mathf.Max(0, _ashes - _scatterPerBot);
        }

        public void Reset()
        {
            _ashes             = 0;
            _rebirthCount      = 0;
            _totalBonusAwarded = 0;
        }
    }
}
