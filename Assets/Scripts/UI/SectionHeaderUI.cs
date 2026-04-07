using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Interactive section header for a ping-tier group inside the room browser.
    ///
    /// Clicking the header toggles the group between expanded and collapsed states.
    /// Visual state is reflected via an expand/collapse indicator Text (▼ / ▶).
    /// The caller receives the toggled <see cref="PingTier"/> via an <c>Action</c>
    /// callback so that <see cref="RoomListUI"/> can rebuild the row list.
    ///
    /// ARCHITECTURE RULES:
    ///   • BattleRobots.UI namespace — no Physics references.
    ///   • No Update / FixedUpdate.
    ///   • All interaction handled via Button.onClick (no per-frame polling).
    ///
    /// Inspector wiring:
    ///   _label          → Text child showing the tier name (e.g. "Excellent (≤80 ms)")
    ///   _expandIndicator → Text child showing "▼" (expanded) or "▶" (collapsed)
    ///
    /// Populated at runtime via <see cref="Setup"/>.
    /// </summary>
    [RequireComponent(typeof(Button))]
    [DisallowMultipleComponent]
    public sealed class SectionHeaderUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Text component displaying the tier label (e.g. \"Excellent (≤80 ms)\").")]
        [SerializeField] private Text _label;

        [Tooltip("Text component displaying the collapse indicator: ▼ = expanded, ▶ = collapsed.")]
        [SerializeField] private Text _expandIndicator;

        // ── Runtime state ─────────────────────────────────────────────────────

        private Button _button;
        private Action<PingTier> _onToggle;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>The ping tier this header represents.</summary>
        public PingTier Tier { get; private set; }

        /// <summary>
        /// True when the section is collapsed (rows in this tier are hidden).
        /// False when the section is expanded (rows are visible).
        /// </summary>
        public bool IsCollapsed { get; private set; }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (_button != null)
                _button.onClick.AddListener(HandleClicked);
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(HandleClicked);
        }

        // ── Setup ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Configures the header for display. Call once after instantiation.
        /// </summary>
        /// <param name="label">Human-readable tier name, e.g. "Excellent (≤80 ms)".</param>
        /// <param name="tier">The <see cref="PingTier"/> this header represents.</param>
        /// <param name="initiallyCollapsed">
        /// Whether the group should start collapsed.
        /// Should match the current value of <see cref="RoomListUI.IsTierCollapsed"/> for this tier.
        /// </param>
        /// <param name="onToggle">
        /// Callback invoked when the player clicks the header.
        /// Receives the <see cref="PingTier"/> that was toggled.
        /// May be null (no action taken on click).
        /// </param>
        public void Setup(string label, PingTier tier, bool initiallyCollapsed, Action<PingTier> onToggle)
        {
            Tier = tier;
            IsCollapsed = initiallyCollapsed;
            _onToggle = onToggle;

            if (_label != null)
                _label.text = label;

            RefreshIndicator();
        }

        // ── Interaction ───────────────────────────────────────────────────────

        /// <summary>
        /// Toggles the collapsed state and fires <c>_onToggle</c>.
        /// Called by the Button's onClick event; also accessible in tests.
        /// </summary>
        public void HandleClicked()
        {
            IsCollapsed = !IsCollapsed;
            RefreshIndicator();
            _onToggle?.Invoke(Tier);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void RefreshIndicator()
        {
            if (_expandIndicator != null)
                _expandIndicator.text = IsCollapsed ? "\u25B6" : "\u25BC"; // ▶ or ▼
        }
    }
}
