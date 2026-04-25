using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureFormalModuliController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureFormalModuliSO _formalModuliSO;
        [SerializeField] private PlayerWallet                      _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onFormalModuliClassified;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _deformationLabel;
        [SerializeField] private Text       _classificationLabel;
        [SerializeField] private Slider     _deformationBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleClassifiedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleClassifiedDelegate   = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onFormalModuliClassified?.RegisterCallback(_handleClassifiedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onFormalModuliClassified?.UnregisterCallback(_handleClassifiedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_formalModuliSO == null) return;
            int bonus = _formalModuliSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_formalModuliSO == null) return;
            _formalModuliSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _formalModuliSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_formalModuliSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_deformationLabel != null)
                _deformationLabel.text = $"Deformations: {_formalModuliSO.Deformations}/{_formalModuliSO.DeformationsNeeded}";

            if (_classificationLabel != null)
                _classificationLabel.text = $"Classifications: {_formalModuliSO.ClassificationCount}";

            if (_deformationBar != null)
                _deformationBar.value = _formalModuliSO.DeformationProgress;
        }

        public ZoneControlCaptureFormalModuliSO FormalModuliSO => _formalModuliSO;
    }
}
