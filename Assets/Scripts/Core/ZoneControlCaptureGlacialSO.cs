using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureGlacial", order = 230)]
    public sealed class ZoneControlCaptureGlacialSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(0)]    private int   _icePerBotCapture  = 25;
        [SerializeField, Min(1)]    private int   _maxIce            = 100;
        [SerializeField, Min(0.1f)] private float _meltMultiplier    = 3f;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onGlacialMelt;

        private int _iceLevel;
        private int _meltCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   IcePerBotCapture  => _icePerBotCapture;
        public int   MaxIce            => _maxIce;
        public float MeltMultiplier    => _meltMultiplier;
        public int   IceLevel          => _iceLevel;
        public int   MeltCount         => _meltCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float IceProgress       => _maxIce > 0
            ? Mathf.Clamp01(_iceLevel / (float)_maxIce)
            : 0f;

        public void RecordBotCapture()
        {
            _iceLevel = Mathf.Min(_iceLevel + _icePerBotCapture, _maxIce);
        }

        public int RecordPlayerCapture()
        {
            if (_iceLevel <= 0)
                return 0;
            int bonus = Mathf.RoundToInt(_iceLevel * _meltMultiplier);
            _meltCount++;
            _totalBonusAwarded += bonus;
            _iceLevel           = 0;
            _onGlacialMelt?.Raise();
            return bonus;
        }

        public void Reset()
        {
            _iceLevel          = 0;
            _meltCount         = 0;
            _totalBonusAwarded = 0;
        }
    }
}
