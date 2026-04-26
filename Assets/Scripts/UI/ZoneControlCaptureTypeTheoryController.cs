using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureTypeTheoryController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureTypeTheorySO _typeTheorySO;
        [SerializeField] private PlayerWallet                   _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onTypeTheoryCompleted;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _typeDerivationLabel;
        [SerializeField] private Text       _derivationCountLabel;
        [SerializeField] private Slider     _typeDerivationBar;
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
            _onTypeTheoryCompleted?.RegisterCallback(_handleCompletedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onTypeTheoryCompleted?.UnregisterCallback(_handleCompletedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_typeTheorySO == null) return;
            int bonus = _typeTheorySO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_typeTheorySO == null) return;
            _typeTheorySO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _typeTheorySO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_typeTheorySO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_typeDerivationLabel != null)
                _typeDerivationLabel.text =
                    $"Type Derivations: {_typeTheorySO.TypeDerivations}/{_typeTheorySO.TypeDerivationsNeeded}";

            if (_derivationCountLabel != null)
                _derivationCountLabel.text = $"Derivations: {_typeTheorySO.DerivationCount}";

            if (_typeDerivationBar != null)
                _typeDerivationBar.value = _typeTheorySO.TypeDerivationProgress;
        }

        public ZoneControlCaptureTypeTheorySO TypeTheorySO => _typeTheorySO;
    }
}
