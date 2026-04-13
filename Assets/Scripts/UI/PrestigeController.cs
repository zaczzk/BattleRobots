using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// Reactive UI controller for the Prestige system.
    ///
    /// Displays the player's current prestige rank and count, enables the "Prestige"
    /// button only when all conditions are met (max level reached, max prestige not yet
    /// reached), and persists the updated count to SaveData via load→mutate→Save.
    ///
    /// ── Inspector wiring ─────────────────────────────────────────────────────────
    ///   Data (all optional):
    ///     _prestigeSystem  → the <see cref="PrestigeSystemSO"/> asset.
    ///     _progression     → the <see cref="PlayerProgressionSO"/> asset.
    ///   Event Channels — In (optional):
    ///     _onPrestige      → the same VoidGameEvent assigned to PrestigeSystemSO._onPrestige.
    ///                        Subscribe here so the panel refreshes immediately after a prestige.
    ///   UI Refs (all optional):
    ///     _prestigeButton  → the "Prestige" Button; interactable only when CanPrestige.
    ///     _prestigeCountText → Text showing "×N" where N = PrestigeCount.
    ///     _prestigeRankText  → Text showing the rank label (e.g. "Gold II").
    ///     _prestigeInfoText  → Text showing a contextual hint string.
    ///
    /// ── Architecture notes ───────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no BattleRobots.Physics references.
    ///   - No allocations in Update/FixedUpdate — this component has no Update loop.
    ///   - All inspector fields optional; null-safe throughout.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PrestigeController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (all optional)")]
        [Tooltip("SO that tracks the player's prestige count and provides rank labels. " +
                 "Leave null to show fallback '—' values on all labels.")]
        [SerializeField] private PrestigeSystemSO _prestigeSystem;

        [Tooltip("SO that tracks the player's XP and level. " +
                 "Required alongside _prestigeSystem to gate the Prestige button. " +
                 "Leave null to keep the button non-interactable.")]
        [SerializeField] private PlayerProgressionSO _progression;

        [Header("Event Channels — In (optional)")]
        [Tooltip("VoidGameEvent raised by PrestigeSystemSO when a prestige occurs. " +
                 "This controller subscribes in OnEnable to refresh labels reactively. " +
                 "Must match the event assigned to PrestigeSystemSO._onPrestige. " +
                 "Leave null to refresh only on OnEnable.")]
        [SerializeField] private VoidGameEvent _onPrestige;

        [Header("UI Refs (all optional)")]
        [Tooltip("Button that triggers DoPrestige(). Interactable only when CanPrestige is true.")]
        [SerializeField] private Button _prestigeButton;

        [Tooltip("Text showing prestige count as '×N' (e.g. '×3'). Leave null to skip.")]
        [SerializeField] private Text _prestigeCountText;

        [Tooltip("Text showing the rank label (e.g. 'Gold II', 'Legend'). Leave null to skip.")]
        [SerializeField] private Text _prestigeRankText;

        [Tooltip("Contextual hint text (e.g. instructions or max-prestige message). Leave null to skip.")]
        [SerializeField] private Text _prestigeInfoText;

        // ── Runtime ───────────────────────────────────────────────────────────

        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;

            if (_prestigeButton != null)
                _prestigeButton.onClick.AddListener(DoPrestige);
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

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Called by the Prestige Button's onClick event.
        /// Guards null references, calls <see cref="PrestigeSystemSO.Prestige"/>,
        /// and persists the updated count to SaveData.
        /// Silent no-op when the prestige SO is null or <see cref="PrestigeSystemSO.CanPrestige"/>
        /// returns false.
        /// </summary>
        public void DoPrestige()
        {
            if (_prestigeSystem == null) return;

            int countBefore = _prestigeSystem.PrestigeCount;
            _prestigeSystem.Prestige(_progression);

            // Only persist when the count actually changed (CanPrestige guard).
            if (_prestigeSystem.PrestigeCount > countBefore)
            {
                SaveData save = SaveSystem.Load();
                save.prestigeCount = _prestigeSystem.PrestigeCount;
                SaveSystem.Save(save);
            }
        }

        // ── Internal ──────────────────────────────────────────────────────────

        /// <summary>
        /// Refreshes all UI elements from the current <see cref="PrestigeSystemSO"/> state.
        /// Null-safe: missing SO shows "—" / "None" fallbacks and disables the button.
        /// </summary>
        private void Refresh()
        {
            if (_prestigeSystem == null)
            {
                // Fallback when no SO is wired.
                if (_prestigeCountText != null) _prestigeCountText.text = "\u2014";
                if (_prestigeRankText  != null) _prestigeRankText.text  = "\u2014";
                if (_prestigeInfoText  != null) _prestigeInfoText.text  = "\u2014";
                if (_prestigeButton    != null) _prestigeButton.interactable = false;
                return;
            }

            bool canPrestige = _prestigeSystem.CanPrestige(_progression);

            if (_prestigeButton    != null) _prestigeButton.interactable = canPrestige;
            if (_prestigeCountText != null) _prestigeCountText.text = $"\u00d7{_prestigeSystem.PrestigeCount}";
            if (_prestigeRankText  != null) _prestigeRankText.text  = _prestigeSystem.GetRankLabel();

            if (_prestigeInfoText != null)
            {
                if (_prestigeSystem.IsMaxPrestige)
                    _prestigeInfoText.text = "Maximum prestige reached. You are a Legend!";
                else if (canPrestige)
                    _prestigeInfoText.text = "You have reached max level. Prestige to earn your next rank badge!";
                else
                    _prestigeInfoText.text = "Reach max level to unlock Prestige.";
            }
        }
    }
}
