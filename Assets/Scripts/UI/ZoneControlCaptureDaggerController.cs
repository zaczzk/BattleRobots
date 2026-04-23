using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureDaggerController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureDaggerSO _daggerSO;
        [SerializeField] private PlayerWallet               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onDaggerFormed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _edgeLabel;
        [SerializeField] private Text       _daggerLabel;
        [SerializeField] private Slider     _edgeBar;
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
            _onDaggerFormed?.RegisterCallback(_handleFormedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onDaggerFormed?.UnregisterCallback(_handleFormedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_daggerSO == null) return;
            int bonus = _daggerSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_daggerSO == null) return;
            _daggerSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _daggerSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_daggerSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_edgeLabel != null)
                _edgeLabel.text = $"Edges: {_daggerSO.Edges}/{_daggerSO.EdgesNeeded}";

            if (_daggerLabel != null)
                _daggerLabel.text = $"Daggers: {_daggerSO.DaggerCount}";

            if (_edgeBar != null)
                _edgeBar.value = _daggerSO.EdgeProgress;
        }

        public ZoneControlCaptureDaggerSO DaggerSO => _daggerSO;
    }
}
