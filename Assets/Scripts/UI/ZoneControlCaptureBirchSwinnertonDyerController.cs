using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureBirchSwinnertonDyerController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureBirchSwinnertonDyerSO _birchSwinnertonDyerSO;
        [SerializeField] private PlayerWallet                             _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onBirchSwinnertonDyerVerified;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _rationalPointLabel;
        [SerializeField] private Text       _verificationLabel;
        [SerializeField] private Slider     _rationalPointBar;
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
            _onBirchSwinnertonDyerVerified?.RegisterCallback(_handleVerifiedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onBirchSwinnertonDyerVerified?.UnregisterCallback(_handleVerifiedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_birchSwinnertonDyerSO == null) return;
            int bonus = _birchSwinnertonDyerSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_birchSwinnertonDyerSO == null) return;
            _birchSwinnertonDyerSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _birchSwinnertonDyerSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_birchSwinnertonDyerSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_rationalPointLabel != null)
                _rationalPointLabel.text = $"Rational Points: {_birchSwinnertonDyerSO.RationalPoints}/{_birchSwinnertonDyerSO.RationalPointsNeeded}";

            if (_verificationLabel != null)
                _verificationLabel.text = $"Verifications: {_birchSwinnertonDyerSO.VerificationCount}";

            if (_rationalPointBar != null)
                _rationalPointBar.value = _birchSwinnertonDyerSO.RationalPointProgress;
        }

        public ZoneControlCaptureBirchSwinnertonDyerSO BirchSwinnertonDyerSO => _birchSwinnertonDyerSO;
    }
}
