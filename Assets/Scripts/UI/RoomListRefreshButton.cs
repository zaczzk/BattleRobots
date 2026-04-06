using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Standalone MonoBehaviour that wires a UI Button to
    /// <see cref="NetworkEventBridge.RequestRoomList"/>.
    ///
    /// Unlike the optional refresh button built into <see cref="RoomListUI"/>
    /// (which only calls <c>Rebuild()</c> from cached SO data), this component
    /// triggers a real adapter-level room-list fetch, which then updates
    /// <see cref="RoomListSO"/> and causes <see cref="RoomListUI"/> to rebuild
    /// with fresh network data.
    ///
    /// Placement:
    ///   Attach to the same GameObject as a UI Button (or assign the Button in the
    ///   Inspector).  The button's <c>onClick</c> is managed here so no manual
    ///   Inspector UnityEvent wiring is required.
    ///
    /// ARCHITECTURE RULES:
    ///   • BattleRobots.UI namespace — no Physics references.
    ///   • No per-frame cost — no Update / FixedUpdate.
    ///   • Allocation-free hot path: click handler calls a single method on bridge.
    ///
    /// Inspector wiring checklist:
    ///   □ _bridge        → NetworkEventBridge MonoBehaviour in scene
    ///   □ _refreshButton → Button component (auto-found on this GO if null)
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class RoomListRefreshButton : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("NetworkEventBridge in the scene. RequestRoomList() is called on click.")]
        [SerializeField] private NetworkEventBridge _bridge;

        [Tooltip("Button that triggers the refresh. Auto-resolved from this GameObject if null.")]
        [SerializeField] private Button _refreshButton;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            if (_refreshButton == null)
                _refreshButton = GetComponent<Button>();

            if (_refreshButton != null)
                _refreshButton.onClick.AddListener(OnRefreshClicked);
        }

        private void OnDestroy()
        {
            if (_refreshButton != null)
                _refreshButton.onClick.RemoveListener(OnRefreshClicked);
        }

        // ── Button callback ───────────────────────────────────────────────────

        /// <summary>
        /// Requests a fresh room list from the network adapter via the bridge.
        /// The adapter response updates <see cref="RoomListSO"/> which fires its
        /// SO event channel — causing any wired <see cref="RoomListUI"/> to rebuild.
        /// </summary>
        private void OnRefreshClicked()
        {
            if (_bridge == null)
            {
                Debug.LogWarning("[RoomListRefreshButton] No NetworkEventBridge assigned. " +
                                 "Room list refresh skipped.", this);
                return;
            }

            _bridge.RequestRoomList();
        }

        // ── Editor helpers ────────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_bridge == null)
                Debug.LogWarning("[RoomListRefreshButton] NetworkEventBridge is not assigned.", this);
        }
#endif
    }
}
