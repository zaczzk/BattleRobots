using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureBifunctorController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureBifunctorSO _bifunctorSO;
        [SerializeField] private PlayerWallet                  _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onBifunctorBimapped;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _pairLabel;
        [SerializeField] private Text       _bimapLabel;
        [SerializeField] private Slider     _pairBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleBimappedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleBimappedDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onBifunctorBimapped?.RegisterCallback(_handleBimappedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onBifunctorBimapped?.UnregisterCallback(_handleBimappedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_bifunctorSO == null) return;
            int bonus = _bifunctorSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_bifunctorSO == null) return;
            _bifunctorSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _bifunctorSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_bifunctorSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_pairLabel != null)
                _pairLabel.text = $"Pairs: {_bifunctorSO.Pairs}/{_bifunctorSO.PairsNeeded}";

            if (_bimapLabel != null)
                _bimapLabel.text = $"Bimaps: {_bifunctorSO.BimapCount}";

            if (_pairBar != null)
                _pairBar.value = _bifunctorSO.PairProgress;
        }

        public ZoneControlCaptureBifunctorSO BifunctorSO => _bifunctorSO;
    }
}
