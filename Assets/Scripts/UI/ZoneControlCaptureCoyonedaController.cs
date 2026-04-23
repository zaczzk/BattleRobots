using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureCoyonedaController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureCoyonedaSO _coyonedaSO;
        [SerializeField] private PlayerWallet                 _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onCoyonedaLifted;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _sampleLabel;
        [SerializeField] private Text       _liftLabel;
        [SerializeField] private Slider     _sampleBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleLiftedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleLiftedDelegate       = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onCoyonedaLifted?.RegisterCallback(_handleLiftedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onCoyonedaLifted?.UnregisterCallback(_handleLiftedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_coyonedaSO == null) return;
            int bonus = _coyonedaSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_coyonedaSO == null) return;
            _coyonedaSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _coyonedaSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_coyonedaSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_sampleLabel != null)
                _sampleLabel.text = $"Samples: {_coyonedaSO.Samples}/{_coyonedaSO.SamplesNeeded}";

            if (_liftLabel != null)
                _liftLabel.text = $"Lifts: {_coyonedaSO.LiftCount}";

            if (_sampleBar != null)
                _sampleBar.value = _coyonedaSO.SampleProgress;
        }

        public ZoneControlCaptureCoyonedaSO CoyonedaSO => _coyonedaSO;
    }
}
