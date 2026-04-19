using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCapturePingPongController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCapturePingPongSO _pingPongSO;
        [SerializeField] private PlayerWalletSO               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onPingPong;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _pingPongLabel;
        [SerializeField] private Text       _bonusLabel;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerCapturedDelegate;
        private Action _handleBotCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handlePingPongDelegate;

        private void Awake()
        {
            _handlePlayerCapturedDelegate = HandlePlayerCaptured;
            _handleBotCapturedDelegate    = HandleBotCaptured;
            _handleMatchStartedDelegate   = HandleMatchStarted;
            _handlePingPongDelegate       = HandlePingPong;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerCapturedDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onPingPong?.RegisterCallback(_handlePingPongDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerCapturedDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onPingPong?.UnregisterCallback(_handlePingPongDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_pingPongSO == null) return;
            _pingPongSO.RecordPlayerCapture();
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_pingPongSO == null) return;
            _pingPongSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _pingPongSO?.Reset();
            Refresh();
        }

        private void HandlePingPong()
        {
            if (_pingPongSO == null) return;
            _wallet?.AddFunds(_pingPongSO.BonusPerPingPong);
            Refresh();
        }

        public void Refresh()
        {
            if (_pingPongSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_pingPongLabel != null)
                _pingPongLabel.text = $"Alternations: {_pingPongSO.PingPongCount}";

            if (_bonusLabel != null)
                _bonusLabel.text = $"Ping-Pong Bonus: {_pingPongSO.TotalBonusAwarded}";
        }

        public ZoneControlCapturePingPongSO PingPongSO => _pingPongSO;
    }
}
