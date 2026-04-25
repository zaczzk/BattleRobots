using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureBrownRepresentabilityController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureBrownRepresentabilitySO _brownRepresentabilitySO;
        [SerializeField] private PlayerWallet                               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onBrownRepresentabilityRepresented;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _representableFunctorLabel;
        [SerializeField] private Text       _representLabel;
        [SerializeField] private Slider     _representableFunctorBar;
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
            _onBrownRepresentabilityRepresented?.RegisterCallback(_handleRepresentedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onBrownRepresentabilityRepresented?.UnregisterCallback(_handleRepresentedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_brownRepresentabilitySO == null) return;
            int bonus = _brownRepresentabilitySO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_brownRepresentabilitySO == null) return;
            _brownRepresentabilitySO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _brownRepresentabilitySO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_brownRepresentabilitySO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_representableFunctorLabel != null)
                _representableFunctorLabel.text = $"Representable Functors: {_brownRepresentabilitySO.RepresentableFunctors}/{_brownRepresentabilitySO.RepresentableFunctorsNeeded}";

            if (_representLabel != null)
                _representLabel.text = $"Representations: {_brownRepresentabilitySO.RepresentationCount}";

            if (_representableFunctorBar != null)
                _representableFunctorBar.value = _brownRepresentabilitySO.RepresentableFunctorProgress;
        }

        public ZoneControlCaptureBrownRepresentabilitySO BrownRepresentabilitySO => _brownRepresentabilitySO;
    }
}
