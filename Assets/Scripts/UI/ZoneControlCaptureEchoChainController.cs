using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureEchoChainController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureEchoChainSO _echoChainSO;
        [SerializeField] private PlayerWalletSO                _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onEchoHit;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _echoLabel;
        [SerializeField] private Text       _countLabel;
        [SerializeField] private Slider     _multiplierBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleEchoDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleEchoDelegate         = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onEchoHit?.RegisterCallback(_handleEchoDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onEchoHit?.UnregisterCallback(_handleEchoDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_echoChainSO == null) return;
            int bonus = _echoChainSO.RecordPlayerCapture(UnityEngine.Time.time);
            if (bonus > 0)
                _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_echoChainSO == null) return;
            _echoChainSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _echoChainSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_echoChainSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_echoLabel != null)
                _echoLabel.text = $"Echo: {_echoChainSO.CurrentMultiplier}\u00d7";

            if (_countLabel != null)
                _countLabel.text = $"Echoes: {_echoChainSO.EchoCount}";

            if (_multiplierBar != null)
                _multiplierBar.value = _echoChainSO.MultiplierProgress;
        }

        public ZoneControlCaptureEchoChainSO EchoChainSO => _echoChainSO;
    }
}
