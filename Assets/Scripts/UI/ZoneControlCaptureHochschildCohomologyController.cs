using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureHochschildCohomologyController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureHochschildCohomologySO _hochschildSO;
        [SerializeField] private PlayerWallet                             _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onHochschildCohomologyDeformed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _deformationLabel;
        [SerializeField] private Text       _deformCountLabel;
        [SerializeField] private Slider     _deformationBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleDeformedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleDeformedDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onHochschildCohomologyDeformed?.RegisterCallback(_handleDeformedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onHochschildCohomologyDeformed?.UnregisterCallback(_handleDeformedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_hochschildSO == null) return;
            int bonus = _hochschildSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_hochschildSO == null) return;
            _hochschildSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _hochschildSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_hochschildSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_deformationLabel != null)
                _deformationLabel.text = $"Deformations: {_hochschildSO.Deformations}/{_hochschildSO.DeformationsNeeded}";

            if (_deformCountLabel != null)
                _deformCountLabel.text = $"Deformings: {_hochschildSO.DeformCount}";

            if (_deformationBar != null)
                _deformationBar.value = _hochschildSO.DeformationProgress;
        }

        public ZoneControlCaptureHochschildCohomologySO HochschildSO => _hochschildSO;
    }
}
