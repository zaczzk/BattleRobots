using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Network room browser UI. Rebuilds its list of <see cref="RoomEntryUI"/> rows
    /// whenever the room list changes.
    ///
    /// Rebuild is triggered by <see cref="OnRoomsUpdated"/>, which should be wired
    /// in the Inspector via a sibling <see cref="VoidGameEventListener"/> component
    /// listening to <see cref="RoomListSO"/>'s <c>_onRoomsUpdated</c> channel.
    ///
    /// When a player presses Join on a row, the join request is delegated to
    /// <see cref="NetworkEventBridge.BeginJoin(string)"/> — keeping all adapter
    /// calls out of the UI layer.
    ///
    /// ARCHITECTURE RULES:
    ///   • BattleRobots.UI namespace — no Physics references.
    ///   • No per-frame cost — no Update / FixedUpdate defined.
    ///   • All allocations occur during Rebuild (one per room entry); never in hot path.
    ///   • <see cref="RoomListSO"/> is read-only here; never mutated by this class.
    ///
    /// Inspector wiring checklist:
    ///   □ _roomList        → RoomListSO asset (shared with the network layer)
    ///   □ _bridge          → NetworkEventBridge MonoBehaviour in scene
    ///   □ _entryPrefab     → RoomEntryUI prefab to instantiate per room
    ///   □ _scrollContent   → Transform (Content of the ScrollRect) rows are parented to
    ///   □ _emptyStateLabel → Text shown when no rooms are available
    ///   □ _refreshButton   → (optional) Button that manually triggers a refresh
    ///
    ///   VoidGameEventListener (same GameObject) wiring:
    ///   □ onRoomsUpdated → RoomListUI.OnRoomsUpdated()
    /// </summary>
    public sealed class RoomListUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("RoomListSO asset managed by the network layer. Read-only from this class.")]
        [SerializeField] private RoomListSO _roomList;

        [Tooltip("NetworkEventBridge MonoBehaviour. Join calls are delegated here.")]
        [SerializeField] private NetworkEventBridge _bridge;

        [Header("Prefab")]
        [Tooltip("RoomEntryUI prefab instantiated for each room in the list.")]
        [SerializeField] private RoomEntryUI _entryPrefab;

        [Header("Layout")]
        [Tooltip("Parent Transform (Content of the ScrollRect) that row prefabs are added to.")]
        [SerializeField] private Transform _scrollContent;

        [Tooltip("Label shown when the room list is empty.")]
        [SerializeField] private Text _emptyStateLabel;

        [Header("Optional")]
        [Tooltip("Optional Refresh button that manually calls OnRoomsUpdated.")]
        [SerializeField] private Button _refreshButton;

        [Tooltip("When enabled, rooms that have reached capacity (IsFull) are hidden from the list.")]
        [SerializeField] private bool _filterFullRooms = false;

        [Tooltip("When enabled, private rooms (password-protected) are hidden from the list.")]
        [SerializeField] private bool _hidePrivateRooms = false;

        [Tooltip("(Optional) InputField where the player enters a password before joining " +
                 "a private room. When assigned the text value is forwarded to BeginJoin.")]
        [SerializeField] private InputField _passwordInputField;

        [Tooltip("(Optional) FavouriteRoomsSO asset. When assigned, each RoomEntryUI row " +
                 "receives it so the star/favourite button is visible and correctly wired. " +
                 "Leave null to hide favourite buttons on all rows.")]
        [SerializeField] private FavouriteRoomsSO _favouriteRoomsSO;

        // ── Runtime state ─────────────────────────────────────────────────────

        // Pool of active row instances; cleared and rebuilt on each Rebuild call.
        private readonly List<RoomEntryUI> _rows = new List<RoomEntryUI>();

        // Active search prefix. Empty string = show all rooms.
        private string _searchPrefix = string.Empty;

        // Active sort mode. Defaults to None (original network order).
        private RoomSortMode _sortMode = RoomSortMode.None;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            if (_refreshButton != null)
                _refreshButton.onClick.AddListener(OnRoomsUpdated);
        }

        private void OnEnable()
        {
            // Rebuild immediately when the panel opens so stale rows are never shown.
            Rebuild();
        }

        private void OnDestroy()
        {
            if (_refreshButton != null)
                _refreshButton.onClick.RemoveListener(OnRoomsUpdated);
        }

        // ── Public API (wired via VoidGameEventListener in Inspector) ─────────

        /// <summary>
        /// Called by a sibling <see cref="VoidGameEventListener"/> when
        /// <see cref="RoomListSO._onRoomsUpdated"/> fires.
        /// Tears down existing rows and instantiates fresh ones.
        /// </summary>
        public void OnRoomsUpdated()
        {
            Rebuild();
        }

        /// <summary>
        /// Applies a case-insensitive prefix filter to the room list and rebuilds.
        /// Pass <c>null</c> or an empty string to clear the filter and show all rooms.
        ///
        /// Called by <see cref="RoomSearchUI"/> whenever the InputField text changes.
        /// No Update cost — invoked only on user interaction.
        /// </summary>
        /// <param name="prefix">
        /// The leading characters to match against <see cref="RoomEntry.roomCode"/>.
        /// </param>
        public void ApplyFilter(string prefix)
        {
            _searchPrefix = prefix ?? string.Empty;
            Rebuild();
        }

        /// <summary>
        /// Changes the sort order of the displayed room list and rebuilds.
        /// The current search prefix is preserved across sort-mode changes.
        ///
        /// Called by <see cref="RoomSortUI"/> when the player selects a sort option.
        /// No Update cost — invoked only on user interaction.
        /// </summary>
        public void ApplySortMode(RoomSortMode mode)
        {
            _sortMode = mode;
            Rebuild();
        }

        // ── Internal ──────────────────────────────────────────────────────────

        /// <summary>
        /// Destroys all existing row instances and recreates them from the
        /// current <see cref="RoomListSO.Rooms"/> snapshot.
        /// </summary>
        private void Rebuild()
        {
            // Destroy previous rows.
            foreach (RoomEntryUI row in _rows)
            {
                if (row != null)
                    Destroy(row.gameObject);
            }
            _rows.Clear();

            // Apply search prefix filter + sort. Empty prefix returns the full list.
            IReadOnlyList<RoomEntry> rooms = _roomList != null
                ? _roomList.GetSortedFilteredRooms(_searchPrefix, _sortMode)
                : System.Array.Empty<RoomEntry>();

            bool hasData = rooms.Count > 0;

            // Empty-state label toggle.
            if (_emptyStateLabel != null)
                _emptyStateLabel.gameObject.SetActive(!hasData);

            if (!hasData || _entryPrefab == null || _scrollContent == null)
                return;

            for (int i = 0; i < rooms.Count; i++)
            {
                RoomEntry entry = rooms[i];

                // Skip rooms at capacity when the filter toggle is on.
                if (_filterFullRooms && entry.IsFull)
                    continue;

                // Skip private rooms when the filter toggle is on.
                if (_hidePrivateRooms && entry.isPrivate)
                    continue;

                RoomEntryUI row = Instantiate(_entryPrefab, _scrollContent);
                row.Setup(entry, HandleJoinRequested, _favouriteRoomsSO);
                _rows.Add(row);
            }
        }

        /// <summary>
        /// Callback passed into each <see cref="RoomEntryUI"/> row's Setup call.
        /// Forwards the join request — including any password entered in
        /// <c>_passwordInputField</c> — to <see cref="NetworkEventBridge.BeginJoin(string,string)"/>.
        /// </summary>
        private void HandleJoinRequested(string roomCode)
        {
            if (string.IsNullOrEmpty(roomCode))
            {
                Debug.LogWarning("[RoomListUI] HandleJoinRequested called with empty room code.", this);
                return;
            }

            if (_bridge == null)
            {
                Debug.LogWarning("[RoomListUI] No NetworkEventBridge assigned. Cannot join room.", this);
                return;
            }

            string password = _passwordInputField != null ? _passwordInputField.text : string.Empty;
            _bridge.BeginJoin(roomCode, password);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_roomList == null)
                Debug.LogWarning("[RoomListUI] RoomListSO is not assigned.", this);
            if (_bridge == null)
                Debug.LogWarning("[RoomListUI] NetworkEventBridge is not assigned.", this);
            if (_entryPrefab == null)
                Debug.LogWarning("[RoomListUI] RoomEntryUI prefab is not assigned.", this);
        }
#endif
    }
}
