using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Arena selector screen.  Displays a scrollable list of <see cref="ArenaConfig"/>
    /// SOs; the player picks one; pressing "Fight!" stores the choice in
    /// <see cref="ArenaSelectionSO"/> and triggers an async scene load.
    ///
    /// Architecture constraints:
    ///   • BattleRobots.UI namespace — no Physics references.
    ///   • No heap allocations in Update (Update not overridden).
    ///   • Communicates with MatchManager via <see cref="ArenaSelectionSO"/> SO (Core).
    ///   • Scene transition delegated to <see cref="SceneTransitionController"/> (Core).
    ///
    /// Inspector wiring checklist:
    ///   □ _catalogue           → array of ArenaConfig SOs (ordered = display order)
    ///   □ _arenaSelection      → ArenaSelectionSO runtime SO
    ///   □ _transitionCtrl      → SceneTransitionController (from Bootstrap scene)
    ///   □ _listContainer       → ScrollView Content transform
    ///   □ _arenaEntryPrefab    → prefab with ArenaEntryUI on root
    ///   □ _detailNameLabel     → Text (detail panel arena name)
    ///   □ _detailTimeLimitLabel→ Text (detail panel time limit)
    ///   □ _detailBonusLabel    → Text (detail panel win bonus)
    ///   □ _detailThumbnail     → Image (detail panel preview)
    ///   □ _confirmButton       → Button (Fight!)
    ///   □ _noSelectionHint     → GameObject (shown when nothing selected)
    ///   □ _arenaSceneName      → string (build name of the Arena scene)
    /// </summary>
    public sealed class ArenaSelectorUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Catalogue")]
        [Tooltip("All ArenaConfig SOs to list. Order = display order.")]
        [SerializeField] private ArenaConfig[] _catalogue;

        [Header("Selection SO")]
        [Tooltip("Runtime ArenaSelectionSO that stores the chosen arena for MatchManager.")]
        [SerializeField] private ArenaSelectionSO _arenaSelection;

        [Header("Scene Transition")]
        [Tooltip("SceneTransitionController from the Bootstrap scene. Used to load the Arena scene.")]
        [SerializeField] private SceneTransitionController _transitionCtrl;

        [Tooltip("Build name of the Arena scene to load when the player confirms.")]
        [SerializeField] private string _arenaSceneName = "Arena";

        [Header("List")]
        [Tooltip("ScrollView Content transform — arena rows are parented here.")]
        [SerializeField] private Transform _listContainer;

        [Tooltip("Prefab instantiated per catalogue entry. Root must have ArenaEntryUI.")]
        [SerializeField] private GameObject _arenaEntryPrefab;

        [Header("Detail Panel")]
        [Tooltip("Label showing the selected arena's display name.")]
        [SerializeField] private Text _detailNameLabel;

        [Tooltip("Label showing the time limit (e.g. '180s' or '∞').")]
        [SerializeField] private Text _detailTimeLimitLabel;

        [Tooltip("Label showing the win bonus currency for the selected arena.")]
        [SerializeField] private Text _detailBonusLabel;

        [Tooltip("Preview thumbnail for the selected arena.")]
        [SerializeField] private Image _detailThumbnail;

        [Header("Confirm")]
        [Tooltip("'Fight!' button — enabled only after a valid selection is made.")]
        [SerializeField] private Button _confirmButton;

        [Tooltip("Hint text / panel shown when no arena is selected yet.")]
        [SerializeField] private GameObject _noSelectionHint;

        // ── Runtime State ─────────────────────────────────────────────────────

        private ArenaConfig   _selectedConfig;
        private ArenaEntryUI[] _entryComponents;  // cached for selection highlighting

        // ── Unity Lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            if (_confirmButton != null)
                _confirmButton.onClick.AddListener(OnConfirmClicked);

            BuildArenaList();
            RefreshDetailPanel();   // clears panel + disables confirm button
        }

        private void OnEnable()
        {
            // Clear any leftover selection from a previous visit.
            _arenaSelection?.Reset();
            _selectedConfig = null;
            RefreshDetailPanel();
            RefreshConfirmButton();
        }

        private void OnDestroy()
        {
            if (_confirmButton != null)
                _confirmButton.onClick.RemoveListener(OnConfirmClicked);
        }

        // ── Public API (called by ArenaEntryUI rows) ──────────────────────────

        /// <summary>
        /// Called by an <see cref="ArenaEntryUI"/> row when the player clicks it.
        /// Stores the selection in the runtime SO and refreshes the detail panel.
        /// </summary>
        public void SelectArena(ArenaConfig config)
        {
            if (config == null) return;

            _selectedConfig = config;
            _arenaSelection?.Select(config);

            RefreshDetailPanel();
            RefreshConfirmButton();
            UpdateRowHighlights();
        }

        // ── Private Helpers ───────────────────────────────────────────────────

        private void BuildArenaList()
        {
            if (_arenaEntryPrefab == null || _listContainer == null || _catalogue == null)
                return;

            _entryComponents = new ArenaEntryUI[_catalogue.Length];

            for (int i = 0; i < _catalogue.Length; i++)
            {
                ArenaConfig config = _catalogue[i];
                if (config == null) continue;

                GameObject go = Instantiate(_arenaEntryPrefab, _listContainer);
                var entry = go.GetComponent<ArenaEntryUI>();
                if (entry != null)
                {
                    entry.Initialise(config, this);
                    _entryComponents[i] = entry;
                }
            }
        }

        private void OnConfirmClicked()
        {
            if (_selectedConfig == null)
            {
                Debug.LogWarning("[ArenaSelectorUI] Confirm clicked with no selection — ignored.");
                return;
            }

            // Ensure the SO is up to date (it should already be, but be safe).
            _arenaSelection?.Select(_selectedConfig);

            if (_transitionCtrl != null)
                _transitionCtrl.LoadScene(_arenaSceneName);
            else
                Debug.LogWarning("[ArenaSelectorUI] SceneTransitionController not assigned. " +
                                 "Cannot load arena scene.");
        }

        private void RefreshDetailPanel()
        {
            if (_selectedConfig == null)
            {
                ClearDetailPanel();
                return;
            }

            if (_detailNameLabel != null)
                _detailNameLabel.text = _selectedConfig.ArenaName;

            if (_detailTimeLimitLabel != null)
            {
                _detailTimeLimitLabel.text = _selectedConfig.TimeLimitSeconds > 0f
                    ? $"Time limit: {_selectedConfig.TimeLimitSeconds:F0}s"
                    : "No time limit";
            }

            if (_detailBonusLabel != null)
                _detailBonusLabel.text = $"Win bonus: +{_selectedConfig.WinBonusCurrency} cr";

            if (_detailThumbnail != null)
            {
                _detailThumbnail.sprite  = _selectedConfig.Thumbnail;
                _detailThumbnail.enabled = _selectedConfig.Thumbnail != null;
            }

            if (_noSelectionHint != null)
                _noSelectionHint.SetActive(false);
        }

        private void ClearDetailPanel()
        {
            if (_detailNameLabel      != null) _detailNameLabel.text          = string.Empty;
            if (_detailTimeLimitLabel != null) _detailTimeLimitLabel.text     = string.Empty;
            if (_detailBonusLabel     != null) _detailBonusLabel.text         = string.Empty;
            if (_detailThumbnail      != null) _detailThumbnail.enabled       = false;
            if (_noSelectionHint      != null) _noSelectionHint.SetActive(true);
        }

        private void RefreshConfirmButton()
        {
            if (_confirmButton != null)
                _confirmButton.interactable = _selectedConfig != null;
        }

        private void UpdateRowHighlights()
        {
            if (_entryComponents == null) return;

            for (int i = 0; i < _entryComponents.Length; i++)
            {
                if (_entryComponents[i] == null) continue;
                bool isSelected = _catalogue[i] == _selectedConfig;
                _entryComponents[i].SetSelected(isSelected);
            }
        }
    }
}
