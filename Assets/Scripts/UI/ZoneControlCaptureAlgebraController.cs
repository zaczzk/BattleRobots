using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureAlgebraController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureAlgebraSO _algebraSO;
        [SerializeField] private PlayerWallet                 _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onAlgebraFolded;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _termLabel;
        [SerializeField] private Text       _foldLabel;
        [SerializeField] private Slider     _termBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleFoldedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleFoldedDelegate       = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onAlgebraFolded?.RegisterCallback(_handleFoldedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onAlgebraFolded?.UnregisterCallback(_handleFoldedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_algebraSO == null) return;
            int bonus = _algebraSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_algebraSO == null) return;
            _algebraSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _algebraSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_algebraSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_termLabel != null)
                _termLabel.text = $"Terms: {_algebraSO.Terms}/{_algebraSO.TermsNeeded}";

            if (_foldLabel != null)
                _foldLabel.text = $"Folds: {_algebraSO.FoldCount}";

            if (_termBar != null)
                _termBar.value = _algebraSO.TermProgress;
        }

        public ZoneControlCaptureAlgebraSO AlgebraSO => _algebraSO;
    }
}
