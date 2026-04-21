using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCapturePestleController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCapturePestleSO _pestleSO;
        [SerializeField] private PlayerWallet               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onPestleProcessed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _poundLabel;
        [SerializeField] private Text       _batchLabel;
        [SerializeField] private Slider     _poundBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleProcessedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleProcessedDelegate    = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onPestleProcessed?.RegisterCallback(_handleProcessedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onPestleProcessed?.UnregisterCallback(_handleProcessedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_pestleSO == null) return;
            int bonus = _pestleSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_pestleSO == null) return;
            _pestleSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _pestleSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_pestleSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_poundLabel != null)
                _poundLabel.text = $"Pounds: {_pestleSO.Pounds}/{_pestleSO.PoundsNeeded}";

            if (_batchLabel != null)
                _batchLabel.text = $"Batches: {_pestleSO.BatchCount}";

            if (_poundBar != null)
                _poundBar.value = _pestleSO.PoundProgress;
        }

        public ZoneControlCapturePestleSO PestleSO => _pestleSO;
    }
}
