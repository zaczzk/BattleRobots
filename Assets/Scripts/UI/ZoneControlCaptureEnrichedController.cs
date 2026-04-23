using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureEnrichedController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureEnrichedSO _enrichedSO;
        [SerializeField] private PlayerWallet                  _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onEnrichedCategoryFormed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _morphismLabel;
        [SerializeField] private Text       _enrichedLabel;
        [SerializeField] private Slider     _morphismBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleFormedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleFormedDelegate       = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onEnrichedCategoryFormed?.RegisterCallback(_handleFormedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onEnrichedCategoryFormed?.UnregisterCallback(_handleFormedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_enrichedSO == null) return;
            int bonus = _enrichedSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_enrichedSO == null) return;
            _enrichedSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _enrichedSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_enrichedSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_morphismLabel != null)
                _morphismLabel.text = $"Morphisms: {_enrichedSO.Morphisms}/{_enrichedSO.MorphismsNeeded}";

            if (_enrichedLabel != null)
                _enrichedLabel.text = $"Enriched: {_enrichedSO.EnrichedCount}";

            if (_morphismBar != null)
                _morphismBar.value = _enrichedSO.MorphismProgress;
        }

        public ZoneControlCaptureEnrichedSO EnrichedSO => _enrichedSO;
    }
}
