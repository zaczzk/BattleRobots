using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that displays a composite read-only career overview by
    /// aggregating data from four optional runtime SOs:
    ///
    ///   • <see cref="PrestigeSystemSO"/>         — rank label and prestige count.
    ///   • <see cref="DamageTypeMasterySO"/>       — number of damage types mastered.
    ///   • <see cref="CombinedBonusCalculatorSO"/> — total score multiplier.
    ///   • <see cref="PrestigeHistorySO"/>          — count of stored prestige events.
    ///
    /// ── Display ──────────────────────────────────────────────────────────────────
    ///   _prestigeRankLabel    → rank string from PrestigeSystemSO (e.g. "Gold II").
    ///                           "None" when _prestigeSystem is null.
    ///   _prestigeCountLabel   → "Prestige N" (N = PrestigeCount). "Prestige 0" when null.
    ///   _masteredTypesLabel   → "N/4" count of mastered damage types. "0/4" when null.
    ///   _bonusMultiplierLabel → "×N.NN" FinalMultiplier. "×1.00" when null.
    ///   _prestigeEventsLabel  → "N" count of stored prestige history entries. "0" when null.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────────
    ///   Awake     → caches _refreshDelegate (zero alloc after init).
    ///   OnEnable  → subscribes _onPrestige + _onMasteryUnlocked → Refresh(); Refresh().
    ///   OnDisable → unsubscribes both channels.
    ///   Refresh() → reads all data SOs and updates all wired UI labels. Fully null-safe.
    ///
    /// ── Architecture rules ────────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   • DisallowMultipleComponent — one summary panel per canvas.
    ///   • All UI and data fields are optional.
    ///   • No Update / FixedUpdate loop.
    ///   • Delegate cached in Awake; zero heap allocations after initialisation.
    ///
    /// Scene wiring:
    ///   _mastery              → shared DamageTypeMasterySO.
    ///   _prestigeSystem       → shared PrestigeSystemSO.
    ///   _combinedBonus        → shared CombinedBonusCalculatorSO.
    ///   _history              → shared PrestigeHistorySO.
    ///   _onPrestige           → VoidGameEvent from PrestigeSystemSO.
    ///   _onMasteryUnlocked    → VoidGameEvent from DamageTypeMasterySO.
    ///   _prestigeRankLabel    → Text showing current rank (e.g. "Gold II").
    ///   _prestigeCountLabel   → Text showing prestige number (e.g. "Prestige 8").
    ///   _masteredTypesLabel   → Text showing mastered count (e.g. "3/4").
    ///   _bonusMultiplierLabel → Text showing total multiplier (e.g. "×1.38").
    ///   _prestigeEventsLabel  → Text showing history size (e.g. "5").
    ///   _panel                → Root panel (activated on Refresh).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CareerSummaryController : MonoBehaviour
    {
        // ── Inspector — Data ──────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("Runtime mastery SO providing per-type mastered flags. " +
                 "Leave null to show '0/4' mastered types.")]
        [SerializeField] private DamageTypeMasterySO _mastery;

        [Tooltip("Runtime prestige SO providing the current count and rank label. " +
                 "Leave null to show 'None' rank and 'Prestige 0'.")]
        [SerializeField] private PrestigeSystemSO _prestigeSystem;

        [Tooltip("Aggregated bonus SO providing the combined score multiplier. " +
                 "Leave null to show '×1.00'.")]
        [SerializeField] private CombinedBonusCalculatorSO _combinedBonus;

        [Tooltip("Prestige history ring-buffer SO. " +
                 "Leave null to show '0' prestige events.")]
        [SerializeField] private PrestigeHistorySO _history;

        // ── Inspector — Event Channels ────────────────────────────────────────

        [Header("Event Channels — In")]
        [Tooltip("Raised each time the player earns a new prestige rank. Triggers Refresh().")]
        [SerializeField] private VoidGameEvent _onPrestige;

        [Tooltip("Raised when any damage type is first mastered. Triggers Refresh().")]
        [SerializeField] private VoidGameEvent _onMasteryUnlocked;

        // ── Inspector — UI (optional) ─────────────────────────────────────────

        [Header("UI (optional)")]
        [Tooltip("Text label for the player's current prestige rank (e.g. 'Gold II').")]
        [SerializeField] private Text _prestigeRankLabel;

        [Tooltip("Text label for the player's prestige count (e.g. 'Prestige 8').")]
        [SerializeField] private Text _prestigeCountLabel;

        [Tooltip("Text label showing how many of the four damage types are mastered (e.g. '3/4').")]
        [SerializeField] private Text _masteredTypesLabel;

        [Tooltip("Text label showing the combined score multiplier (e.g. '×1.38').")]
        [SerializeField] private Text _bonusMultiplierLabel;

        [Tooltip("Text label showing the number of stored prestige history events.")]
        [SerializeField] private Text _prestigeEventsLabel;

        [Tooltip("Root panel activated on Refresh(). Optional.")]
        [SerializeField] private GameObject _panel;

        // ── Internal state ────────────────────────────────────────────────────

        private static readonly DamageType[] s_types =
        {
            DamageType.Physical,
            DamageType.Energy,
            DamageType.Thermal,
            DamageType.Shock
        };

        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPrestige?.RegisterCallback(_refreshDelegate);
            _onMasteryUnlocked?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPrestige?.UnregisterCallback(_refreshDelegate);
            _onMasteryUnlocked?.UnregisterCallback(_refreshDelegate);
        }

        // ── Logic ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Reads all four optional data SOs and updates all wired UI labels.
        /// Fully null-safe on every optional reference; each missing SO defaults to its
        /// "empty" display value.
        /// </summary>
        public void Refresh()
        {
            // ── Prestige rank + count ─────────────────────────────────────────
            string rank  = _prestigeSystem != null ? _prestigeSystem.GetRankLabel() : "None";
            int    count = _prestigeSystem != null ? _prestigeSystem.PrestigeCount  : 0;

            // ── Mastered type count ───────────────────────────────────────────
            int masteredCount = 0;
            if (_mastery != null)
            {
                for (int i = 0; i < s_types.Length; i++)
                {
                    if (_mastery.IsTypeMastered(s_types[i]))
                        masteredCount++;
                }
            }

            // ── Total score multiplier ────────────────────────────────────────
            float totalM = _combinedBonus != null ? _combinedBonus.FinalMultiplier : 1f;

            // ── Prestige history size ─────────────────────────────────────────
            int historyCount = _history != null ? _history.Count : 0;

            // ── Update labels ─────────────────────────────────────────────────
            if (_prestigeRankLabel    != null) _prestigeRankLabel.text    = rank;
            if (_prestigeCountLabel   != null) _prestigeCountLabel.text   = $"Prestige {count}";
            if (_masteredTypesLabel   != null) _masteredTypesLabel.text   = $"{masteredCount}/4";
            if (_bonusMultiplierLabel != null) _bonusMultiplierLabel.text = $"\xd7{totalM:F2}";
            if (_prestigeEventsLabel  != null) _prestigeEventsLabel.text  = $"{historyCount}";

            _panel?.SetActive(true);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="DamageTypeMasterySO"/>. May be null.</summary>
        public DamageTypeMasterySO Mastery => _mastery;

        /// <summary>The assigned <see cref="PrestigeSystemSO"/>. May be null.</summary>
        public PrestigeSystemSO PrestigeSystem => _prestigeSystem;

        /// <summary>The assigned <see cref="CombinedBonusCalculatorSO"/>. May be null.</summary>
        public CombinedBonusCalculatorSO CombinedBonus => _combinedBonus;

        /// <summary>The assigned <see cref="PrestigeHistorySO"/>. May be null.</summary>
        public PrestigeHistorySO History => _history;
    }
}
