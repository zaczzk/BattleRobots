using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureStableInftyCat", order = 466)]
    public sealed class ZoneControlCaptureStableInftyCatSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _exactTrianglesNeeded = 7;
        [SerializeField, Min(1)] private int _breakPerBot          = 2;
        [SerializeField, Min(0)] private int _bonusPerStabilize    = 3730;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onStableInftyCatStabilized;

        private int _exactTriangles;
        private int _stabilizeCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ExactTrianglesNeeded => _exactTrianglesNeeded;
        public int   BreakPerBot          => _breakPerBot;
        public int   BonusPerStabilize    => _bonusPerStabilize;
        public int   ExactTriangles       => _exactTriangles;
        public int   StabilizeCount       => _stabilizeCount;
        public int   TotalBonusAwarded    => _totalBonusAwarded;
        public float ExactTriangleProgress => _exactTrianglesNeeded > 0
            ? Mathf.Clamp01(_exactTriangles / (float)_exactTrianglesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _exactTriangles = Mathf.Min(_exactTriangles + 1, _exactTrianglesNeeded);
            if (_exactTriangles >= _exactTrianglesNeeded)
            {
                int bonus = _bonusPerStabilize;
                _stabilizeCount++;
                _totalBonusAwarded += bonus;
                _exactTriangles     = 0;
                _onStableInftyCatStabilized?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _exactTriangles = Mathf.Max(0, _exactTriangles - _breakPerBot);
        }

        public void Reset()
        {
            _exactTriangles    = 0;
            _stabilizeCount    = 0;
            _totalBonusAwarded = 0;
        }
    }
}
