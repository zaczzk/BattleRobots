using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Post-match UI controller that records the current match's per-type damage
    /// totals into a <see cref="MatchDamageHistorySO"/> ring buffer and displays
    /// rolling averages by damage type.
    ///
    /// ── Data flow ────────────────────────────────────────────────────────────────
    ///   Awake     → caches _onMatchEndedDelegate (zero-alloc after Awake).
    ///   OnEnable  → subscribes _onMatchEnded → OnMatchEnded().
    ///   OnMatchEnded() → snapshots GetDealtByType for all four types from
    ///                    _matchStatistics; calls _history.AddEntry(snapshot);
    ///                    calls ShowAverages().
    ///   ShowAverages() → null _history → hide panel;
    ///                    otherwise show panel and write "Type avg: N" labels.
    ///   OnDisable → unsubscribes _onMatchEnded.
    ///
    /// ── Architecture rules ────────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   • DisallowMultipleComponent — one history panel per results canvas.
    ///   • All UI and data fields optional — assign only those present in the scene.
    ///   • No Update / FixedUpdate loop.
    ///   • Delegate cached in Awake; zero heap allocations after initialisation.
    ///
    /// Scene wiring:
    ///   _history        → MatchDamageHistorySO asset (the persistent ring buffer).
    ///   _matchStatistics→ shared MatchStatisticsSO (accumulates damage during a match).
    ///   _onMatchEnded   → same VoidGameEvent as MatchManager._onMatchEnded.
    ///   _historyPanel   → root panel; shown when history has data, hidden otherwise.
    ///   _physicalAvgText→ receives "Physical avg: N" (Mathf.RoundToInt of rolling avg).
    ///   _energyAvgText  → receives "Energy avg: N".
    ///   _thermalAvgText → receives "Thermal avg: N".
    ///   _shockAvgText   → receives "Shock avg: N".
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PostMatchDamageHistoryController : MonoBehaviour
    {
        // ── Inspector — Data ──────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("MatchDamageHistorySO ring-buffer asset. New match snapshots are " +
                 "appended here on each match-end event.")]
        [SerializeField] private MatchDamageHistorySO _history;

        [Tooltip("MatchStatisticsSO that accumulated per-type damage during the match. " +
                 "Read at match end to build the snapshot.")]
        [SerializeField] private MatchStatisticsSO _matchStatistics;

        // ── Inspector — Event Channel ─────────────────────────────────────────

        [Header("Event Channel — In")]
        [Tooltip("Raised when the match ends. Triggers snapshot + ShowAverages().")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        // ── Inspector — UI ────────────────────────────────────────────────────

        [Header("Panel (optional)")]
        [Tooltip("Root panel GameObject. Shown on ShowAverages() when history has data; " +
                 "hidden when _history is null.")]
        [SerializeField] private GameObject _historyPanel;

        [Header("Rolling Average Labels (optional)")]
        [Tooltip("Displays 'Physical avg: N' where N = Mathf.RoundToInt of rolling average.")]
        [SerializeField] private Text _physicalAvgText;

        [Tooltip("Displays 'Energy avg: N'.")]
        [SerializeField] private Text _energyAvgText;

        [Tooltip("Displays 'Thermal avg: N'.")]
        [SerializeField] private Text _thermalAvgText;

        [Tooltip("Displays 'Shock avg: N'.")]
        [SerializeField] private Text _shockAvgText;

        // ── Private state ─────────────────────────────────────────────────────

        private Action _onMatchEndedDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _onMatchEndedDelegate = OnMatchEnded;
        }

        private void OnEnable()
        {
            _onMatchEnded?.RegisterCallback(_onMatchEndedDelegate);
        }

        private void OnDisable()
        {
            _onMatchEnded?.UnregisterCallback(_onMatchEndedDelegate);
        }

        // ── Logic ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Reads per-type damage totals from <c>_matchStatistics</c>, adds a new
        /// <see cref="DamageTypeSnapshot"/> to <c>_history</c>, then calls
        /// <see cref="ShowAverages"/>.
        ///
        /// <para>Safe to call with null <c>_history</c> or <c>_matchStatistics</c> —
        /// returns immediately without throwing.</para>
        /// </summary>
        private void OnMatchEnded()
        {
            if (_history == null || _matchStatistics == null) return;

            var snapshot = new DamageTypeSnapshot
            {
                physical = _matchStatistics.GetDealtByType(DamageType.Physical),
                energy   = _matchStatistics.GetDealtByType(DamageType.Energy),
                thermal  = _matchStatistics.GetDealtByType(DamageType.Thermal),
                shock    = _matchStatistics.GetDealtByType(DamageType.Shock),
            };

            _history.AddEntry(snapshot);
            ShowAverages();
        }

        /// <summary>
        /// Refreshes the rolling-average labels from the current <c>_history</c> state.
        ///
        /// <para>Hides <c>_historyPanel</c> when <c>_history</c> is null.
        /// Shows the panel and writes all four labels otherwise.</para>
        /// </summary>
        public void ShowAverages()
        {
            if (_history == null)
            {
                _historyPanel?.SetActive(false);
                return;
            }

            _historyPanel?.SetActive(true);

            if (_physicalAvgText != null)
                _physicalAvgText.text =
                    $"Physical avg: {Mathf.RoundToInt(_history.GetRollingAverage(DamageType.Physical))}";

            if (_energyAvgText != null)
                _energyAvgText.text =
                    $"Energy avg: {Mathf.RoundToInt(_history.GetRollingAverage(DamageType.Energy))}";

            if (_thermalAvgText != null)
                _thermalAvgText.text =
                    $"Thermal avg: {Mathf.RoundToInt(_history.GetRollingAverage(DamageType.Thermal))}";

            if (_shockAvgText != null)
                _shockAvgText.text =
                    $"Shock avg: {Mathf.RoundToInt(_history.GetRollingAverage(DamageType.Shock))}";
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>The currently assigned <see cref="MatchDamageHistorySO"/>. May be null.</summary>
        public MatchDamageHistorySO History => _history;

        /// <summary>The currently assigned <see cref="MatchStatisticsSO"/>. May be null.</summary>
        public MatchStatisticsSO MatchStatistics => _matchStatistics;
    }
}
