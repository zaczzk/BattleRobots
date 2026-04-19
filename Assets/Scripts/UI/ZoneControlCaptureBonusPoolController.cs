using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureBonusPoolController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureBonusPoolSO _poolSO;
        [SerializeField] private PlayerWallet                  _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onPoolAwarded;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _poolLabel;
        [SerializeField] private Slider     _poolBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerCapturedDelegate;
        private Action _handleBotCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handlePoolAwardedDelegate;

        private void Awake()
        {
            _handlePlayerCapturedDelegate = HandlePlayerCaptured;
            _handleBotCapturedDelegate    = HandleBotCaptured;
            _handleMatchStartedDelegate   = HandleMatchStarted;
            _handlePoolAwardedDelegate    = HandlePoolAwarded;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerCapturedDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onPoolAwarded?.RegisterCallback(_handlePoolAwardedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerCapturedDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onPoolAwarded?.UnregisterCallback(_handlePoolAwardedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_poolSO == null) return;
            int prevTotal = _poolSO.TotalAwarded;
            _poolSO.RecordPlayerCapture();
            int earned = _poolSO.TotalAwarded - prevTotal;
            if (earned > 0)
                _wallet?.AddFunds(earned);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_poolSO == null) return;
            _poolSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _poolSO?.Reset();
            Refresh();
        }

        private void HandlePoolAwarded()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (_poolSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_poolLabel != null)
                _poolLabel.text = $"Pool: {_poolSO.CurrentPool}/{_poolSO.PoolCapacity}";

            if (_poolBar != null)
                _poolBar.value = _poolSO.PoolProgress;
        }

        public ZoneControlCaptureBonusPoolSO PoolSO => _poolSO;
    }
}
