using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureMayerVietoris", order = 481)]
    public sealed class ZoneControlCaptureMayerVietorisSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _patchesNeeded  = 5;
        [SerializeField, Min(1)] private int _collapsePerBot = 1;
        [SerializeField, Min(0)] private int _bonusPerStitch = 3955;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onMayerVietorisStitched;

        private int _patches;
        private int _stitchCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   PatchesNeeded     => _patchesNeeded;
        public int   CollapsePerBot    => _collapsePerBot;
        public int   BonusPerStitch    => _bonusPerStitch;
        public int   Patches           => _patches;
        public int   StitchCount       => _stitchCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float PatchProgress     => _patchesNeeded > 0
            ? Mathf.Clamp01(_patches / (float)_patchesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _patches = Mathf.Min(_patches + 1, _patchesNeeded);
            if (_patches >= _patchesNeeded)
            {
                int bonus = _bonusPerStitch;
                _stitchCount++;
                _totalBonusAwarded += bonus;
                _patches            = 0;
                _onMayerVietorisStitched?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _patches = Mathf.Max(0, _patches - _collapsePerBot);
        }

        public void Reset()
        {
            _patches           = 0;
            _stitchCount       = 0;
            _totalBonusAwarded = 0;
        }
    }
}
