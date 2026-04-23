using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureHomologyController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureHomologySO _homologySO;
        [SerializeField] private PlayerWallet                  _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onHomologyFormed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _cycleLabel;
        [SerializeField] private Text       _homologyLabel;
        [SerializeField] private Slider     _cycleBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleFormedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleFormedDelegate       = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onHomologyFormed?.RegisterCallback(_handleFormedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onHomologyFormed?.UnregisterCallback(_handleFormedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_homologySO == null) return;
            int bonus = _homologySO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_homologySO == null) return;
            _homologySO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _homologySO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_homologySO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_cycleLabel != null)
                _cycleLabel.text = $"Cycles: {_homologySO.Cycles}/{_homologySO.CyclesNeeded}";

            if (_homologyLabel != null)
                _homologyLabel.text = $"Homologies: {_homologySO.HomologyCount}";

            if (_cycleBar != null)
                _cycleBar.value = _homologySO.CycleProgress;
        }

        public ZoneControlCaptureHomologySO HomologySO => _homologySO;
    }
}
