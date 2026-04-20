using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureGardenController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureGardenSO _gardenSO;
        [SerializeField] private PlayerWallet               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onGardenBloomed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _bloomLabel;
        [SerializeField] private Text       _gardenLabel;
        [SerializeField] private Slider     _bloomBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleGardenBloomedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleGardenBloomedDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onGardenBloomed?.RegisterCallback(_handleGardenBloomedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onGardenBloomed?.UnregisterCallback(_handleGardenBloomedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_gardenSO == null) return;
            int bonus = _gardenSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_gardenSO == null) return;
            _gardenSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _gardenSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_gardenSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_bloomLabel != null)
                _bloomLabel.text = $"Blooms: {_gardenSO.Blooms}/{_gardenSO.BloomsNeeded}";

            if (_gardenLabel != null)
                _gardenLabel.text = $"Gardens: {_gardenSO.GardenCount}";

            if (_bloomBar != null)
                _bloomBar.value = _gardenSO.BloomProgress;
        }

        public ZoneControlCaptureGardenSO GardenSO => _gardenSO;
    }
}
