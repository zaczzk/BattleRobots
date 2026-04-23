using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureInitialController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureInitialSO _initialSO;
        [SerializeField] private PlayerWallet                 _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onInitialInjected;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _morphismLabel;
        [SerializeField] private Text       _initialLabel;
        [SerializeField] private Slider     _morphismBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleInjectedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleInjectedDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onInitialInjected?.RegisterCallback(_handleInjectedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onInitialInjected?.UnregisterCallback(_handleInjectedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_initialSO == null) return;
            int bonus = _initialSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_initialSO == null) return;
            _initialSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _initialSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_initialSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_morphismLabel != null)
                _morphismLabel.text = $"Morphisms: {_initialSO.Morphisms}/{_initialSO.MorphismsNeeded}";

            if (_initialLabel != null)
                _initialLabel.text = $"Initials: {_initialSO.InitialCount}";

            if (_morphismBar != null)
                _morphismBar.value = _initialSO.MorphismProgress;
        }

        public ZoneControlCaptureInitialSO InitialSO => _initialSO;
    }
}
