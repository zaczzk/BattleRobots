using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureNebulaController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureNebulaSO _nebulaSO;
        [SerializeField] private PlayerWalletSO              _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onNebulaDispersed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _densityLabel;
        [SerializeField] private Text       _dispersalCountLabel;
        [SerializeField] private Slider     _nebulaBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleNebulaDispersedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate          = HandlePlayerCaptured;
            _handleBotDelegate             = HandleBotCaptured;
            _handleMatchStartedDelegate    = HandleMatchStarted;
            _handleNebulaDispersedDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onNebulaDispersed?.RegisterCallback(_handleNebulaDispersedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onNebulaDispersed?.UnregisterCallback(_handleNebulaDispersedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_nebulaSO == null) return;
            _nebulaSO.RecordPlayerCapture();
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_nebulaSO == null) return;
            int payout = _nebulaSO.RecordBotCapture();
            if (payout > 0)
                _wallet?.AddFunds(payout);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _nebulaSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_nebulaSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_densityLabel != null)
                _densityLabel.text = $"Nebula: {_nebulaSO.CurrentDensity:F0}%";

            if (_dispersalCountLabel != null)
                _dispersalCountLabel.text = $"Dispersals: {_nebulaSO.DispersalCount}";

            if (_nebulaBar != null)
                _nebulaBar.value = _nebulaSO.NebulaProgress;
        }

        public ZoneControlCaptureNebulaSO NebulaSO => _nebulaSO;
    }
}
