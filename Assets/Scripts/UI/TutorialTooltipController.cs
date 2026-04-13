using System;
using System.Collections.Generic;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// Shows a small tooltip hint the first time a player opens a tagged UI panel.
    /// Once the player dismisses it, the panel tag is persisted in
    /// <see cref="SaveData.seenTooltipPanelTags"/> so the tooltip never appears again.
    ///
    /// ── When does the tooltip show? ──────────────────────────────────────────────────
    ///   • <c>OnEnable</c>: checks <c>SaveData.seenTooltipPanelTags</c> for <c>_panelTag</c>.
    ///     First visit (tag not in the seen-set) → shows tooltip.
    ///     Returning visit (tag already seen) → hides immediately.
    ///     Empty / whitespace <c>_panelTag</c> → hides immediately (nothing to track).
    ///
    /// ── Dismiss ──────────────────────────────────────────────────────────────────────
    ///   • <c>_dismissButton.onClick</c> is wired to <see cref="Dismiss"/> in <c>Awake</c>.
    ///   • <see cref="Dismiss"/> adds <c>_panelTag</c> to
    ///     <c>SaveData.seenTooltipPanelTags</c> via the load→mutate→Save pattern, then
    ///     calls <see cref="Hide"/>.
    ///
    /// ── Inspector wiring ─────────────────────────────────────────────────────────────
    ///   Data (optional):
    ///     _config    → <see cref="TutorialTooltipConfig"/> SO mapping tags to hint texts.
    ///                  Leave null — the tooltip panel still shows, but with no text.
    ///     _panelTag  → Unity tag identifying this panel in the seen-set. Must be non-empty
    ///                  for the tooltip to appear (empty → tooltip always hidden).
    ///   UI Refs (optional — all null-safe):
    ///     _tooltipPanel   → Root tooltip container. Shown on first visit; hidden if seen.
    ///     _tooltipText    → Label populated from <c>_config.GetTooltipForTag(_panelTag)</c>.
    ///     _dismissButton  → Button wired to <see cref="Dismiss"/> in <c>Awake</c>.
    ///
    /// ── Architecture notes ───────────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no BattleRobots.Physics references.
    ///   - No allocations in Update / FixedUpdate — no Update loop at all.
    ///   - All inspector fields optional; fully null-safe throughout.
    ///   - Persists seen state via the standard load→mutate→Save pattern.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TutorialTooltipController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("Maps panel tags to first-visit hint texts. " +
                 "Leave null — the tooltip panel still appears on first visit but with no text.")]
        [SerializeField] private TutorialTooltipConfig _config;

        [Tooltip("Unity tag identifying this panel in the seen-set. " +
                 "Must be non-empty for the tooltip to show. " +
                 "Checked against SaveData.seenTooltipPanelTags on OnEnable.")]
        [SerializeField] private string _panelTag = "";

        [Header("UI Refs (optional)")]
        [Tooltip("Root tooltip container shown on first visit. " +
                 "Hidden if the panel was already seen or the tag is empty.")]
        [SerializeField] private GameObject _tooltipPanel;

        [Tooltip("Text label populated from the config tooltip text. " +
                 "Unchanged when _config is null or no entry matches _panelTag.")]
        [SerializeField] private Text _tooltipText;

        [Tooltip("Dismiss / got-it button. Wired to Dismiss() in Awake. " +
                 "Clicking marks this panel seen, saves, and hides the tooltip.")]
        [SerializeField] private Button _dismissButton;

        // ── Runtime ───────────────────────────────────────────────────────────

        private Action _dismissDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _dismissDelegate = Dismiss;

            if (_dismissButton != null)
                _dismissButton.onClick.AddListener(_dismissDelegate);
        }

        private void OnEnable()
        {
            // No panel tag — nothing to track; stay hidden.
            if (string.IsNullOrWhiteSpace(_panelTag))
            {
                Hide();
                return;
            }

            // Check the persisted seen-set.
            SaveData save = SaveSystem.Load();
            if (save.seenTooltipPanelTags != null
                && save.seenTooltipPanelTags.Contains(_panelTag))
            {
                Hide();
                return;
            }

            // First visit — populate tooltip text (if wired) and show.
            if (_tooltipText != null && _config != null)
                _tooltipText.text = _config.GetTooltipForTag(_panelTag);

            _tooltipPanel?.SetActive(true);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Marks this panel as seen, persists the seen-set via the load→mutate→Save
        /// pattern, then hides the tooltip.
        ///
        /// No-op when <c>_panelTag</c> is null or whitespace.
        /// Wired to <c>_dismissButton.onClick</c> in <c>Awake</c>.
        /// </summary>
        public void Dismiss()
        {
            if (string.IsNullOrWhiteSpace(_panelTag)) return;

            SaveData save = SaveSystem.Load();

            if (save.seenTooltipPanelTags == null)
                save.seenTooltipPanelTags = new List<string>();

            if (!save.seenTooltipPanelTags.Contains(_panelTag))
                save.seenTooltipPanelTags.Add(_panelTag);

            SaveSystem.Save(save);
            Hide();
        }

        /// <summary>
        /// Hides the tooltip panel immediately without persisting anything.
        /// Called when the panel has already been seen, the tag is empty, or the
        /// player dismisses the tooltip.
        /// </summary>
        public void Hide()
        {
            _tooltipPanel?.SetActive(false);
        }
    }
}
