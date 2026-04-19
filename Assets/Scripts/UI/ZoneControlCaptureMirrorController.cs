using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureMirrorController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureMirrorSO _mirrorSO;
        [SerializeField] private PlayerWalletSO             _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMirrorHit;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _scoreLabel;
        [SerializeField] private Text       _mirrorCountLabel;
        [SerializeField] private Text       _statusLabel;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerCaptureDelegate;
        private Action _handleBotCaptureDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleMirrorHitDelegate;

        private void Awake()
        {
            _handlePlayerCaptureDelegate = HandlePlayerCaptured;
            _handleBotCaptureDelegate    = HandleBotCaptured;
            _handleMatchStartedDelegate  = HandleMatchStarted;
            _handleMirrorHitDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerCaptureDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotCaptureDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onMirrorHit?.RegisterCallback(_handleMirrorHitDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerCaptureDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotCaptureDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onMirrorHit?.UnregisterCallback(_handleMirrorHitDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_mirrorSO == null) return;
            int prev = _mirrorSO.MirrorCount;
            _mirrorSO.RecordPlayerCapture();
            if (_mirrorSO.MirrorCount > prev)
                _wallet?.AddFunds(_mirrorSO.BonusPerMirror);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_mirrorSO == null) return;
            _mirrorSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _mirrorSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_mirrorSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_scoreLabel != null)
                _scoreLabel.text = $"P: {_mirrorSO.PlayerCaptures} / B: {_mirrorSO.BotCaptures}";

            if (_mirrorCountLabel != null)
                _mirrorCountLabel.text = $"Mirrors: {_mirrorSO.MirrorCount}";

            if (_statusLabel != null)
                _statusLabel.text = _mirrorSO.IsTied ? "MIRROR!" : "Normal";
        }

        public ZoneControlCaptureMirrorSO MirrorSO => _mirrorSO;
    }
}
