using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Displays a scrollable quick-rejoin list of recently-visited rooms built
    /// from <see cref="RecentRoomsSO"/>. Each row shows the room code and a
    /// "Join" button that calls <see cref="NetworkEventBridge.BeginJoin(string)"/>.
    ///
    /// The list rebuilds via <see cref="OnRecentRoomsUpdated"/>, which should be
    /// wired in the Inspector via a sibling <see cref="VoidGameEventListener"/>
    /// listening to <see cref="RecentRoomsSO"/>'s <c>_onRecentRoomsChanged</c>
    /// channel. The list also rebuilds in <c>OnEnable</c> so it is always current
    /// when the panel opens.
    ///
    /// ARCHITECTURE RULES:
    ///   • BattleRobots.UI namespace — no Physics references.
    ///   • No per-frame cost — no Update / FixedUpdate.
    ///   • All allocations occur during Rebuild (one per recent-room entry).
    ///   • <see cref="RecentRoomsSO"/> is read-only here; never mutated by this class.
    ///
    /// Inspector wiring checklist:
    ///   □ _recentRooms      → RecentRoomsSO asset
    ///   □ _bridge           → NetworkEventBridge MonoBehaviour in scene
    ///   □ _rowPrefab        → RecentRoomEntryUI prefab
    ///   □ _scrollContent    → Transform (Content of the ScrollRect)
    ///   □ _emptyStateLabel  → Text shown when no recent rooms exist
    ///
    ///   VoidGameEventListener (same GameObject) wiring:
    ///   □ onRecentRoomsChanged → RecentRoomsUI.OnRecentRoomsUpdated()
    /// </summary>
    public sealed class RecentRoomsUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("RecentRoomsSO managed by the network layer. Read-only from this class.")]
        [SerializeField] private RecentRoomsSO _recentRooms;

        [Tooltip("NetworkEventBridge MonoBehaviour. Join calls are delegated here.")]
        [SerializeField] private NetworkEventBridge _bridge;

        [Header("Prefab")]
        [Tooltip("RecentRoomEntryUI prefab instantiated for each recent room.")]
        [SerializeField] private RecentRoomEntryUI _rowPrefab;

        [Header("Layout")]
        [Tooltip("Parent Transform (Content of the ScrollRect) that row prefabs are added to.")]
        [SerializeField] private Transform _scrollContent;

        [Tooltip("Label shown when the recent-rooms list is empty.")]
        [SerializeField] private Text _emptyStateLabel;

        // ── Runtime state ─────────────────────────────────────────────────────

        private readonly List<RecentRoomEntryUI> _rows = new List<RecentRoomEntryUI>();

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable()
        {
            Rebuild();
        }

        // ── Public API (wired via VoidGameEventListener in Inspector) ─────────

        /// <summary>
        /// Called by a sibling <see cref="VoidGameEventListener"/> when
        /// <see cref="RecentRoomsSO._onRecentRoomsChanged"/> fires.
        /// </summary>
        public void OnRecentRoomsUpdated()
        {
            Rebuild();
        }

        // ── Internal ──────────────────────────────────────────────────────────

        private void Rebuild()
        {
            // Destroy previous rows.
            foreach (RecentRoomEntryUI row in _rows)
            {
                if (row != null)
                    Destroy(row.gameObject);
            }
            _rows.Clear();

            bool hasData = _recentRooms != null && _recentRooms.Count > 0;

            if (_emptyStateLabel != null)
                _emptyStateLabel.gameObject.SetActive(!hasData);

            if (!hasData || _rowPrefab == null || _scrollContent == null)
                return;

            IReadOnlyList<string> recent = _recentRooms.Recent;
            for (int i = 0; i < recent.Count; i++)
            {
                RecentRoomEntryUI row = Instantiate(_rowPrefab, _scrollContent);
                row.Setup(recent[i], HandleJoinRequested);
                _rows.Add(row);
            }
        }

        private void HandleJoinRequested(string roomCode)
        {
            if (string.IsNullOrEmpty(roomCode))
            {
                Debug.LogWarning("[RecentRoomsUI] HandleJoinRequested called with empty room code.", this);
                return;
            }

            if (_bridge == null)
            {
                Debug.LogWarning("[RecentRoomsUI] No NetworkEventBridge assigned. Cannot join room.", this);
                return;
            }

            _bridge.BeginJoin(roomCode);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_recentRooms == null)
                Debug.LogWarning("[RecentRoomsUI] RecentRoomsSO is not assigned.", this);
            if (_bridge == null)
                Debug.LogWarning("[RecentRoomsUI] NetworkEventBridge is not assigned.", this);
            if (_rowPrefab == null)
                Debug.LogWarning("[RecentRoomsUI] RecentRoomEntryUI prefab is not assigned.", this);
        }
#endif
    }
}
