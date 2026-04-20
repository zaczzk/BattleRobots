using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureGlyphController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureGlyphSO _glyphSO;
        [SerializeField] private PlayerWallet              _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onGlyphInscribed;
        [SerializeField] private VoidGameEvent _onEmpowermentEnded;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _statusLabel;
        [SerializeField] private Text       _inscriptionLabel;
        [SerializeField] private Slider     _glyphBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleInscribedDelegate;
        private Action _handleEmpowermentEndedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate          = HandlePlayerCaptured;
            _handleBotDelegate             = HandleBotCaptured;
            _handleMatchStartedDelegate    = HandleMatchStarted;
            _handleInscribedDelegate       = Refresh;
            _handleEmpowermentEndedDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onGlyphInscribed?.RegisterCallback(_handleInscribedDelegate);
            _onEmpowermentEnded?.RegisterCallback(_handleEmpowermentEndedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onGlyphInscribed?.UnregisterCallback(_handleInscribedDelegate);
            _onEmpowermentEnded?.UnregisterCallback(_handleEmpowermentEndedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_glyphSO == null) return;
            int bonus = _glyphSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_glyphSO == null) return;
            _glyphSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _glyphSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_glyphSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_statusLabel != null)
                _statusLabel.text = _glyphSO.IsEmpowered
                    ? $"EMPOWERED ({_glyphSO.EmpoweredRemaining} left)"
                    : $"Glyphs: {_glyphSO.GlyphCount}/{_glyphSO.GlyphsNeeded}";

            if (_inscriptionLabel != null)
                _inscriptionLabel.text = $"Inscriptions: {_glyphSO.InscriptionCount}";

            if (_glyphBar != null)
                _glyphBar.value = _glyphSO.GlyphProgress;
        }

        public ZoneControlCaptureGlyphSO GlyphSO => _glyphSO;
    }
}
