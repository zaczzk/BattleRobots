using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureRigController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureRigSO _rigSO;
        [SerializeField] private PlayerWallet            _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onDistributed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _rigCellLabel;
        [SerializeField] private Text       _distributeLabel;
        [SerializeField] private Slider     _rigCellBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleDistributedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleDistributedDelegate  = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onDistributed?.RegisterCallback(_handleDistributedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onDistributed?.UnregisterCallback(_handleDistributedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_rigSO == null) return;
            int bonus = _rigSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_rigSO == null) return;
            _rigSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _rigSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_rigSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_rigCellLabel != null)
                _rigCellLabel.text = $"Rig-Cells: {_rigSO.RigCells}/{_rigSO.RigCellsNeeded}";

            if (_distributeLabel != null)
                _distributeLabel.text = $"Distributions: {_rigSO.DistributeCount}";

            if (_rigCellBar != null)
                _rigCellBar.value = _rigSO.RigCellProgress;
        }

        public ZoneControlCaptureRigSO RigSO => _rigSO;
    }
}
