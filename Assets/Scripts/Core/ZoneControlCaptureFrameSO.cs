using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureFrame", order = 345)]
    public sealed class ZoneControlCaptureFrameSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _framesNeeded     = 7;
        [SerializeField, Min(1)] private int _corruptPerBot    = 2;
        [SerializeField, Min(0)] private int _bonusPerTransmit = 1915;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onFrameTransmitted;

        private int _frames;
        private int _transmitCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   FramesNeeded      => _framesNeeded;
        public int   CorruptPerBot     => _corruptPerBot;
        public int   BonusPerTransmit  => _bonusPerTransmit;
        public int   Frames            => _frames;
        public int   TransmitCount     => _transmitCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float FrameProgress     => _framesNeeded > 0
            ? Mathf.Clamp01(_frames / (float)_framesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _frames = Mathf.Min(_frames + 1, _framesNeeded);
            if (_frames >= _framesNeeded)
            {
                int bonus = _bonusPerTransmit;
                _transmitCount++;
                _totalBonusAwarded += bonus;
                _frames             = 0;
                _onFrameTransmitted?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _frames = Mathf.Max(0, _frames - _corruptPerBot);
        }

        public void Reset()
        {
            _frames            = 0;
            _transmitCount     = 0;
            _totalBonusAwarded = 0;
        }
    }
}
