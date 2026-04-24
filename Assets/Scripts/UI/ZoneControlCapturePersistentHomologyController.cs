using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCapturePersistentHomologyController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCapturePersistentHomologySO _persistentHomologySO;
        [SerializeField] private PlayerWallet                            _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onPersistentHomologyPersisted;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _barLabel;
        [SerializeField] private Text       _persistLabel;
        [SerializeField] private Slider     _barBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handlePersistedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handlePersistedDelegate    = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onPersistentHomologyPersisted?.RegisterCallback(_handlePersistedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onPersistentHomologyPersisted?.UnregisterCallback(_handlePersistedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_persistentHomologySO == null) return;
            int bonus = _persistentHomologySO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_persistentHomologySO == null) return;
            _persistentHomologySO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _persistentHomologySO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_persistentHomologySO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_barLabel != null)
                _barLabel.text = $"Bars: {_persistentHomologySO.Bars}/{_persistentHomologySO.BarsNeeded}";

            if (_persistLabel != null)
                _persistLabel.text = $"Persistences: {_persistentHomologySO.PersistCount}";

            if (_barBar != null)
                _barBar.value = _persistentHomologySO.BarProgress;
        }

        public ZoneControlCapturePersistentHomologySO PersistentHomologySO => _persistentHomologySO;
    }
}
