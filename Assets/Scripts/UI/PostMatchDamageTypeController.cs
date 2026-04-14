using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// MonoBehaviour that displays a per-damage-type breakdown on the post-match
    /// results screen, sourced from a <see cref="MatchStatisticsSO"/> blackboard.
    ///
    /// ── Data flow ──────────────────────────────────────────────────────────────
    ///   Awake     → caches _showDelegate / _resetDelegate (Action).
    ///   OnEnable  → subscribes _onMatchEnded → ShowResults();
    ///               subscribes _onMatchStarted → ResetView();
    ///               calls ResetView() immediately (panel starts hidden).
    ///   ShowResults() → reads MatchStatisticsSO; hides panel when null;
    ///                   otherwise shows panel and writes Physical/Energy/Thermal/Shock
    ///                   dealt labels, Total dealt label, and optional type-ratio Sliders.
    ///   ResetView()   → hides panel; clears all text labels; zeros all Sliders.
    ///   OnDisable → unsubscribes both channels.
    ///
    /// ── ARCHITECTURE RULES ─────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   • DisallowMultipleComponent — one controller per post-match panel.
    ///   • All UI fields are optional — assign only those present in the scene.
    ///   • No Update / FixedUpdate loop.
    ///   • Delegates cached in Awake; zero heap allocations after initialisation.
    ///
    /// Scene wiring:
    ///   _statistics     → the shared MatchStatisticsSO asset used during the match.
    ///   _onMatchEnded   → same VoidGameEvent as MatchManager._onMatchEnded.
    ///   _onMatchStarted → same VoidGameEvent as MatchManager._matchStartedEvent (optional).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PostMatchDamageTypeController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("Runtime MatchStatisticsSO that accumulates per-type damage during the match.")]
        [SerializeField] private MatchStatisticsSO _statistics;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised when the match ends. Triggers ShowResults().")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        [Tooltip("Raised when a new match starts. Triggers ResetView() to hide stale data.")]
        [SerializeField] private VoidGameEvent _onMatchStarted;

        [Header("Panel (optional)")]
        [Tooltip("Root panel GameObject. Hidden during a match; shown on ShowResults().")]
        [SerializeField] private GameObject _statisticsPanel;

        [Header("Damage Type Labels (optional)")]
        [Tooltip("Displays 'Physical: N' where N is Mathf.RoundToInt of dealt damage.")]
        [SerializeField] private Text _physicalText;

        [Tooltip("Displays 'Energy: N'.")]
        [SerializeField] private Text _energyText;

        [Tooltip("Displays 'Thermal: N'.")]
        [SerializeField] private Text _thermalText;

        [Tooltip("Displays 'Shock: N'.")]
        [SerializeField] private Text _shockText;

        [Tooltip("Displays 'Total: N' (sum of all damage dealt).")]
        [SerializeField] private Text _totalDamageText;

        [Header("Type Ratio Bars (optional)")]
        [Tooltip("Slider value = DamageTypeRatio(Physical) in [0, 1].")]
        [SerializeField] private Slider _physicalSlider;

        [Tooltip("Slider value = DamageTypeRatio(Energy) in [0, 1].")]
        [SerializeField] private Slider _energySlider;

        [Tooltip("Slider value = DamageTypeRatio(Thermal) in [0, 1].")]
        [SerializeField] private Slider _thermalSlider;

        [Tooltip("Slider value = DamageTypeRatio(Shock) in [0, 1].")]
        [SerializeField] private Slider _shockSlider;

        // ── Private state ─────────────────────────────────────────────────────

        private Action _showDelegate;
        private Action _resetDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _showDelegate  = ShowResults;
            _resetDelegate = ResetView;
        }

        private void OnEnable()
        {
            _onMatchEnded?.RegisterCallback(_showDelegate);
            _onMatchStarted?.RegisterCallback(_resetDelegate);
            ResetView();
        }

        private void OnDisable()
        {
            _onMatchEnded?.UnregisterCallback(_showDelegate);
            _onMatchStarted?.UnregisterCallback(_resetDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Populates the per-type breakdown panel from the current
        /// <see cref="MatchStatisticsSO"/> state.
        /// Hides the panel when <see cref="_statistics"/> is null.
        /// </summary>
        public void ShowResults()
        {
            if (_statistics == null)
            {
                _statisticsPanel?.SetActive(false);
                return;
            }

            _statisticsPanel?.SetActive(true);

            if (_physicalText != null)
                _physicalText.text =
                    $"Physical: {Mathf.RoundToInt(_statistics.GetDealtByType(DamageType.Physical))}";

            if (_energyText != null)
                _energyText.text =
                    $"Energy: {Mathf.RoundToInt(_statistics.GetDealtByType(DamageType.Energy))}";

            if (_thermalText != null)
                _thermalText.text =
                    $"Thermal: {Mathf.RoundToInt(_statistics.GetDealtByType(DamageType.Thermal))}";

            if (_shockText != null)
                _shockText.text =
                    $"Shock: {Mathf.RoundToInt(_statistics.GetDealtByType(DamageType.Shock))}";

            if (_totalDamageText != null)
                _totalDamageText.text =
                    $"Total: {Mathf.RoundToInt(_statistics.TotalDamageDealt)}";

            if (_physicalSlider != null)
                _physicalSlider.value = _statistics.DamageTypeRatio(DamageType.Physical);

            if (_energySlider != null)
                _energySlider.value = _statistics.DamageTypeRatio(DamageType.Energy);

            if (_thermalSlider != null)
                _thermalSlider.value = _statistics.DamageTypeRatio(DamageType.Thermal);

            if (_shockSlider != null)
                _shockSlider.value = _statistics.DamageTypeRatio(DamageType.Shock);
        }

        /// <summary>
        /// Hides the statistics panel and clears all labels and Slider values.
        /// Called automatically on OnEnable and when _onMatchStarted fires.
        /// </summary>
        public void ResetView()
        {
            _statisticsPanel?.SetActive(false);

            if (_physicalText    != null) _physicalText.text    = string.Empty;
            if (_energyText      != null) _energyText.text      = string.Empty;
            if (_thermalText     != null) _thermalText.text     = string.Empty;
            if (_shockText       != null) _shockText.text       = string.Empty;
            if (_totalDamageText != null) _totalDamageText.text = string.Empty;

            if (_physicalSlider != null) _physicalSlider.value = 0f;
            if (_energySlider   != null) _energySlider.value   = 0f;
            if (_thermalSlider  != null) _thermalSlider.value  = 0f;
            if (_shockSlider    != null) _shockSlider.value    = 0f;
        }
    }
}
