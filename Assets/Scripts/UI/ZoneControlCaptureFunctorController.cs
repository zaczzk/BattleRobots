using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureFunctorController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureFunctorSO _functorSO;
        [SerializeField] private PlayerWallet                _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onFunctorLifted;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _elementLabel;
        [SerializeField] private Text       _liftLabel;
        [SerializeField] private Slider     _elementBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleFunctorLiftedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate        = HandlePlayerCaptured;
            _handleBotDelegate           = HandleBotCaptured;
            _handleMatchStartedDelegate  = HandleMatchStarted;
            _handleFunctorLiftedDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onFunctorLifted?.RegisterCallback(_handleFunctorLiftedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onFunctorLifted?.UnregisterCallback(_handleFunctorLiftedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_functorSO == null) return;
            int bonus = _functorSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_functorSO == null) return;
            _functorSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _functorSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_functorSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_elementLabel != null)
                _elementLabel.text = $"Elements: {_functorSO.Elements}/{_functorSO.ElementsNeeded}";

            if (_liftLabel != null)
                _liftLabel.text = $"Lifts: {_functorSO.LiftCount}";

            if (_elementBar != null)
                _elementBar.value = _functorSO.ElementProgress;
        }

        public ZoneControlCaptureFunctorSO FunctorSO => _functorSO;
    }
}
