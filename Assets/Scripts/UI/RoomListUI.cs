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

        [Header("Ping-Tier Grouping (optional)")]
        [Tooltip("When enabled, rooms are sorted by ping quality and a section header is " +
                 "inserted before each tier group: Excellent → Good → High → Unknown.")]
        [SerializeField] private bool _groupByPingTier = false;

        [Tooltip("(Optional) Interactive SectionHeaderUI prefab that adds a collapse/expand " +
                 "toggle button to each tier group. When assigned, takes priority over " +
                 "_sectionHeaderPrefab. Leave null to use the plain Text header.")]
        [SerializeField] private SectionHeaderUI _sectionHeaderUIPrefab;

        [Tooltip("(Optional) Plain Text prefab used as a non-interactive section header. " +
                 "Used only when _sectionHeaderUIPrefab is not assigned. Leave null to suppress " +
                 "headers while still sorting by tier.")]
        [SerializeField] private Text _sectionHeaderPrefab;

        // ── Runtime state ─────────────────────────────────────────────────────

        // Pool of active row instances; cleared and rebuilt on each Rebuild call.
        private readonly List<RoomEntryUI> _rows = new List<RoomEntryUI>();

        // Pool of active section header instances; cleared on each Rebuild call.
        private readonly List<GameObject> _headers = new List<GameObject>();

        // Active search prefix. Empty string = show all rooms.
        private string _searchPrefix = string.Empty;

        // Active sort mode. Defaults to None (original network order).
        private RoomSortMode _sortMode = RoomSortMode.None;

        // Ping tiers whose rows are currently hidden (collapsed by the player).
        private readonly HashSet<PingTier> _collapsedTiers = new HashSet<PingTier>();

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

        /// <summary>
        /// Toggles the collapsed state of a ping-tier section and rebuilds the list.
        /// When collapsed, all rows in that tier are hidden; the section header remains visible.
        /// Calling this method a second time with the same tier re-expands it.
        ///
        /// Wired as the <c>onToggle</c> callback passed into each <see cref="SectionHeaderUI.Setup"/>.
        /// No Update cost — invoked only on player interaction.
        /// </summary>
        public void ToggleTierCollapse(PingTier tier)
        {
            if (_collapsedTiers.Contains(tier))
                _collapsedTiers.Remove(tier);
            else
                _collapsedTiers.Add(tier);

            Rebuild();
        }

        /// <summary>
        /// Returns true when the given <paramref name="tier"/> is currently collapsed.
        /// Used by <see cref="SectionHeaderUI.Setup"/> to initialise the visual indicator
        /// after a Rebuild creates a fresh header instance.
        /// </summary>
        public bool IsTierCollapsed(PingTier tier) => _collapsedTiers.Contains(tier);

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

            // Destroy previous section headers.
            foreach (GameObject header in _headers)
            {
                if (header != null)
                    Destroy(header);
            }
            _headers.Clear();

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

            if (_groupByPingTier)
                RebuildGrouped(rooms);
            else
                RebuildFlat(rooms);
        }

        /// <summary>
        /// Flat rebuild: instantiate one row per room in the supplied order.
        /// Used when <c>_groupByPingTier</c> is false.
        /// </summary>
        private void RebuildFlat(IReadOnlyList<RoomEntry> rooms)
        {
            for (int i = 0; i < rooms.Count; i++)
            {
                RoomEntry entry = rooms[i];

                if (_filterFullRooms && entry.IsFull)        continue;
                if (_hidePrivateRooms && entry.isPrivate)    continue;

                RoomEntryUI row = Instantiate(_entryPrefab, _scrollContent);
                row.Setup(entry, HandleJoinRequested, _favouriteRoomsSO);
                _rows.Add(row);
            }
        }

        /// <summary>
        /// Grouped rebuild: sorts rooms by ping tier, inserts a section header
        /// at each tier boundary, then instantiates one row per room.
        /// Rows belonging to a collapsed tier are skipped (hidden).
        /// Used when <c>_groupByPingTier</c> is true.
        /// </summary>
        private void RebuildGrouped(IReadOnlyList<RoomEntry> rooms)
        {
            // Sort by tier (Excellent first, Unknown last) while preserving the
            // existing relative order within each tier (stable via index tiebreak).
            List<RoomEntry> sorted = SortByTier(rooms);

            PingTier lastTier = (PingTier)(-1); // sentinel — no header emitted yet

            for (int i = 0; i < sorted.Count; i++)
            {
                RoomEntry entry = sorted[i];

                if (_filterFullRooms && entry.IsFull)        continue;
                if (_hidePrivateRooms && entry.isPrivate)    continue;

                PingTier tier = RoomEntryUI.GetPingTier(entry.pingMs);
                if (tier != lastTier)
                {
                    EmitSectionHeader(RoomEntryUI.GetTierLabel(tier), tier);
                    lastTier = tier;
                }

                // Rows whose tier is collapsed are hidden; the header is still shown.
                if (_collapsedTiers.Contains(tier))
                    continue;

                RoomEntryUI row = Instantiate(_entryPrefab, _scrollContent);
                row.Setup(entry, HandleJoinRequested, _favouriteRoomsSO);
                _rows.Add(row);
            }
        }

        /// <summary>
        /// Instantiates a section header and parents it to <see cref="_scrollContent"/>.
        ///
        /// When <see cref="_sectionHeaderUIPrefab"/> is assigned, an interactive
        /// <see cref="SectionHeaderUI"/> is created with a collapse/expand toggle button.
        /// Otherwise falls back to a plain <see cref="Text"/> from
        /// <see cref="_sectionHeaderPrefab"/> (no interaction).
        /// When both prefabs are null the call is a no-op — rooms are still sorted by
        /// tier; only the header render is suppressed.
        /// </summary>
        private void EmitSectionHeader(string label, PingTier tier)
        {
            if (_scrollContent == null)
                return;

            // Interactive path: SectionHeaderUI with collapse/expand toggle.
            if (_sectionHeaderUIPrefab != null)
            {
                SectionHeaderUI header = Instantiate(_sectionHeaderUIPrefab, _scrollContent);
                header.Setup(label, tier, _collapsedTiers.Contains(tier), ToggleTierCollapse);
                _headers.Add(header.gameObject);
                return;
            }

            // Fallback: plain non-interactive Text header.
            if (_sectionHeaderPrefab == null)
                return;

            Text textHeader = Instantiate(_sectionHeaderPrefab, _scrollContent);
            textHeader.text = label;
            _headers.Add(textHeader.gameObject);
        }

        /// <summary>
        /// Returns a new list containing the same entries as <paramref name="rooms"/>
        /// sorted by ascending <see cref="PingTier"/> (Excellent=0 first, Unknown=3 last).
        /// Relative order within a tier is preserved (stable sort via original index).
        /// Allocates; called only during Rebuild (not in Update / FixedUpdate).
        /// </summary>
        internal static List<RoomEntry> SortByTier(IReadOnlyList<RoomEntry> rooms)
        {
            var sorted = new List<RoomEntry>(rooms.Count);
            for (int i = 0; i < rooms.Count; i++)
                sorted.Add(rooms[i]);

            // Stable sort: use original index stored alongside each entry.
            var indexed = new List<(RoomEntry entry, int idx)>(rooms.Count);
            for (int i = 0; i < rooms.Count; i++)
                indexed.Add((rooms[i], i));

            indexed.Sort((a, b) =>
            {
                int ta = (int)RoomEntryUI.GetPingTier(a.entry.pingMs);
                int tb = (int)RoomEntryUI.GetPingTier(b.entry.pingMs);
                int cmp = ta.CompareTo(tb);
                return cmp != 0 ? cmp : a.idx.CompareTo(b.idx); // stable
            });

            sorted.Clear();
            for (int i = 0; i < indexed.Count; i++)
                sorted.Add(indexed[i].entry);

            return sorted;
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
