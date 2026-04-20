using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureSundialController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureSundialSO _sundialSO;
        [SerializeField] private PlayerWallet                _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onNoonReached;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _hourLabel;
        [SerializeField] private Text       _noonLabel;
        [SerializeField] private Slider     _hourBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleNoonReachedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleNoonReachedDelegate  = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onNoonReached?.RegisterCallback(_handleNoonReachedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onNoonReached?.UnregisterCallback(_handleNoonReachedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_sundialSO == null) return;
            int bonus = _sundialSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_sundialSO == null) return;
            _sundialSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _sundialSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_sundialSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_hourLabel != null)
                _hourLabel.text = $"Hours: {_sundialSO.Hours}/{_sundialSO.HoursNeeded}";

            if (_noonLabel != null)
                _noonLabel.text = $"Noons: {_sundialSO.NoonCount}";

            if (_hourBar != null)
                _hourBar.value = _sundialSO.HourProgress;
        }

        public ZoneControlCaptureSundialSO SundialSO => _sundialSO;
    }
}
