using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureCrystalController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureCrystalSO _crystalSO;
        [SerializeField] private PlayerWalletSO               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onShatter;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _crystalLabel;
        [SerializeField] private Text       _shatterCountLabel;
        [SerializeField] private Slider     _crystalBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleShatterDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleShatterDelegate      = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onShatter?.RegisterCallback(_handleShatterDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onShatter?.UnregisterCallback(_handleShatterDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_crystalSO == null) return;
            int prev  = _crystalSO.ShatterCount;
            int bonus = _crystalSO.RecordPlayerCapture();
            if (_crystalSO.ShatterCount > prev)
                _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_crystalSO == null) return;
            _crystalSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _crystalSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_crystalSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_crystalLabel != null)
                _crystalLabel.text = $"Crystal: {_crystalSO.CrystalGrowth}/{_crystalSO.CapturesForShatter}";

            if (_shatterCountLabel != null)
                _shatterCountLabel.text = $"Shatterings: {_crystalSO.ShatterCount}";

            if (_crystalBar != null)
                _crystalBar.value = _crystalSO.CrystalProgress;
        }

        public ZoneControlCaptureCrystalSO CrystalSO => _crystalSO;
    }
}
