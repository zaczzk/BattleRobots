using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureTurbineController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureTurbineSO _turbineSO;
        [SerializeField] private PlayerWallet                _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onTurbineSpun;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _bladeLabel;
        [SerializeField] private Text       _spinLabel;
        [SerializeField] private Slider     _bladeBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleSpunDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleSpunDelegate         = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onTurbineSpun?.RegisterCallback(_handleSpunDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onTurbineSpun?.UnregisterCallback(_handleSpunDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_turbineSO == null) return;
            int bonus = _turbineSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_turbineSO == null) return;
            _turbineSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _turbineSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_turbineSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_bladeLabel != null)
                _bladeLabel.text = $"Blades: {_turbineSO.Blades}/{_turbineSO.BladesNeeded}";

            if (_spinLabel != null)
                _spinLabel.text = $"Spins: {_turbineSO.SpinCount}";

            if (_bladeBar != null)
                _bladeBar.value = _turbineSO.BladeProgress;
        }

        public ZoneControlCaptureTurbineSO TurbineSO => _turbineSO;
    }
}
