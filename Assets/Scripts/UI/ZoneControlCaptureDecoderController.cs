using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureDecoderController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureDecoderSO _decoderSO;
        [SerializeField] private PlayerWallet                 _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onDecoderDecoded;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _packetLabel;
        [SerializeField] private Text       _decodeLabel;
        [SerializeField] private Slider     _packetBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleDecodedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleDecodedDelegate      = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onDecoderDecoded?.RegisterCallback(_handleDecodedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onDecoderDecoded?.UnregisterCallback(_handleDecodedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_decoderSO == null) return;
            int bonus = _decoderSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_decoderSO == null) return;
            _decoderSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _decoderSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_decoderSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_packetLabel != null)
                _packetLabel.text = $"Packets: {_decoderSO.Packets}/{_decoderSO.PacketsNeeded}";

            if (_decodeLabel != null)
                _decodeLabel.text = $"Decodes: {_decoderSO.DecodeCount}";

            if (_packetBar != null)
                _packetBar.value = _decoderSO.PacketProgress;
        }

        public ZoneControlCaptureDecoderSO DecoderSO => _decoderSO;
    }
}
