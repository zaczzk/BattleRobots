using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureGoldbachConjectureController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureGoldbachConjectureSO _goldbachSO;
        [SerializeField] private PlayerWallet                           _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onGoldbachConjectureVerified;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _primePairLabel;
        [SerializeField] private Text       _verificationLabel;
        [SerializeField] private Slider     _primePairBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleVerificationDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleVerificationDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onGoldbachConjectureVerified?.RegisterCallback(_handleVerificationDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onGoldbachConjectureVerified?.UnregisterCallback(_handleVerificationDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_goldbachSO == null) return;
            int bonus = _goldbachSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_goldbachSO == null) return;
            _goldbachSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _goldbachSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_goldbachSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_primePairLabel != null)
                _primePairLabel.text = $"Prime Pairs: {_goldbachSO.PrimePairs}/{_goldbachSO.PrimePairsNeeded}";

            if (_verificationLabel != null)
                _verificationLabel.text = $"Verifications: {_goldbachSO.VerificationCount}";

            if (_primePairBar != null)
                _primePairBar.value = _goldbachSO.PrimePairProgress;
        }

        public ZoneControlCaptureGoldbachConjectureSO GoldbachSO => _goldbachSO;
    }
}
