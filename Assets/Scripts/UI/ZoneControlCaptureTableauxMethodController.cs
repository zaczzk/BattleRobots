using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureTableauxMethodController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureTableauxMethodSO _tableauxMethodSO;
        [SerializeField] private PlayerWallet                       _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onTableauxMethodCompleted;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _closedBranchLabel;
        [SerializeField] private Text       _closureCountLabel;
        [SerializeField] private Slider     _closedBranchBar;
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
            _onTableauxMethodCompleted?.RegisterCallback(_handleCompletedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onTableauxMethodCompleted?.UnregisterCallback(_handleCompletedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_tableauxMethodSO == null) return;
            int bonus = _tableauxMethodSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_tableauxMethodSO == null) return;
            _tableauxMethodSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _tableauxMethodSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_tableauxMethodSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_closedBranchLabel != null)
                _closedBranchLabel.text =
                    $"Closed Branches: {_tableauxMethodSO.ClosedBranches}/{_tableauxMethodSO.ClosedBranchesNeeded}";

            if (_closureCountLabel != null)
                _closureCountLabel.text = $"Closures: {_tableauxMethodSO.ClosureCount}";

            if (_closedBranchBar != null)
                _closedBranchBar.value = _tableauxMethodSO.ClosedBranchProgress;
        }

        public ZoneControlCaptureTableauxMethodSO TableauxMethodSO => _tableauxMethodSO;
    }
}
