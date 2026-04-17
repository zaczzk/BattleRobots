using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that visualises the bot fatigue state from
    /// <see cref="ZoneControlFatigueSystemSO"/>.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   <c>_onBotZoneCaptured</c>: calls <c>RecordBotCapture</c> then refreshes.
    ///   <c>_onMatchStarted</c>: resets the fatigue SO and refreshes.
    ///   <c>_onFatigueTriggered/_onFatigueRecovered</c>: refreshes display.
    ///   <see cref="Refresh"/> hides the panel when <c>_fatigueSO</c> is null;
    ///   otherwise shows fatigue label "Fatigued!" or "Active" and capture count.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - All inspector fields optional and null-safe.
    ///   - Delegates cached in Awake; zero alloc after init.
    ///   - DisallowMultipleComponent.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlFatigueSystemController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlFatigueSystemSO _fatigueSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onFatigueTriggered;
        [SerializeField] private VoidGameEvent _onFatigueRecovered;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _statusLabel;
        [SerializeField] private Text       _captureCountLabel;
        [SerializeField] private GameObject _panel;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handleBotCaptureDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleBotCaptureDelegate   = HandleBotCapture;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _refreshDelegate            = Refresh;
        }

        private void OnEnable()
        {
            _onBotZoneCaptured?.RegisterCallback(_handleBotCaptureDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onFatigueTriggered?.RegisterCallback(_refreshDelegate);
            _onFatigueRecovered?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onBotZoneCaptured?.UnregisterCallback(_handleBotCaptureDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onFatigueTriggered?.UnregisterCallback(_refreshDelegate);
            _onFatigueRecovered?.UnregisterCallback(_refreshDelegate);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        private void HandleBotCapture()
        {
            _fatigueSO?.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _fatigueSO?.Reset();
            Refresh();
        }

        // ── Display ───────────────────────────────────────────────────────────

        /// <summary>Updates the fatigue status panel.</summary>
        public void Refresh()
        {
            if (_fatigueSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_statusLabel != null)
                _statusLabel.text = _fatigueSO.IsFatigued ? "Fatigued!" : "Active";

            if (_captureCountLabel != null)
                _captureCountLabel.text = $"Captures: {_fatigueSO.ConsecutiveCaptures}";
        }

        // ── Properties ────────────────────────────────────────────────────────

        public ZoneControlFatigueSystemSO FatigueSO => _fatigueSO;
    }
}
