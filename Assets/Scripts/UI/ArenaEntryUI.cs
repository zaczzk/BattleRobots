using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Represents one row in the arena selector list.
    ///
    /// Displays the arena's thumbnail and name; clicking the row tells
    /// <see cref="ArenaSelectorUI"/> to select this arena.
    ///
    /// Architecture constraints:
    ///   • BattleRobots.UI namespace — no Physics references.
    ///   • No heap allocations in Update (Update not overridden).
    ///   • Initialised once via <see cref="Initialise"/>; afterwards event-driven only.
    ///
    /// Prefab checklist:
    ///   □ Root Button component (_rowButton)
    ///   □ Child Image for thumbnail (_thumbnailImage)
    ///   □ Child Text for arena name (_arenaNameLabel)
    ///   □ Child Text for time-limit (_timeLimitLabel)  — optional
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class ArenaEntryUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Image component showing the arena thumbnail.")]
        [SerializeField] private Image _thumbnailImage;

        [Tooltip("Text label for the arena display name.")]
        [SerializeField] private Text _arenaNameLabel;

        [Tooltip("Text label showing the arena's time limit. Optional.")]
        [SerializeField] private Text _timeLimitLabel;

        // ── Runtime State ─────────────────────────────────────────────────────

        private ArenaConfig     _config;
        private ArenaSelectorUI _owner;
        private Button          _rowButton;

        // ── Unity Lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            _rowButton = GetComponent<Button>();
            if (_rowButton != null)
                _rowButton.onClick.AddListener(OnRowClicked);
        }

        private void OnDestroy()
        {
            if (_rowButton != null)
                _rowButton.onClick.RemoveListener(OnRowClicked);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Populates this entry with <paramref name="config"/> and registers
        /// <paramref name="owner"/> as the recipient of click events.
        /// Call once after Instantiate.
        /// </summary>
        public void Initialise(ArenaConfig config, ArenaSelectorUI owner)
        {
            _config = config;
            _owner  = owner;

            if (_arenaNameLabel != null)
                _arenaNameLabel.text = config.ArenaName;

            if (_timeLimitLabel != null)
            {
                _timeLimitLabel.text = config.TimeLimitSeconds > 0f
                    ? $"{config.TimeLimitSeconds:F0}s"
                    : "∞";
            }

            if (_thumbnailImage != null)
            {
                _thumbnailImage.sprite  = config.Thumbnail;
                _thumbnailImage.enabled = config.Thumbnail != null;
            }
        }

        /// <summary>
        /// Visually marks (or unmarks) this row as the currently selected arena.
        /// The exact visual (e.g. colour tint) is handled by the Button's transition.
        /// </summary>
        public void SetSelected(bool selected)
        {
            if (_rowButton != null)
                _rowButton.interactable = !selected; // selected row dims to show it's chosen
        }

        // ── Private ───────────────────────────────────────────────────────────

        private void OnRowClicked()
        {
            if (_owner != null && _config != null)
                _owner.SelectArena(_config);
        }
    }
}
