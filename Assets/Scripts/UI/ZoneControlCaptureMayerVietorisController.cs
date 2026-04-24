using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureMayerVietorisController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureMayerVietorisSO _mayerSO;
        [SerializeField] private PlayerWallet                       _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMayerVietorisStitched;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _patchLabel;
        [SerializeField] private Text       _stitchLabel;
        [SerializeField] private Slider     _patchBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleStitchedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleStitchedDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onMayerVietorisStitched?.RegisterCallback(_handleStitchedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onMayerVietorisStitched?.UnregisterCallback(_handleStitchedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_mayerSO == null) return;
            int bonus = _mayerSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_mayerSO == null) return;
            _mayerSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _mayerSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_mayerSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_patchLabel != null)
                _patchLabel.text = $"Patches: {_mayerSO.Patches}/{_mayerSO.PatchesNeeded}";

            if (_stitchLabel != null)
                _stitchLabel.text = $"Stitches: {_mayerSO.StitchCount}";

            if (_patchBar != null)
                _patchBar.value = _mayerSO.PatchProgress;
        }

        public ZoneControlCaptureMayerVietorisSO MayerSO => _mayerSO;
    }
}
