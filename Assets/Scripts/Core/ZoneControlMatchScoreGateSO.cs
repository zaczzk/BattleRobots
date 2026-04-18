using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Enforces a per-match minimum zone-capture count gate.
    /// <c>RecordCapture()</c> increments the capture counter and fires
    /// <c>_onGatePassed</c> exactly once when <c>_gateTarget</c> is reached.
    /// Idempotent after the gate is passed.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlMatchScoreGate.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlMatchScoreGate", order = 125)]
    public sealed class ZoneControlMatchScoreGateSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _gateTarget     = 5;
        [SerializeField, Min(0)] private int _bonusOnPass    = 300;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onGatePassed;

        private int  _captureCount;
        private bool _gatePassed;

        private void OnEnable() => Reset();

        public int  GateTarget    => _gateTarget;
        public int  BonusOnPass   => _bonusOnPass;
        public int  CaptureCount  => _captureCount;
        public bool GatePassed    => _gatePassed;

        /// <summary>Progress toward gate [0,1].</summary>
        public float GateProgress => Mathf.Clamp01((float)_captureCount / Mathf.Max(1, _gateTarget));

        /// <summary>Increments capture count and fires gate event on first passage.</summary>
        public void RecordCapture()
        {
            if (_gatePassed) return;
            _captureCount++;
            if (_captureCount >= _gateTarget)
            {
                _gatePassed = true;
                _onGatePassed?.Raise();
            }
        }

        /// <summary>Clears all runtime state silently.</summary>
        public void Reset()
        {
            _captureCount = 0;
            _gatePassed   = false;
        }
    }
}
