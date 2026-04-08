using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// Displays a brief achievement-unlock popup when a new achievement is earned.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Attach this MB to the achievement popup panel root GameObject.
    ///   2. Add a sibling <see cref="BattleRobots.Core.StringGameEventListener"/>:
    ///        _event    → <c>AchievementProgressSO._onAchievementTitle</c> SO channel
    ///        _response → AchievementUI.ShowUnlock(string)
    ///   3. Assign _panel, _titleLabel, and optionally tune _displayDuration.
    ///
    /// ── Architecture rules ────────────────────────────────────────────────────
    ///   • <c>BattleRobots.UI</c> namespace — no Physics references.
    ///   • No heap allocations in Update (there is no Update; the auto-hide
    ///     coroutine runs only while a popup is visible).
    ///   • <see cref="IsVisible"/> and <see cref="LastTitle"/> are observable
    ///     properties for EditMode testing via MonoBehaviour instantiation.
    /// </summary>
    public sealed class AchievementUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Panel")]
        [Tooltip("Root GameObject to show/hide for the achievement popup.")]
        [SerializeField] private GameObject _panel;

        [Tooltip("Text label that displays the achievement title.")]
        [SerializeField] private Text _titleLabel;

        [Tooltip("How long (seconds) the popup stays visible before auto-hiding.")]
        [SerializeField, Min(0.1f)] private float _displayDuration = 3f;

        // ── Observable state (testable without UI wiring) ─────────────────────

        /// <summary>True while the popup panel is visible.</summary>
        public bool   IsVisible  { get; private set; }

        /// <summary>Title of the most recently shown achievement. Empty before first call.</summary>
        public string LastTitle  { get; private set; } = string.Empty;

        // ── Private ───────────────────────────────────────────────────────────

        private Coroutine _hideCoroutine;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            _panel?.SetActive(false);
            IsVisible = false;
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Shows the achievement popup with the given title and starts the auto-hide timer.
        /// If a popup is already visible its timer is restarted.
        /// Called via the _response UnityEvent on a sibling StringGameEventListener.
        /// </summary>
        public void ShowUnlock(string title)
        {
            LastTitle = title ?? string.Empty;

            if (_titleLabel != null)
                _titleLabel.text = LastTitle;

            if (_panel != null)
                _panel.SetActive(true);

            IsVisible = true;

            // Restart the auto-hide timer if a previous popup was already showing.
            if (_hideCoroutine != null)
                StopCoroutine(_hideCoroutine);
            _hideCoroutine = StartCoroutine(AutoHide());
        }

        /// <summary>
        /// Immediately hides the popup and cancels any pending auto-hide timer.
        /// Safe to call when not visible.
        /// </summary>
        public void HidePanel()
        {
            if (_hideCoroutine != null)
            {
                StopCoroutine(_hideCoroutine);
                _hideCoroutine = null;
            }

            if (_panel != null)
                _panel.SetActive(false);

            IsVisible = false;
        }

        // ── Private ───────────────────────────────────────────────────────────

        private IEnumerator AutoHide()
        {
            yield return new WaitForSeconds(_displayDuration);
            HidePanel();
        }
    }
}
