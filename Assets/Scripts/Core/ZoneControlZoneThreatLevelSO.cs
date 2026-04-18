using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>Threat level classification for zone-control pressure.</summary>
    public enum ZoneControlThreatLevel { Low = 0, Medium = 1, High = 2 }

    /// <summary>
    /// Runtime SO that models zone threat as a 0–100 float value. Bot captures raise
    /// threat; player captures lower it; <c>Tick(dt)</c> decays it over time.
    /// Fires <c>_onThreatChanged</c> on Low/Medium/High transitions (idempotent).
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlZoneThreatLevel.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlZoneThreatLevel", order = 115)]
    public sealed class ZoneControlZoneThreatLevelSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(0f)]           private float _threatIncreasePerBotCapture = 20f;
        [SerializeField, Min(0f)]           private float _threatDecayRate             = 5f;
        [SerializeField, Range(0f, 100f)]   private float _mediumThreshold             = 40f;
        [SerializeField, Range(0f, 100f)]   private float _highThreshold               = 70f;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onThreatChanged;

        private float                  _threatValue;
        private ZoneControlThreatLevel _currentThreat;

        private void OnEnable()   => Reset();
        private void OnValidate() => _highThreshold = Mathf.Max(_highThreshold, _mediumThreshold);

        public float                  ThreatValue     => _threatValue;
        public float                  ThreatProgress  => Mathf.Clamp01(_threatValue / 100f);
        public ZoneControlThreatLevel CurrentThreat   => _currentThreat;
        public float                  MediumThreshold => _mediumThreshold;
        public float                  HighThreshold   => _highThreshold;

        /// <summary>Increases threat by <c>_threatIncreasePerBotCapture</c>.</summary>
        public void RecordBotCapture()
        {
            _threatValue = Mathf.Min(100f, _threatValue + _threatIncreasePerBotCapture);
            EvaluateThreat();
        }

        /// <summary>Reduces threat by half the bot-capture increment.</summary>
        public void RecordPlayerCapture()
        {
            _threatValue = Mathf.Max(0f, _threatValue - _threatIncreasePerBotCapture * 0.5f);
            EvaluateThreat();
        }

        /// <summary>Decays threat by <c>_threatDecayRate * dt</c> per call.</summary>
        public void Tick(float dt)
        {
            if (_threatValue <= 0f) return;
            _threatValue = Mathf.Max(0f, _threatValue - _threatDecayRate * dt);
            EvaluateThreat();
        }

        /// <summary>Returns a display string for the current threat tier.</summary>
        public string GetThreatLabel()
        {
            return _currentThreat switch
            {
                ZoneControlThreatLevel.High   => "HIGH",
                ZoneControlThreatLevel.Medium => "MEDIUM",
                _                             => "LOW",
            };
        }

        /// <summary>Clears all runtime state silently.</summary>
        public void Reset()
        {
            _threatValue   = 0f;
            _currentThreat = ZoneControlThreatLevel.Low;
        }

        private void EvaluateThreat()
        {
            ZoneControlThreatLevel next;
            if (_threatValue >= _highThreshold)
                next = ZoneControlThreatLevel.High;
            else if (_threatValue >= _mediumThreshold)
                next = ZoneControlThreatLevel.Medium;
            else
                next = ZoneControlThreatLevel.Low;

            if (next == _currentThreat) return;
            _currentThreat = next;
            _onThreatChanged?.Raise();
        }
    }
}
