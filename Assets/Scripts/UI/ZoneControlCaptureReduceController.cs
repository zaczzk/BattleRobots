using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureReduceController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureReduceSO _reduceSO;
        [SerializeField] private PlayerWallet               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onReduceComplete;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _elementLabel;
        [SerializeField] private Text       _reduceLabel;
        [SerializeField] private Slider     _elementBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleReduceCompleteDelegate;

        private void Awake()
        {
            _handlePlayerDelegate        = HandlePlayerCaptured;
            _handleBotDelegate           = HandleBotCaptured;
            _handleMatchStartedDelegate  = HandleMatchStarted;
            _handleReduceCompleteDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onReduceComplete?.RegisterCallback(_handleReduceCompleteDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onReduceComplete?.UnregisterCallback(_handleReduceCompleteDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_reduceSO == null) return;
            int bonus = _reduceSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_reduceSO == null) return;
            _reduceSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _reduceSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_reduceSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_elementLabel != null)
                _elementLabel.text = $"Elements: {_reduceSO.Elements}/{_reduceSO.ElementsNeeded}";

            if (_reduceLabel != null)
                _reduceLabel.text = $"Reduces: {_reduceSO.ReduceCount}";

            if (_elementBar != null)
                _elementBar.value = _reduceSO.ElementProgress;
        }

        public ZoneControlCaptureReduceSO ReduceSO => _reduceSO;
    }
}
