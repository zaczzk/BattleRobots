using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureCantorDiagonalController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureCantorDiagonalSO _cantorDiagonalSO;
        [SerializeField] private PlayerWallet                       _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onCantorDiagonalConstructed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _diagonalConstructionLabel;
        [SerializeField] private Text       _diagonalCountLabel;
        [SerializeField] private Slider     _diagonalBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleDiagonalDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleDiagonalDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onCantorDiagonalConstructed?.RegisterCallback(_handleDiagonalDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onCantorDiagonalConstructed?.UnregisterCallback(_handleDiagonalDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_cantorDiagonalSO == null) return;
            int bonus = _cantorDiagonalSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_cantorDiagonalSO == null) return;
            _cantorDiagonalSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _cantorDiagonalSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_cantorDiagonalSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_diagonalConstructionLabel != null)
                _diagonalConstructionLabel.text =
                    $"Diagonal Constructions: {_cantorDiagonalSO.DiagonalConstructions}/{_cantorDiagonalSO.DiagonalConstructionsNeeded}";

            if (_diagonalCountLabel != null)
                _diagonalCountLabel.text = $"Diagonals: {_cantorDiagonalSO.DiagonalCount}";

            if (_diagonalBar != null)
                _diagonalBar.value = _cantorDiagonalSO.DiagonalConstructionProgress;
        }

        public ZoneControlCaptureCantorDiagonalSO CantorDiagonalSO => _cantorDiagonalSO;
    }
}
