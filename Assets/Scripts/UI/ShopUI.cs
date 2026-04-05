using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Shop screen controller.  Displays available parts, shows the current
    /// wallet balance, and triggers <see cref="PlayerWallet.Deduct"/> on purchase.
    ///
    /// Architecture constraints:
    ///   • <c>BattleRobots.UI</c> namespace — no reference to BattleRobots.Physics.
    ///   • Reads <see cref="PlayerWallet"/> SO (Core); never writes balance directly.
    ///   • Balance label is updated via <see cref="OnBalanceChanged(int)"/>, which
    ///     is wired to the wallet's IntGameEvent via an <c>IntGameEventListener</c>
    ///     component in the Inspector (UnityEvent → this method).
    ///   • No heap allocations in Update (Update not overridden; all UI work is
    ///     event-driven from button clicks and SO event channels).
    ///
    /// Inspector wiring checklist:
    ///   □ _wallet               → PlayerWallet SO
    ///   □ _catalogue            → array of PartDefinition SOs
    ///   □ _partListContainer    → ScrollView Content transform
    ///   □ _partEntryPrefab      → prefab with ShopPartEntry on root
    ///   □ _partNameLabel / _partDescLabel / _partCostLabel / _walletLabel → UI Text
    ///   □ _partThumbnailImage   → Image component
    ///   □ _buyButton            → Button component
    ///   □ _affordFeedbackLabel  → Text component (hidden by default)
    ///   □ IntGameEventListener (sibling component) _response wired to OnBalanceChanged
    /// </summary>
    public sealed class ShopUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Economy")]
        [Tooltip("PlayerWallet SO — read for balance display; Deduct called on buy.")]
        [SerializeField] private PlayerWallet _wallet;

        [Header("Part Catalogue")]
        [Tooltip("All PartDefinition SOs available for purchase. Order = display order.")]
        [SerializeField] private PartDefinition[] _catalogue;

        [Tooltip("ScrollView Content transform — part entry rows are parented here.")]
        [SerializeField] private Transform _partListContainer;

        [Tooltip("Prefab instantiated per catalogue entry. Root must have a ShopPartEntry component.")]
        [SerializeField] private GameObject _partEntryPrefab;

        [Header("Detail Panel")]
        [Tooltip("Label showing the selected part's display name.")]
        [SerializeField] private Text _partNameLabel;

        [Tooltip("Label showing the selected part's description.")]
        [SerializeField] private Text _partDescLabel;

        [Tooltip("Label showing the selected part's cost (e.g. '150 cr').")]
        [SerializeField] private Text _partCostLabel;

        [Tooltip("Image showing the selected part's thumbnail sprite.")]
        [SerializeField] private Image _partThumbnailImage;

        [Tooltip("'Buy' button — interactable only when wallet balance ≥ selected part cost.")]
        [SerializeField] private Button _buyButton;

        [Tooltip("Small feedback label shown when purchase fails due to insufficient funds.")]
        [SerializeField] private Text _affordFeedbackLabel;

        [Header("Wallet Display")]
        [Tooltip("Label that always reflects the current wallet balance.")]
        [SerializeField] private Text _walletLabel;

        // ── Runtime State ─────────────────────────────────────────────────────

        private PartDefinition _selectedPart;

        // ── Unity Lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            if (_buyButton != null)
                _buyButton.onClick.AddListener(OnBuyClicked);

            BuildPartList();

            // Initialise UI from current wallet state (before any events fire).
            int startBalance = _wallet != null ? _wallet.Balance : 0;
            RefreshWalletLabel(startBalance);
            ClearDetailPanel();
        }

        private void OnDestroy()
        {
            if (_buyButton != null)
                _buyButton.onClick.RemoveListener(OnBuyClicked);
        }

        // ── Public API (event-wired from Inspector) ───────────────────────────

        /// <summary>
        /// Called whenever the PlayerWallet balance changes.
        /// Wire this to the wallet's <c>IntGameEvent</c> via an
        /// <c>IntGameEventListener</c> component's UnityEvent response field.
        /// </summary>
        public void OnBalanceChanged(int newBalance)
        {
            RefreshWalletLabel(newBalance);
            if (_selectedPart != null)
                RefreshBuyButton(newBalance);
        }

        /// <summary>
        /// Selects a part and populates the detail panel.
        /// Called by <see cref="ShopPartEntry"/> when a row is clicked.
        /// </summary>
        public void SelectPart(PartDefinition part)
        {
            _selectedPart = part;
            RefreshDetailPanel();
        }

        // ── Private Helpers ───────────────────────────────────────────────────

        private void BuildPartList()
        {
            if (_partEntryPrefab == null || _partListContainer == null || _catalogue == null)
                return;

            for (int i = 0; i < _catalogue.Length; i++)
            {
                PartDefinition part = _catalogue[i];
                if (part == null) continue;

                GameObject go = Instantiate(_partEntryPrefab, _partListContainer);
                var entry = go.GetComponent<ShopPartEntry>();
                if (entry != null)
                    entry.Initialise(part, this);
            }
        }

        private void OnBuyClicked()
        {
            if (_selectedPart == null || _wallet == null) return;

            bool success = _wallet.Deduct(_selectedPart.Cost);
            if (!success)
            {
                if (_affordFeedbackLabel != null)
                {
                    _affordFeedbackLabel.text = "Not enough funds!";
                    _affordFeedbackLabel.gameObject.SetActive(true);
                }
                return;
            }

            if (_affordFeedbackLabel != null)
                _affordFeedbackLabel.gameObject.SetActive(false);

            Debug.Log($"[ShopUI] Purchased '{_selectedPart.DisplayName}' for {_selectedPart.Cost} credits.");

            // Wallet fires OnBalanceChanged automatically after Deduct,
            // which will call our OnBalanceChanged listener and refresh the button.
        }

        private void RefreshWalletLabel(int balance)
        {
            if (_walletLabel != null)
                _walletLabel.text = $"Credits: {balance}";
        }

        private void RefreshDetailPanel()
        {
            if (_selectedPart == null)
            {
                ClearDetailPanel();
                return;
            }

            if (_partNameLabel != null)  _partNameLabel.text = _selectedPart.DisplayName;
            if (_partDescLabel != null)  _partDescLabel.text = _selectedPart.Description;
            if (_partCostLabel != null)  _partCostLabel.text = $"{_selectedPart.Cost} cr";

            if (_partThumbnailImage != null)
            {
                _partThumbnailImage.sprite  = _selectedPart.Thumbnail;
                _partThumbnailImage.enabled = _selectedPart.Thumbnail != null;
            }

            if (_affordFeedbackLabel != null)
                _affordFeedbackLabel.gameObject.SetActive(false);

            int balance = _wallet != null ? _wallet.Balance : 0;
            RefreshBuyButton(balance);
        }

        private void ClearDetailPanel()
        {
            if (_partNameLabel      != null) _partNameLabel.text          = string.Empty;
            if (_partDescLabel      != null) _partDescLabel.text          = string.Empty;
            if (_partCostLabel      != null) _partCostLabel.text          = string.Empty;
            if (_partThumbnailImage != null) _partThumbnailImage.enabled  = false;
            if (_buyButton          != null) _buyButton.interactable      = false;
            if (_affordFeedbackLabel != null) _affordFeedbackLabel.gameObject.SetActive(false);
        }

        private void RefreshBuyButton(int balance)
        {
            if (_buyButton == null || _selectedPart == null) return;
            _buyButton.interactable = balance >= _selectedPart.Cost;
        }
    }
}
