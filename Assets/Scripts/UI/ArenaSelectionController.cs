using BattleRobots.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// MonoBehaviour that lets the player cycle through available arena presets
    /// in the pre-match lobby or main-menu UI.
    ///
    /// ── Behaviour ─────────────────────────────────────────────────────────────
    ///   • Reads the ordered preset list from <see cref="ArenaPresetsConfig"/>.
    ///   • Writes the selected <see cref="ArenaPresetsConfig.ArenaPreset"/> to
    ///     <see cref="SelectedArenaSO"/> immediately on each navigation step so
    ///     <see cref="ArenaManager"/> reads the correct config at match-start time.
    ///   • Updates an optional name label on every selection change.
    ///   • Previous / Next buttons cycle wrap-around through the preset list.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   BattleRobots.UI namespace.  No BattleRobots.Physics references.
    ///   Button listener delegates cached in Awake; zero per-frame allocations.
    ///   No Update method.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign <c>_presets</c> (ArenaPresetsConfig SO).
    ///   2. Assign <c>_selectedArena</c> (SelectedArenaSO — the same instance
    ///      wired to ArenaManager._selectedArena and MatchManager._selectedArena
    ///      in the Arena scene).
    ///   3. Optionally assign <c>_nameLabel</c>, <c>_prevButton</c>,
    ///      <c>_nextButton</c> — null-safe; buttons may also be wired in Inspector.
    ///   4. <c>PreviousArena()</c> and <c>NextArena()</c> are public and can be
    ///      wired directly to Button.onClick in the Inspector as an alternative.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ArenaSelectionController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("Immutable SO listing all available arena presets in selection order.")]
        [SerializeField] private ArenaPresetsConfig _presets;

        [Tooltip("Mutable runtime SO that stores the chosen preset across scene loads. " +
                 "Assign the same instance to ArenaManager and MatchManager in the Arena scene.")]
        [SerializeField] private SelectedArenaSO _selectedArena;

        [Header("UI (optional)")]
        [Tooltip("Label displaying the current preset's display name. May be null.")]
        [SerializeField] private Text _nameLabel;

        [Tooltip("Button that cycles to the previous preset (wrap-around). May be null.")]
        [SerializeField] private Button _prevButton;

        [Tooltip("Button that cycles to the next preset (wrap-around). May be null.")]
        [SerializeField] private Button _nextButton;

        // ── Runtime state ─────────────────────────────────────────────────────

        private int _selectedIndex;

        /// <summary>Current zero-based index into <see cref="ArenaPresetsConfig.Presets"/>.</summary>
        public int SelectedIndex => _selectedIndex;

        // ── Cached delegates ──────────────────────────────────────────────────

        private UnityAction _prevAction;
        private UnityAction _nextAction;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _prevAction = PreviousArena;
            _nextAction = NextArena;
        }

        private void OnEnable()
        {
            _prevButton?.onClick.AddListener(_prevAction);
            _nextButton?.onClick.AddListener(_nextAction);
            // Apply the displayed selection immediately so SelectedArenaSO
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
        /// Advances to the next arena preset.
        /// Wraps from the last preset back to the first.
        /// </summary>
        public void NextArena()
        {
            if (_presets == null || _presets.Presets.Count == 0) return;

            _selectedIndex = (_selectedIndex + 1) % _presets.Presets.Count;
            ApplySelection();
        }

        /// <summary>
        /// Steps back to the previous arena preset.
        /// Wraps from the first preset around to the last.
        /// </summary>
        public void PreviousArena()
        {
            if (_presets == null || _presets.Presets.Count == 0) return;

            _selectedIndex = (_selectedIndex - 1 + _presets.Presets.Count) % _presets.Presets.Count;
            ApplySelection();
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void ApplySelection()
        {
            if (_presets == null || _presets.Presets.Count == 0)
            {
                if (_nameLabel != null) _nameLabel.text = "\u2014"; // em-dash
                return;
            }

            var preset = _presets.Presets[_selectedIndex];
            _selectedArena?.Select(preset);

            if (_nameLabel != null)
                _nameLabel.text = preset?.displayName ?? "\u2014";
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_presets == null)
                Debug.LogWarning("[ArenaSelectionController] _presets is not assigned.", this);
            if (_selectedArena == null)
                Debug.LogWarning("[ArenaSelectionController] _selectedArena is not assigned.", this);
        }
#endif
    }
}
