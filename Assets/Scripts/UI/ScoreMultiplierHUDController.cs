using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that displays the player's current score multiplier and the
    /// label of the prestige reward that provided it.
    ///
    /// ── Display ───────────────────────────────────────────────────────────────────
    ///   <c>_multiplierLabel</c> → "x{Multiplier:F2}" (e.g. "x1.25") from
    ///     <see cref="_scoreMultiplier"/>; defaults to "x1.00" when the SO is null.
    ///   <c>_rewardLabel</c>     → label string from <see cref="PrestigeRewardCatalogSO"/>
    ///     entry whose rank == <see cref="PrestigeSystemSO.PrestigeCount"/>; "—" when no
    ///     matching reward exists, or when <see cref="_rewardCatalog"/> or
    ///     <see cref="_prestigeSystem"/> is null.
    ///   <c>_panel</c>          → activated on every Refresh().
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────────
    ///   Awake     → caches _refreshDelegate (zero alloc after init).
    ///   OnEnable  → subscribes _onPrestige → Refresh(); calls Refresh().
    ///   OnDisable → unsubscribes.
    ///   Refresh() → reads ScoreMultiplierSO.Multiplier; looks up prestige reward label.
    ///               Fully null-safe on all optional fields.
    ///
    /// ── Architecture rules ────────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   • DisallowMultipleComponent — one multiplier panel per canvas.
    ///   • All UI fields are optional — assign only those present in the scene.
    ///   • No Update / FixedUpdate loop.
    ///   • Delegate cached in Awake; zero heap allocations after initialisation.
    ///
    /// Scene wiring:
    ///   _scoreMultiplier  → shared ScoreMultiplierSO (prestige bonus).
    ///   _rewardCatalog    → PrestigeRewardCatalogSO to look up the active reward label.
    ///   _prestigeSystem   → shared PrestigeSystemSO for current prestige count.
    ///   _onPrestige       → VoidGameEvent raised on each prestige.
    ///   _multiplierLabel  → Text that receives "xN.NN".
    ///   _rewardLabel      → Text that receives the reward label (or "—").
    ///   _panel            → Optional root panel (activated on Refresh).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ScoreMultiplierHUDController : MonoBehaviour
    {
        // ── Inspector — Data ──────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("Runtime ScoreMultiplierSO providing the current multiplier value. " +
                 "Leave null to display 'x1.00'.")]
        [SerializeField] private ScoreMultiplierSO _scoreMultiplier;

        [Tooltip("Prestige reward catalog used to look up the label for the current rank. " +
                 "Leave null to show '—' in the reward label.")]
        [SerializeField] private PrestigeRewardCatalogSO _rewardCatalog;

        [Tooltip("Runtime prestige SO providing the current prestige count. " +
                 "Leave null to treat current prestige as 0.")]
        [SerializeField] private PrestigeSystemSO _prestigeSystem;

        // ── Inspector — Event Channel ─────────────────────────────────────────

        [Header("Event Channel — In")]
        [Tooltip("Raised each time the player earns a new prestige rank. " +
                 "Triggers Refresh(). Leave null to disable auto-refresh.")]
        [SerializeField] private VoidGameEvent _onPrestige;

        // ── Inspector — UI (optional) ─────────────────────────────────────────

        [Header("UI (optional)")]
        [Tooltip("Text label displaying the current multiplier (e.g. 'x1.25'). " +
                 "Shows 'x1.00' when _scoreMultiplier is null.")]
        [SerializeField] private Text _multiplierLabel;

        [Tooltip("Text label displaying the prestige reward label for the current rank " +
                 "(e.g. 'Bronze Frame Skin'). Shows '—' when no matching reward exists.")]
        [SerializeField] private Text _rewardLabel;

        [Tooltip("Root panel activated on Refresh(). Optional.")]
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
            _onPrestige?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPrestige?.UnregisterCallback(_refreshDelegate);
        }

        // ── Logic ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Reads the current multiplier and prestige reward label, then updates all
        /// wired UI elements. Fully null-safe on every optional reference.
        /// </summary>
        public void Refresh()
        {
            // ── Multiplier label ──────────────────────────────────────────────
            float multiplier = _scoreMultiplier != null ? _scoreMultiplier.Multiplier : 1f;
            if (_multiplierLabel != null)
                _multiplierLabel.text = $"x{multiplier:F2}";

            // ── Reward label ──────────────────────────────────────────────────
            string rewardText = "—";
            if (_rewardCatalog != null && _prestigeSystem != null)
            {
                int currentPrestige = _prestigeSystem.PrestigeCount;
                if (_rewardCatalog.TryGetRewardForRank(currentPrestige, out PrestigeRewardEntry entry)
                    && !string.IsNullOrEmpty(entry.label))
                {
                    rewardText = entry.label;
                }
            }
            if (_rewardLabel != null)
                _rewardLabel.text = rewardText;

            // ── Panel ──────────────────────────────────────────────────────────
            _panel?.SetActive(true);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="ScoreMultiplierSO"/>. May be null.</summary>
        public ScoreMultiplierSO ScoreMultiplier => _scoreMultiplier;

        /// <summary>The assigned <see cref="PrestigeRewardCatalogSO"/>. May be null.</summary>
        public PrestigeRewardCatalogSO RewardCatalog => _rewardCatalog;

        /// <summary>The assigned <see cref="PrestigeSystemSO"/>. May be null.</summary>
        public PrestigeSystemSO PrestigeSystem => _prestigeSystem;
    }
}
