using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Displays and allows editing of the player's profile:
    /// display name, avatar selection, and accumulated career stats.
    ///
    /// ── Scene wiring instructions ─────────────────────────────────────────────
    ///   • Assign _profile → PlayerProfileSO asset.
    ///   • Add a VoidGameEventListener on this GO:
    ///       Event = PlayerProfileSO._onProfileChanged, Response = HandleProfileChanged()
    ///   • Add a StringGameEventListener on this GO:
    ///       Event = PlayerProfileSO._onNameChanged, Response = HandleNameChanged(string)
    ///   • Wire UI fields in the Inspector (all optional — null refs are guarded).
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.UI only — no Physics namespace references.
    ///   - No per-frame allocations: Refresh() only called via events or OnEnable.
    ///   - Name editing deactivates the display label and activates the InputField;
    ///     confirming via EnterKey or focus-lost writes SetDisplayName on the SO.
    /// </summary>
    public sealed class PlayerProfileUI : MonoBehaviour
    {
        // ── Inspector — Data ──────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("The runtime player profile SO.")]
        [SerializeField] private PlayerProfileSO _profile;

        // ── Inspector — Identity panel ────────────────────────────────────────

        [Header("Identity")]
        [Tooltip("Label that shows the current display name (read mode).")]
        [SerializeField] private Text _nameLabel;

        [Tooltip("InputField shown while the player is editing their name.")]
        [SerializeField] private InputField _nameInputField;

        [Tooltip("Button that activates the name edit mode.")]
        [SerializeField] private Button _editNameButton;

        [Tooltip("Button that confirms the name edit and writes back to the SO.")]
        [SerializeField] private Button _confirmNameButton;

        [Tooltip("Label showing the avatar index (e.g. '3 / ?').")]
        [SerializeField] private Text _avatarIndexLabel;

        [Tooltip("Button to go to the previous avatar index.")]
        [SerializeField] private Button _avatarPrevButton;

        [Tooltip("Button to advance to the next avatar index.")]
        [SerializeField] private Button _avatarNextButton;

        // ── Inspector — Career stats panel ────────────────────────────────────

        [Header("Career Stats")]
        [Tooltip("Shows total wins.")]
        [SerializeField] private Text _winsLabel;

        [Tooltip("Shows total losses.")]
        [SerializeField] private Text _lossesLabel;

        [Tooltip("Shows win rate as a percentage string, e.g. '62%'.")]
        [SerializeField] private Text _winRateLabel;

        [Tooltip("Shows total career earnings.")]
        [SerializeField] private Text _earningsLabel;

        [Tooltip("Shows total career damage dealt.")]
        [SerializeField] private Text _damageDoneLabel;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            if (_editNameButton   != null) _editNameButton  .onClick.AddListener(HandleEditNameClicked);
            if (_confirmNameButton != null) _confirmNameButton.onClick.AddListener(HandleConfirmNameClicked);
            if (_avatarPrevButton  != null) _avatarPrevButton .onClick.AddListener(HandleAvatarPrev);
            if (_avatarNextButton  != null) _avatarNextButton .onClick.AddListener(HandleAvatarNext);

            // Submit (Enter) on the InputField also confirms the name
            if (_nameInputField != null)
                _nameInputField.onEndEdit.AddListener(HandleNameInputEndEdit);

            SetEditMode(false);
        }

        private void OnDestroy()
        {
            if (_editNameButton    != null) _editNameButton   .onClick.RemoveListener(HandleEditNameClicked);
            if (_confirmNameButton != null) _confirmNameButton.onClick.RemoveListener(HandleConfirmNameClicked);
            if (_avatarPrevButton  != null) _avatarPrevButton .onClick.RemoveListener(HandleAvatarPrev);
            if (_avatarNextButton  != null) _avatarNextButton .onClick.RemoveListener(HandleAvatarNext);
            if (_nameInputField    != null) _nameInputField   .onEndEdit.RemoveListener(HandleNameInputEndEdit);
        }

        private void OnEnable() => Refresh();

        // ── Public API (VoidGameEventListener / StringGameEventListener wiring) ─

        /// <summary>
        /// Call via VoidGameEventListener response wired to PlayerProfileSO._onProfileChanged.
        /// Refreshes all stats and avatar labels.
        /// </summary>
        public void HandleProfileChanged() => Refresh();

        /// <summary>
        /// Call via StringGameEventListener response wired to PlayerProfileSO._onNameChanged.
        /// Refreshes only the name label (cheaper than a full Refresh when only name changed).
        /// </summary>
        public void HandleNameChanged(string newName)
        {
            if (_nameLabel != null)
                _nameLabel.text = newName;
        }

        // ── Internal: refresh ─────────────────────────────────────────────────

        private void Refresh()
        {
            if (_profile == null) return;

            if (_nameLabel      != null) _nameLabel.text      = _profile.DisplayName;
            if (_avatarIndexLabel != null) _avatarIndexLabel.text = _profile.AvatarIndex.ToString();

            if (_winsLabel      != null) _winsLabel.text      = _profile.CareerWins.ToString();
            if (_lossesLabel    != null) _lossesLabel.text    = _profile.CareerLosses.ToString();
            if (_winRateLabel   != null) _winRateLabel.text   = FormatWinRate(_profile.WinRate);
            if (_earningsLabel  != null) _earningsLabel.text  = _profile.CareerEarnings.ToString();
            if (_damageDoneLabel != null) _damageDoneLabel.text = Mathf.RoundToInt(_profile.CareerDamageDone).ToString();
        }

        // ── Internal: name editing ────────────────────────────────────────────

        private void HandleEditNameClicked()
        {
            if (_profile == null || _nameInputField == null) return;
            _nameInputField.text = _profile.DisplayName;
            SetEditMode(true);
            _nameInputField.Select();
            _nameInputField.ActivateInputField();
        }

        private void HandleConfirmNameClicked()
        {
            CommitNameEdit();
        }

        private void HandleNameInputEndEdit(string value)
        {
            // onEndEdit fires on Enter key AND on focus-lost; commit in both cases.
            CommitNameEdit();
        }

        private void CommitNameEdit()
        {
            if (_profile != null && _nameInputField != null)
                _profile.SetDisplayName(_nameInputField.text);

            SetEditMode(false);
        }

        private void SetEditMode(bool editing)
        {
            if (_nameLabel        != null) _nameLabel.gameObject.SetActive(!editing);
            if (_nameInputField   != null) _nameInputField.gameObject.SetActive(editing);
            if (_editNameButton   != null) _editNameButton.gameObject.SetActive(!editing);
            if (_confirmNameButton != null) _confirmNameButton.gameObject.SetActive(editing);
        }

        // ── Internal: avatar ──────────────────────────────────────────────────

        private void HandleAvatarPrev()
        {
            if (_profile == null) return;
            int next = _profile.AvatarIndex - 1;
            _profile.SetAvatarIndex(next < 0 ? 0 : next);
        }

        private void HandleAvatarNext()
        {
            if (_profile == null) return;
            _profile.SetAvatarIndex(_profile.AvatarIndex + 1);
        }

        // ── Static helpers ────────────────────────────────────────────────────

        /// <summary>
        /// Formats a win rate float [0,1] into a human-readable percentage string.
        /// Public static for testability.
        /// </summary>
        public static string FormatWinRate(float rate)
        {
            return Mathf.RoundToInt(rate * 100f) + "%";
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_profile == null)
                Debug.LogWarning("[PlayerProfileUI] _profile PlayerProfileSO not assigned.");
        }
#endif
    }
}
