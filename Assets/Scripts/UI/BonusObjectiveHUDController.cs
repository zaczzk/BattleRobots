using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// HUD controller that displays the state of a <see cref="MatchBonusObjectiveSO"/>:
    /// title, status (countdown / COMPLETE! / EXPIRED / Active), reward, and a timer bar.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────────
    ///   Update ticks MatchBonusObjectiveSO.Tick(dt).
    ///   _onChanged → Refresh().
    ///   Refresh reads SO state and updates all UI elements.
    ///
    /// ── Status label text ─────────────────────────────────────────────────────────
    ///   IsComplete  → "COMPLETE!"
    ///   IsExpired   → "EXPIRED"
    ///   HasTimeLimit → "{TimeRemaining:F1}s" (live countdown)
    ///   else         → "Active"
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   - Delegate cached in Awake; zero heap allocations after initialisation.
    ///   - DisallowMultipleComponent — one bonus-objective HUD per canvas.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BonusObjectiveHUDController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("MatchBonusObjectiveSO driving this HUD panel.")]
        [SerializeField] private MatchBonusObjectiveSO _bonusObjective;

        [Header("Event Channel — In (optional)")]
        [Tooltip("VoidGameEvent raised by MatchBonusObjectiveSO._onChanged. Triggers Refresh.")]
        [SerializeField] private VoidGameEvent _onChanged;

        [Header("UI References (optional)")]
        [Tooltip("Root panel; shown when a valid MatchBonusObjectiveSO is assigned.")]
        [SerializeField] private GameObject _panel;

        [Tooltip("Text showing the BonusTitle of the objective.")]
        [SerializeField] private Text _titleLabel;

        [Tooltip("Text showing status: 'COMPLETE!', 'EXPIRED', 'N.Ns', or 'Active'.")]
        [SerializeField] private Text _statusLabel;

        [Tooltip("Text showing the reward, e.g. 'Reward: 50'.")]
        [SerializeField] private Text _rewardLabel;

        [Tooltip("Slider driven by TimeRatio [0,1]; full = time remaining, empty = expired.")]
        [SerializeField] private Slider _timerBar;

        // ── Cached delegate ───────────────────────────────────────────────────

        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onChanged?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onChanged?.UnregisterCallback(_refreshDelegate);
        }

        private void Update()
        {
            _bonusObjective?.Tick(Time.deltaTime);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Reads the current <see cref="MatchBonusObjectiveSO"/> state and updates all
        /// UI elements. Hides the panel when <c>_bonusObjective</c> is null.
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
                _titleLabel.text = _bonusObjective.BonusTitle;

            if (_statusLabel != null)
            {
                if (_bonusObjective.IsComplete)
                    _statusLabel.text = "COMPLETE!";
                else if (_bonusObjective.IsExpired)
                    _statusLabel.text = "EXPIRED";
                else if (_bonusObjective.HasTimeLimit)
                    _statusLabel.text = string.Format("{0:F1}s", _bonusObjective.TimeRemaining);
                else
                    _statusLabel.text = "Active";
            }

            if (_rewardLabel != null)
                _rewardLabel.text = string.Format("Reward: {0}", _bonusObjective.BonusReward);

            if (_timerBar != null)
                _timerBar.value = _bonusObjective.TimeRatio;
        }

        /// <summary>The assigned <see cref="MatchBonusObjectiveSO"/>. May be null.</summary>
        public MatchBonusObjectiveSO BonusObjective => _bonusObjective;
    }
}
