using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlZoneHeatshieldController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlZoneHeatshieldSO _heatshieldSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onShieldActivated;
        [SerializeField] private VoidGameEvent _onShieldDepleted;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _shieldLabel;
        [SerializeField] private Slider     _shieldBar;
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
            _onShieldActivated?.RegisterCallback(_refreshDelegate);
            _onShieldDepleted?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onShieldActivated?.UnregisterCallback(_refreshDelegate);
            _onShieldDepleted?.UnregisterCallback(_refreshDelegate);
        }

        private void Update()
        {
            if (_heatshieldSO == null) return;
            _heatshieldSO.Tick(Time.deltaTime);
            Refresh();
        }

        private void HandleZoneCaptured()
        {
            _heatshieldSO?.Activate();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _heatshieldSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_heatshieldSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_shieldLabel != null)
                _shieldLabel.text = $"Shield: {Mathf.RoundToInt(_heatshieldSO.ShieldProgress * 100)}%";

            if (_shieldBar != null)
                _shieldBar.value = _heatshieldSO.ShieldProgress;
        }

        public ZoneControlZoneHeatshieldSO HeatshieldSO => _heatshieldSO;
    }
}
