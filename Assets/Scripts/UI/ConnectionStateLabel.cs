using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Text label that maps the current <see cref="NetworkConnectionState"/> to a
    /// human-readable (and optionally localised) string.
    ///
    /// Updated exclusively through public callbacks wired to
    /// <see cref="NetworkSessionSO"/> event channels via
    /// <see cref="VoidGameEventListener"/> MonoBehaviours on the same GameObject.
    /// No Update / FixedUpdate — zero per-frame cost.
    ///
    /// The displayed strings are fully configurable from the Inspector so they can
    /// be replaced with localisation keys without touching code.
    ///
    /// Inspector Wiring:
    ///   □ _label              → Text component showing the state string
    ///
    ///   Add one VoidGameEventListener per channel on the same GameObject:
    ///   □ onDisconnected  → ConnectionStateLabel.OnDisconnected()
    ///   □ onConnecting    → ConnectionStateLabel.OnConnecting()
    ///   □ onConnected     → ConnectionStateLabel.OnConnected()
    ///   □ onMatchJoined   → ConnectionStateLabel.OnMatchJoined()
    ///   □ onReconnecting  → ConnectionStateLabel.OnReconnecting()
    ///
    /// BattleRobots.UI namespace — no Physics references.
    /// </summary>
    public sealed class ConnectionStateLabel : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Label")]
        [Tooltip("Text component that displays the connection state string.")]
        [SerializeField] private Text _label;

        [Header("State Strings")]
        [Tooltip("Displayed when disconnected from the network.")]
        [SerializeField] private string _disconnectedText  = "Offline";

        [Tooltip("Displayed while a connection attempt is in progress.")]
        [SerializeField] private string _connectingText    = "Connecting\u2026";

        [Tooltip("Displayed when connected to the lobby server.")]
        [SerializeField] private string _connectedText     = "Online";

        [Tooltip("Displayed while inside an active match room.")]
        [SerializeField] private string _inMatchText       = "In Match";

        [Tooltip("Displayed while a reconnection attempt is in progress.")]
        [SerializeField] private string _reconnectingText  = "Reconnecting\u2026";

        // ── Runtime state ─────────────────────────────────────────────────────

        /// <summary>The connection state most recently reflected by this label.</summary>
        public NetworkConnectionState CurrentState { get; private set; } =
            NetworkConnectionState.Disconnected;

        /// <summary>The text currently shown on the label.</summary>
        public string CurrentText { get; private set; }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            Apply(NetworkConnectionState.Disconnected);
        }

        // ── Public callbacks (wired via VoidGameEventListener in Inspector) ───

        /// <summary>Call via a VoidGameEventListener on <c>NetworkSessionSO.onDisconnected</c>.</summary>
        public void OnDisconnected()  => Apply(NetworkConnectionState.Disconnected);

        /// <summary>Call via a VoidGameEventListener on <c>NetworkSessionSO.onConnecting</c>.</summary>
        public void OnConnecting()    => Apply(NetworkConnectionState.Connecting);

        /// <summary>Call via a VoidGameEventListener on <c>NetworkSessionSO.onConnected</c>.</summary>
        public void OnConnected()     => Apply(NetworkConnectionState.Connected);

        /// <summary>Call via a VoidGameEventListener on <c>NetworkSessionSO.onMatchJoined</c>.</summary>
        public void OnMatchJoined()   => Apply(NetworkConnectionState.InMatch);

        /// <summary>Call via a VoidGameEventListener on <c>NetworkSessionSO.onReconnecting</c>.</summary>
        public void OnReconnecting()  => Apply(NetworkConnectionState.Reconnecting);

        // ── Internal ──────────────────────────────────────────────────────────

        private void Apply(NetworkConnectionState state)
        {
            CurrentState = state;
            CurrentText  = TextFor(state);

            if (_label != null)
                _label.text = CurrentText;
        }

        private string TextFor(NetworkConnectionState state)
        {
            switch (state)
            {
                case NetworkConnectionState.Connecting:    return _connectingText;
                case NetworkConnectionState.Connected:     return _connectedText;
                case NetworkConnectionState.InMatch:       return _inMatchText;
                case NetworkConnectionState.Reconnecting:  return _reconnectingText;
                default:                                   return _disconnectedText;
            }
        }
    }
}
