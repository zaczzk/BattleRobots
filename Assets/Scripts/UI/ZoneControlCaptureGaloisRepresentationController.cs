using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureGaloisRepresentationController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureGaloisRepresentationSO _galoisRepresentationSO;
        [SerializeField] private PlayerWallet                              _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onGaloisRepresentationRealized;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _representationLabel;
        [SerializeField] private Text       _realizeLabel;
        [SerializeField] private Slider     _representationBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleRealizedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleRealizedDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onGaloisRepresentationRealized?.RegisterCallback(_handleRealizedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onGaloisRepresentationRealized?.UnregisterCallback(_handleRealizedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_galoisRepresentationSO == null) return;
            int bonus = _galoisRepresentationSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_galoisRepresentationSO == null) return;
            _galoisRepresentationSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _galoisRepresentationSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_galoisRepresentationSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_representationLabel != null)
                _representationLabel.text = $"Representations: {_galoisRepresentationSO.Representations}/{_galoisRepresentationSO.RepresentationsNeeded}";

            if (_realizeLabel != null)
                _realizeLabel.text = $"Realizations: {_galoisRepresentationSO.RealizationCount}";

            if (_representationBar != null)
                _representationBar.value = _galoisRepresentationSO.RepresentationProgress;
        }

        public ZoneControlCaptureGaloisRepresentationSO GaloisRepresentationSO => _galoisRepresentationSO;
    }
}
