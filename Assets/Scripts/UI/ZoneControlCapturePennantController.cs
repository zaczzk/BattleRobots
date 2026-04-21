using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCapturePennantController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCapturePennantSO _pennantSO;
        [SerializeField] private PlayerWallet                 _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onPennantRaised;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _stripeLabel;
        [SerializeField] private Text       _pennantLabel;
        [SerializeField] private Slider     _stripeBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handlePennantRaisedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate        = HandlePlayerCaptured;
            _handleBotDelegate           = HandleBotCaptured;
            _handleMatchStartedDelegate  = HandleMatchStarted;
            _handlePennantRaisedDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onPennantRaised?.RegisterCallback(_handlePennantRaisedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onPennantRaised?.UnregisterCallback(_handlePennantRaisedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_pennantSO == null) return;
            int bonus = _pennantSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_pennantSO == null) return;
            _pennantSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _pennantSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_pennantSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_stripeLabel != null)
                _stripeLabel.text = $"Stripes: {_pennantSO.Stripes}/{_pennantSO.StripesNeeded}";

            if (_pennantLabel != null)
                _pennantLabel.text = $"Pennants: {_pennantSO.PennantCount}";

            if (_stripeBar != null)
                _stripeBar.value = _pennantSO.StripeProgress;
        }

        public ZoneControlCapturePennantSO PennantSO => _pennantSO;
    }
}
