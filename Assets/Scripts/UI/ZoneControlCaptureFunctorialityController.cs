using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureFunctorialityController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureFunctorialitySO _functorialitySO;
        [SerializeField] private PlayerWallet                       _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onFunctorialityRealized;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _transferLabel;
        [SerializeField] private Text       _realizeLabel;
        [SerializeField] private Slider     _transferBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleRealizedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleRealizedDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onFunctorialityRealized?.RegisterCallback(_handleRealizedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onFunctorialityRealized?.UnregisterCallback(_handleRealizedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_functorialitySO == null) return;
            int bonus = _functorialitySO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_functorialitySO == null) return;
            _functorialitySO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _functorialitySO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_functorialitySO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_transferLabel != null)
                _transferLabel.text = $"Transfers: {_functorialitySO.Transfers}/{_functorialitySO.TransfersNeeded}";

            if (_realizeLabel != null)
                _realizeLabel.text = $"Realizations: {_functorialitySO.RealizationCount}";

            if (_transferBar != null)
                _transferBar.value = _functorialitySO.TransferProgress;
        }

        public ZoneControlCaptureFunctorialitySO FunctorialitySO => _functorialitySO;
    }
}
