using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureLeverController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureLeverSO _leverSO;
        [SerializeField] private PlayerWallet              _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onLeverFulcrumed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _liftLabel;
        [SerializeField] private Text       _fulcrumLabel;
        [SerializeField] private Slider     _liftBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleFulcrumedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleFulcrumedDelegate    = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onLeverFulcrumed?.RegisterCallback(_handleFulcrumedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onLeverFulcrumed?.UnregisterCallback(_handleFulcrumedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_leverSO == null) return;
            int bonus = _leverSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_leverSO == null) return;
            _leverSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _leverSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_leverSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_liftLabel != null)
                _liftLabel.text = $"Lifts: {_leverSO.Lifts}/{_leverSO.LiftsNeeded}";

            if (_fulcrumLabel != null)
                _fulcrumLabel.text = $"Fulcrums: {_leverSO.FulcrumCount}";

            if (_liftBar != null)
                _liftBar.value = _leverSO.LiftProgress;
        }

        public ZoneControlCaptureLeverSO LeverSO => _leverSO;
    }
}
