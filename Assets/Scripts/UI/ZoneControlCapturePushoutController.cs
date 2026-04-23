using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCapturePushoutController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCapturePushoutSO _pushoutSO;
        [SerializeField] private PlayerWallet                 _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onPushoutPushed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _arrowLabel;
        [SerializeField] private Text       _pushoutLabel;
        [SerializeField] private Slider     _arrowBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handlePushedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handlePushedDelegate       = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onPushoutPushed?.RegisterCallback(_handlePushedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onPushoutPushed?.UnregisterCallback(_handlePushedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_pushoutSO == null) return;
            int bonus = _pushoutSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_pushoutSO == null) return;
            _pushoutSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _pushoutSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_pushoutSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_arrowLabel != null)
                _arrowLabel.text = $"Arrows: {_pushoutSO.Arrows}/{_pushoutSO.ArrowsNeeded}";

            if (_pushoutLabel != null)
                _pushoutLabel.text = $"Pushouts: {_pushoutSO.PushoutCount}";

            if (_arrowBar != null)
                _arrowBar.value = _pushoutSO.ArrowProgress;
        }

        public ZoneControlCapturePushoutSO PushoutSO => _pushoutSO;
    }
}
