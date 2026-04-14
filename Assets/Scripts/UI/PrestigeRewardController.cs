using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that displays the player's next upcoming prestige reward sourced
    /// from a <see cref="PrestigeRewardCatalogSO"/>.
    ///
    /// ── Display ──────────────────────────────────────────────────────────────────────
    ///   When a next reward exists (rank &gt; current prestige):
    ///   • <c>_rewardLabel</c>       → reward label (e.g. "Bronze Frame Skin") or "—" if empty.
    ///   • <c>_multiplierLabel</c>   → formatted bonus multiplier (e.g. "x1.25").
    ///   • <c>_rankLabel</c>         → "At Prestige N" (e.g. "At Prestige 2").
    ///   • <c>_noMoreRewardsPanel</c>→ deactivated.
    ///
    ///   When all rewards have been collected (or catalog is null / empty):
    ///   • All labels → "—".
    ///   • <c>_noMoreRewardsPanel</c>→ activated.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────────────
    ///   Awake     → caches _refreshDelegate.
    ///   OnEnable  → subscribes _onPrestige → Refresh(); calls Refresh().
    ///   OnDisable → unsubscribes.
    ///   Refresh() → reads PrestigeSystemSO.PrestigeCount; calls
    ///               PrestigeRewardCatalogSO.TryGetNextReward; null-safe.
    ///
    /// ── Architecture rules ────────────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   • DisallowMultipleComponent — one reward panel per canvas.
    ///   • All UI fields optional — assign only what the scene has.
    ///   • No Update / FixedUpdate loop.
    ///   • Delegate cached in Awake; zero heap allocations after initialisation.
    ///
    /// Scene wiring:
    ///   _catalog             → PrestigeRewardCatalogSO asset.
    ///   _prestigeSystem      → shared PrestigeSystemSO.
    ///   _onPrestige          → same VoidGameEvent as PrestigeSystemSO._onPrestige.
    ///   _rewardLabel         → Text that receives the reward name.
    ///   _multiplierLabel     → Text that receives "xN.NN".
    ///   _rankLabel           → Text that receives "At Prestige N".
    ///   _noMoreRewardsPanel  → Panel shown when no more rewards remain.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PrestigeRewardController : MonoBehaviour
    {
        // ── Inspector — Data ──────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("Catalog SO containing per-rank rewards. Leave null to show no-more-rewards state.")]
        [SerializeField] private PrestigeRewardCatalogSO _catalog;

        [Tooltip("Runtime prestige SO. Provides the current prestige count. " +
                 "Leave null to treat current prestige as 0.")]
        [SerializeField] private PrestigeSystemSO _prestigeSystem;

        // ── Inspector — Event Channel ─────────────────────────────────────────

        [Header("Event Channel — In")]
        [Tooltip("Raised each time the player earns a new prestige rank. " +
                 "Triggers Refresh(). Leave null to disable auto-refresh.")]
        [SerializeField] private VoidGameEvent _onPrestige;

        // ── Inspector — UI ────────────────────────────────────────────────────

        [Header("UI (optional)")]
        [Tooltip("Text label displaying the reward name (e.g. 'Bronze Frame Skin'). " +
                 "Shows '—' when no reward is found.")]
        [SerializeField] private Text _rewardLabel;

        [Tooltip("Text label displaying the bonus multiplier (e.g. 'x1.25'). " +
                 "Shows '—' when no reward is found.")]
        [SerializeField] private Text _multiplierLabel;

        [Tooltip("Text label displaying 'At Prestige N'. " +
                 "Shows '—' when no reward is found.")]
        [SerializeField] private Text _rankLabel;

        [Tooltip("Panel activated when all rewards have been collected. " +
                 "Deactivated while a next reward exists.")]
        [SerializeField] private GameObject _noMoreRewardsPanel;

        // ── Cached state ──────────────────────────────────────────────────────

        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPrestige?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPrestige?.UnregisterCallback(_refreshDelegate);
        }

        // ── Logic ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Reads the current prestige count and looks up the next upcoming reward.
        /// Updates all wired UI labels accordingly. Fully null-safe.
        /// </summary>
        public void Refresh()
        {
            int  currentPrestige = _prestigeSystem?.PrestigeCount ?? 0;
            bool hasNext         = _catalog != null
                                   && _catalog.TryGetNextReward(currentPrestige, out PrestigeRewardEntry next);

            if (hasNext)
            {
                if (_rewardLabel != null)
                    _rewardLabel.text = string.IsNullOrEmpty(next.label) ? "—" : next.label;

                if (_multiplierLabel != null)
                    _multiplierLabel.text = $"x{next.bonusMultiplier:F2}";

                if (_rankLabel != null)
                    _rankLabel.text = $"At Prestige {next.rank}";

                _noMoreRewardsPanel?.SetActive(false);
            }
            else
            {
                if (_rewardLabel     != null) _rewardLabel.text     = "—";
                if (_multiplierLabel != null) _multiplierLabel.text = "—";
                if (_rankLabel       != null) _rankLabel.text       = "—";

                _noMoreRewardsPanel?.SetActive(true);
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>The currently assigned <see cref="PrestigeRewardCatalogSO"/>. May be null.</summary>
        public PrestigeRewardCatalogSO Catalog => _catalog;

        /// <summary>The currently assigned <see cref="PrestigeSystemSO"/>. May be null.</summary>
        public PrestigeSystemSO PrestigeSystem => _prestigeSystem;
    }
}
