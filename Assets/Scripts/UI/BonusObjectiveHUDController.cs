using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// HUD controller that displays a single bonus objective from a
    /// <see cref="MatchBonusObjectiveSO"/> and shows a completion overlay when done.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────
    ///   _onCompleted event fires
    ///   ──► BonusObjectiveHUDController.Refresh()
    ///         reads IsCompleted → activates _completedOverlay.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   _bonusObjective   → the MatchBonusObjectiveSO asset for this objective.
    ///   _onCompleted      → VoidGameEvent wired to MatchBonusObjectiveSO._onCompleted.
    ///   _panel            → Root panel; hidden when _bonusObjective is null.
    ///   _titleLabel       → Shows ObjectiveTitle.
    ///   _rewardLabel      → Shows "+N" bonus reward.
    ///   _completedOverlay → GameObject shown when IsCompleted is true.
    ///
    /// ── Architecture notes ───────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   - No Update / FixedUpdate — purely event-driven.
    ///   - Delegate cached in Awake; zero heap allocations per event firing.
    ///   - All fields optional and null-safe.
    ///   - DisallowMultipleComponent — one bonus HUD per canvas.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BonusObjectiveHUDController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("MatchBonusObjectiveSO describing this bonus challenge.")]
        [SerializeField] private MatchBonusObjectiveSO _bonusObjective;

        [Header("Event Channel — In (optional)")]
        [Tooltip("VoidGameEvent raised by MatchBonusObjectiveSO._onCompleted. " +
                 "Triggers Refresh() to update the completed overlay.")]
        [SerializeField] private VoidGameEvent _onCompleted;

        [Header("UI Refs (optional)")]
        [Tooltip("Root panel; hidden when _bonusObjective is null.")]
        [SerializeField] private GameObject _panel;

        [Tooltip("Text showing the objective title, e.g. 'Win Without Taking Damage'.")]
        [SerializeField] private Text _titleLabel;

        [Tooltip("Text showing the credit reward, e.g. '+100'.")]
        [SerializeField] private Text _rewardLabel;

        [Tooltip("GameObject shown on top of the panel when the objective is completed. " +
                 "Hidden while the objective is still active.")]
        [SerializeField] private GameObject _completedOverlay;

        // ── Cached delegate ───────────────────────────────────────────────────

        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onCompleted?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onCompleted?.UnregisterCallback(_refreshDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Reads the current <see cref="MatchBonusObjectiveSO"/> state and updates
        /// all UI elements. Hides the panel when <c>_bonusObjective</c> is null.
        /// Fully null-safe.
        /// </summary>
        public void Refresh()
        {
            if (_bonusObjective == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_titleLabel != null)
                _titleLabel.text = _bonusObjective.ObjectiveTitle;

            if (_rewardLabel != null)
                _rewardLabel.text = string.Format("+{0}", _bonusObjective.BonusReward);

            _completedOverlay?.SetActive(_bonusObjective.IsCompleted);
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="MatchBonusObjectiveSO"/>. May be null.</summary>
        public MatchBonusObjectiveSO BonusObjective => _bonusObjective;
    }
}
