using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureShimuraVariety", order = 512)]
    public sealed class ZoneControlCaptureShimuraVarietySO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _cmPointsNeeded        = 5;
        [SerializeField, Min(1)] private int _badPrimesPerBot        = 1;
        [SerializeField, Min(0)] private int _bonusPerUniformization = 4420;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onShimuraVarietyUniformized;

        private int _cmPoints;
        private int _uniformizationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   CMPointsNeeded        => _cmPointsNeeded;
        public int   BadPrimesPerBot        => _badPrimesPerBot;
        public int   BonusPerUniformization => _bonusPerUniformization;
        public int   CMPoints               => _cmPoints;
        public int   UniformizationCount    => _uniformizationCount;
        public int   TotalBonusAwarded      => _totalBonusAwarded;
        public float CMPointProgress => _cmPointsNeeded > 0
            ? Mathf.Clamp01(_cmPoints / (float)_cmPointsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _cmPoints = Mathf.Min(_cmPoints + 1, _cmPointsNeeded);
            if (_cmPoints >= _cmPointsNeeded)
            {
                int bonus = _bonusPerUniformization;
                _uniformizationCount++;
                _totalBonusAwarded += bonus;
                _cmPoints           = 0;
                _onShimuraVarietyUniformized?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _cmPoints = Mathf.Max(0, _cmPoints - _badPrimesPerBot);
        }

        public void Reset()
        {
            _cmPoints            = 0;
            _uniformizationCount = 0;
            _totalBonusAwarded   = 0;
        }
    }
}
