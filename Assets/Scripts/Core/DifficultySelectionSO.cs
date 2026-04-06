using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime-only SO that stores the player's current difficulty selection.
    ///
    /// Follows the <see cref="ArenaSelectionSO"/> pattern:
    ///   • Mutation only via <see cref="Select"/> / <see cref="Reset"/>.
    ///   • Calls <see cref="DifficultySO.LoadPreset"/> on the wired <see cref="DifficultySO"/>
    ///     asset so the same SO instance that <see cref="MatchManager"/> references is
    ///     always up to date — no MatchManager changes required.
    ///   • Broadcasts via a SO event channel so UI can react without polling.
    ///
    /// Reset() should be called when the difficulty selector screen opens to
    /// restore the default level and clear any stale play-session value.
    ///
    /// Create via:  Assets ▶ Create ▶ BattleRobots ▶ Difficulty ▶ DifficultySelectionSO
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Difficulty/DifficultySelectionSO", order = 1)]
    public sealed class DifficultySelectionSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("The shared DifficultySO asset also referenced by MatchManager. " +
                 "Select() calls LoadPreset on this asset so MatchManager sees the " +
                 "updated values at StartMatch without any extra wiring.")]
        [SerializeField] private DifficultySO _difficultySO;

        [Tooltip("Difficulty applied on Reset() and on first scene load. " +
                 "Typically Medium.")]
        [SerializeField] private DifficultyLevel _defaultLevel = DifficultyLevel.Medium;

        [Tooltip("Fired after every Select() or Reset() so the UI can update " +
                 "button highlights without polling.")]
        [SerializeField] private VoidGameEvent _onDifficultyChanged;

        // ── Runtime State ─────────────────────────────────────────────────────

        /// <summary>
        /// The currently selected difficulty level.
        /// Defaults to <see cref="_defaultLevel"/> until <see cref="Select"/> is called.
        /// </summary>
        public DifficultyLevel SelectedLevel { get; private set; }

        /// <summary>
        /// True once <see cref="Select"/> has been explicitly called this session.
        /// False after construction or after <see cref="Reset"/>.
        /// </summary>
        public bool HasSelection { get; private set; }

        /// <summary>
        /// The shared <see cref="DifficultySO"/> asset that reflects the active preset.
        /// MatchManager should be wired to this same asset via its Inspector field.
        /// </summary>
        public DifficultySO ActiveDifficulty => _difficultySO;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Stores <paramref name="level"/> as the current selection,
        /// calls <see cref="DifficultySO.LoadPreset"/> on the shared SO,
        /// and raises <see cref="_onDifficultyChanged"/>.
        /// Safe to call from UI button callbacks; not intended for FixedUpdate.
        /// </summary>
        public void Select(DifficultyLevel level)
        {
            SelectedLevel = level;
            HasSelection  = true;

            if (_difficultySO != null)
                _difficultySO.LoadPreset(level);
            else
                Debug.LogWarning("[DifficultySelectionSO] No DifficultySO assigned — " +
                                 "preset cannot be applied to MatchManager.", this);

            _onDifficultyChanged?.Raise();

            Debug.Log($"[DifficultySelectionSO] Difficulty selected: {level}.");
        }

        /// <summary>
        /// Resets the selection to <see cref="_defaultLevel"/> and clears
        /// <see cref="HasSelection"/>.  Also applies the default preset to the
        /// shared SO.  Call when the difficulty selector screen opens.
        /// </summary>
        public void Reset()
        {
            SelectedLevel = _defaultLevel;
            HasSelection  = false;

            if (_difficultySO != null)
                _difficultySO.LoadPreset(_defaultLevel);

            _onDifficultyChanged?.Raise();
        }
    }
}
