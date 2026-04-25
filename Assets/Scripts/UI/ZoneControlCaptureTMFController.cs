using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureTMFController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureTMFSO _tmfSO;
        [SerializeField] private PlayerWallet            _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onTMFResolved;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _levelStructureLabel;
        [SerializeField] private Text       _resolveLabel;
        [SerializeField] private Slider     _levelStructureBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleResolvedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleResolvedDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onTMFResolved?.RegisterCallback(_handleResolvedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onTMFResolved?.UnregisterCallback(_handleResolvedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_tmfSO == null) return;
            int bonus = _tmfSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_tmfSO == null) return;
            _tmfSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _tmfSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_tmfSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_levelStructureLabel != null)
                _levelStructureLabel.text = $"Level Structures: {_tmfSO.LevelStructures}/{_tmfSO.LevelStructuresNeeded}";

            if (_resolveLabel != null)
                _resolveLabel.text = $"Resolutions: {_tmfSO.ResolutionCount}";

            if (_levelStructureBar != null)
                _levelStructureBar.value = _tmfSO.LevelStructureProgress;
        }

        public ZoneControlCaptureTMFSO TMFSO => _tmfSO;
    }
}
