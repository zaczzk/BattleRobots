using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureFourColorTheoremController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureFourColorTheoremSO _fourColorSO;
        [SerializeField] private PlayerWallet                         _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onFourColorTheoremColored;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _reducibleConfigLabel;
        [SerializeField] private Text       _coloringLabel;
        [SerializeField] private Slider     _reducibleConfigBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleColoringDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleColoringDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onFourColorTheoremColored?.RegisterCallback(_handleColoringDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onFourColorTheoremColored?.UnregisterCallback(_handleColoringDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_fourColorSO == null) return;
            int bonus = _fourColorSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_fourColorSO == null) return;
            _fourColorSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _fourColorSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_fourColorSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_reducibleConfigLabel != null)
                _reducibleConfigLabel.text = $"Reducible Configs: {_fourColorSO.ReducibleConfigs}/{_fourColorSO.ReducibleConfigsNeeded}";

            if (_coloringLabel != null)
                _coloringLabel.text = $"Colorings: {_fourColorSO.ColoringCount}";

            if (_reducibleConfigBar != null)
                _reducibleConfigBar.value = _fourColorSO.ReducibleConfigProgress;
        }

        public ZoneControlCaptureFourColorTheoremSO FourColorSO => _fourColorSO;
    }
}
