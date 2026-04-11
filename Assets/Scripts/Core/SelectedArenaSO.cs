using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Mutable runtime SO that stores the currently-selected
    /// <see cref="ArenaPresetsConfig.ArenaPreset"/> and persists it across scene loads
    /// (within a single play session — cleared on domain reload).
    ///
    /// ── Write path ────────────────────────────────────────────────────────────
    ///   <see cref="BattleRobots.UI.ArenaSelectionController"/> calls
    ///   <see cref="Select"/> each time the player picks a preset in the pre-match UI.
    ///
    /// ── Read paths ────────────────────────────────────────────────────────────
    ///   <see cref="ArenaManager"/>: reads <see cref="Current"/> in
    ///   <c>HandleMatchStarted()</c>. When <see cref="HasSelection"/> is true and
    ///   <c>Current.config != null</c>, the preset config overrides the inspector
    ///   <c>_arenaConfig</c> field — no per-arena Inspector changes needed after
    ///   the player's selection.
    ///
    ///   <see cref="MatchManager"/>: reads <c>Current.config.ArenaIndex</c> in
    ///   <c>EndMatch()</c> and writes it to <see cref="MatchRecord.arenaIndex"/>
    ///   so post-match analytics can identify which arena was played.
    ///
    /// ── Null / unset semantics ────────────────────────────────────────────────
    ///   <see cref="HasSelection"/> == false means no runtime override has been set;
    ///   ArenaManager falls back to its inspector <c>_arenaConfig</c> field.
    ///   <see cref="Reset"/> clears the selection without raising the event,
    ///   e.g. to restore inspector-driven config between sessions.
    ///
    /// ── Runtime state lifetime ────────────────────────────────────────────────
    ///   <c>_current</c> and <c>_hasSelection</c> are NOT serialized fields.
    ///   They default to null/false on domain reload (Enter Play Mode, Editor restart).
    ///   This is intentional: the pre-match UI always writes a fresh selection.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ SelectedArena.
    /// Assign the same SO instance to <see cref="BattleRobots.UI.ArenaSelectionController"/>,
    /// <see cref="ArenaManager"/>, and <see cref="MatchManager"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/SelectedArena", order = 2)]
    public sealed class SelectedArenaSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Raised when Select() is called. Subscribe in UI if a preview needs to " +
                 "update reactively (e.g. a stats panel that shows arena dimensions).")]
        [SerializeField] private VoidGameEvent _onArenaSelected;

        // ── Runtime state (not serialized — resets on domain reload) ──────────

        private ArenaPresetsConfig.ArenaPreset _current;
        private bool _hasSelection;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// The currently-selected arena preset, or <c>null</c> if no runtime
        /// selection has been made (<c>HasSelection</c> will be false).
        /// </summary>
        public ArenaPresetsConfig.ArenaPreset Current => _current;

        /// <summary>
        /// True if <see cref="Select"/> has been called at least once since
        /// the last <see cref="Reset"/> or domain reload.
        /// </summary>
        public bool HasSelection => _hasSelection;

        /// <summary>
        /// Display name of the current preset, or "Arena" when nothing is selected
        /// (or the selected preset has a null/whitespace display name).
        /// </summary>
        public string CurrentDisplayName =>
            _hasSelection && _current != null && !string.IsNullOrWhiteSpace(_current.displayName)
                ? _current.displayName
                : "Arena";

        /// <summary>
        /// Sets the active arena preset and fires <c>_onArenaSelected</c>.
        /// Passing <c>null</c> is allowed — it clears the config override so
        /// ArenaManager falls back to its inspector <c>_arenaConfig</c> field.
        /// <see cref="HasSelection"/> is set to <c>true</c> regardless.
        /// </summary>
        public void Select(ArenaPresetsConfig.ArenaPreset preset)
        {
            _current      = preset;
            _hasSelection = true;
            _onArenaSelected?.Raise();
        }

        /// <summary>
        /// Clears the active selection without firing <c>_onArenaSelected</c>.
        /// ArenaManager will use its inspector <c>_arenaConfig</c> field
        /// on the next <c>HandleMatchStarted</c> call.
        /// </summary>
        public void Reset()
        {
            _current      = null;
            _hasSelection = false;
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_onArenaSelected == null)
                Debug.LogWarning($"[SelectedArenaSO] '{name}': " +
                                 "_onArenaSelected is not assigned. " +
                                 "UI will not receive reactive updates on arena change.");
        }
#endif
    }
}
