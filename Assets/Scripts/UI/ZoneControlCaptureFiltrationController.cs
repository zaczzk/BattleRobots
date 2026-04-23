using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureFiltrationController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureFiltrationSO _filtrationSO;
        [SerializeField] private PlayerWallet                    _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onFiltrationAscended;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _levelLabel;
        [SerializeField] private Text       _filtrationLabel;
        [SerializeField] private Slider     _levelBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleAscendedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleAscendedDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onFiltrationAscended?.RegisterCallback(_handleAscendedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onFiltrationAscended?.UnregisterCallback(_handleAscendedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_filtrationSO == null) return;
            int bonus = _filtrationSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_filtrationSO == null) return;
            _filtrationSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _filtrationSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_filtrationSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_levelLabel != null)
                _levelLabel.text = $"Levels: {_filtrationSO.Levels}/{_filtrationSO.LevelsNeeded}";

            if (_filtrationLabel != null)
                _filtrationLabel.text = $"Filtrations: {_filtrationSO.FiltrationCount}";

            if (_levelBar != null)
                _levelBar.value = _filtrationSO.LevelProgress;
        }

        public ZoneControlCaptureFiltrationSO FiltrationSO => _filtrationSO;
    }
}
