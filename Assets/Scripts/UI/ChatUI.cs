using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Scroll-panel chat UI for the in-room chat channel.
    ///
    /// ── Architecture constraints ────────────────────────────────────────────────
    ///   - BattleRobots.UI may reference BattleRobots.Core but NEVER BattleRobots.Physics.
    ///   - No heap allocations in Update (this class has no Update).
    ///   - Messages are received via <see cref="AppendMessage"/> — wire this via a
    ///     <see cref="StringGameEventListener"/> on the same GameObject, pointing at the
    ///     <c>_onMessageReceived</c> channel on the <see cref="ChatSO"/>.
    ///
    /// ── Scene wiring instructions ───────────────────────────────────────────────
    ///   1. Add this MonoBehaviour to a ChatPanel GameObject in your Lobby/Match UI.
    ///   2. Assign _scrollContent (a Transform inside a ScrollRect content).
    ///   3. Assign _messagePrefab (a Text-based row prefab to instantiate per message).
    ///   4. Assign _inputField (InputField where the player types).
    ///   5. Assign _sendButton (Button; wire OnClick to HandleSend).
    ///   6. Assign _bridge (NetworkEventBridge) so send calls reach the adapter.
    ///   7. Set _localPlayerName (e.g. from SettingsSO or a runtime session field).
    ///   8. On the same GO, add a StringGameEventListener:
    ///        Event = ChatSO._onMessageReceived SO asset
    ///        Response = ChatUI.AppendMessage
    ///   9. Optionally assign _chatSO so OnEnable repopulates history from the ring buffer.
    /// </summary>
    public sealed class ChatUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Layout")]
        [Tooltip("Transform that acts as the scroll view's Content area. " +
                 "New message Text rows are parented here.")]
        [SerializeField] private Transform _scrollContent;

        [Tooltip("Prefab with a Text (or TMP_Text) component used for each chat row.")]
        [SerializeField] private Text _messagePrefab;

        [Header("Input")]
        [SerializeField] private InputField _inputField;
        [SerializeField] private Button     _sendButton;

        [Header("Network")]
        [Tooltip("Bridge used to broadcast the message via the adapter.")]
        [SerializeField] private NetworkEventBridge _bridge;

        [Tooltip("Display name sent as the sender label for outgoing messages.")]
        [SerializeField] private string _localPlayerName = "Player";

        [Header("History")]
        [Tooltip("(Optional) ChatSO whose ring-buffer is used to repopulate the panel " +
                 "on OnEnable (e.g. after a scene reload). Leave unassigned to skip history replay.")]
        [SerializeField] private ChatSO _chatSO;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            if (_sendButton != null)
                _sendButton.onClick.AddListener(HandleSend);
        }

        private void OnDestroy()
        {
            if (_sendButton != null)
                _sendButton.onClick.RemoveListener(HandleSend);
        }

        private void OnEnable()
        {
            // Repopulate from ring-buffer history so the player sees past messages
            // when the panel is first shown or re-enabled.
            if (_chatSO == null) return;

            var history = _chatSO.GetMessages();
            for (int i = 0; i < history.Count; i++)
                SpawnRow(history[i]);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Append a pre-formatted message to the scroll list.
        ///
        /// Wire this via a <see cref="StringGameEventListener"/> Response, or call
        /// directly from test code.
        ///
        /// Null or empty messages are silently ignored.
        /// </summary>
        public void AppendMessage(string formattedMessage)
        {
            if (string.IsNullOrEmpty(formattedMessage)) return;
            SpawnRow(formattedMessage);
        }

        /// <summary>
        /// Read the current InputField text and broadcast it via the bridge.
        /// Clears the input field on success.
        ///
        /// Wire to the send button's OnClick, or call from tests.
        /// </summary>
        public void HandleSend()
        {
            if (_inputField == null) return;

            string text = _inputField.text;
            if (string.IsNullOrWhiteSpace(text)) return;

            if (_bridge != null)
                _bridge.SendChat(_localPlayerName, text);

            _inputField.text = string.Empty;
            _inputField.ActivateInputField(); // keep focus for rapid typing
        }

        /// <summary>
        /// Remove all message rows from the scroll content.
        /// Does NOT clear the <see cref="ChatSO"/> ring buffer — call
        /// <see cref="ChatSO.Clear"/> separately if needed.
        /// </summary>
        public void ClearPanel()
        {
            if (_scrollContent == null) return;

            for (int i = _scrollContent.childCount - 1; i >= 0; i--)
                Object.Destroy(_scrollContent.GetChild(i).gameObject);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void SpawnRow(string text)
        {
            if (_messagePrefab == null || _scrollContent == null) return;

            Text row = Instantiate(_messagePrefab, _scrollContent);
            row.text = text;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_scrollContent == null)
                Debug.LogWarning("[ChatUI] _scrollContent not assigned — messages will not be displayed.", this);
            if (_messagePrefab == null)
                Debug.LogWarning("[ChatUI] _messagePrefab not assigned — message rows cannot be spawned.", this);
            if (_bridge == null)
                Debug.LogWarning("[ChatUI] _bridge (NetworkEventBridge) not assigned — sending will be a no-op.", this);
        }
#endif
    }
}
