using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// A single row in the key-rebind panel.
    /// Displays the action name and currently assigned key; provides a "Rebind" button.
    ///
    /// Scene setup:
    ///   - Place in the rebind panel as a child of the row container.
    ///   - Assign _actionLabel, _keyLabel, and _rebindButton in Inspector.
    ///   - Call <see cref="Setup"/> from <see cref="SettingsUI"/> after instantiation.
    ///
    /// Architecture:
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - All interaction routed through the callback — SettingsUI owns rebind state.
    /// </summary>
    public sealed class RebindRowUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Label displaying the action name (e.g. 'Forward').")]
        [SerializeField] private Text _actionLabel;

        [Tooltip("Label displaying the currently assigned key (e.g. 'W').")]
        [SerializeField] private Text _keyLabel;

        [Tooltip("Button the player clicks to begin key capture for this action.")]
        [SerializeField] private Button _rebindButton;

        // ── Runtime state ─────────────────────────────────────────────────────

        private string _actionName;
        private Action<string> _onRebindClicked;

        /// <summary>The logical action name this row represents (e.g. "Forward").</summary>
        public string ActionName => _actionName;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Initialises this row with action data and wires the rebind button.
        /// Call once after instantiation / pool reuse.
        /// </summary>
        /// <param name="actionName">Logical action name.</param>
        /// <param name="currentKey">The currently bound KeyCode (displayed as its name).</param>
        /// <param name="onRebindClicked">
        ///   Callback invoked when the player clicks "Rebind".
        ///   Receives the <paramref name="actionName"/> so SettingsUI knows which action to recapture.
        /// </param>
        public void Setup(string actionName, KeyCode currentKey, Action<string> onRebindClicked)
        {
            _actionName       = actionName;
            _onRebindClicked  = onRebindClicked;

            if (_actionLabel != null) _actionLabel.text = actionName;
            UpdateKeyDisplay(currentKey);

            if (_rebindButton != null)
                _rebindButton.onClick.AddListener(OnRebindButtonClicked);
        }

        /// <summary>Updates the displayed key name without re-wiring the button.</summary>
        public void UpdateKeyDisplay(KeyCode key)
        {
            if (_keyLabel != null)
                _keyLabel.text = key == KeyCode.None ? "—" : key.ToString();
        }

        /// <summary>Enables or disables the rebind button (used to lock rows during capture).</summary>
        public void SetInteractable(bool interactable)
        {
            if (_rebindButton != null)
                _rebindButton.interactable = interactable;
        }

        private void OnDestroy()
        {
            if (_rebindButton != null)
                _rebindButton.onClick.RemoveListener(OnRebindButtonClicked);
        }

        // ── Private ───────────────────────────────────────────────────────────

        private void OnRebindButtonClicked() => _onRebindClicked?.Invoke(_actionName);
    }
}
