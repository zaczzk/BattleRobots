using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlEarlyBirdBonusController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlEarlyBirdBonusSO _earlyBirdSO;
        [SerializeField] private PlayerWallet                 _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onEarlyDominance;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _progressLabel;
        [SerializeField] private Text       _statusLabel;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerCapturedDelegate;
        private Action _handleBotCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleEarlyDominanceDelegate;

        private void Awake()
        {
            _handlePlayerCapturedDelegate  = HandlePlayerCaptured;
            _handleBotCapturedDelegate     = HandleBotCaptured;
            _handleMatchStartedDelegate    = HandleMatchStarted;
            _handleEarlyDominanceDelegate  = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerCapturedDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onEarlyDominance?.RegisterCallback(_handleEarlyDominanceDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerCapturedDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onEarlyDominance?.UnregisterCallback(_handleEarlyDominanceDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_earlyBirdSO == null) return;
            bool prevComplete = _earlyBirdSO.EarlyComplete;
            _earlyBirdSO.RecordPlayerCapture();
            if (!prevComplete && _earlyBirdSO.EarlyComplete)
                _wallet?.AddFunds(_earlyBirdSO.BonusOnEarlyDominance);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_earlyBirdSO == null) return;
            _earlyBirdSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _earlyBirdSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_earlyBirdSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_progressLabel != null)
                _progressLabel.text =
                    $"Early: {_earlyBirdSO.PlayerEarlyCaptures}/{_earlyBirdSO.RequiredEarlyCaptures}";

            if (_statusLabel != null)
            {
                string status;
                if (_earlyBirdSO.EarlyComplete)
                    status = "DOMINATED!";
                else if (_earlyBirdSO.BotInterrupted)
                    status = "Interrupted";
                else
                    status = "Building...";
                _statusLabel.text = status;
            }
        }

        public ZoneControlEarlyBirdBonusSO EarlyBirdSO => _earlyBirdSO;
    }
}
