using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// Displays the player's current level, XP progress bar, and XP label.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────
    ///   • Subscribes to <c>_onLevelUp</c> VoidGameEvent — calls <see cref="Refresh"/>
    ///     each time the player gains a level.
    ///   • Subscribes to <c>_onXPGained</c> IntGameEvent — calls <see cref="Refresh"/>
    ///     when XP is awarded (even without a level-up).
    ///   • <see cref="Refresh"/> reads <see cref="PlayerProgressionSO"/> directly;
    ///     no Update or polling.
    ///
    /// ── UI elements (all optional) ────────────────────────────────────────────
    ///   • <c>_levelLabel</c> (Text)  — shows "Level N" or "Level MAX".
    ///   • <c>_xpLabel</c> (Text)     — shows "450 / 500 XP" or "MAX" at max level.
    ///   • <c>_xpFillImage</c> (Image, type = Filled) — fill amount [0, 1].
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace — no BattleRobots.Physics references.
    ///   - All Action delegates cached in Awake; zero alloc after Awake.
    ///   - No Update or FixedUpdate.
    ///
    /// ── Scene wiring instructions ─────────────────────────────────────────────
    ///   1. Add this MB to any persistent Canvas GameObject (Main Menu / HUD).
    ///   2. Assign <c>_progression</c> → the PlayerProgressionSO asset.
    ///   3. Assign <c>_onLevelUp</c>  → the VoidGameEvent SO wired to
    ///      <c>PlayerProgressionSO._onLevelUp</c>.
    ///   4. Assign <c>_onXPGained</c> → the IntGameEvent SO wired to
    ///      <c>PlayerProgressionSO._onXPGained</c>.
    ///   5. Optionally assign Text/Image UI refs for level label, XP label, and fill.
    /// </summary>
    public sealed class PlayerLevelController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("PlayerProgressionSO asset — source of level and XP state.")]
        [SerializeField] private PlayerProgressionSO _progression;

        [Tooltip("VoidGameEvent raised by PlayerProgressionSO when the player levels up.")]
        [SerializeField] private VoidGameEvent _onLevelUp;

        [Tooltip("IntGameEvent raised by PlayerProgressionSO when XP is gained. " +
                 "Payload = XP amount awarded.")]
        [SerializeField] private IntGameEvent _onXPGained;

        [Header("UI (all optional)")]
        [Tooltip("Text element showing the current level. Displays 'Level N' or 'Level MAX'.")]
        [SerializeField] private Text _levelLabel;

        [Tooltip("Text element showing XP within the current level. " +
                 "Displays 'N / M XP' or 'MAX' when at max level.")]
        [SerializeField] private Text _xpLabel;

        [Tooltip("Image (type = Filled) driven by XP progress fraction [0, 1].")]
        [SerializeField] private Image _xpFillImage;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action    _refreshVoid;
        private Action<int> _refreshInt;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshVoid = Refresh;
            _refreshInt  = _ => Refresh();
        }

        private void OnEnable()
        {
            _onLevelUp?. RegisterCallback(_refreshVoid);
            _onXPGained?.RegisterCallback(_refreshInt);
            Refresh();
        }

        private void OnDisable()
        {
            _onLevelUp?. UnregisterCallback(_refreshVoid);
            _onXPGained?.UnregisterCallback(_refreshInt);
        }

        // ── Display ───────────────────────────────────────────────────────────

        /// <summary>
        /// Reads the current state from <see cref="_progression"/> and updates all
        /// wired UI elements.  Safe to call when <see cref="_progression"/> is null.
        /// </summary>
        public void Refresh()
        {
            if (_progression == null) return;

            bool atMax = _progression.IsMaxLevel;

            if (_levelLabel != null)
                _levelLabel.text = atMax
                    ? $"Level MAX"
                    : $"Level {_progression.CurrentLevel}";

            if (_xpLabel != null)
                _xpLabel.text = atMax
                    ? "MAX"
                    : $"{_progression.XpInCurrentLevel} / {_progression.XpRequiredForNextLevel} XP";

            if (_xpFillImage != null)
                _xpFillImage.fillAmount = _progression.XpProgressFraction;
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_progression == null)
                Debug.LogWarning("[PlayerLevelController] _progression not assigned.", this);
        }
#endif
    }
}
