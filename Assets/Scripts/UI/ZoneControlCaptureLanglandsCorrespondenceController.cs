using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureLanglandsCorrespondenceController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureLanglandsCorrespondenceSO _langlandsCorrespondenceSO;
        [SerializeField] private PlayerWallet                                 _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onLanglandsCorrespondenceEstablished;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _matchingPairLabel;
        [SerializeField] private Text       _correspondenceLabel;
        [SerializeField] private Slider     _matchingPairBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleEstablishedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate      = HandlePlayerCaptured;
            _handleBotDelegate         = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleEstablishedDelegate  = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onLanglandsCorrespondenceEstablished?.RegisterCallback(_handleEstablishedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onLanglandsCorrespondenceEstablished?.UnregisterCallback(_handleEstablishedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_langlandsCorrespondenceSO == null) return;
            int bonus = _langlandsCorrespondenceSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_langlandsCorrespondenceSO == null) return;
            _langlandsCorrespondenceSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _langlandsCorrespondenceSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_langlandsCorrespondenceSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_matchingPairLabel != null)
                _matchingPairLabel.text = $"Matching Pairs: {_langlandsCorrespondenceSO.MatchingPairs}/{_langlandsCorrespondenceSO.MatchingPairsNeeded}";

            if (_correspondenceLabel != null)
                _correspondenceLabel.text = $"Correspondences: {_langlandsCorrespondenceSO.CorrespondenceCount}";

            if (_matchingPairBar != null)
                _matchingPairBar.value = _langlandsCorrespondenceSO.MatchingPairProgress;
        }

        public ZoneControlCaptureLanglandsCorrespondenceSO LanglandsCorrespondenceSO => _langlandsCorrespondenceSO;
    }
}
