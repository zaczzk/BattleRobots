using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// Subscribes to the match timer <see cref="FloatGameEvent"/> channel and delegates
    /// threshold-detection to <see cref="MatchTimerWarningSO.CheckAndFire"/>.
    ///
    /// ── Behaviour ─────────────────────────────────────────────────────────────────
    ///   • Registers on <see cref="_onTimerUpdated"/> in <c>OnEnable</c>;
    ///     unregisters and hides the overlay in <c>OnDisable</c>.
    ///   • Each timer tick calls <see cref="MatchTimerWarningSO.CheckAndFire"/> which
    ///     fires the appropriate <see cref="VoidGameEvent"/> channels (for audio / VFX).
    ///   • Optionally shows / hides <see cref="_warningPanel"/> when seconds remaining
    ///     drops to or below <see cref="_showPanelBelow"/> (and hides when time is up
    ///     or timer rises above the value again after a modifier change).
    ///   • Optionally updates <see cref="_warningText"/> with an "M:SS" formatted
    ///     countdown label on each tick while the panel is visible.
    ///
    /// ── Architecture ──────────────────────────────────────────────────────────────
    ///   BattleRobots.UI namespace.  No BattleRobots.Physics references.
    ///   <see cref="_timerAction"/> delegate cached in <c>Awake</c>; zero per-frame allocations.
    ///   No <c>Update</c> method — purely event-driven via the FloatGameEvent channel.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────────
    ///   1. Assign <c>_warningConfig</c> → MatchTimerWarningSO asset.
    ///   2. Assign <c>_onTimerUpdated</c> → FloatGameEvent SO (same as MatchManager._onTimerUpdated).
    ///   3. Optionally assign <c>_warningPanel</c> → a Canvas overlay to show when time is low.
    ///   4. Optionally adjust <c>_showPanelBelow</c> (default 30 s).
    ///   5. Optionally assign <c>_warningText</c> → a Text component for the live countdown label.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MatchTimerWarningController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Warning Config")]
        [Tooltip("SO containing the list of time thresholds and their VoidGameEvent channels. " +
                 "CheckAndFire() is called on every timer tick while a match is running.")]
        [SerializeField] private MatchTimerWarningSO _warningConfig;

        [Header("Event Channel — In")]
        [Tooltip("FloatGameEvent raised by MatchManager every frame while a match is running. " +
                 "Payload = seconds remaining. Must be the same SO as MatchManager._onTimerUpdated.")]
        [SerializeField] private FloatGameEvent _onTimerUpdated;

        [Header("Warning Overlay (optional)")]
        [Tooltip("Root GameObject of a low-time warning panel. " +
                 "SetActive(true) when secondsRemaining > 0 and ≤ _showPanelBelow; " +
                 "SetActive(false) when secondsRemaining > _showPanelBelow or time is up. " +
                 "Leave null to skip panel management.")]
        [SerializeField] private GameObject _warningPanel;

        [Tooltip("Panel is shown when seconds remaining drops to or below this value. " +
                 "Set to 0 to disable panel management entirely. Default: 30 s.")]
        [SerializeField, Min(0f)] private float _showPanelBelow = 30f;

        [Tooltip("Optional Text label inside the warning panel. " +
                 "Updated with an 'M:SS' formatted countdown string on every timer tick " +
                 "while the panel is active. Leave null to skip text updates.")]
        [SerializeField] private Text _warningText;

        // ── Cached delegate ───────────────────────────────────────────────────────

        private Action<float> _timerAction;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            _timerAction = OnTimerUpdated;
        }

        private void OnEnable()
        {
            _onTimerUpdated?.RegisterCallback(_timerAction);

            // Ensure the overlay is hidden the moment the component activates so it
            // does not show before the first timer tick arrives.
            _warningPanel?.SetActive(false);
        }

        private void OnDisable()
        {
            _onTimerUpdated?.UnregisterCallback(_timerAction);
            _warningPanel?.SetActive(false);
        }

        // ── Private implementation ─────────────────────────────────────────────────

        private void OnTimerUpdated(float secondsRemaining)
        {
            // Delegate threshold-event firing to the config SO.
            _warningConfig?.CheckAndFire(secondsRemaining);

            // Manage the optional overlay panel independently of the threshold events.
            if (_warningPanel != null)
            {
                bool showPanel = secondsRemaining > 0f && secondsRemaining <= _showPanelBelow;
                _warningPanel.SetActive(showPanel);

                if (showPanel && _warningText != null)
                {
                    int totalSeconds = Mathf.CeilToInt(secondsRemaining);
                    int minutes      = totalSeconds / 60;
                    int seconds      = totalSeconds % 60;
                    _warningText.text = $"{minutes}:{seconds:D2}";
                }
            }
        }
    }
}
