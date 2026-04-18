using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlFirstBloodController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlFirstBloodBonusSO _firstBloodSO;

        [Header("Economy (optional)")]
        [SerializeField] private PlayerWallet _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onFirstBloodPlayer;
        [SerializeField] private VoidGameEvent _onFirstBloodBot;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _statusLabel;
        [SerializeField] private GameObject _playerBadge;
        [SerializeField] private GameObject _botBadge;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerCapturedDelegate;
        private Action _handleBotCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleFirstBloodPlayerDelegate;
        private Action _handleFirstBloodBotDelegate;

        private void Awake()
        {
            _handlePlayerCapturedDelegate    = HandlePlayerCaptured;
            _handleBotCapturedDelegate       = HandleBotCaptured;
            _handleMatchStartedDelegate      = HandleMatchStarted;
            _handleFirstBloodPlayerDelegate  = Refresh;
            _handleFirstBloodBotDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerCapturedDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onFirstBloodPlayer?.RegisterCallback(_handleFirstBloodPlayerDelegate);
            _onFirstBloodBot?.RegisterCallback(_handleFirstBloodBotDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerCapturedDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onFirstBloodPlayer?.UnregisterCallback(_handleFirstBloodPlayerDelegate);
            _onFirstBloodBot?.UnregisterCallback(_handleFirstBloodBotDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_firstBloodSO == null) return;
            int bonus = _firstBloodSO.RecordPlayerCapture();
            if (bonus > 0)
                _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_firstBloodSO == null) return;
            _firstBloodSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _firstBloodSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_firstBloodSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_statusLabel != null)
            {
                if (!_firstBloodSO.FirstBloodFired)
                    _statusLabel.text = "First Blood Pending";
                else if (_firstBloodSO.PlayerWasFirst)
                    _statusLabel.text = "First Blood: Player!";
                else
                    _statusLabel.text = "First Blood: Bot";
            }

            _playerBadge?.SetActive(_firstBloodSO.FirstBloodFired && _firstBloodSO.PlayerWasFirst);
            _botBadge?.SetActive(_firstBloodSO.FirstBloodFired && !_firstBloodSO.PlayerWasFirst);
        }

        public ZoneControlFirstBloodBonusSO FirstBloodSO => _firstBloodSO;
    }
}
