using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCapturePillarController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCapturePillarSO _pillarSO;
        [SerializeField] private PlayerWallet               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onStructureComplete;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _pillarLabel;
        [SerializeField] private Text       _bonusLabel;
        [SerializeField] private Slider     _buildBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleStructureCompleteDelegate;

        private void Awake()
        {
            _handlePlayerDelegate            = HandlePlayerCaptured;
            _handleBotDelegate               = HandleBotCaptured;
            _handleMatchStartedDelegate      = HandleMatchStarted;
            _handleStructureCompleteDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onStructureComplete?.RegisterCallback(_handleStructureCompleteDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onStructureComplete?.UnregisterCallback(_handleStructureCompleteDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_pillarSO == null) return;
            int bonus = _pillarSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_pillarSO == null) return;
            _pillarSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _pillarSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_pillarSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_pillarLabel != null)
                _pillarLabel.text = $"Pillars: {_pillarSO.PillarCount}/{_pillarSO.MaxPillars}";

            if (_bonusLabel != null)
                _bonusLabel.text = $"Bonus Caps: {_pillarSO.BonusCaptureCount}";

            if (_buildBar != null)
                _buildBar.value = _pillarSO.BuildProgress;
        }

        public ZoneControlCapturePillarSO PillarSO => _pillarSO;
    }
}
