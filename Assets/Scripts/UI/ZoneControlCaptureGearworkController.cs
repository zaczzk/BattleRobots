using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureGearworkController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureGearworkSO _gearworkSO;
        [SerializeField] private PlayerWallet                 _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onGearworkMeshed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _gearLabel;
        [SerializeField] private Text       _meshLabel;
        [SerializeField] private Slider     _gearBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleMeshedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleMeshedDelegate       = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onGearworkMeshed?.RegisterCallback(_handleMeshedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onGearworkMeshed?.UnregisterCallback(_handleMeshedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_gearworkSO == null) return;
            int bonus = _gearworkSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_gearworkSO == null) return;
            _gearworkSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _gearworkSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_gearworkSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_gearLabel != null)
                _gearLabel.text = $"Gears: {_gearworkSO.Gears}/{_gearworkSO.GearsNeeded}";

            if (_meshLabel != null)
                _meshLabel.text = $"Meshes: {_gearworkSO.MeshCount}";

            if (_gearBar != null)
                _gearBar.value = _gearworkSO.GearProgress;
        }

        public ZoneControlCaptureGearworkSO GearworkSO => _gearworkSO;
    }
}
