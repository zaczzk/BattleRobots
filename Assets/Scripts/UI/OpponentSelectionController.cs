using BattleRobots.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// MonoBehaviour that lets the player cycle through available opponents in
    /// the pre-match lobby or main-menu UI.
    ///
    /// ── Behaviour ─────────────────────────────────────────────────────────────
    ///   • Reads the ordered opponent list from <see cref="OpponentRosterSO"/>.
    ///   • Writes the selected <see cref="OpponentProfileSO"/> to
    ///     <see cref="SelectedOpponentSO"/> immediately on each navigation step
    ///     so <see cref="BattleRobots.Physics.RobotAIController"/> reads the correct
    ///     profile at Arena-scene Awake time.
    ///   • Updates optional name and description labels on every selection change.
    ///   • Previous / Next buttons cycle wrap-around through the roster list.
    ///
    /// ── Architecture notes ─────────────────────────────────────────────────────
    ///   BattleRobots.UI namespace.  No BattleRobots.Physics references.
    ///   Button listener delegates cached in Awake; zero per-frame allocations.
    ///   No Update method.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign <c>_roster</c> (OpponentRosterSO SO).
    ///   2. Assign <c>_selectedOpponent</c> (SelectedOpponentSO — the same
    ///      instance wired to RobotAIController._selectedOpponent in the Arena).
    ///   3. Optionally assign <c>_nameLabel</c>, <c>_descriptionLabel</c>,
    ///      <c>_prevButton</c>, <c>_nextButton</c> — all null-safe.
    ///   4. <c>PreviousOpponent()</c> and <c>NextOpponent()</c> are public and can
    ///      be wired directly to Button.onClick in the Inspector as an alternative.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OpponentSelectionController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("Immutable SO listing all available opponents in selection order.")]
        [SerializeField] private OpponentRosterSO _roster;

        [Tooltip("Mutable SO that stores the chosen opponent across scene loads. " +
                 "Assign the same instance to every enemy RobotAIController in the Arena scene.")]
        [SerializeField] private SelectedOpponentSO _selectedOpponent;

        [Header("UI (optional)")]
        [Tooltip("Label displaying the current opponent's display name. May be null.")]
        [SerializeField] private Text _nameLabel;

        [Tooltip("Label displaying the current opponent's description text. May be null.")]
        [SerializeField] private Text _descriptionLabel;

        [Tooltip("Button that cycles to the previous opponent (wrap-around). May be null.")]
        [SerializeField] private Button _prevButton;

        [Tooltip("Button that cycles to the next opponent (wrap-around). May be null.")]
        [SerializeField] private Button _nextButton;

        // ── Runtime state ─────────────────────────────────────────────────────

        private int _selectedIndex;

        /// <summary>Current zero-based index into <see cref="OpponentRosterSO.Opponents"/>.</summary>
        public int SelectedIndex => _selectedIndex;

        // ── Cached delegates ──────────────────────────────────────────────────

        private UnityAction _prevAction;
        private UnityAction _nextAction;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _prevAction = PreviousOpponent;
            _nextAction = NextOpponent;
        }

        private void OnEnable()
        {
            _prevButton?.onClick.AddListener(_prevAction);
            _nextButton?.onClick.AddListener(_nextAction);
            // Apply the displayed selection immediately so SelectedOpponentSO
            // is consistent with the UI the moment the panel opens.
            ApplySelection();
        }

        private void OnDisable()
        {
            _prevButton?.onClick.RemoveListener(_prevAction);
            _nextButton?.onClick.RemoveListener(_nextAction);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Advances to the next opponent in the roster.
        /// Wraps from the last opponent back to the first.
        /// </summary>
        public void NextOpponent()
        {
            if (_roster == null || _roster.Opponents.Count == 0) return;

            _selectedIndex = (_selectedIndex + 1) % _roster.Opponents.Count;
            ApplySelection();
        }

        /// <summary>
        /// Steps back to the previous opponent in the roster.
        /// Wraps from the first opponent around to the last.
        /// </summary>
        public void PreviousOpponent()
        {
            if (_roster == null || _roster.Opponents.Count == 0) return;

            _selectedIndex = (_selectedIndex - 1 + _roster.Opponents.Count) % _roster.Opponents.Count;
            ApplySelection();
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void ApplySelection()
        {
            if (_roster == null || _roster.Opponents.Count == 0)
            {
                if (_nameLabel        != null) _nameLabel.text        = "\u2014"; // em-dash
                if (_descriptionLabel != null) _descriptionLabel.text = string.Empty;
                return;
            }

            var profile = _roster.Opponents[_selectedIndex];
            _selectedOpponent?.Select(profile);

            if (_nameLabel != null)
                _nameLabel.text = profile?.DisplayName ?? "\u2014";

            if (_descriptionLabel != null)
                _descriptionLabel.text = profile?.Description ?? string.Empty;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_roster == null)
                Debug.LogWarning("[OpponentSelectionController] _roster is not assigned.", this);
            if (_selectedOpponent == null)
                Debug.LogWarning("[OpponentSelectionController] _selectedOpponent is not assigned.", this);
        }
#endif
    }
}
