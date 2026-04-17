using System;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Core MonoBehaviour that bridges <see cref="ZoneControlThreatAssessmentSO"/>
    /// threat-level changes into <see cref="ZoneControlThreatPressureBridgeSO"/>.
    ///
    /// Subscribes <c>_onThreatChanged</c> → <see cref="HandleThreatChanged"/> →
    /// reads <c>_threatSO.CurrentThreat</c> and calls <c>_bridgeSO.ApplyBridge</c>.
    /// Subscribes <c>_onMatchStarted</c> → <see cref="HandleMatchStarted"/> →
    /// resets the bridge SO.
    ///
    /// No Update loop; no UI references.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlThreatPressureBridgeController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlThreatPressureBridgeSO _bridgeSO;
        [SerializeField] private ZoneControlThreatAssessmentSO     _threatSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onThreatChanged;
        [SerializeField] private VoidGameEvent _onMatchStarted;

        private Action _handleThreatChangedDelegate;
        private Action _handleMatchStartedDelegate;

        private void Awake()
        {
            _handleThreatChangedDelegate = HandleThreatChanged;
            _handleMatchStartedDelegate  = HandleMatchStarted;
        }

        private void OnEnable()
        {
            _onThreatChanged?.RegisterCallback(_handleThreatChangedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
        }

        private void OnDisable()
        {
            _onThreatChanged?.UnregisterCallback(_handleThreatChangedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
        }

        public void HandleThreatChanged()
        {
            if (_bridgeSO == null || _threatSO == null) return;
            _bridgeSO.ApplyBridge(_threatSO.CurrentThreat);
        }

        public void HandleMatchStarted()
        {
            if (_bridgeSO == null) return;
            _bridgeSO.Reset();
        }

        public ZoneControlThreatPressureBridgeSO BridgeSO => _bridgeSO;
        public ZoneControlThreatAssessmentSO     ThreatSO => _threatSO;
    }
}
