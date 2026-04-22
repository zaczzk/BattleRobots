using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCapturePacketController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCapturePacketSO _packetSO;
        [SerializeField] private PlayerWallet               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onPacketDelivered;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _payloadLabel;
        [SerializeField] private Text       _deliveryLabel;
        [SerializeField] private Slider     _payloadBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleDeliveredDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleDeliveredDelegate    = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onPacketDelivered?.RegisterCallback(_handleDeliveredDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onPacketDelivered?.UnregisterCallback(_handleDeliveredDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_packetSO == null) return;
            int bonus = _packetSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_packetSO == null) return;
            _packetSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _packetSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_packetSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_payloadLabel != null)
                _payloadLabel.text = $"Payloads: {_packetSO.Payloads}/{_packetSO.PayloadsNeeded}";

            if (_deliveryLabel != null)
                _deliveryLabel.text = $"Deliveries: {_packetSO.DeliveryCount}";

            if (_payloadBar != null)
                _payloadBar.value = _packetSO.PayloadProgress;
        }

        public ZoneControlCapturePacketSO PacketSO => _packetSO;
    }
}
