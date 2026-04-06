using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Overlay panel shown when the network session enters the
    /// <see cref="NetworkConnectionState.Reconnecting"/> state.
    ///
    /// Shows an attempt counter ("Attempt N of Max"), a Cancel button
    /// (calls <see cref="NetworkEventBridge.BeginDisconnect"/>), and a Retry
    /// button (calls <see cref="NetworkEventBridge.BeginConnectAsClient"/> or
    /// <see cref="NetworkEventBridge.BeginConnectAsHost"/> according to the
    /// stored role). The Retry button is disabled when
    /// <see cref="NetworkSessionSO.ReconnectAttempts"/> reaches
    /// <see cref="NetworkSessionSO.MaxReconnectAttempts"/>.
    ///
    /// ── Inspector Wiring ────────────────────────────────────────────────────
    ///   □ _session            → NetworkSessionSO asset
    ///   □ _bridge             → NetworkEventBridge MonoBehaviour
    ///   □ _reconnectPanel     → root GameObject of the overlay
    ///   □ _attemptLabel       → Text showing "Attempt N of Max"
    ///   □ _cancelButton       → Button → BeginDisconnect
    ///   □ _retryButton        → Button → BeginConnect (re-uses last role)
    ///
    ///   VoidGameEventListener (same GO) wiring:
    ///   □ onReconnecting  → ReconnectUI.OnReconnecting()
    ///   □ onConnected     → ReconnectUI.OnConnectionRestored()
    ///   □ onDisconnected  → ReconnectUI.OnConnectionRestored()
    ///
    /// ── Architecture Notes ──────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — no Physics references.
    ///   • No Update / FixedUpdate — zero per-frame cost.
    ///   • All network calls delegated to NetworkEventBridge (Core).
    /// </summary>
    public sealed class ReconnectUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("Shared NetworkSessionSO — reads state; never mutates.")]
        [SerializeField] private NetworkSessionSO _session;

        [Tooltip("NetworkEventBridge MonoBehaviour. Reconnect/Disconnect calls go here.")]
        [SerializeField] private NetworkEventBridge _bridge;

        [Header("Panel")]
        [Tooltip("Root GameObject of the reconnect overlay. Shown/hidden by this component.")]
        [SerializeField] private GameObject _reconnectPanel;

        [Tooltip("Label that shows the current attempt count, e.g. 'Attempt 2 of 3'.")]
        [SerializeField] private Text _attemptLabel;

        [Header("Buttons")]
        [Tooltip("Cancels reconnection and returns to Disconnected state.")]
        [SerializeField] private Button _cancelButton;

        [Tooltip("Triggers another connection attempt.")]
        [SerializeField] private Button _retryButton;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            // Wire buttons once — allocation outside hot path is acceptable.
            if (_cancelButton != null) _cancelButton.onClick.AddListener(OnCancelClicked);
            if (_retryButton  != null) _retryButton.onClick.AddListener(OnRetryClicked);

            // Start hidden.
            SetPanelVisible(false);
        }

        private void OnDestroy()
        {
            if (_cancelButton != null) _cancelButton.onClick.RemoveListener(OnCancelClicked);
            if (_retryButton  != null) _retryButton.onClick.RemoveListener(OnRetryClicked);
        }

        // ── SO Event Channel callbacks (wired via VoidGameEventListener in Inspector) ──

        /// <summary>
        /// Called by a VoidGameEventListener wired to the NetworkSessionSO
        /// <c>onReconnecting</c> channel. Shows the panel and refreshes state.
        /// </summary>
        public void OnReconnecting()
        {
            SetPanelVisible(true);
            RefreshAttemptLabel();
            RefreshRetryButton();
        }

        /// <summary>
        /// Called by VoidGameEventListeners wired to the <c>onConnected</c> and
        /// <c>onDisconnected</c> channels. Hides the panel in both cases.
        /// </summary>
        public void OnConnectionRestored()
        {
            SetPanelVisible(false);
        }

        // ── Button callbacks ──────────────────────────────────────────────────

        private void OnCancelClicked()
        {
            _bridge?.BeginDisconnect();
        }

        private void OnRetryClicked()
        {
            if (_bridge == null) return;

            // Re-connect using the same role the session last used.
            NetworkRole role = _session != null ? _session.Role : NetworkRole.Client;

            if (role == NetworkRole.Host)
                _bridge.BeginConnectAsHost();
            else
                _bridge.BeginConnectAsClient();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void SetPanelVisible(bool visible)
        {
            if (_reconnectPanel != null)
                _reconnectPanel.SetActive(visible);
        }

        /// <summary>
        /// Updates the attempt counter label. Pre-builds the string only when
        /// needed (in response to an event, not in Update).
        /// </summary>
        private void RefreshAttemptLabel()
        {
            if (_attemptLabel == null || _session == null) return;

            int  attempts = _session.ReconnectAttempts;
            int  max      = _session.MaxReconnectAttempts;

            _attemptLabel.text = max > 0
                ? $"Attempt {attempts} of {max}"
                : $"Attempt {attempts}";
        }

        /// <summary>
        /// Disables the Retry button when the maximum attempt count is reached.
        /// </summary>
        private void RefreshRetryButton()
        {
            if (_retryButton == null || _session == null) return;

            int max = _session.MaxReconnectAttempts;
            bool limitReached = max > 0 && _session.ReconnectAttempts >= max;
            _retryButton.interactable = !limitReached;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_session == null)
                Debug.LogWarning("[ReconnectUI] NetworkSessionSO is not assigned.", this);
            if (_bridge == null)
                Debug.LogWarning("[ReconnectUI] NetworkEventBridge is not assigned.", this);
        }
#endif
    }
}
