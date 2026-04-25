using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureWeilConjecturesController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureWeilConjecturesSO _weilConjecturesSO;
        [SerializeField] private PlayerWallet                        _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onWeilConjecturesVerified;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _zetaTermLabel;
        [SerializeField] private Text       _verificationLabel;
        [SerializeField] private Slider     _zetaTermBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleVerifiedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleVerifiedDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onWeilConjecturesVerified?.RegisterCallback(_handleVerifiedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onWeilConjecturesVerified?.UnregisterCallback(_handleVerifiedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_weilConjecturesSO == null) return;
            int bonus = _weilConjecturesSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_weilConjecturesSO == null) return;
            _weilConjecturesSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _weilConjecturesSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_weilConjecturesSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_zetaTermLabel != null)
                _zetaTermLabel.text = $"Zeta Terms: {_weilConjecturesSO.ZetaTerms}/{_weilConjecturesSO.ZetaTermsNeeded}";

            if (_verificationLabel != null)
                _verificationLabel.text = $"Verifications: {_weilConjecturesSO.VerificationCount}";

            if (_zetaTermBar != null)
                _zetaTermBar.value = _weilConjecturesSO.ZetaTermProgress;
        }

        public ZoneControlCaptureWeilConjecturesSO WeilConjecturesSO => _weilConjecturesSO;
    }
}
