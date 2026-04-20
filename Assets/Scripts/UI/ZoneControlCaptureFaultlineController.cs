using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureFaultlineController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureFaultlineSO _faultlineSO;
        [SerializeField] private PlayerWallet                  _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onFaultRupture;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _tensionLabel;
        [SerializeField] private Text       _ruptureLabel;
        [SerializeField] private Slider     _tensionBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleRuptureDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleRuptureDelegate      = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onFaultRupture?.RegisterCallback(_handleRuptureDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onFaultRupture?.UnregisterCallback(_handleRuptureDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_faultlineSO == null) return;
            int bonus = _faultlineSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_faultlineSO == null) return;
            int bonus = _faultlineSO.RecordBotCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _faultlineSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_faultlineSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_tensionLabel != null)
                _tensionLabel.text = $"Tension: {_faultlineSO.TotalTension}/{_faultlineSO.TensionThreshold}";

            if (_ruptureLabel != null)
                _ruptureLabel.text = $"Ruptures: {_faultlineSO.RuptureCount}";

            if (_tensionBar != null)
                _tensionBar.value = _faultlineSO.TensionProgress;
        }

        public ZoneControlCaptureFaultlineSO FaultlineSO => _faultlineSO;
    }
}
