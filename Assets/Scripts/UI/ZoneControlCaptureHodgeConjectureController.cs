using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureHodgeConjectureController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureHodgeConjectureSO _hodgeConjectureSO;
        [SerializeField] private PlayerWallet                         _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onHodgeConjectureClassified;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _hodgeCycleLabel;
        [SerializeField] private Text       _classifyLabel;
        [SerializeField] private Slider     _hodgeCycleBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleClassifiedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate      = HandlePlayerCaptured;
            _handleBotDelegate         = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleClassifiedDelegate  = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onHodgeConjectureClassified?.RegisterCallback(_handleClassifiedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onHodgeConjectureClassified?.UnregisterCallback(_handleClassifiedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_hodgeConjectureSO == null) return;
            int bonus = _hodgeConjectureSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_hodgeConjectureSO == null) return;
            _hodgeConjectureSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _hodgeConjectureSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_hodgeConjectureSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_hodgeCycleLabel != null)
                _hodgeCycleLabel.text = $"Hodge Cycles: {_hodgeConjectureSO.HodgeCycles}/{_hodgeConjectureSO.HodgeCyclesNeeded}";

            if (_classifyLabel != null)
                _classifyLabel.text = $"Classifications: {_hodgeConjectureSO.ClassificationCount}";

            if (_hodgeCycleBar != null)
                _hodgeCycleBar.value = _hodgeConjectureSO.HodgeCycleProgress;
        }

        public ZoneControlCaptureHodgeConjectureSO HodgeConjectureSO => _hodgeConjectureSO;
    }
}
