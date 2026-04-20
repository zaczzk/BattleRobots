using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureCairnController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureCairnSO _cairnSO;
        [SerializeField] private PlayerWallet              _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onCairnComplete;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _stoneLabel;
        [SerializeField] private Text       _cairnLabel;
        [SerializeField] private Slider     _stoneBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleCairnCompleteDelegate;

        private void Awake()
        {
            _handlePlayerDelegate        = HandlePlayerCaptured;
            _handleBotDelegate           = HandleBotCaptured;
            _handleMatchStartedDelegate  = HandleMatchStarted;
            _handleCairnCompleteDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onCairnComplete?.RegisterCallback(_handleCairnCompleteDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onCairnComplete?.UnregisterCallback(_handleCairnCompleteDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_cairnSO == null) return;
            int bonus = _cairnSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_cairnSO == null) return;
            _cairnSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _cairnSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_cairnSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_stoneLabel != null)
                _stoneLabel.text = $"Stones: {_cairnSO.Stones}/{_cairnSO.StonesNeeded}";

            if (_cairnLabel != null)
                _cairnLabel.text = $"Cairns: {_cairnSO.CairnCount}";

            if (_stoneBar != null)
                _stoneBar.value = _cairnSO.StoneProgress;
        }

        public ZoneControlCaptureCairnSO CairnSO => _cairnSO;
    }
}
