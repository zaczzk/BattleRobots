using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Small HUD badge showing the current network connection state.
    ///
    /// The badge consists of an optional coloured icon (<see cref="Image"/>) and an
    /// optional short text label. Both update only in response to
    /// <see cref="NetworkSessionSO"/> event channels — no Update / FixedUpdate,
    /// zero per-frame allocations.
    ///
    /// Colour convention:
    ///   Disconnected  → grey
    ///   Connecting    → yellow
    ///   Connected     → green
    ///   In Match      → green (brighter / distinct tint if desired)
    ///   Reconnecting  → orange
    ///
    /// Inspector Wiring:
    ///   □ _badgeIcon          → Image component (optional)
    ///   □ _stateLabel         → Text component (optional)
    ///   □ Colour fields       → tweak per project palette
    ///
    ///   Add one VoidGameEventListener per channel on the same GameObject:
    ///   □ onDisconnected  → ConnectionBadgeUI.OnDisconnected()
    ///   □ onConnecting    → ConnectionBadgeUI.OnConnecting()
    ///   □ onConnected     → ConnectionBadgeUI.OnConnected()
    ///   □ onMatchJoined   → ConnectionBadgeUI.OnMatchJoined()
    ///   □ onReconnecting  → ConnectionBadgeUI.OnReconnecting()
    ///
    /// BattleRobots.UI namespace — no Physics references.
    /// </summary>
    public sealed class ConnectionBadgeUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Visual Elements")]
        [Tooltip("Image used as the coloured connection indicator dot/icon.")]
        [SerializeField] private Image _badgeIcon;

        [Tooltip("Short text label next to the badge (e.g. 'Online'). Optional.")]
        [SerializeField] private Text _stateLabel;

        [Header("Colours")]
        [SerializeField] private Color _disconnectedColor = new Color(0.55f, 0.55f, 0.55f, 1f); // grey
        [SerializeField] private Color _connectingColor   = new Color(1.00f, 0.85f, 0.10f, 1f); // yellow
        [SerializeField] private Color _connectedColor    = new Color(0.20f, 0.85f, 0.30f, 1f); // green
        [SerializeField] private Color _inMatchColor      = new Color(0.10f, 0.70f, 0.20f, 1f); // darker green
        [SerializeField] private Color _reconnectingColor = new Color(1.00f, 0.50f, 0.10f, 1f); // orange

        [Header("Labels")]
        [SerializeField] private string _disconnectedText  = "Offline";
        [SerializeField] private string _connectingText    = "Connecting\u2026";
        [SerializeField] private string _connectedText     = "Online";
        [SerializeField] private string _inMatchText       = "In Match";
        [SerializeField] private string _reconnectingText  = "Reconnecting\u2026";

        // ── Runtime state ─────────────────────────────────────────────────────

        /// <summary>The connection state most recently reflected by this badge.</summary>
        public NetworkConnectionState CurrentState { get; private set; } =
            NetworkConnectionState.Disconnected;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            // Start in the disconnected visual state.
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

            Color  color = ColorFor(state);
            string text  = TextFor(state);

            if (_badgeIcon  != null) _badgeIcon.color  = color;
            if (_stateLabel != null) _stateLabel.text  = text;
        }

        private Color ColorFor(NetworkConnectionState state)
        {
            switch (state)
            {
                case NetworkConnectionState.Connecting:    return _connectingColor;
                case NetworkConnectionState.Connected:     return _connectedColor;
                case NetworkConnectionState.InMatch:       return _inMatchColor;
                case NetworkConnectionState.Reconnecting:  return _reconnectingColor;
                default:                                   return _disconnectedColor;
            }
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
