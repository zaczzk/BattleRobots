using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureGearwork", order = 296)]
    public sealed class ZoneControlCaptureGearworkSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _gearsNeeded  = 6;
        [SerializeField, Min(1)] private int _slipPerBot   = 2;
        [SerializeField, Min(0)] private int _bonusPerMesh = 1180;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onGearworkMeshed;

        private int _gears;
        private int _meshCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   GearsNeeded       => _gearsNeeded;
        public int   SlipPerBot        => _slipPerBot;
        public int   BonusPerMesh      => _bonusPerMesh;
        public int   Gears             => _gears;
        public int   MeshCount         => _meshCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float GearProgress      => _gearsNeeded > 0
            ? Mathf.Clamp01(_gears / (float)_gearsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _gears = Mathf.Min(_gears + 1, _gearsNeeded);
            if (_gears >= _gearsNeeded)
            {
                int bonus = _bonusPerMesh;
                _meshCount++;
                _totalBonusAwarded += bonus;
                _gears              = 0;
                _onGearworkMeshed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _gears = Mathf.Max(0, _gears - _slipPerBot);
        }

        public void Reset()
        {
            _gears             = 0;
            _meshCount         = 0;
            _totalBonusAwarded = 0;
        }
    }
}
