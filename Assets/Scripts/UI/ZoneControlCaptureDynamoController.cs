using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureDynamoController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureDynamoSO _dynamoSO;
        [SerializeField] private PlayerWallet               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onDynamoGenerated;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _fieldLabel;
        [SerializeField] private Text       _generationLabel;
        [SerializeField] private Slider     _fieldBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleGeneratedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleGeneratedDelegate    = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onDynamoGenerated?.RegisterCallback(_handleGeneratedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onDynamoGenerated?.UnregisterCallback(_handleGeneratedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_dynamoSO == null) return;
            int bonus = _dynamoSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_dynamoSO == null) return;
            _dynamoSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _dynamoSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_dynamoSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_fieldLabel != null)
                _fieldLabel.text = $"Fields: {_dynamoSO.Fields}/{_dynamoSO.FieldsNeeded}";

            if (_generationLabel != null)
                _generationLabel.text = $"Generations: {_dynamoSO.GenerationCount}";

            if (_fieldBar != null)
                _fieldBar.value = _dynamoSO.FieldProgress;
        }

        public ZoneControlCaptureDynamoSO DynamoSO => _dynamoSO;
    }
}
