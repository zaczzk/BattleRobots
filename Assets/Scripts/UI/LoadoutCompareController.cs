using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Side-by-side comparison of the current equipped loadout against the most-recent
    /// loadout history snapshot from <see cref="LoadoutHistorySO"/>.
    ///
    /// ── Display ──────────────────────────────────────────────────────────────────
    ///   _currentPartsLabel → "Current: N parts"
    ///   _historyPartsLabel → "Previous: N parts"
    ///   _partDeltaLabel    → "+N" / "-N" / "Same"  (delta = current − previous)
    ///   _winRateLabel      → "Win Rate: N%"  (wins / total history entries × 100)
    ///
    ///   Panel is hidden when either <see cref="_currentLoadout"/> is null, or
    ///   <see cref="_loadoutHistory"/> is null / empty.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────────
    ///   Awake     → caches _refreshDelegate.
    ///   OnEnable  → subscribes _onLoadoutChanged → Refresh(); calls Refresh().
    ///   OnDisable → unsubscribes.
    ///   Refresh() → reads PlayerLoadout.EquippedPartIds count and LoadoutHistorySO
    ///               ring buffer; updates all labels. Null-safe.
    ///
    /// ── Architecture rules ────────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   • DisallowMultipleComponent — one compare panel per canvas.
    ///   • All UI fields are optional — assign only those present in the scene.
    ///   • No Update / FixedUpdate loop.
    ///   • Delegate cached in Awake; zero heap allocations after initialisation.
    ///
    /// Scene wiring:
    ///   _currentLoadout    → shared PlayerLoadout SO.
    ///   _loadoutHistory    → shared LoadoutHistorySO ring-buffer.
    ///   _onLoadoutChanged  → VoidGameEvent raised when the loadout changes.
    ///   _comparePanel      → root compare panel.
    ///   _currentPartsLabel → Text for "Current: N parts".
    ///   _historyPartsLabel → Text for "Previous: N parts".
    ///   _partDeltaLabel    → Text for the part-count delta.
    ///   _winRateLabel      → Text for the history win-rate percentage.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LoadoutCompareController : MonoBehaviour
    {
        // ── Inspector — Data ─────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("Runtime SO for the currently equipped loadout.")]
        [SerializeField] private PlayerLoadout _currentLoadout;

        [Tooltip("Ring-buffer SO containing recent loadout snapshots.")]
        [SerializeField] private LoadoutHistorySO _loadoutHistory;

        // ── Inspector — Event ────────────────────────────────────────────────

        [Header("Event Channel — In (optional)")]
        [Tooltip("Raised when the player changes their loadout. Triggers a Refresh.")]
        [SerializeField] private VoidGameEvent _onLoadoutChanged;

        // ── Inspector — UI ───────────────────────────────────────────────────

        [Header("UI (all optional)")]
        [Tooltip("Root compare panel. Hidden when data is unavailable.")]
        [SerializeField] private GameObject _comparePanel;

        [Tooltip("Text label: 'Current: N parts'.")]
        [SerializeField] private Text _currentPartsLabel;

        [Tooltip("Text label: 'Previous: N parts'.")]
        [SerializeField] private Text _historyPartsLabel;

        [Tooltip("Text label showing the part-count delta: '+N', '-N', or 'Same'.")]
        [SerializeField] private Text _partDeltaLabel;

        [Tooltip("Text label: 'Win Rate: N%' computed from all history entries.")]
        [SerializeField] private Text _winRateLabel;

        // ── Cached delegate ──────────────────────────────────────────────────

        private Action _refreshDelegate;

        // ── Lifecycle ────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onLoadoutChanged?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onLoadoutChanged?.UnregisterCallback(_refreshDelegate);
        }

        // ── Logic ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Compares the current loadout against the latest history snapshot and
        /// updates all wired labels.
        /// Hides the panel when <see cref="_currentLoadout"/> is null or the history
        /// is null / empty.  Fully null-safe.
        /// </summary>
        public void Refresh()
        {
            if (_currentLoadout == null ||
                _loadoutHistory == null ||
                _loadoutHistory.Count == 0)
            {
                _comparePanel?.SetActive(false);
                return;
            }

            _comparePanel?.SetActive(true);

            int    currentCount = _currentLoadout.EquippedPartIds?.Count ?? 0;
            var    latest       = _loadoutHistory.GetLatest();
            int    historyCount = latest?.partIds?.Length ?? 0;
            int    delta        = currentCount - historyCount;

            if (_currentPartsLabel != null)
                _currentPartsLabel.text = string.Format("Current: {0} parts", currentCount);

            if (_historyPartsLabel != null)
                _historyPartsLabel.text = string.Format("Previous: {0} parts", historyCount);

            if (_partDeltaLabel != null)
            {
                _partDeltaLabel.text = delta > 0 ? string.Format("+{0}", delta)
                    : delta < 0                  ? delta.ToString()
                    : "Same";
            }

            // Win rate across all history entries.
            if (_winRateLabel != null)
            {
                int wins  = 0;
                int total = _loadoutHistory.Count;
                for (int i = 0; i < total; i++)
                {
                    var entry = _loadoutHistory.GetEntry(i);
                    if (entry.HasValue && entry.Value.playerWon) wins++;
                }
                int winPct = total > 0
                    ? Mathf.RoundToInt(100f * wins / total)
                    : 0;
                _winRateLabel.text = string.Format("Win Rate: {0}%", winPct);
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="PlayerLoadout"/>. May be null.</summary>
        public PlayerLoadout CurrentLoadout => _currentLoadout;

        /// <summary>The assigned <see cref="LoadoutHistorySO"/>. May be null.</summary>
        public LoadoutHistorySO LoadoutHistory => _loadoutHistory;
    }
}
