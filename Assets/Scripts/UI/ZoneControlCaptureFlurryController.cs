using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureFlurryController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureFlurrySO _flurrySO;
        [SerializeField] private PlayerWalletSO             _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onFlurry;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _flurryLabel;
        [SerializeField] private Text       _flurryCountLabel;
        [SerializeField] private Slider     _progressBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleFlurryDelegate;

        private void Awake()
        {
            _handlePlayerCapturedDelegate = HandlePlayerCaptured;
            _handleMatchStartedDelegate   = HandleMatchStarted;
            _handleFlurryDelegate         = HandleFlurry;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onFlurry?.RegisterCallback(_handleFlurryDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onFlurry?.UnregisterCallback(_handleFlurryDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_flurrySO == null) return;
            _flurrySO.RecordCapture(Time.time);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _flurrySO?.Reset();
            Refresh();
        }

        private void HandleFlurry()
        {
            if (_flurrySO == null) return;
            _wallet?.AddFunds(_flurrySO.BonusPerFlurry);
            Refresh();
        }

        public void Refresh()
        {
            if (_flurrySO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_flurryLabel != null)
                _flurryLabel.text = $"Flurry: {_flurrySO.CaptureCount}/{_flurrySO.FlurryTarget}";

            if (_flurryCountLabel != null)
                _flurryCountLabel.text = $"Flurries: {_flurrySO.FlurryCount}";

            if (_progressBar != null)
                _progressBar.value = _flurrySO.FlurryProgress;
        }

        public ZoneControlCaptureFlurrySO FlurrySO => _flurrySO;
    }
}
