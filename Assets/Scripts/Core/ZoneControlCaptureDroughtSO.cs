using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureDrought", order = 96)]
    public sealed class ZoneControlCaptureDroughtSO : ScriptableObject
    {
        [Header("Drought Settings")]
        [Min(1f)]
        [SerializeField] private float _droughtThreshold = 20f;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onDroughtStarted;
        [SerializeField] private VoidGameEvent _onDroughtEnded;

        private float _timeSinceCapture;
        private bool  _isDrought;

        private void OnEnable() => Reset();

        public float DroughtThreshold  => _droughtThreshold;
        public float TimeSinceCapture  => _timeSinceCapture;
        public bool  IsDrought         => _isDrought;

        public void RecordCapture()
        {
            _timeSinceCapture = 0f;
            if (_isDrought)
                EndDrought();
        }

        public void Tick(float dt)
        {
            _timeSinceCapture += dt;
            if (!_isDrought && _timeSinceCapture >= _droughtThreshold)
                StartDrought();
        }

        public void Reset()
        {
            _timeSinceCapture = 0f;
            _isDrought        = false;
        }

        private void StartDrought()
        {
            _isDrought = true;
            _onDroughtStarted?.Raise();
        }

        private void EndDrought()
        {
            _isDrought = false;
            _onDroughtEnded?.Raise();
        }
    }
}
