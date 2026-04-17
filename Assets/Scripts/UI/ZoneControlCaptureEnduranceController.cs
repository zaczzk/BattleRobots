using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that drives the capture-endurance lifecycle and displays
    /// endurance state and progress toward the required capture count.
    ///
    /// <c>_onZoneCaptured</c>: calls <c>RecordCapture(Time.time)</c> + Refresh.
    /// <c>_onMatchStarted</c>: resets the tracker + Refresh.
    /// <c>_onEnduranceAchieved/_onEnduranceLost</c>: refreshes HUD.
    /// <see cref="Update"/> ticks the tracker each frame (window pruning).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureEnduranceController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureEnduranceSO _enduranceSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onEnduranceAchieved;
        [SerializeField] private VoidGameEvent _onEnduranceLost;

        [Header("UI References (optional)")]
        [SerializeField] private Slider     _progressBar;
        [SerializeField] private Text       _statusLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleZoneCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _refreshDelegate;

        private void Awake()
        {
            _handleZoneCapturedDelegate = HandleZoneCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _refreshDelegate            = Refresh;
        }

        private void OnEnable()
        {
            _onZoneCaptured?.RegisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onEnduranceAchieved?.RegisterCallback(_refreshDelegate);
            _onEnduranceLost?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onEnduranceAchieved?.UnregisterCallback(_refreshDelegate);
            _onEnduranceLost?.UnregisterCallback(_refreshDelegate);
        }

        private void Update()
        {
            _enduranceSO?.Tick(Time.time);
        }

        private void HandleZoneCaptured()
        {
            _enduranceSO?.RecordCapture(Time.time);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _enduranceSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_enduranceSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_progressBar != null)
                _progressBar.value = _enduranceSO.Progress;

            if (_statusLabel != null)
                _statusLabel.text = _enduranceSO.IsEnduring ? "Enduring!" : "Building...";
        }

        public ZoneControlCaptureEnduranceSO EnduranceSO => _enduranceSO;
    }
}
