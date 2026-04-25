using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureKolmogorovComplexityController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureKolmogorovComplexitySO _kolmogorovSO;
        [SerializeField] private PlayerWallet                             _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onDescriptionCompressed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _descriptionLabel;
        [SerializeField] private Text       _compressionLabel;
        [SerializeField] private Slider     _descriptionBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleCompressedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleCompressedDelegate   = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onDescriptionCompressed?.RegisterCallback(_handleCompressedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onDescriptionCompressed?.UnregisterCallback(_handleCompressedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_kolmogorovSO == null) return;
            int bonus = _kolmogorovSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_kolmogorovSO == null) return;
            _kolmogorovSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _kolmogorovSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_kolmogorovSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_descriptionLabel != null)
                _descriptionLabel.text = $"Descriptions: {_kolmogorovSO.CompressedDescriptions}/{_kolmogorovSO.CompressedDescriptionsNeeded}";

            if (_compressionLabel != null)
                _compressionLabel.text = $"Compressions: {_kolmogorovSO.CompressionCount}";

            if (_descriptionBar != null)
                _descriptionBar.value = _kolmogorovSO.DescriptionProgress;
        }

        public ZoneControlCaptureKolmogorovComplexitySO KolmogorovSO => _kolmogorovSO;
    }
}
