using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureQuillController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureQuillSO _quillSO;
        [SerializeField] private PlayerWallet              _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onQuillInscribed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _strokeLabel;
        [SerializeField] private Text       _inscriptionLabel;
        [SerializeField] private Slider     _strokeBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleQuillInscribedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate         = HandlePlayerCaptured;
            _handleBotDelegate            = HandleBotCaptured;
            _handleMatchStartedDelegate   = HandleMatchStarted;
            _handleQuillInscribedDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onQuillInscribed?.RegisterCallback(_handleQuillInscribedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onQuillInscribed?.UnregisterCallback(_handleQuillInscribedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_quillSO == null) return;
            int bonus = _quillSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_quillSO == null) return;
            _quillSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _quillSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_quillSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_strokeLabel != null)
                _strokeLabel.text = $"Strokes: {_quillSO.Strokes}/{_quillSO.StrokesNeeded}";

            if (_inscriptionLabel != null)
                _inscriptionLabel.text = $"Inscriptions: {_quillSO.InscriptionCount}";

            if (_strokeBar != null)
                _strokeBar.value = _quillSO.StrokeProgress;
        }

        public ZoneControlCaptureQuillSO QuillSO => _quillSO;
    }
}
