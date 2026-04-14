using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Expanded post-match history card that shows per-damage-type dealt amounts from
    /// <see cref="MatchStatisticsSO"/> alongside the equipped loadout part count and
    /// match outcome from the latest <see cref="LoadoutHistorySO"/> entry.
    ///
    /// ── Responsibilities ─────────────────────────────────────────────────────────
    ///   1. Subscribe <c>_onMatchEnded</c> → <see cref="Refresh"/>.
    ///   2. On Refresh:
    ///      • Read per-type damage dealt from <see cref="MatchStatisticsSO"/>.
    ///      • Read part count and win/loss outcome from the latest
    ///        <see cref="LoadoutHistorySO"/> entry (most recent snapshot).
    ///      • Show <c>_detailPanel</c>; hide it only when both data refs are null.
    ///   3. On OnEnable: hide the panel so it starts closed.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   - DisallowMultipleComponent — one detail card per canvas.
    ///   - All inspector fields optional — safe with no refs assigned.
    ///   - Delegate cached in Awake; zero heap allocations after initialisation.
    ///
    /// Scene wiring:
    ///   _matchStatistics     → shared MatchStatisticsSO (per-type dealt amounts).
    ///   _loadoutHistory      → shared LoadoutHistorySO ring-buffer.
    ///   _onMatchEnded        → same VoidGameEvent as MatchManager raises.
    ///   _detailPanel         → root panel (starts inactive; shown after each match).
    ///   _outcomeLabel        → Text: "WIN" or "LOSS".
    ///   _partCountLabel      → Text: "N parts equipped".
    ///   _physicalDamageLabel → Text: "Physical: N".
    ///   _energyDamageLabel   → Text: "Energy: N".
    ///   _thermalDamageLabel  → Text: "Thermal: N".
    ///   _shockDamageLabel    → Text: "Shock: N".
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MatchHistoryDetailController : MonoBehaviour
    {
        // ── Inspector — Data ─────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("MatchStatisticsSO providing per-type damage-dealt amounts.")]
        [SerializeField] private MatchStatisticsSO _matchStatistics;

        [Tooltip("LoadoutHistorySO ring-buffer. The most-recent entry provides part count and outcome.")]
        [SerializeField] private LoadoutHistorySO _loadoutHistory;

        // ── Inspector — Event ────────────────────────────────────────────────

        [Header("Event Channel — In (optional)")]
        [Tooltip("Raised by MatchManager at the end of each match.")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        // ── Inspector — UI ───────────────────────────────────────────────────

        [Header("UI (all optional)")]
        [Tooltip("Root detail card panel. Starts inactive; shown after each match ends.")]
        [SerializeField] private GameObject _detailPanel;

        [Tooltip("Text label for the match outcome: 'WIN' or 'LOSS'.")]
        [SerializeField] private Text _outcomeLabel;

        [Tooltip("Text label for the equipped part count: 'N parts equipped'.")]
        [SerializeField] private Text _partCountLabel;

        [Tooltip("Text label for Physical damage dealt: 'Physical: N'.")]
        [SerializeField] private Text _physicalDamageLabel;

        [Tooltip("Text label for Energy damage dealt: 'Energy: N'.")]
        [SerializeField] private Text _energyDamageLabel;

        [Tooltip("Text label for Thermal damage dealt: 'Thermal: N'.")]
        [SerializeField] private Text _thermalDamageLabel;

        [Tooltip("Text label for Shock damage dealt: 'Shock: N'.")]
        [SerializeField] private Text _shockDamageLabel;

        // ── Cached delegate ──────────────────────────────────────────────────

        private Action _onMatchEndedDelegate;

        // ── Lifecycle ────────────────────────────────────────────────────────

        private void Awake()
        {
            _onMatchEndedDelegate = OnMatchEnded;
        }

        private void OnEnable()
        {
            _onMatchEnded?.RegisterCallback(_onMatchEndedDelegate);
            _detailPanel?.SetActive(false);
        }

        private void OnDisable()
        {
            _onMatchEnded?.UnregisterCallback(_onMatchEndedDelegate);
        }

        // ── Logic ─────────────────────────────────────────────────────────────

        private void OnMatchEnded()
        {
            Refresh();
        }

        /// <summary>
        /// Populates the detail card with the latest per-type damage and loadout snapshot.
        /// Hides the panel only when both <see cref="_matchStatistics"/> and
        /// <see cref="_loadoutHistory"/> are null.  Safe with any combination of null refs.
        /// </summary>
        public void Refresh()
        {
            if (_matchStatistics == null && _loadoutHistory == null)
            {
                _detailPanel?.SetActive(false);
                return;
            }

            _detailPanel?.SetActive(true);

            // Per-type damage from MatchStatisticsSO.
            if (_matchStatistics != null)
            {
                if (_physicalDamageLabel != null)
                    _physicalDamageLabel.text = string.Format(
                        "Physical: {0}",
                        Mathf.RoundToInt(_matchStatistics.GetDealtByType(DamageType.Physical)));

                if (_energyDamageLabel != null)
                    _energyDamageLabel.text = string.Format(
                        "Energy: {0}",
                        Mathf.RoundToInt(_matchStatistics.GetDealtByType(DamageType.Energy)));

                if (_thermalDamageLabel != null)
                    _thermalDamageLabel.text = string.Format(
                        "Thermal: {0}",
                        Mathf.RoundToInt(_matchStatistics.GetDealtByType(DamageType.Thermal)));

                if (_shockDamageLabel != null)
                    _shockDamageLabel.text = string.Format(
                        "Shock: {0}",
                        Mathf.RoundToInt(_matchStatistics.GetDealtByType(DamageType.Shock)));
            }

            // Latest loadout snapshot from LoadoutHistorySO.
            if (_loadoutHistory != null)
            {
                var entry = _loadoutHistory.GetLatest();
                if (entry.HasValue)
                {
                    if (_partCountLabel != null)
                        _partCountLabel.text = string.Format(
                            "{0} parts equipped",
                            entry.Value.partIds?.Length ?? 0);

                    if (_outcomeLabel != null)
                        _outcomeLabel.text = entry.Value.playerWon ? "WIN" : "LOSS";
                }
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="MatchStatisticsSO"/>. May be null.</summary>
        public MatchStatisticsSO MatchStatistics => _matchStatistics;

        /// <summary>The assigned <see cref="LoadoutHistorySO"/>. May be null.</summary>
        public LoadoutHistorySO LoadoutHistory => _loadoutHistory;
    }
}
