using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that drives <see cref="ZoneControlScoreDecaySO"/> and
    /// displays the current decay state with a status label and idle timer.
    ///
    /// <c>_onZoneCaptured</c>: calls RecordCapture + Refresh.
    /// <c>_onMatchStarted</c>: resets the decay SO + Refresh.
    /// <c>_onMatchEnded</c>: resets the decay SO + Refresh.
    /// <c>_onDecayStarted/_onDecayEnded</c>: Refresh.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlScoreDecayController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlScoreDecaySO _decaySO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMatchEnded;
        [SerializeField] private VoidGameEvent _onDecayStarted;
        [SerializeField] private VoidGameEvent _onDecayEnded;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _statusLabel;
        [SerializeField] private Text       _timerLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleZoneCapturedDelegate;
        private Action _handleMatchBoundaryDelegate;
        private Action _refreshDelegate;

        private void Awake()
        {
            _handleZoneCapturedDelegate  = HandleZoneCaptured;
            _handleMatchBoundaryDelegate = HandleMatchBoundary;
            _refreshDelegate             = Refresh;
        }

        private void OnEnable()
        {
            _onZoneCaptured?.RegisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchBoundaryDelegate);
            _onMatchEnded?.RegisterCallback(_handleMatchBoundaryDelegate);
            _onDecayStarted?.RegisterCallback(_refreshDelegate);
            _onDecayEnded?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchBoundaryDelegate);
            _onMatchEnded?.UnregisterCallback(_handleMatchBoundaryDelegate);
            _onDecayStarted?.UnregisterCallback(_refreshDelegate);
            _onDecayEnded?.UnregisterCallback(_refreshDelegate);
        }

        private void Update()
        {
            _decaySO?.Tick(Time.deltaTime);
            if (_decaySO != null) Refresh();
        }

        private void HandleZoneCaptured()
        {
            _decaySO?.RecordCapture();
            Refresh();
        }

        private void HandleMatchBoundary()
        {
            _decaySO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_decaySO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_statusLabel != null)
                _statusLabel.text = _decaySO.IsDecaying ? "Decaying!" : "Active";

            if (_timerLabel != null)
                _timerLabel.text = $"Idle: {_decaySO.TimeSinceCapture:F1}s";
        }

        public ZoneControlScoreDecaySO DecaySO => _decaySO;
    }
}
