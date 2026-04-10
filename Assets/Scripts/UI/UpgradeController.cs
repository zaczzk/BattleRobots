using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Drives the per-part upgrade panel: shows current tier as star icons,
    /// displays the next upgrade cost, and wires the upgrade button.
    ///
    /// ── Usage ────────────────────────────────────────────────────────────────
    ///   1. Call <see cref="Setup"/> with the part to display.
    ///   2. The controller shows the tier, cost, and upgrade button state.
    ///   3. Wires to <c>_onUpgradesChanged</c> so the panel auto-refreshes
    ///      when any upgrade is applied (not just from this panel).
    ///
    /// ── Lifecycle notes ──────────────────────────────────────────────────────
    ///   Subscribes the cached refresh delegate on OnEnable; unsubscribes on
    ///   OnDisable — safe for panels that are shown/hidden without being destroyed.
    ///   No heap allocations after Awake (cached delegate, string.Format avoided).
    ///
    /// ── Architecture ─────────────────────────────────────────────────────────
    ///   BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   No Update / FixedUpdate — purely event-driven.
    ///
    /// ── Scene wiring ─────────────────────────────────────────────────────────
    ///   • _upgradeManager  → UpgradeManager MB in the scene
    ///   • _upgradeConfig   → same PartUpgradeConfig SO as UpgradeManager
    ///   • _onUpgradesChanged → same VoidGameEvent as PlayerPartUpgrades._onUpgradesChanged
    ///   • _tierLabel        (optional) Text — e.g. "★★☆" or "MAX"
    ///   • _costLabel        (optional) Text — e.g. "Cost: 250" or "MAX"
    ///   • _upgradeButton    (optional) Button — triggers UpgradePart on click
    ///   Call <see cref="Setup"/> from a parent list controller (e.g. after
    ///   LoadoutBuilderController builds a row) to set the active part.
    /// </summary>
    public sealed class UpgradeController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Dependencies")]
        [SerializeField] private UpgradeManager  _upgradeManager;
        [SerializeField] private PartUpgradeConfig _upgradeConfig;

        [Header("Event Channels — In")]
        [Tooltip("Subscribed for automatic refresh when any upgrade changes. " +
                 "Assign the same SO as PlayerPartUpgrades._onUpgradesChanged.")]
        [SerializeField] private VoidGameEvent _onUpgradesChanged;

        [Header("UI Refs (optional)")]
        [SerializeField] private Text   _tierLabel;
        [SerializeField] private Text   _costLabel;
        [SerializeField] private Button _upgradeButton;

        // ── Runtime state ─────────────────────────────────────────────────────

        private PartDefinition _currentPart;
        private System.Action  _cachedRefresh;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            _cachedRefresh = Refresh;
            if (_upgradeButton != null)
                _upgradeButton.onClick.AddListener(OnUpgradeClicked);
        }

        private void OnEnable()
        {
            _onUpgradesChanged?.RegisterCallback(_cachedRefresh);
            Refresh();
        }

        private void OnDisable()
        {
            _onUpgradesChanged?.UnregisterCallback(_cachedRefresh);
        }

        private void OnDestroy()
        {
            if (_upgradeButton != null)
                _upgradeButton.onClick.RemoveListener(OnUpgradeClicked);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Sets the part this controller displays and refreshes all UI widgets.
        /// Safe to call with null (clears the display and disables the button).
        /// </summary>
        public void Setup(PartDefinition part)
        {
            _currentPart = part;
            Refresh();
        }

        // ── Private ───────────────────────────────────────────────────────────

        private void OnUpgradeClicked()
        {
            if (_currentPart == null || _upgradeManager == null) return;
            _upgradeManager.UpgradePart(_currentPart);
            // Refresh will be triggered by _onUpgradesChanged event after success.
        }

        private void Refresh()
        {
            bool validContext = _currentPart != null
                             && _upgradeConfig != null
                             && _upgradeManager != null;

            if (!validContext)
            {
                if (_tierLabel     != null) _tierLabel.text     = string.Empty;
                if (_costLabel     != null) _costLabel.text     = string.Empty;
                if (_upgradeButton != null) _upgradeButton.interactable = false;
                return;
            }

            int  currentTier = _upgradeManager.GetCurrentTier(_currentPart);
            int  maxTier     = _upgradeConfig.MaxTier;
            bool atMax       = currentTier >= maxTier;
            int  nextCost    = _upgradeManager.GetNextUpgradeCost(_currentPart);

            if (_tierLabel != null)
                _tierLabel.text = atMax ? "MAX" : FormatTierStars(currentTier, maxTier);

            if (_costLabel != null)
                _costLabel.text = atMax ? "MAX" : $"Cost: {nextCost}";

            if (_upgradeButton != null)
                _upgradeButton.interactable = !atMax;
        }

        /// <summary>
        /// Formats a tier as filled/empty star characters, e.g. "★★☆" for tier 2 of 3.
        /// Internal so tests can reach it via reflection without a full scene.
        /// </summary>
        internal static string FormatTierStars(int tier, int maxTier)
        {
            if (maxTier <= 0) return string.Empty;
            char[] stars = new char[maxTier];
            for (int i = 0; i < maxTier; i++)
                stars[i] = i < tier ? '\u2605' : '\u2606'; // ★ / ☆
            return new string(stars);
        }
    }
}
