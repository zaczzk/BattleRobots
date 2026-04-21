using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureCamshaftController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureCamshaftSO _camshaftSO;
        [SerializeField] private PlayerWallet                 _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onCamshaftRotated;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _lobeLabel;
        [SerializeField] private Text       _rotationLabel;
        [SerializeField] private Slider     _lobeBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleRotatedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleRotatedDelegate      = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onCamshaftRotated?.RegisterCallback(_handleRotatedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onCamshaftRotated?.UnregisterCallback(_handleRotatedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_camshaftSO == null) return;
            int bonus = _camshaftSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_camshaftSO == null) return;
            _camshaftSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _camshaftSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_camshaftSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_lobeLabel != null)
                _lobeLabel.text = $"Lobes: {_camshaftSO.Lobes}/{_camshaftSO.LobesNeeded}";

            if (_rotationLabel != null)
                _rotationLabel.text = $"Rotations: {_camshaftSO.RotationCount}";

            if (_lobeBar != null)
                _lobeBar.value = _camshaftSO.LobeProgress;
        }

        public ZoneControlCaptureCamshaftSO CamshaftSO => _camshaftSO;
    }
}
