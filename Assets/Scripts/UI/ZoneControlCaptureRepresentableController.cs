using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureRepresentableController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureRepresentableSO _representableSO;
        [SerializeField] private PlayerWallet                       _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onRepresentableRepresented;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _setLabel;
        [SerializeField] private Text       _representationLabel;
        [SerializeField] private Slider     _setBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleRepresentedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleRepresentedDelegate  = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onRepresentableRepresented?.RegisterCallback(_handleRepresentedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onRepresentableRepresented?.UnregisterCallback(_handleRepresentedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_representableSO == null) return;
            int bonus = _representableSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_representableSO == null) return;
            _representableSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _representableSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_representableSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_setLabel != null)
                _setLabel.text = $"Sets: {_representableSO.Sets}/{_representableSO.SetsNeeded}";

            if (_representationLabel != null)
                _representationLabel.text = $"Representations: {_representableSO.RepresentationCount}";

            if (_setBar != null)
                _setBar.value = _representableSO.SetProgress;
        }

        public ZoneControlCaptureRepresentableSO RepresentableSO => _representableSO;
    }
}
