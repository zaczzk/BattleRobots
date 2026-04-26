using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureSubstructuralLogicController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureSubstructuralLogicSO _substructuralLogicSO;
        [SerializeField] private PlayerWallet                           _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onSubstructuralLogicCompleted;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _structuralRuleLabel;
        [SerializeField] private Text       _ruleApplicationLabel;
        [SerializeField] private Slider     _structuralRuleBar;
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
            _onSubstructuralLogicCompleted?.RegisterCallback(_handleCompletedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onSubstructuralLogicCompleted?.UnregisterCallback(_handleCompletedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_substructuralLogicSO == null) return;
            int bonus = _substructuralLogicSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_substructuralLogicSO == null) return;
            _substructuralLogicSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _substructuralLogicSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_substructuralLogicSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_structuralRuleLabel != null)
                _structuralRuleLabel.text =
                    $"Structural Rules: {_substructuralLogicSO.StructuralRules}/{_substructuralLogicSO.StructuralRulesNeeded}";

            if (_ruleApplicationLabel != null)
                _ruleApplicationLabel.text = $"Applications: {_substructuralLogicSO.RuleApplicationCount}";

            if (_structuralRuleBar != null)
                _structuralRuleBar.value = _substructuralLogicSO.StructuralRuleProgress;
        }

        public ZoneControlCaptureSubstructuralLogicSO SubstructuralLogicSO => _substructuralLogicSO;
    }
}
