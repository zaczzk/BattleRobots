using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that drives <see cref="ZoneControlZoneMultiplierSO"/> and
    /// displays the current capture-score multiplier and idle timer.
    ///
    /// <c>_onZoneCaptured</c>: calls RecordCapture + Refresh.
    /// <c>_onMatchStarted</c>: resets the multiplier SO + Refresh.
    /// <c>_onMultiplierChanged</c>: Refresh.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlZoneMultiplierController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlZoneMultiplierSO _multiplierSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMultiplierChanged;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _multiplierLabel;
        [SerializeField] private Text       _timeLabel;
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
            _onMultiplierChanged?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onMultiplierChanged?.UnregisterCallback(_refreshDelegate);
        }

        private void Update()
        {
            _multiplierSO?.Tick(Time.deltaTime);
        }

        private void HandleZoneCaptured()
        {
            _multiplierSO?.RecordCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _multiplierSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_multiplierSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_multiplierLabel != null)
                _multiplierLabel.text = $"Multiplier: x{_multiplierSO.CurrentMultiplier:F2}";

            if (_timeLabel != null)
                _timeLabel.text = _multiplierSO.IsActive
                    ? $"Resets in: {(_multiplierSO.ResetWindow - _multiplierSO.TimeSinceLastCapture):F1}s"
                    : "Idle";
        }

        public ZoneControlZoneMultiplierSO MultiplierSO => _multiplierSO;
    }
}
