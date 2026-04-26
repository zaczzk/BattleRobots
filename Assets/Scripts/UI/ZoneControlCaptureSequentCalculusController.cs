using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureSequentCalculusController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureSequentCalculusSO _sequentCalculusSO;
        [SerializeField] private PlayerWallet                        _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onSequentDerivationCompleted;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _sequentDerivationLabel;
        [SerializeField] private Text       _derivationCountLabel;
        [SerializeField] private Slider     _sequentDerivationBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleCompletedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleCompletedDelegate    = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onSequentDerivationCompleted?.RegisterCallback(_handleCompletedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onSequentDerivationCompleted?.UnregisterCallback(_handleCompletedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_sequentCalculusSO == null) return;
            int bonus = _sequentCalculusSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_sequentCalculusSO == null) return;
            _sequentCalculusSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _sequentCalculusSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_sequentCalculusSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_sequentDerivationLabel != null)
                _sequentDerivationLabel.text =
                    $"Sequent Derivations: {_sequentCalculusSO.SequentDerivations}/{_sequentCalculusSO.SequentDerivationsNeeded}";

            if (_derivationCountLabel != null)
                _derivationCountLabel.text = $"Derivations: {_sequentCalculusSO.DerivationCount}";

            if (_sequentDerivationBar != null)
                _sequentDerivationBar.value = _sequentCalculusSO.SequentDerivationProgress;
        }

        public ZoneControlCaptureSequentCalculusSO SequentCalculusSO => _sequentCalculusSO;
    }
}
