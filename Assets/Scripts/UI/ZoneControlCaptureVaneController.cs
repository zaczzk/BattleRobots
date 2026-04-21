using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureVaneController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureVaneSO _vaneSO;
        [SerializeField] private PlayerWallet             _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onVaneSpun;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _spinLabel;
        [SerializeField] private Text       _rotationLabel;
        [SerializeField] private Slider     _spinBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleVaneSpunDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleVaneSpunDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onVaneSpun?.RegisterCallback(_handleVaneSpunDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onVaneSpun?.UnregisterCallback(_handleVaneSpunDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_vaneSO == null) return;
            int bonus = _vaneSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_vaneSO == null) return;
            _vaneSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _vaneSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_vaneSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_spinLabel != null)
                _spinLabel.text = $"Spins: {_vaneSO.Spins}/{_vaneSO.SpinsNeeded}";

            if (_rotationLabel != null)
                _rotationLabel.text = $"Rotations: {_vaneSO.RotationCount}";

            if (_spinBar != null)
                _spinBar.value = _vaneSO.SpinProgress;
        }

        public ZoneControlCaptureVaneSO VaneSO => _vaneSO;
    }
}
