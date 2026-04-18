using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCapturePenalty", order = 101)]
    public sealed class ZoneControlCapturePenaltySO : ScriptableObject
    {
        [Header("Penalty Settings")]
        [Min(0)]
        [SerializeField] private int _penaltyPerBotCapture = 25;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onPenaltyApplied;

        private int _botCaptureCount;
        private int _totalPenaltyApplied;

        private void OnEnable() => Reset();

        public int BotCaptureCount      => _botCaptureCount;
        public int TotalPenaltyApplied  => _totalPenaltyApplied;
        public int PenaltyPerBotCapture => _penaltyPerBotCapture;

        public void RecordBotCapture()
        {
            _botCaptureCount++;
            _totalPenaltyApplied += _penaltyPerBotCapture;
            _onPenaltyApplied?.Raise();
        }

        public void Reset()
        {
            _botCaptureCount     = 0;
            _totalPenaltyApplied = 0;
        }
    }
}
