using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureGlacialController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureGlacialSO _glacialSO;
        [SerializeField] private PlayerWallet                _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onGlacialMelt;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _iceLabel;
        [SerializeField] private Text       _meltLabel;
        [SerializeField] private Slider     _iceBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleMeltDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleMeltDelegate         = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onGlacialMelt?.RegisterCallback(_handleMeltDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onGlacialMelt?.UnregisterCallback(_handleMeltDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_glacialSO == null) return;
            int prev  = _glacialSO.MeltCount;
            int bonus = _glacialSO.RecordPlayerCapture();
            if (_glacialSO.MeltCount > prev) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_glacialSO == null) return;
            _glacialSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _glacialSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_glacialSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_iceLabel != null)
                _iceLabel.text = $"Ice: {_glacialSO.IceLevel}/{_glacialSO.MaxIce}";

            if (_meltLabel != null)
                _meltLabel.text = $"Melts: {_glacialSO.MeltCount}";

            if (_iceBar != null)
                _iceBar.value = _glacialSO.IceProgress;
        }

        public ZoneControlCaptureGlacialSO GlacialSO => _glacialSO;
    }
}
