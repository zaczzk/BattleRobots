using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureStack", order = 347)]
    public sealed class ZoneControlCaptureStackSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _framesNeeded   = 5;
        [SerializeField, Min(1)] private int _popPerBot      = 1;
        [SerializeField, Min(0)] private int _bonusPerReturn = 1945;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onStackReturned;

        private int _frames;
        private int _returnCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   FramesNeeded      => _framesNeeded;
        public int   PopPerBot         => _popPerBot;
        public int   BonusPerReturn    => _bonusPerReturn;
        public int   Frames            => _frames;
        public int   ReturnCount       => _returnCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float FrameProgress     => _framesNeeded > 0
            ? Mathf.Clamp01(_frames / (float)_framesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _frames = Mathf.Min(_frames + 1, _framesNeeded);
            if (_frames >= _framesNeeded)
            {
                int bonus = _bonusPerReturn;
                _returnCount++;
                _totalBonusAwarded += bonus;
                _frames             = 0;
                _onStackReturned?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _frames = Mathf.Max(0, _frames - _popPerBot);
        }

        public void Reset()
        {
            _frames            = 0;
            _returnCount       = 0;
            _totalBonusAwarded = 0;
        }
    }
}
