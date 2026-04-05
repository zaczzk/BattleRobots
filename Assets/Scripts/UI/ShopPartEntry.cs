using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Component placed on each row prefab in the shop's part browser list.
    /// Populated at runtime by <see cref="ShopUI.BuildPartList"/>; not set up in the Inspector.
    ///
    /// Architecture: BattleRobots.UI only — no Physics references.
    /// </summary>
    public sealed class ShopPartEntry : MonoBehaviour
    {
        [Tooltip("Label showing the part name.")]
        [SerializeField] private Text _nameLabel;

        [Tooltip("Label showing the part cost.")]
        [SerializeField] private Text _costLabel;

        [Tooltip("Thumbnail image for the part.")]
        [SerializeField] private Image _thumbnail;

        [Tooltip("Button the player clicks to select this row.")]
        [SerializeField] private Button _selectButton;

        // Kept alive for the lifetime of this entry — no GC concern.
        private PartDefinition _part;
        private ShopUI         _shopUI;

        private void OnDestroy()
        {
            if (_selectButton != null)
                _selectButton.onClick.RemoveListener(OnSelectClicked);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Initialises this entry row with a part and a back-reference to the shop.
        /// Called immediately after Instantiate by ShopUI.
        /// </summary>
        public void Initialise(PartDefinition part, ShopUI shopUI)
        {
            _part   = part;
            _shopUI = shopUI;

            if (_nameLabel != null)  _nameLabel.text = part.DisplayName;
            if (_costLabel != null)  _costLabel.text = $"{part.Cost} cr";

            if (_thumbnail != null)
            {
                _thumbnail.sprite  = part.Thumbnail;
                _thumbnail.enabled = part.Thumbnail != null;
            }

            if (_selectButton != null)
                _selectButton.onClick.AddListener(OnSelectClicked);
        }

        private void OnSelectClicked()
        {
            _shopUI?.SelectPart(_part);
        }
    }
}
