using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureGödelsCompletenessTheoremController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureGödelsCompletenessTheoremSO _gödelsCompletnessSO;
        [SerializeField] private PlayerWallet                                   _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onGödelsCompletenessTheoremCompleted;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _consistentExtensionLabel;
        [SerializeField] private Text       _completionCountLabel;
        [SerializeField] private Slider     _consistentExtensionBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleCompletedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleCompletedDelegate    = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onGödelsCompletenessTheoremCompleted?.RegisterCallback(_handleCompletedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onGödelsCompletenessTheoremCompleted?.UnregisterCallback(_handleCompletedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_gödelsCompletnessSO == null) return;
            int bonus = _gödelsCompletnessSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_gödelsCompletnessSO == null) return;
            _gödelsCompletnessSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _gödelsCompletnessSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_gödelsCompletnessSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_consistentExtensionLabel != null)
                _consistentExtensionLabel.text =
                    $"Consistent Extensions: {_gödelsCompletnessSO.ConsistentExtensions}/{_gödelsCompletnessSO.ConsistentExtensionsNeeded}";

            if (_completionCountLabel != null)
                _completionCountLabel.text = $"Completions: {_gödelsCompletnessSO.CompletionCount}";

            if (_consistentExtensionBar != null)
                _consistentExtensionBar.value = _gödelsCompletnessSO.ConsistentExtensionProgress;
        }

        public ZoneControlCaptureGödelsCompletenessTheoremSO GödelsCompletenessSO => _gödelsCompletnessSO;
    }
}
