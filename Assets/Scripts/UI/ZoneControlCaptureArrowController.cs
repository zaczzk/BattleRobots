using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureArrowController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureArrowSO _arrowSO;
        [SerializeField] private PlayerWallet              _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onArrowComposed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _arrowLabel;
        [SerializeField] private Text       _composeLabel;
        [SerializeField] private Slider     _arrowBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleArrowComposedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate        = HandlePlayerCaptured;
            _handleBotDelegate           = HandleBotCaptured;
            _handleMatchStartedDelegate  = HandleMatchStarted;
            _handleArrowComposedDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onArrowComposed?.RegisterCallback(_handleArrowComposedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onArrowComposed?.UnregisterCallback(_handleArrowComposedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_arrowSO == null) return;
            int bonus = _arrowSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_arrowSO == null) return;
            _arrowSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _arrowSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_arrowSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_arrowLabel != null)
                _arrowLabel.text = $"Arrows: {_arrowSO.Arrows}/{_arrowSO.ArrowsNeeded}";

            if (_composeLabel != null)
                _composeLabel.text = $"Composes: {_arrowSO.ComposeCount}";

            if (_arrowBar != null)
                _arrowBar.value = _arrowSO.ArrowProgress;
        }

        public ZoneControlCaptureArrowSO ArrowSO => _arrowSO;
    }
}
