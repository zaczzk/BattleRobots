using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureArrow", order = 371)]
    public sealed class ZoneControlCaptureArrowSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _arrowsNeeded    = 5;
        [SerializeField, Min(1)] private int _deflectPerBot   = 1;
        [SerializeField, Min(0)] private int _bonusPerCompose = 2305;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onArrowComposed;

        private int _arrows;
        private int _composeCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ArrowsNeeded       => _arrowsNeeded;
        public int   DeflectPerBot      => _deflectPerBot;
        public int   BonusPerCompose    => _bonusPerCompose;
        public int   Arrows             => _arrows;
        public int   ComposeCount       => _composeCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float ArrowProgress      => _arrowsNeeded > 0
            ? Mathf.Clamp01(_arrows / (float)_arrowsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _arrows = Mathf.Min(_arrows + 1, _arrowsNeeded);
            if (_arrows >= _arrowsNeeded)
            {
                int bonus = _bonusPerCompose;
                _composeCount++;
                _totalBonusAwarded += bonus;
                _arrows             = 0;
                _onArrowComposed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _arrows = Mathf.Max(0, _arrows - _deflectPerBot);
        }

        public void Reset()
        {
            _arrows            = 0;
            _composeCount      = 0;
            _totalBonusAwarded = 0;
        }
    }
}
