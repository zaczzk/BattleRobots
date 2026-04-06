using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Network lobby screen — lets the player choose Host or Join, enter a room
    /// code, connect, and see live status feedback.
    ///
    /// Architecture constraints:
    ///   • <c>BattleRobots.UI</c> namespace — no Physics references.
    ///   • All network operations are delegated to <see cref="NetworkEventBridge"/>
    ///     (Core) via public method calls; this class never touches
    ///     <see cref="INetworkAdapter"/> directly.
    ///   • State is read from <see cref="NetworkSessionSO"/> (Core) — UI never
    ///     mutates the SO; it only calls methods on the bridge.
    ///   • Reacts to SO event channels via <see cref="VoidGameEventListener"/>
    ///     components on the same GameObject — no per-frame polling.
    ///   • No Update / FixedUpdate — zero per-frame cost.
    ///
    /// Inspector wiring checklist:
    ///   □ _session            → NetworkSessionSO asset (shared with NetworkEventBridge)
    ///   □ _bridge             → NetworkEventBridge MonoBehaviour in scene
    ///   □ _hostButton         → Button (selects Host role)
    ///   □ _joinButton         → Button (selects Join / Client role)
    ///   □ _connectButton      → Button ("Connect" / "Start")
    ///   □ _disconnectButton   → Button ("Cancel" / "Disconnect")
    ///   □ _roomCodeInput      → InputField (4-char room code)
    ///   □ _statusLabel        → Text (shows connection state or errors)
    ///   □ _hostButtonImage    → Image on Host button (role highlight)
    ///   □ _joinButtonImage    → Image on Join button (role highlight)
    ///   □ _selectedRoleColor  → Color for the active role button
    ///   □ _normalRoleColor    → Color for inactive role button
    ///
    ///   VoidGameEventListener (same GO) wiring:
    ///   □ onConnecting  → NetworkLobbyUI.OnConnecting()
    ///   □ onConnected   → NetworkLobbyUI.OnConnected()
    ///   □ onMatchJoined → NetworkLobbyUI.OnMatchJoined()
    ///   □ onDisconnected → NetworkLobbyUI.OnDisconnected()
    /// </summary>
    public sealed class NetworkLobbyUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("Shared NetworkSessionSO — reads state; never mutates.")]
        [SerializeField] private NetworkSessionSO _session;

        [Tooltip("NetworkEventBridge MonoBehaviour. All connect/host/join calls go here.")]
        [SerializeField] private NetworkEventBridge _bridge;

        [Header("Role Buttons")]
        [Tooltip("Button that selects Host mode.")]
        [SerializeField] private Button _hostButton;

        [Tooltip("Button that selects Client / Join mode.")]
        [SerializeField] private Button _joinButton;

        [Tooltip("Image on the Host button used for role highlight.")]
        [SerializeField] private Image _hostButtonImage;

        [Tooltip("Image on the Join button used for role highlight.")]
        [SerializeField] private Image _joinButtonImage;

        [Header("Role Highlight Colours")]
        [SerializeField] private Color _selectedRoleColor = new Color(0.25f, 0.75f, 0.25f, 1f);
        [SerializeField] private Color _normalRoleColor   = Color.white;

        [Header("Room Code")]
        [Tooltip("InputField for entering (client) or displaying (host) the room code.")]
        [SerializeField] private InputField _roomCodeInput;

        [Header("Action Buttons")]
        [Tooltip("Button that triggers connect → host or connect → join.")]
        [SerializeField] private Button _connectButton;

        [Tooltip("Button that cancels or disconnects.")]
        [SerializeField] private Button _disconnectButton;

        [Header("Status")]
        [Tooltip("Label that shows the current connection state / error messages.")]
        [SerializeField] private Text _statusLabel;

        // ── Runtime state ─────────────────────────────────────────────────────

        // Role currently chosen in the UI (may differ from session role until Connect).
        private NetworkRole _pendingRole = NetworkRole.Host;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            // Wire button listeners once — allocation acceptable outside FixedUpdate.
            if (_hostButton      != null) _hostButton.onClick.AddListener(OnHostButtonClicked);
            if (_joinButton      != null) _joinButton.onClick.AddListener(OnJoinButtonClicked);
            if (_connectButton   != null) _connectButton.onClick.AddListener(OnConnectClicked);
            if (_disconnectButton != null) _disconnectButton.onClick.AddListener(OnDisconnectClicked);
        }

        private void OnEnable()
        {
            // Reset to default role each time the panel opens.
            _pendingRole = NetworkRole.Host;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_hostButton       != null) _hostButton.onClick.RemoveListener(OnHostButtonClicked);
            if (_joinButton       != null) _joinButton.onClick.RemoveListener(OnJoinButtonClicked);
            if (_connectButton    != null) _connectButton.onClick.RemoveListener(OnConnectClicked);
            if (_disconnectButton != null) _disconnectButton.onClick.RemoveListener(OnDisconnectClicked);
        }

        // ── Button Callbacks ──────────────────────────────────────────────────

        private void OnHostButtonClicked()
        {
            _pendingRole = NetworkRole.Host;
            Refresh();
        }

        private void OnJoinButtonClicked()
        {
            _pendingRole = NetworkRole.Client;
            Refresh();
        }

        private void OnConnectClicked()
        {
            if (_bridge == null)
            {
                SetStatus("No NetworkEventBridge assigned.");
                return;
            }

            // Kick off the connect → (host|join) pipeline.
            // The bridge will call adapter.Connect(); on callback it will call
            // BeginHost/BeginJoin once SetConnected fires.
            if (_pendingRole == NetworkRole.Host)
            {
                // Generate a 4-char room code if none is set.
                string code = GetRoomCode();
                if (string.IsNullOrEmpty(code))
                    code = GenerateRoomCode();

                if (_roomCodeInput != null)
                    _roomCodeInput.text = code;

                _bridge.BeginConnectAsHost();

                // After connected, automatically host the room.
                // BeginHost is safe to call; bridge guards the Connected state check.
                // We call it after a frame via the onConnected event channel (wired in Inspector).
                // Store pending code for use in OnConnected callback.
                _pendingRoomCode = code;
            }
            else
            {
                string code = GetRoomCode();
                if (string.IsNullOrEmpty(code))
                {
                    SetStatus("Enter a room code to join.");
                    return;
                }

                _bridge.BeginConnectAsClient();
                _pendingRoomCode = code;
            }

            Refresh();
        }

        private void OnDisconnectClicked()
        {
            _pendingRoomCode = string.Empty;
            _bridge?.BeginDisconnect();
            Refresh();
        }

        // ── SO Event Channel callbacks (wired via VoidGameEventListener in Inspector)

        /// <summary>Called via VoidGameEventListener when the session enters Connecting.</summary>
        public void OnConnecting()
        {
            SetStatus("Connecting…");
            Refresh();
        }

        /// <summary>
        /// Called via VoidGameEventListener when the session enters Connected.
        /// Automatically proceeds to host or join the pending room.
        /// </summary>
        public void OnConnected()
        {
            SetStatus("Connected. Entering room…");
            Refresh();

            if (_bridge == null) return;

            if (_pendingRole == NetworkRole.Host)
                _bridge.BeginHost(_pendingRoomCode);
            else
                _bridge.BeginJoin(_pendingRoomCode);
        }

        /// <summary>Called via VoidGameEventListener when the session enters InMatch.</summary>
        public void OnMatchJoined()
        {
            string code = _session != null ? _session.RoomCode : _pendingRoomCode;
            SetStatus($"In room '{code}'. Waiting for match…");
            Refresh();
        }

        /// <summary>Called via VoidGameEventListener when the session disconnects.</summary>
        public void OnDisconnected()
        {
            _pendingRoomCode = string.Empty;
            SetStatus("Disconnected.");
            Refresh();
        }

        // ── UI Refresh ────────────────────────────────────────────────────────

        /// <summary>
        /// Sync all UI elements to the current session + pending-role state.
        /// Called on button clicks and event callbacks.
        /// Zero heap allocation — no string concatenation in hot path.
        /// </summary>
        private void Refresh()
        {
            bool isIdle = _session == null ||
                          _session.ConnectionState == NetworkConnectionState.Disconnected;

            bool isBusy = _session != null &&
                          (_session.ConnectionState == NetworkConnectionState.Connecting ||
                           _session.ConnectionState == NetworkConnectionState.InMatch);

            // Role buttons — only interactive when not connected.
            if (_hostButton != null) _hostButton.interactable = isIdle;
            if (_joinButton != null) _joinButton.interactable = isIdle;

            // Connect button — interactive when idle and a role is chosen.
            if (_connectButton != null)
                _connectButton.interactable = isIdle;

            // Disconnect button — interactive when busy or connected.
            if (_disconnectButton != null)
                _disconnectButton.interactable = !isIdle;

            // Room code input — editable when idle.
            if (_roomCodeInput != null)
                _roomCodeInput.interactable = isIdle;

            // Role highlights.
            SetButtonHighlight(_hostButtonImage, _pendingRole == NetworkRole.Host);
            SetButtonHighlight(_joinButtonImage, _pendingRole == NetworkRole.Client);

            // Status label — updated by event callbacks; just keep it in sync.
            if (_statusLabel != null && _session != null && string.IsNullOrEmpty(_statusLabel.text))
                SetStatus(_session.ConnectionState.ToString());
        }

        private void SetButtonHighlight(Image image, bool isSelected)
        {
            if (image == null) return;
            image.color = isSelected ? _selectedRoleColor : _normalRoleColor;
        }

        private void SetStatus(string message)
        {
            if (_statusLabel != null)
                _statusLabel.text = message;
        }

        private string GetRoomCode() =>
            _roomCodeInput != null
                ? _roomCodeInput.text.Trim().ToUpperInvariant()
                : string.Empty;

        // ── Helpers ───────────────────────────────────────────────────────────

        // Pending room code bridged between OnConnectClicked and OnConnected callback.
        private string _pendingRoomCode = string.Empty;

        /// <summary>
        /// Generates a pseudo-random 4-letter room code from A–Z.
        /// Allocation occurs once per host session — not on the hot path.
        /// </summary>
        private static string GenerateRoomCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ"; // omit I, O to avoid confusion
            char[] code = new char[4];
            for (int i = 0; i < 4; i++)
                code[i] = chars[UnityEngine.Random.Range(0, chars.Length)];
            return new string(code);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_session == null)
                Debug.LogWarning("[NetworkLobbyUI] NetworkSessionSO is not assigned.", this);
            if (_bridge == null)
                Debug.LogWarning("[NetworkLobbyUI] NetworkEventBridge is not assigned.", this);
        }
#endif
    }
}
