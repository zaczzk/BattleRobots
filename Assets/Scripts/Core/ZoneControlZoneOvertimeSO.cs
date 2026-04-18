using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Rewards the player for capturing a zone that a bot was close to completing (overtime save).
    /// <c>RecordBotProgress(float)</c> arms the overtime window when bot progress ≥
    /// <c>_overtimeThreshold</c>. <c>RecordPlayerCapture()</c> credits the overtime if armed,
    /// fires <c>_onOvertimeCapture</c>, and clears the arm flag.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlZoneOvertime.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlZoneOvertime", order = 120)]
    public sealed class ZoneControlZoneOvertimeSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Range(0f, 1f)] private float _overtimeThreshold = 0.75f;
        [SerializeField, Min(0)]        private int   _bonusPerOvertime  = 150;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onOvertimeCapture;

        private bool _isArmed;
        private int  _overtimeCount;
        private int  _totalBonusAwarded;

        private void OnEnable() => Reset();

        public float OvertimeThreshold  => _overtimeThreshold;
        public int   BonusPerOvertime   => _bonusPerOvertime;
        public int   OvertimeCount      => _overtimeCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public bool  IsArmed            => _isArmed;

        /// <summary>
        /// Notifies bot capture progress [0,1]. Arms the overtime flag when progress ≥ threshold.
        /// </summary>
        public void RecordBotProgress(float progress)
        {
            if (progress >= _overtimeThreshold)
                _isArmed = true;
        }

        /// <summary>
        /// Records a player capture. Credits overtime bonus if armed, then disarms.
        /// </summary>
        public void RecordPlayerCapture()
        {
            if (_isArmed)
            {
                _overtimeCount++;
                _totalBonusAwarded += _bonusPerOvertime;
                _onOvertimeCapture?.Raise();
            }
            _isArmed = false;
        }

        /// <summary>Clears all runtime state silently.</summary>
        public void Reset()
        {
            _isArmed           = false;
            _overtimeCount     = 0;
            _totalBonusAwarded = 0;
        }
    }
}
