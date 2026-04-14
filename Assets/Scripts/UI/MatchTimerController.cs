using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// In-match HUD controller that displays the remaining match time in MM:SS format,
    /// driven by the <see cref="MatchTimerSO"/> FloatGameEvent channel.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────
    ///   Awake     → caches _timerDelegate (Action&lt;float&gt;).
    ///   OnEnable  → subscribes _onTimerUpdated channel; hides panel.
    ///   OnDisable → unsubscribes channel; hides panel.
    ///   OnTimerUpdated(remaining) → formats MM:SS label; applies warning colour
    ///                               when remaining ≤ _warningThreshold.
    ///
    /// ── Label colours ─────────────────────────────────────────────────────────
    ///   Normal  → <see cref="_normalColor"/> (default white).
    ///   Warning → <see cref="_warningColor"/> (default red) when remaining ≤ threshold.
    ///
    /// ── Architecture rules ────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   • No Update / FixedUpdate — purely event-driven.
    ///   • Delegate cached in Awake; zero heap allocations after initialisation
    ///     (string formatting only on the cold event-response path).
    ///   • DisallowMultipleComponent — one timer HUD per canvas.
    ///
    /// Scene wiring:
    ///   _onTimerUpdated    → FloatGameEvent SO (same as MatchTimerSO._onTimerUpdated).
    ///   _timerLabel        → Text component showing "M:SS".
    ///   _timerPanel        → Root panel; shown on first timer tick, hidden on OnEnable.
    ///   _warningThreshold  → Seconds below which the label turns warning colour (default 30).
    ///   _normalColor       → Label colour above threshold (default white).
    ///   _warningColor      → Label colour at or below threshold (default red).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MatchTimerController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Event Channel — In (optional)")]
        [Tooltip("FloatGameEvent raised by MatchTimerSO each Tick. Payload = seconds remaining.")]
        [SerializeField] private FloatGameEvent _onTimerUpdated;

        [Header("UI References (optional)")]
        [Tooltip("Text label updated with 'M:SS' formatted remaining time.")]
        [SerializeField] private Text _timerLabel;

        [Tooltip("Root panel shown on first timer tick; hidden on OnEnable/OnDisable.")]
        [SerializeField] private GameObject _timerPanel;

        [Header("Warning Settings")]
        [Tooltip("Label colour turns to _warningColor when remaining seconds ≤ this value.")]
        [SerializeField, Min(0f)] private float _warningThreshold = 30f;

        [Tooltip("Label colour when time remaining is above the warning threshold.")]
        [SerializeField] private Color _normalColor = Color.white;

        [Tooltip("Label colour when time remaining is at or below the warning threshold.")]
        [SerializeField] private Color _warningColor = Color.red;

        // ── Cached delegate ───────────────────────────────────────────────────

        private Action<float> _timerDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _timerDelegate = OnTimerUpdated;
        }

        private void OnEnable()
        {
            _onTimerUpdated?.RegisterCallback(_timerDelegate);
            _timerPanel?.SetActive(false);
        }

        private void OnDisable()
        {
            _onTimerUpdated?.UnregisterCallback(_timerDelegate);
            _timerPanel?.SetActive(false);
        }

        // ── Private implementation ─────────────────────────────────────────────

        private void OnTimerUpdated(float secondsRemaining)
        {
            _timerPanel?.SetActive(true);

            if (_timerLabel != null)
            {
                int totalSeconds = Mathf.CeilToInt(secondsRemaining);
                int minutes      = totalSeconds / 60;
                int seconds      = totalSeconds % 60;
                _timerLabel.text  = string.Format("{0}:{1:D2}", minutes, seconds);
                _timerLabel.color = secondsRemaining <= _warningThreshold
                    ? _warningColor
                    : _normalColor;
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Seconds remaining below which the warning colour is applied.</summary>
        public float WarningThreshold => _warningThreshold;

        /// <summary>Label colour when above the warning threshold.</summary>
        public Color NormalColor => _normalColor;

        /// <summary>Label colour when at or below the warning threshold.</summary>
        public Color WarningColor => _warningColor;
    }
}
