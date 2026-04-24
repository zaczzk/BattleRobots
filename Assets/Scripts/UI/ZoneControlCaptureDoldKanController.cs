using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureDoldKanController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureDoldKanSO _doldKanSO;
        [SerializeField] private PlayerWallet                _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onDoldKanCorresponded;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _degeneracyLabel;
        [SerializeField] private Text       _correspondLabel;
        [SerializeField] private Slider     _degeneracyBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleCorrespondedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleCorrespondedDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onDoldKanCorresponded?.RegisterCallback(_handleCorrespondedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onDoldKanCorresponded?.UnregisterCallback(_handleCorrespondedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_doldKanSO == null) return;
            int bonus = _doldKanSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_doldKanSO == null) return;
            _doldKanSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _doldKanSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_doldKanSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_degeneracyLabel != null)
                _degeneracyLabel.text = $"Degeneracies: {_doldKanSO.Degeneracies}/{_doldKanSO.DegeneraciesNeeded}";

            if (_correspondLabel != null)
                _correspondLabel.text = $"Correspondences: {_doldKanSO.CorrespondCount}";

            if (_degeneracyBar != null)
                _degeneracyBar.value = _doldKanSO.DegeneracyProgress;
        }

        public ZoneControlCaptureDoldKanSO DoldKanSO => _doldKanSO;
    }
}
