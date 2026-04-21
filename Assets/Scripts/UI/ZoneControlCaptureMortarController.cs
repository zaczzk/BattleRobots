using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureMortarController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureMortarSO _mortarSO;
        [SerializeField] private PlayerWallet               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMortarGround;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _grindLabel;
        [SerializeField] private Text       _grindCountLabel;
        [SerializeField] private Slider     _grindBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleGroundDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleGroundDelegate       = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onMortarGround?.RegisterCallback(_handleGroundDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onMortarGround?.UnregisterCallback(_handleGroundDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_mortarSO == null) return;
            int bonus = _mortarSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_mortarSO == null) return;
            _mortarSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _mortarSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_mortarSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_grindLabel != null)
                _grindLabel.text = $"Grinds: {_mortarSO.Grinds}/{_mortarSO.GrindsNeeded}";

            if (_grindCountLabel != null)
                _grindCountLabel.text = $"Total Grinds: {_mortarSO.GrindCount}";

            if (_grindBar != null)
                _grindBar.value = _mortarSO.GrindProgress;
        }

        public ZoneControlCaptureMortarSO MortarSO => _mortarSO;
    }
}
