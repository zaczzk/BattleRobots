using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureNova", order = 181)]
    public sealed class ZoneControlCaptureNovaSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1f)] private float _novaThreshold = 5f;
        [SerializeField, Min(0f)] private float _decayRate     = 0.5f;
        [SerializeField, Min(0)]  private int   _bonusPerNova  = 250;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onNova;

        private float _novaPool;
        private int   _novaCount;
        private int   _totalBonusAwarded;

        private void OnEnable() => Reset();

        public float NovaThreshold     => _novaThreshold;
        public float DecayRate         => _decayRate;
        public int   BonusPerNova      => _bonusPerNova;
        public float NovaPool          => _novaPool;
        public int   NovaCount         => _novaCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float NovaProgress      => _novaThreshold > 0f ? Mathf.Clamp01(_novaPool / _novaThreshold) : 0f;

        public void RecordCapture()
        {
            _novaPool++;
            if (_novaPool >= _novaThreshold)
            {
                _novaCount++;
                _totalBonusAwarded += _bonusPerNova;
                _novaPool = 0f;
                _onNova?.Raise();
            }
        }

        public void Tick(float dt)
        {
            if (_novaPool <= 0f) return;
            _novaPool = Mathf.Max(0f, _novaPool - _decayRate * dt);
        }

        public void Reset()
        {
            _novaPool          = 0f;
            _novaCount         = 0;
            _totalBonusAwarded = 0;
        }
    }
}
