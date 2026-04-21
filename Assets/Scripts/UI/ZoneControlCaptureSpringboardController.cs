using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureSpringboardController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureSpringboardSO _springboardSO;
        [SerializeField] private PlayerWallet                    _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onSpringboardLaunched;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _bounceLabel;
        [SerializeField] private Text       _launchLabel;
        [SerializeField] private Slider     _bounceBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleLaunchedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleLaunchedDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onSpringboardLaunched?.RegisterCallback(_handleLaunchedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onSpringboardLaunched?.UnregisterCallback(_handleLaunchedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_springboardSO == null) return;
            int bonus = _springboardSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_springboardSO == null) return;
            _springboardSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _springboardSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_springboardSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_bounceLabel != null)
                _bounceLabel.text = $"Bounces: {_springboardSO.Bounces}/{_springboardSO.BouncesNeeded}";

            if (_launchLabel != null)
                _launchLabel.text = $"Launches: {_springboardSO.LaunchCount}";

            if (_bounceBar != null)
                _bounceBar.value = _springboardSO.BounceProgress;
        }

        public ZoneControlCaptureSpringboardSO SpringboardSO => _springboardSO;
    }
}
