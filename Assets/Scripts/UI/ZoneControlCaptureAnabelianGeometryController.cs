using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureAnabelianGeometryController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureAnabelianGeometrySO _anabelianGeometrySO;
        [SerializeField] private PlayerWallet                          _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onAnabelianGeometryReconstructed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _fundamentalGroupLabel;
        [SerializeField] private Text       _reconstructLabel;
        [SerializeField] private Slider     _fundamentalGroupBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleReconstructedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate         = HandlePlayerCaptured;
            _handleBotDelegate            = HandleBotCaptured;
            _handleMatchStartedDelegate   = HandleMatchStarted;
            _handleReconstructedDelegate  = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onAnabelianGeometryReconstructed?.RegisterCallback(_handleReconstructedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onAnabelianGeometryReconstructed?.UnregisterCallback(_handleReconstructedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_anabelianGeometrySO == null) return;
            int bonus = _anabelianGeometrySO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_anabelianGeometrySO == null) return;
            _anabelianGeometrySO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _anabelianGeometrySO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_anabelianGeometrySO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_fundamentalGroupLabel != null)
                _fundamentalGroupLabel.text = $"Fund. Group Data: {_anabelianGeometrySO.FundamentalGroupData}/{_anabelianGeometrySO.FundamentalGroupDataNeeded}";

            if (_reconstructLabel != null)
                _reconstructLabel.text = $"Reconstructions: {_anabelianGeometrySO.ReconstructionCount}";

            if (_fundamentalGroupBar != null)
                _fundamentalGroupBar.value = _anabelianGeometrySO.FundamentalGroupProgress;
        }

        public ZoneControlCaptureAnabelianGeometrySO AnabelianGeometrySO => _anabelianGeometrySO;
    }
}
