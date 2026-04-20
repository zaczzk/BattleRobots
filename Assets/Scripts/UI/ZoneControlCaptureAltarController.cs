using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureAltarController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureAltarSO _altarSO;
        [SerializeField] private PlayerWallet              _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onAltarConsecrated;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _offeringLabel;
        [SerializeField] private Text       _consecrationLabel;
        [SerializeField] private Slider     _offeringBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleConsecratedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleConsecratedDelegate  = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onAltarConsecrated?.RegisterCallback(_handleConsecratedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onAltarConsecrated?.UnregisterCallback(_handleConsecratedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_altarSO == null) return;
            int bonus = _altarSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_altarSO == null) return;
            _altarSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _altarSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_altarSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_offeringLabel != null)
                _offeringLabel.text = $"Offerings: {_altarSO.Offerings}/{_altarSO.CapturesNeeded}";

            if (_consecrationLabel != null)
                _consecrationLabel.text = $"Consecrations: {_altarSO.ConsecrationCount}";

            if (_offeringBar != null)
                _offeringBar.value = _altarSO.OfferingProgress;
        }

        public ZoneControlCaptureAltarSO AltarSO => _altarSO;
    }
}
