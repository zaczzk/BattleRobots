using UnityEngine;

namespace BattleRobots.Core
{
    public enum MatchTempo { Low, Normal, High }

    /// <summary>
    /// Runtime SO that classifies the current match capture rate into a
    /// <see cref="MatchTempo"/> (Low / Normal / High) and fires
    /// <c>_onTempoChanged</c> on transitions.
    /// <see cref="Reset"/> restores <see cref="MatchTempo.Low"/> silently; called from <c>OnEnable</c>.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlMatchTempo.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlMatchTempo", order = 107)]
    public sealed class ZoneControlMatchTempoSO : ScriptableObject
    {
        [Header("Thresholds (captures/min)")]
        [Min(0.1f)]
        [SerializeField] private float _slowThreshold = 1f;
        [Min(0.1f)]
        [SerializeField] private float _fastThreshold = 3f;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onTempoChanged;

        private MatchTempo _currentTempo = MatchTempo.Low;

        private void OnEnable() => Reset();

        private void OnValidate()
        {
            if (_fastThreshold < _slowThreshold)
                _fastThreshold = _slowThreshold;
        }

        public MatchTempo CurrentTempo    => _currentTempo;
        public float      SlowThreshold   => _slowThreshold;
        public float      FastThreshold   => _fastThreshold;

        /// <summary>Classifies <paramref name="ratePerMinute"/> and fires <c>_onTempoChanged</c> on change.</summary>
        public void EvaluateTempo(float ratePerMinute)
        {
            MatchTempo next = ratePerMinute >= _fastThreshold ? MatchTempo.High
                            : ratePerMinute >= _slowThreshold ? MatchTempo.Normal
                            :                                   MatchTempo.Low;

            if (next == _currentTempo) return;
            _currentTempo = next;
            _onTempoChanged?.Raise();
        }

        /// <summary>Returns a display-ready label for the current tempo.</summary>
        public string GetTempoLabel() => _currentTempo switch
        {
            MatchTempo.Low    => "Low",
            MatchTempo.Normal => "Normal",
            MatchTempo.High   => "High",
            _                 => "Normal"
        };

        /// <summary>Restores <see cref="MatchTempo.Low"/> silently.</summary>
        public void Reset() => _currentTempo = MatchTempo.Low;
    }
}
