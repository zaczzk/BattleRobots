using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureScepterController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureScepterSO _scepterSO;
        [SerializeField] private PlayerWallet                _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onScepterInvested;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _gemLabel;
        [SerializeField] private Text       _investitureLabel;
        [SerializeField] private Slider     _gemBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleInvestedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleInvestedDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onScepterInvested?.RegisterCallback(_handleInvestedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onScepterInvested?.UnregisterCallback(_handleInvestedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_scepterSO == null) return;
            int bonus = _scepterSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_scepterSO == null) return;
            _scepterSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _scepterSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_scepterSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_gemLabel != null)
                _gemLabel.text = $"Gems: {_scepterSO.Gems}/{_scepterSO.GemsNeeded}";

            if (_investitureLabel != null)
                _investitureLabel.text = $"Investitures: {_scepterSO.InvestitureCount}";

            if (_gemBar != null)
                _gemBar.value = _scepterSO.GemProgress;
        }

        public ZoneControlCaptureScepterSO ScepterSO => _scepterSO;
    }
}
