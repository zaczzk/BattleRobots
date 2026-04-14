using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Real-time HUD panel showing the player's level progress toward earning the next
    /// prestige rank, sourced from <see cref="PlayerProgressionSO"/> and
    /// <see cref="PrestigeSystemSO"/>.
    ///
    /// ── Display ──────────────────────────────────────────────────────────────────
    ///   _levelLabel   → "Level {current} / {max}" (e.g. "Level 7 / 10")
    ///   _xpProgressBar → XpProgressFraction [0, 1]
    ///   _statusLabel  → one of:
    ///       • "Prestige Ready!" — at max level and not yet at max prestige
    ///       • "Legend"          — max prestige reached
    ///       • ""                — any other state
    ///
    ///   When either data SO is null the panel is hidden.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────────
    ///   Awake    → caches _refreshDelegate.
    ///   OnEnable → subscribes _onLevelUp + _onPrestige → Refresh(); calls Refresh().
    ///   OnDisable → unsubscribes both channels.
    ///   Refresh() → reads SOs; updates labels and bar.  Null-safe.
    ///
    /// ── Architecture rules ────────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   • DisallowMultipleComponent — one progress HUD per canvas.
    ///   • All UI fields are optional — assign only those present in the scene.
    ///   • No Update / FixedUpdate loop.
    ///   • Delegate cached in Awake; zero heap allocations after initialisation.
    ///
    /// Scene wiring:
    ///   _progression    → shared PlayerProgressionSO asset.
    ///   _prestigeSystem → shared PrestigeSystemSO asset.
    ///   _onLevelUp      → same VoidGameEvent as PlayerProgressionSO._onLevelUp.
    ///   _onPrestige     → same VoidGameEvent as PrestigeSystemSO._onPrestige.
    ///   _panel          → root HUD panel.
    ///   _levelLabel     → Text showing "Level N / Max".
    ///   _statusLabel    → Text showing prestige-readiness state.
    ///   _xpProgressBar  → Slider fill bar [0, 1] for XP within the current level.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PrestigeProgressHUDController : MonoBehaviour
    {
        // ── Inspector — Data ─────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("Runtime SO tracking player XP and level.")]
        [SerializeField] private PlayerProgressionSO _progression;

        [Tooltip("Runtime SO tracking prestige count and max prestige.")]
        [SerializeField] private PrestigeSystemSO _prestigeSystem;

        // ── Inspector — Events ───────────────────────────────────────────────

        [Header("Event Channels — In (optional)")]
        [Tooltip("Raised by PlayerProgressionSO each time the player gains a level.")]
        [SerializeField] private VoidGameEvent _onLevelUp;

        [Tooltip("Raised by PrestigeSystemSO each time the player prestiges.")]
        [SerializeField] private VoidGameEvent _onPrestige;

        // ── Inspector — UI ───────────────────────────────────────────────────

        [Header("UI (all optional)")]
        [Tooltip("Root HUD panel. Hidden when data SOs are null.")]
        [SerializeField] private GameObject _panel;

        [Tooltip("Text label receiving 'Level N / Max'.")]
        [SerializeField] private Text _levelLabel;

        [Tooltip("Text label showing 'Prestige Ready!', 'Legend', or empty.")]
        [SerializeField] private Text _statusLabel;

        [Tooltip("Slider fill bar for XP progress within the current level [0, 1].")]
        [SerializeField] private Slider _xpProgressBar;

        // ── Cached delegate ──────────────────────────────────────────────────

        private Action _refreshDelegate;

        // ── Lifecycle ────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onLevelUp?.RegisterCallback(_refreshDelegate);
            _onPrestige?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onLevelUp?.UnregisterCallback(_refreshDelegate);
            _onPrestige?.UnregisterCallback(_refreshDelegate);
        }

        // ── Logic ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Reads current progression and prestige state and updates all wired UI elements.
        /// Hidden when either data SO is null.  Fully null-safe.
        /// </summary>
        public void Refresh()
        {
            if (_progression == null || _prestigeSystem == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            int   current    = _progression.CurrentLevel;
            int   max        = _progression.MaxLevel;
            float xpFraction = _progression.XpProgressFraction;

            if (_levelLabel != null)
                _levelLabel.text = string.Format("Level {0} / {1}", current, max);

            if (_xpProgressBar != null)
                _xpProgressBar.value = xpFraction;

            if (_statusLabel != null)
            {
                if (_prestigeSystem.IsMaxPrestige)
                    _statusLabel.text = "Legend";
                else if (_progression.IsMaxLevel)
                    _statusLabel.text = "Prestige Ready!";
                else
                    _statusLabel.text = string.Empty;
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="PlayerProgressionSO"/>. May be null.</summary>
        public PlayerProgressionSO Progression => _progression;

        /// <summary>The assigned <see cref="PrestigeSystemSO"/>. May be null.</summary>
        public PrestigeSystemSO PrestigeSystem => _prestigeSystem;
    }
}
