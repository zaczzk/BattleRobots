using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Drives a single row in the shop's part browser.
    ///
    /// ── Responsibilities ──────────────────────────────────────────────────────
    ///   • Displays part name, description, cost, and thumbnail for one PartDefinition.
    ///   • Shows an "Owned" badge and disables the buy button for already-owned parts.
    ///   • Refreshes on demand (called by <see cref="ShopCatalogView"/> when wallet or
    ///     inventory changes); zero allocations after initial text writes in Setup().
    ///
    /// ── Scene / prefab wiring ─────────────────────────────────────────────────
    ///   • Attach to the root of a shop-row prefab.
    ///   • Assign UI refs in the Inspector: _nameLabel, _costLabel, _descriptionLabel,
    ///     _thumbnail (Image), _buyButton (Button), _ownedBadge (any GameObject).
    ///   • Do NOT assign _partDefinition or _shopManager in the prefab Inspector —
    ///     <see cref="ShopCatalogView"/> injects them at runtime via <see cref="Setup"/>.
    ///   • Wire _buyButton.onClick → <see cref="OnBuyClicked"/> in the Inspector
    ///     (or it is wired automatically in Awake).
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   - No Update or FixedUpdate.
    ///   - <see cref="Refresh"/> is called externally by <see cref="ShopCatalogView"/>
    ///     in response to SO event channels; this component itself does not subscribe.
    ///   - String allocations in <see cref="Refresh"/> are cold-path only (event-driven,
    ///     not per-frame), so they are architecturally acceptable.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShopItemController : MonoBehaviour
    {
        // ── Inspector — UI ────────────────────────────────────────────────────

        [Header("UI References")]
        [Tooltip("Displays the part's display name.")]
        [SerializeField] private Text _nameLabel;

        [Tooltip("Displays cost (e.g. '250g') or 'Owned' when already purchased.")]
        [SerializeField] private Text _costLabel;

        [Tooltip("Optional description / stat text area.")]
        [SerializeField] private Text _descriptionLabel;

        [Tooltip("Thumbnail sprite. Disabled when PartDefinition.Thumbnail is null.")]
        [SerializeField] private Image _thumbnail;

        [Tooltip("Disabled when the player already owns this part.")]
        [SerializeField] private Button _buyButton;

        [Tooltip("Activated when the player owns this part (badge / 'Owned' overlay).")]
        [SerializeField] private GameObject _ownedBadge;

        [Header("Rarity (optional)")]
        [Tooltip("Maps part rarity to display name, tint colour, and drop-weight multiplier. " +
                 "Leave null to skip rarity display in the shop.")]
        [SerializeField] private PartRarityConfig _rarityConfig;

        [Tooltip("Image tinted by the part's rarity colour. Leave null to skip.")]
        [SerializeField] private Image _rarityBadge;

        [Tooltip("Text label for the rarity display name (e.g. \"Rare\"). Leave null to skip.")]
        [SerializeField] private Text _rarityLabel;

        // ── Runtime data (injected by ShopCatalogView, not set in prefab) ─────

        private ShopManager      _shopManager;
        private PartDefinition   _partDefinition;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            if (_buyButton != null)
                _buyButton.onClick.AddListener(OnBuyClicked);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Called once by <see cref="ShopCatalogView"/> after instantiation.
        /// Writes static content (name, description, thumbnail) and then
        /// calls <see cref="Refresh"/> for dynamic state (owned/interactable).
        /// </summary>
        public void Setup(PartDefinition part, ShopManager shopManager)
        {
            _partDefinition = part;
            _shopManager    = shopManager;

            if (part == null) return;

            // Static content — set once, no further updates needed.
            if (_nameLabel        != null) _nameLabel.text        = part.DisplayName;
            if (_descriptionLabel != null) _descriptionLabel.text = part.Description;

            if (_thumbnail != null)
            {
                _thumbnail.sprite  = part.Thumbnail;
                _thumbnail.enabled = part.Thumbnail != null;
            }

            // Rarity display — requires _rarityConfig; individual UI refs are optional.
            if (_rarityConfig != null)
            {
                if (_rarityBadge != null)
                    _rarityBadge.color = _rarityConfig.GetTintColor(part.Rarity);
                if (_rarityLabel != null)
                    _rarityLabel.text  = _rarityConfig.GetDisplayName(part.Rarity);
            }

            Refresh();
        }

        /// <summary>
        /// Updates dynamic state: cost label text, buy-button interactability,
        /// and owned-badge visibility.  Called by <see cref="ShopCatalogView"/>
        /// whenever the player's wallet balance or inventory changes.
        /// </summary>
        public void Refresh()
        {
            if (_partDefinition == null || _shopManager == null) return;

            bool owned = _shopManager.IsOwned(_partDefinition);

            if (_costLabel != null)
                _costLabel.text = owned ? "Owned" : _partDefinition.Cost + "g";

            if (_buyButton != null)
                _buyButton.interactable = !owned;

            if (_ownedBadge != null)
                _ownedBadge.SetActive(owned);
        }

        /// <summary>
        /// Called by _buyButton.onClick (wired in Awake or Inspector).
        /// Delegates to <see cref="ShopManager.BuyPart"/>;
        /// state refresh follows automatically via the SO event channels.
        /// </summary>
        public void OnBuyClicked()
        {
            if (_shopManager == null || _partDefinition == null) return;
            _shopManager.BuyPart(_partDefinition);
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_buyButton == null)
                Debug.LogWarning("[ShopItemController] _buyButton not assigned — " +
                                 "purchases cannot be triggered from this row.", this);
        }
#endif
    }
}
