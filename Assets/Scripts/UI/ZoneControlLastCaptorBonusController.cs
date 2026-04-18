using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlLastCaptorBonusController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlLastCaptorBonusSO _lastCaptorSO;
        [SerializeField] private PlayerWallet                 _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMatchEnded;
        [SerializeField] private VoidGameEvent _onBonusAwarded;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _statusLabel;
        [SerializeField] private Text       _bonusLabel;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerCapturedDelegate;
        private Action _handleBotCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleMatchEndedDelegate;
        private Action _handleBonusAwardedDelegate;

        private void Awake()
        {
            _handlePlayerCapturedDelegate = HandlePlayerCaptured;
            _handleBotCapturedDelegate    = HandleBotCaptured;
            _handleMatchStartedDelegate   = HandleMatchStarted;
            _handleMatchEndedDelegate     = HandleMatchEnded;
            _handleBonusAwardedDelegate   = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerCapturedDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onMatchEnded?.RegisterCallback(_handleMatchEndedDelegate);
            _onBonusAwarded?.RegisterCallback(_handleBonusAwardedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerCapturedDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
            _onBonusAwarded?.UnregisterCallback(_handleBonusAwardedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_lastCaptorSO == null) return;
            _lastCaptorSO.RecordPlayerCapture();
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_lastCaptorSO == null) return;
            _lastCaptorSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _lastCaptorSO?.Reset();
            Refresh();
        }

        private void HandleMatchEnded()
        {
            if (_lastCaptorSO == null) return;
            int bonus = _lastCaptorSO.ApplyMatchEndBonus();
            if (bonus > 0)
                _wallet?.AddFunds(bonus);
            Refresh();
        }

        public void Refresh()
        {
            if (_lastCaptorSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_statusLabel != null)
            {
                string status = !_lastCaptorSO.HasAnyCapture
                    ? "Last: None"
                    : _lastCaptorSO.PlayerWasLast
                        ? "Last: Player"
                        : "Last: Bot";
                _statusLabel.text = status;
            }

            if (_bonusLabel != null)
                _bonusLabel.text = $"Last Captor Bonus: {_lastCaptorSO.LastBonus}";
        }

        public ZoneControlLastCaptorBonusSO LastCaptorSO => _lastCaptorSO;
    }
}
