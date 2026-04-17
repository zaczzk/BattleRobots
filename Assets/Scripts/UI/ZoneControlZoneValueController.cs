using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlZoneValueController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlZoneValueSO _zoneValueSO;
        [SerializeField] private PlayerWallet            _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onZoneLost;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onValueChanged;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _valueLabel;
        [SerializeField] private Slider     _valueBar;
        [SerializeField] private GameObject _panel;

        private Action _handleZoneCapturedDelegate;
        private Action _handleZoneLostDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _refreshDelegate;

        private void Awake()
        {
            _handleZoneCapturedDelegate  = HandleZoneCaptured;
            _handleZoneLostDelegate      = HandleZoneLost;
            _handleMatchStartedDelegate  = HandleMatchStarted;
            _refreshDelegate             = Refresh;
        }

        private void OnEnable()
        {
            _onZoneCaptured?.RegisterCallback(_handleZoneCapturedDelegate);
            _onZoneLost?.RegisterCallback(_handleZoneLostDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onValueChanged?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
            _onZoneLost?.UnregisterCallback(_handleZoneLostDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onValueChanged?.UnregisterCallback(_refreshDelegate);
        }

        private void Update()
        {
            if (_zoneValueSO == null) return;
            _zoneValueSO.Tick(Time.deltaTime);
            Refresh();
        }

        private void HandleZoneCaptured()
        {
            if (_zoneValueSO == null) return;
            int value = _zoneValueSO.Harvest();
            _wallet?.AddFunds(value);
            _zoneValueSO.StartAccruing();
            Refresh();
        }

        private void HandleZoneLost()
        {
            _zoneValueSO?.StopAccruing();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _zoneValueSO?.Reset();
            _zoneValueSO?.StartAccruing();
            Refresh();
        }

        public void Refresh()
        {
            if (_zoneValueSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_valueLabel != null)
                _valueLabel.text = $"Zone Value: {_zoneValueSO.CurrentValue}";

            if (_valueBar != null)
                _valueBar.value = _zoneValueSO.AccrualProgress;
        }

        public ZoneControlZoneValueSO ZoneValueSO => _zoneValueSO;
    }
}
