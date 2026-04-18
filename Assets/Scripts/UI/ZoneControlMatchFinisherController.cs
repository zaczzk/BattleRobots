using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlMatchFinisherController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlMatchFinisherSO _finisherSO;

        [Header("Economy (optional)")]
        [SerializeField] private PlayerWallet _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMatchEnded;
        [SerializeField] private VoidGameEvent _onFinisherApplied;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _leadLabel;
        [SerializeField] private Text       _bonusLabel;
        [SerializeField] private GameObject _panel;

        private int _playerCaptureCount;
        private int _botCaptureCount;

        private Action _handlePlayerCapturedDelegate;
        private Action _handleBotCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleMatchEndedDelegate;
        private Action _handleFinisherAppliedDelegate;

        private void Awake()
        {
            _handlePlayerCapturedDelegate  = HandlePlayerCaptured;
            _handleBotCapturedDelegate     = HandleBotCaptured;
            _handleMatchStartedDelegate    = HandleMatchStarted;
            _handleMatchEndedDelegate      = HandleMatchEnded;
            _handleFinisherAppliedDelegate = HandleFinisherApplied;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerCapturedDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onMatchEnded?.RegisterCallback(_handleMatchEndedDelegate);
            _onFinisherApplied?.RegisterCallback(_handleFinisherAppliedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerCapturedDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
            _onFinisherApplied?.UnregisterCallback(_handleFinisherAppliedDelegate);
        }

        private void HandlePlayerCaptured() => _playerCaptureCount++;
        private void HandleBotCaptured()    => _botCaptureCount++;

        private void HandleMatchStarted()
        {
            _playerCaptureCount = 0;
            _botCaptureCount    = 0;
            _finisherSO?.Reset();
            Refresh();
        }

        private void HandleMatchEnded()
        {
            if (_finisherSO == null) return;
            int bonus = _finisherSO.ApplyFinisher(_playerCaptureCount, _botCaptureCount);
            if (bonus > 0)
                _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleFinisherApplied() => Refresh();

        public void Refresh()
        {
            if (_finisherSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            int lead = _playerCaptureCount - _botCaptureCount;
            if (_leadLabel != null)
                _leadLabel.text = $"Lead: {lead}";

            if (_bonusLabel != null)
                _bonusLabel.text = $"Finisher Bonus: {_finisherSO.LastBonus}";
        }

        public ZoneControlMatchFinisherSO FinisherSO => _finisherSO;
    }
}
