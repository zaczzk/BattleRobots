using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureLambdaController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureLambdaSO _lambdaSO;
        [SerializeField] private PlayerWallet               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onLambdaExecuted;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _lambdaLabel;
        [SerializeField] private Text       _executeLabel;
        [SerializeField] private Slider     _lambdaBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleLambdaExecutedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate        = HandlePlayerCaptured;
            _handleBotDelegate           = HandleBotCaptured;
            _handleMatchStartedDelegate  = HandleMatchStarted;
            _handleLambdaExecutedDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onLambdaExecuted?.RegisterCallback(_handleLambdaExecutedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onLambdaExecuted?.UnregisterCallback(_handleLambdaExecutedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_lambdaSO == null) return;
            int bonus = _lambdaSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_lambdaSO == null) return;
            _lambdaSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _lambdaSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_lambdaSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_lambdaLabel != null)
                _lambdaLabel.text = $"Lambdas: {_lambdaSO.Lambdas}/{_lambdaSO.LambdasNeeded}";

            if (_executeLabel != null)
                _executeLabel.text = $"Executions: {_lambdaSO.ExecutionCount}";

            if (_lambdaBar != null)
                _lambdaBar.value = _lambdaSO.LambdaProgress;
        }

        public ZoneControlCaptureLambdaSO LambdaSO => _lambdaSO;
    }
}
