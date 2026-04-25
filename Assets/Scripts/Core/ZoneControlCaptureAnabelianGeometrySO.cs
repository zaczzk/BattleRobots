using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureAnabelianGeometry", order = 502)]
    public sealed class ZoneControlCaptureAnabelianGeometrySO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _fundamentalGroupDataNeeded = 5;
        [SerializeField, Min(1)] private int _outerAutomorphismsPerBot   = 1;
        [SerializeField, Min(0)] private int _bonusPerReconstruction     = 4270;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onAnabelianGeometryReconstructed;

        private int _fundamentalGroupData;
        private int _reconstructionCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   FundamentalGroupDataNeeded => _fundamentalGroupDataNeeded;
        public int   OuterAutomorphismsPerBot   => _outerAutomorphismsPerBot;
        public int   BonusPerReconstruction     => _bonusPerReconstruction;
        public int   FundamentalGroupData       => _fundamentalGroupData;
        public int   ReconstructionCount        => _reconstructionCount;
        public int   TotalBonusAwarded          => _totalBonusAwarded;
        public float FundamentalGroupProgress => _fundamentalGroupDataNeeded > 0
            ? Mathf.Clamp01(_fundamentalGroupData / (float)_fundamentalGroupDataNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _fundamentalGroupData = Mathf.Min(_fundamentalGroupData + 1, _fundamentalGroupDataNeeded);
            if (_fundamentalGroupData >= _fundamentalGroupDataNeeded)
            {
                int bonus = _bonusPerReconstruction;
                _reconstructionCount++;
                _totalBonusAwarded    += bonus;
                _fundamentalGroupData  = 0;
                _onAnabelianGeometryReconstructed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _fundamentalGroupData = Mathf.Max(0, _fundamentalGroupData - _outerAutomorphismsPerBot);
        }

        public void Reset()
        {
            _fundamentalGroupData = 0;
            _reconstructionCount  = 0;
            _totalBonusAwarded    = 0;
        }
    }
}
