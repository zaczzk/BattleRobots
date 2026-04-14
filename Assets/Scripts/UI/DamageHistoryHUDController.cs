using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// HUD controller that displays historical per-type damage rolling averages sourced
    /// from a <see cref="MatchDamageHistorySO"/> ring buffer.
    ///
    /// ── Display ──────────────────────────────────────────────────────────────────────
    ///   Computes <c>GetRollingAverage(type)</c> for all four damage types and:
    ///   • Updates optional <see cref="Text"/> labels with rounded average values.
    ///   • Updates optional <see cref="Slider"/> bars with the ratio of each type's
    ///     average to the sum of all averages (so bars always sum to 1).
    ///   • Hides <c>_panel</c> when the history SO is null or contains no entries.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────────────
    ///   Awake     → caches _refreshDelegate (Action).
    ///   OnEnable  → subscribes _onHistoryUpdated → Refresh(); calls Refresh().
    ///   OnDisable → unsubscribes _onHistoryUpdated; hides _panel.
    ///   Refresh() → reads MatchDamageHistorySO; hides panel when null/empty;
    ///               otherwise sets four avg labels and four ratio sliders.
    ///
    /// ── Architecture rules ────────────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   • DisallowMultipleComponent — one history HUD per canvas.
    ///   • All UI refs are optional — assign only those present in the scene.
    ///   • No Update / FixedUpdate loop — fully event-driven.
    ///   • Delegate cached in Awake; zero heap allocations after initialisation.
    ///
    /// Scene wiring:
    ///   _history           → shared MatchDamageHistorySO ring buffer.
    ///   _onHistoryUpdated  → VoidGameEvent raised when the history SO gains a new entry
    ///                        (wire from PostMatchDamageHistoryController._onMatchEnded
    ///                         or a custom broadcast).
    ///   _physicalAvgText … _shockAvgText  → Text labels for rounded per-type averages.
    ///   _physicalBar     … _shockBar      → Slider bars for per-type ratio in [0, 1].
    ///   _panel             → Root panel hidden when no data exists.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DamageHistoryHUDController : MonoBehaviour
    {
        // ── Inspector — Data ──────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("Runtime history ring-buffer SO. Leave null to hide the panel.")]
        [SerializeField] private MatchDamageHistorySO _history;

        // ── Inspector — Event Channel ─────────────────────────────────────────

        [Header("Event Channel — In")]
        [Tooltip("Raised when the history SO gains a new entry (e.g. at match end). " +
                 "Triggers Refresh(). Leave null to refresh only on enable.")]
        [SerializeField] private VoidGameEvent _onHistoryUpdated;

        // ── Inspector — Average Labels (optional) ────────────────────────────

        [Header("Average Labels (optional)")]
        [Tooltip("Text label showing Mathf.RoundToInt(rolling average) for Physical.")]
        [SerializeField] private Text _physicalAvgText;

        [Tooltip("Text label showing Mathf.RoundToInt(rolling average) for Energy.")]
        [SerializeField] private Text _energyAvgText;

        [Tooltip("Text label showing Mathf.RoundToInt(rolling average) for Thermal.")]
        [SerializeField] private Text _thermalAvgText;

        [Tooltip("Text label showing Mathf.RoundToInt(rolling average) for Shock.")]
        [SerializeField] private Text _shockAvgText;

        // ── Inspector — Ratio Bars (optional) ─────────────────────────────────

        [Header("Ratio Bars (optional)")]
        [Tooltip("Slider.value = Physical avg / total avg in [0, 1].")]
        [SerializeField] private Slider _physicalBar;

        [Tooltip("Slider.value = Energy avg / total avg in [0, 1].")]
        [SerializeField] private Slider _energyBar;

        [Tooltip("Slider.value = Thermal avg / total avg in [0, 1].")]
        [SerializeField] private Slider _thermalBar;

        [Tooltip("Slider.value = Shock avg / total avg in [0, 1].")]
        [SerializeField] private Slider _shockBar;

        // ── Inspector — Panel (optional) ─────────────────────────────────────

        [Header("Panel (optional)")]
        [Tooltip("Root panel GameObject. Hidden when history SO is null or empty.")]
        [SerializeField] private GameObject _panel;

        // ── Cached state ──────────────────────────────────────────────────────

        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onHistoryUpdated?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onHistoryUpdated?.UnregisterCallback(_refreshDelegate);
            _panel?.SetActive(false);
        }

        // ── Logic ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Reads rolling averages from the history SO and updates all wired labels and bars.
        /// Hides the panel when no SO is assigned or the ring buffer is empty.
        /// Fully null-safe — skips any unassigned UI ref.
        /// </summary>
        public void Refresh()
        {
            if (_history == null || _history.Count == 0)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            float physical = _history.GetRollingAverage(DamageType.Physical);
            float energy   = _history.GetRollingAverage(DamageType.Energy);
            float thermal  = _history.GetRollingAverage(DamageType.Thermal);
            float shock    = _history.GetRollingAverage(DamageType.Shock);
            float total    = physical + energy + thermal + shock;

            // Labels — rounded integer averages
            if (_physicalAvgText != null) _physicalAvgText.text = Mathf.RoundToInt(physical).ToString();
            if (_energyAvgText   != null) _energyAvgText.text   = Mathf.RoundToInt(energy).ToString();
            if (_thermalAvgText  != null) _thermalAvgText.text  = Mathf.RoundToInt(thermal).ToString();
            if (_shockAvgText    != null) _shockAvgText.text    = Mathf.RoundToInt(shock).ToString();

            // Bars — ratio relative to combined total (safe divide)
            float physRatio    = total > 0f ? physical / total : 0f;
            float energyRatio  = total > 0f ? energy   / total : 0f;
            float thermalRatio = total > 0f ? thermal  / total : 0f;
            float shockRatio   = total > 0f ? shock    / total : 0f;

            if (_physicalBar != null) _physicalBar.value = physRatio;
            if (_energyBar   != null) _energyBar.value   = energyRatio;
            if (_thermalBar  != null) _thermalBar.value  = thermalRatio;
            if (_shockBar    != null) _shockBar.value    = shockRatio;
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>The currently assigned <see cref="MatchDamageHistorySO"/>. May be null.</summary>
        public MatchDamageHistorySO History => _history;
    }
}
