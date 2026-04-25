using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureBanachTarskiController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureBanachTarskiSO _banachTarskiSO;
        [SerializeField] private PlayerWallet                     _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onBanachTarskiParadoxCompleted;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _decompositionPieceLabel;
        [SerializeField] private Text       _paradoxCountLabel;
        [SerializeField] private Slider     _decompositionBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleParadoxDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleParadoxDelegate      = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onBanachTarskiParadoxCompleted?.RegisterCallback(_handleParadoxDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onBanachTarskiParadoxCompleted?.UnregisterCallback(_handleParadoxDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_banachTarskiSO == null) return;
            int bonus = _banachTarskiSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_banachTarskiSO == null) return;
            _banachTarskiSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _banachTarskiSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_banachTarskiSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_decompositionPieceLabel != null)
                _decompositionPieceLabel.text =
                    $"Decomposition Pieces: {_banachTarskiSO.DecompositionPieces}/{_banachTarskiSO.DecompositionPiecesNeeded}";

            if (_paradoxCountLabel != null)
                _paradoxCountLabel.text = $"Paradoxes: {_banachTarskiSO.ParadoxCount}";

            if (_decompositionBar != null)
                _decompositionBar.value = _banachTarskiSO.DecompositionPieceProgress;
        }

        public ZoneControlCaptureBanachTarskiSO BanachTarskiSO => _banachTarskiSO;
    }
}
