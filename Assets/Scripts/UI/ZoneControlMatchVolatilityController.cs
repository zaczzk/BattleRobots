using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlMatchVolatilityController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlMatchVolatilitySO _volatilitySO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onMajorityAchieved;
        [SerializeField] private VoidGameEvent _onMajorityLost;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onHighVolatility;
        [SerializeField] private VoidGameEvent _onVolatilityUpdated;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _changesLabel;
        [SerializeField] private Text       _statusLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleMajorityAchievedDelegate;
        private Action _handleMajorityLostDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleHighVolatilityDelegate;
        private Action _handleVolatilityUpdatedDelegate;

        private void Awake()
        {
            _handleMajorityAchievedDelegate  = HandleMajorityAchieved;
            _handleMajorityLostDelegate      = HandleMajorityLost;
            _handleMatchStartedDelegate      = HandleMatchStarted;
            _handleHighVolatilityDelegate    = Refresh;
            _handleVolatilityUpdatedDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onMajorityAchieved?.RegisterCallback(_handleMajorityAchievedDelegate);
            _onMajorityLost?.RegisterCallback(_handleMajorityLostDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onHighVolatility?.RegisterCallback(_handleHighVolatilityDelegate);
            _onVolatilityUpdated?.RegisterCallback(_handleVolatilityUpdatedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMajorityAchieved?.UnregisterCallback(_handleMajorityAchievedDelegate);
            _onMajorityLost?.UnregisterCallback(_handleMajorityLostDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onHighVolatility?.UnregisterCallback(_handleHighVolatilityDelegate);
            _onVolatilityUpdated?.UnregisterCallback(_handleVolatilityUpdatedDelegate);
        }

        private void HandleMajorityAchieved()
        {
            if (_volatilitySO == null) return;
            _volatilitySO.RecordLeadChange(true);
            Refresh();
        }

        private void HandleMajorityLost()
        {
            if (_volatilitySO == null) return;
            _volatilitySO.RecordLeadChange(false);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _volatilitySO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_volatilitySO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_changesLabel != null)
                _changesLabel.text = $"Volatility: {_volatilitySO.LeadChanges} changes";

            if (_statusLabel != null)
                _statusLabel.text = _volatilitySO.IsHighVolatility ? "HIGH!" : "Stable";
        }

        public ZoneControlMatchVolatilitySO VolatilitySO => _volatilitySO;
    }
}
