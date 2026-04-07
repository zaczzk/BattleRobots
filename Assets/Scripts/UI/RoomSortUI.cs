using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Button group that lets the player choose a sort order for the room browser.
    ///
    /// Three mutually-exclusive buttons correspond to the three <see cref="RoomSortMode"/>
    /// values. When a button is pressed:
    ///   1. <see cref="RoomListUI.ApplySortMode"/> is called with the selected mode.
    ///   2. The active button's image is tinted with <see cref="_activeColor"/>; the
    ///      others revert to <see cref="_inactiveColor"/>.
    ///
    /// ARCHITECTURE RULES:
    ///   • BattleRobots.UI namespace — no Physics references.
    ///   • No Update / FixedUpdate defined (purely event-driven).
    ///   • Listener wiring via AddListener / RemoveListener — no anonymous lambdas.
    ///
    /// Inspector wiring:
    ///   □ _roomListUI          → RoomListUI MonoBehaviour on the room browser panel
    ///   □ _noneButton          → Button for "Original order" (RoomSortMode.None)
    ///   □ _byPlayerCountButton → Button for "Most players first" (ByPlayerCountDesc)
    ///   □ _byRoomCodeButton    → Button for "A→Z by room code" (ByRoomCodeAsc)
    ///   □ _activeColor         → Tint applied to the currently selected button image
    ///   □ _inactiveColor       → Tint applied to unselected button images
    /// </summary>
    [AddComponentMenu("BattleRobots/UI/Room Sort UI")]
    public sealed class RoomSortUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Target")]
        [Tooltip("RoomListUI whose ApplySortMode will be called when the player picks a sort option.")]
        [SerializeField] private RoomListUI _roomListUI;

        [Header("Sort Buttons")]
        [Tooltip("Button that selects RoomSortMode.None (original network order).")]
        [SerializeField] private Button _noneButton;

        [Tooltip("Button that selects RoomSortMode.ByPlayerCountDesc (most players first).")]
        [SerializeField] private Button _byPlayerCountButton;

        [Tooltip("Button that selects RoomSortMode.ByRoomCodeAsc (A→Z room code).")]
        [SerializeField] private Button _byRoomCodeButton;

        [Header("Highlight")]
        [Tooltip("Background colour applied to the image of the currently-selected button.")]
        [SerializeField] private Color _activeColor = new Color(0.25f, 0.75f, 0.25f, 1f);

        [Tooltip("Background colour applied to the images of unselected buttons.")]
        [SerializeField] private Color _inactiveColor = Color.white;

        // ── Testable state ────────────────────────────────────────────────────

        /// <summary>The sort mode currently selected by the player.</summary>
        public RoomSortMode CurrentSort { get; private set; } = RoomSortMode.None;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            if (_noneButton          != null) _noneButton.onClick.AddListener(OnNoneClicked);
            if (_byPlayerCountButton != null) _byPlayerCountButton.onClick.AddListener(OnByPlayerCountClicked);
            if (_byRoomCodeButton    != null) _byRoomCodeButton.onClick.AddListener(OnByRoomCodeClicked);
        }

        private void OnEnable()
        {
            // Sync button highlights to match the current sort when the panel opens.
            RefreshHighlights();
        }

        private void OnDestroy()
        {
            if (_noneButton          != null) _noneButton.onClick.RemoveListener(OnNoneClicked);
            if (_byPlayerCountButton != null) _byPlayerCountButton.onClick.RemoveListener(OnByPlayerCountClicked);
            if (_byRoomCodeButton    != null) _byRoomCodeButton.onClick.RemoveListener(OnByRoomCodeClicked);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Programmatically sets the active sort mode, applies it to the room list,
        /// and updates button highlights. Useful for tests and external reset calls.
        /// </summary>
        public void SetSort(RoomSortMode mode)
        {
            CurrentSort = mode;
            _roomListUI?.ApplySortMode(mode);
            RefreshHighlights();
        }

        // ── Button handlers ───────────────────────────────────────────────────

        private void OnNoneClicked()          => SetSort(RoomSortMode.None);
        private void OnByPlayerCountClicked() => SetSort(RoomSortMode.ByPlayerCountDesc);
        private void OnByRoomCodeClicked()    => SetSort(RoomSortMode.ByRoomCodeAsc);

        // ── Internal ──────────────────────────────────────────────────────────

        private void RefreshHighlights()
        {
            TintButton(_noneButton,          CurrentSort == RoomSortMode.None);
            TintButton(_byPlayerCountButton, CurrentSort == RoomSortMode.ByPlayerCountDesc);
            TintButton(_byRoomCodeButton,    CurrentSort == RoomSortMode.ByRoomCodeAsc);
        }

        private void TintButton(Button button, bool active)
        {
            if (button == null) return;

            Image img = button.GetComponent<Image>();
            if (img != null)
                img.color = active ? _activeColor : _inactiveColor;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_roomListUI == null)
                Debug.LogWarning("[RoomSortUI] No RoomListUI assigned.", this);
        }
#endif
    }
}
