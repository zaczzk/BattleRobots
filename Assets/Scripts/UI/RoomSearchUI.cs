using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// InputField + clear button that filters the room browser in real time.
    ///
    /// As the player types, <see cref="RoomListUI.ApplyFilter"/> is called with
    /// the current text as a prefix.  The list rebuilds immediately — no Update
    /// polling is needed.  Pressing the clear button (or hiding this panel) resets
    /// the filter so the next panel open shows all rooms.
    ///
    /// ARCHITECTURE RULES:
    ///   • BattleRobots.UI namespace — no Physics references.
    ///   • No Update / FixedUpdate defined (purely event-driven).
    ///   • All listener wiring is AddListener / RemoveListener — no anonymous lambdas
    ///     to prevent accidental leaks across domain reloads.
    ///
    /// Inspector wiring:
    ///   □ _searchInput  → InputField (the text field the player types into)
    ///   □ _clearButton  → Button that resets the search text and filter
    ///   □ _roomListUI   → RoomListUI MonoBehaviour on the room browser panel
    /// </summary>
    [AddComponentMenu("BattleRobots/UI/Room Search UI")]
    public sealed class RoomSearchUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Input")]
        [Tooltip("InputField the player types into. " +
                 "onValueChanged drives RoomListUI.ApplyFilter in real time.")]
        [SerializeField] private InputField _searchInput;

        [Tooltip("Button that clears the InputField text and resets the filter to show all rooms.")]
        [SerializeField] private Button _clearButton;

        [Header("Target")]
        [Tooltip("RoomListUI to filter. ApplyFilter is called with the current " +
                 "InputField text whenever the text changes or the clear button is pressed.")]
        [SerializeField] private RoomListUI _roomListUI;

        // ── Testable state ────────────────────────────────────────────────────

        /// <summary>
        /// The prefix currently applied to the room list filter.
        /// Exposed for EditMode testing; not intended for runtime mutation.
        /// </summary>
        public string CurrentPrefix => _searchInput != null ? _searchInput.text : string.Empty;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            if (_searchInput != null)
                _searchInput.onValueChanged.AddListener(OnSearchChanged);

            if (_clearButton != null)
                _clearButton.onClick.AddListener(OnClearClicked);
        }

        private void OnDisable()
        {
            // Reset the filter when this panel is hidden so subsequent opens
            // start with an empty search and the full room list visible.
            ClearFilter();
        }

        private void OnDestroy()
        {
            if (_searchInput != null)
                _searchInput.onValueChanged.RemoveListener(OnSearchChanged);

            if (_clearButton != null)
                _clearButton.onClick.RemoveListener(OnClearClicked);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Clears the InputField text and resets the filter to show all rooms.
        /// May be called externally (e.g., when the room-list panel is shown).
        /// </summary>
        public void ClearFilter()
        {
            if (_searchInput != null)
                _searchInput.SetTextWithoutNotify(string.Empty);

            _roomListUI?.ApplyFilter(string.Empty);
        }

        // ── Private handlers ──────────────────────────────────────────────────

        private void OnSearchChanged(string prefix)
        {
            _roomListUI?.ApplyFilter(prefix);
        }

        private void OnClearClicked()
        {
            ClearFilter();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_searchInput == null)
                Debug.LogWarning("[RoomSearchUI] No InputField assigned.", this);
            if (_roomListUI == null)
                Debug.LogWarning("[RoomSearchUI] No RoomListUI assigned.", this);
        }
#endif
    }
}
