using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that displays a breakdown of the two score-bonus contributors
    /// held by <see cref="CombinedBonusCalculatorSO"/>:
    ///   Prestige ×N.NN  |  Mastery ×N.NN  |  Total ×N.NN
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────────
    ///   Awake     → caches _refreshDelegate (zero alloc after init).
    ///   OnEnable  → subscribes _onPrestige + _onMasteryUnlocked → Refresh(); Refresh().
    ///   OnDisable → unsubscribes both channels.
    ///   Refresh() → reads prestige multiplier, mastery multiplier, and FinalMultiplier
    ///               from the optional <see cref="_combinedBonus"/> SO; each defaults to
    ///               1× when the SO or its sub-SOs are null.  Fully null-safe on all
    ///               optional UI fields.
    ///
    /// ── Architecture rules ────────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   • DisallowMultipleComponent — one bonus breakdown panel per canvas.
    ///   • All UI fields are optional — assign only those present in the scene.
    ///   • No Update / FixedUpdate loop.
    ///   • Delegate cached in Awake; zero heap allocations after initialisation.
    ///
    /// Scene wiring:
    ///   _combinedBonus     → shared CombinedBonusCalculatorSO.
    ///   _onPrestige        → VoidGameEvent raised on each prestige.
    ///   _onMasteryUnlocked → VoidGameEvent raised when a damage type is mastered.
    ///   _prestigeLabel     → Text that receives "Prestige ×N.NN".
    ///   _masteryLabel      → Text that receives "Mastery ×N.NN".
    ///   _totalLabel        → Text that receives "Total ×N.NN".
    ///   _panel             → Optional root panel (activated on Refresh).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CombinedBonusHUDController : MonoBehaviour
    {
        // ── Inspector — Data ──────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("Aggregated bonus SO providing prestige and mastery multipliers. " +
                 "Leave null to display all values as '×1.00'.")]
        [SerializeField] private CombinedBonusCalculatorSO _combinedBonus;

        // ── Inspector — Event Channels ────────────────────────────────────────

        [Header("Event Channels — In")]
        [Tooltip("Raised each time the player earns a new prestige rank. Triggers Refresh().")]
        [SerializeField] private VoidGameEvent _onPrestige;

        [Tooltip("Raised when any damage type is first mastered. Triggers Refresh().")]
        [SerializeField] private VoidGameEvent _onMasteryUnlocked;

        // ── Inspector — UI (optional) ─────────────────────────────────────────

        [Header("UI (optional)")]
        [Tooltip("Text label displaying the prestige score contribution (e.g. 'Prestige ×1.25').")]
        [SerializeField] private Text _prestigeLabel;

        [Tooltip("Text label displaying the mastery score contribution (e.g. 'Mastery ×1.10').")]
        [SerializeField] private Text _masteryLabel;

        [Tooltip("Text label displaying the combined total multiplier (e.g. 'Total ×1.38').")]
        [SerializeField] private Text _totalLabel;

        [Tooltip("Root panel activated on Refresh(). Optional.")]
        [SerializeField] private GameObject _panel;

        // ── Cached delegate ───────────────────────────────────────────────────

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
        /// Reads the current prestige multiplier, mastery multiplier, and total
        /// multiplier from the optional <see cref="_combinedBonus"/> SO and updates
        /// all wired UI labels.  Fully null-safe on every optional reference.
        /// </summary>
        public void Refresh()
        {
            float prestigeM = 1f;
            float masteryM  = 1f;
            float totalM    = 1f;

            if (_combinedBonus != null)
            {
                prestigeM = _combinedBonus.ScoreMultiplier != null
                    ? _combinedBonus.ScoreMultiplier.Multiplier
                    : 1f;

                masteryM = _combinedBonus.MasteryBonusCatalog != null
                    ? _combinedBonus.MasteryBonusCatalog.GetTotalMultiplier(_combinedBonus.Mastery)
                    : 1f;

                totalM = _combinedBonus.FinalMultiplier;
            }

            if (_prestigeLabel != null)
                _prestigeLabel.text = $"Prestige \xd7{prestigeM:F2}";

            if (_masteryLabel != null)
                _masteryLabel.text = $"Mastery \xd7{masteryM:F2}";

            if (_totalLabel != null)
                _totalLabel.text = $"Total \xd7{totalM:F2}";

            _panel?.SetActive(true);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="CombinedBonusCalculatorSO"/>. May be null.</summary>
        public CombinedBonusCalculatorSO CombinedBonus => _combinedBonus;
    }
}
