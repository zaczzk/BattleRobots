using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// Star-toggle button that marks or unmarks a network room as a favourite.
    ///
    /// Usage:
    ///   1. Attach to a child GameObject of <see cref="RoomEntryUI"/>.
    ///   2. Call <see cref="Setup"/> once after the row is instantiated to wire
    ///      it to the shared <see cref="FavouriteRoomsSO"/> and the room code.
    ///   3. The button automatically reflects and persists favourite state.
    ///
    /// Inspector wiring:
    ///   □ _button      → the UnityEngine.UI.Button to click
    ///   □ _starIcon    → (optional) Image that changes colour to show active state
    ///   □ _activeColor → colour when the room IS favourited   (default: yellow)
    ///   □ _inactiveColor → colour when the room is NOT favourited (default: grey)
    ///
    /// ARCHITECTURE RULES:
    ///   • BattleRobots.UI namespace — no Physics references.
    ///   • No per-frame cost — no Update / FixedUpdate.
    ///   • Allocation only in Awake (AddListener) and Setup.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FavouriteButtonUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Button")]
        [Tooltip("The Button component the player clicks to toggle the favourite state.")]
        [SerializeField] private Button _button;

        [Header("Star Icon (optional)")]
        [Tooltip("Image that changes colour to reflect active/inactive state. " +
                 "Omit if colour feedback is not needed.")]
        [SerializeField] private Image _starIcon;

        [Tooltip("Image colour when the room IS favourited.")]
        [SerializeField] private Color _activeColor = new Color(1f, 0.85f, 0f, 1f); // gold

        [Tooltip("Image colour when the room is NOT favourited.")]
        [SerializeField] private Color _inactiveColor = new Color(0.55f, 0.55f, 0.55f, 1f); // grey

        // ── Runtime state ─────────────────────────────────────────────────────

        private FavouriteRoomsSO _favourites;
        private string           _roomCode = string.Empty;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            if (_button != null)
                _button.onClick.AddListener(ToggleFavourite);
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(ToggleFavourite);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Wires this button to a shared <see cref="FavouriteRoomsSO"/> for
        /// the given <paramref name="roomCode"/>.
        /// Must be called once immediately after instantiation / row setup.
        /// </summary>
        public void Setup(FavouriteRoomsSO favourites, string roomCode)
        {
            _favourites = favourites;
            _roomCode   = roomCode ?? string.Empty;
            Refresh();
        }

        /// <summary>
        /// Toggles the favourite state for the current room code.
        /// Called automatically by the Button onClick listener.
        /// Can also be called from code (e.g. keyboard shortcut).
        /// </summary>
        public void ToggleFavourite()
        {
            if (_favourites == null || string.IsNullOrEmpty(_roomCode)) return;

            if (_favourites.IsFavourite(_roomCode))
                _favourites.RemoveFavourite(_roomCode);
            else
                _favourites.AddFavourite(_roomCode);

            Refresh();
        }

        /// <summary>
        /// <c>true</c> when the current room is starred in <see cref="_favourites"/>.
        /// Returns <c>false</c> if <see cref="Setup"/> has not been called.
        /// </summary>
        public bool IsFavourite =>
            _favourites != null && _favourites.IsFavourite(_roomCode);

        // ── Private ───────────────────────────────────────────────────────────

        private void Refresh()
        {
            bool fav = IsFavourite;

            if (_starIcon != null)
                _starIcon.color = fav ? _activeColor : _inactiveColor;

            if (_button != null)
                _button.interactable = !string.IsNullOrEmpty(_roomCode);
        }
    }
}
