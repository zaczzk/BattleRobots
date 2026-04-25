using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureAbcConjectureController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureAbcConjectureSO _abcConjectureSO;
        [SerializeField] private PlayerWallet                       _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onAbcConjectured;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _coprimeTripleLabel;
        [SerializeField] private Text       _conjectureLabel;
        [SerializeField] private Slider     _coprimeTripleBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleConjecturedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleConjecturedDelegate  = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onAbcConjectured?.RegisterCallback(_handleConjecturedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onAbcConjectured?.UnregisterCallback(_handleConjecturedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_abcConjectureSO == null) return;
            int bonus = _abcConjectureSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_abcConjectureSO == null) return;
            _abcConjectureSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _abcConjectureSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_abcConjectureSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_coprimeTripleLabel != null)
                _coprimeTripleLabel.text = $"Coprime Triples: {_abcConjectureSO.CoprimeTriples}/{_abcConjectureSO.CoprimeTriplesNeeded}";

            if (_conjectureLabel != null)
                _conjectureLabel.text = $"Conjectures: {_abcConjectureSO.ConjectureCount}";

            if (_coprimeTripleBar != null)
                _coprimeTripleBar.value = _abcConjectureSO.CoprimeTripleProgress;
        }

        public ZoneControlCaptureAbcConjectureSO AbcConjectureSO => _abcConjectureSO;
    }
}
