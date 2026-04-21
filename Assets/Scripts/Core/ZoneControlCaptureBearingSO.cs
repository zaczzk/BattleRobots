using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureBearing", order = 302)]
    public sealed class ZoneControlCaptureBearingSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _racesNeeded   = 7;
        [SerializeField, Min(1)] private int _driftPerBot   = 2;
        [SerializeField, Min(0)] private int _bonusPerSpin  = 1270;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onBearingSpun;

        private int _races;
        private int _spinCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   RacesNeeded       => _racesNeeded;
        public int   DriftPerBot       => _driftPerBot;
        public int   BonusPerSpin      => _bonusPerSpin;
        public int   Races             => _races;
        public int   SpinCount         => _spinCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float RaceProgress      => _racesNeeded > 0
            ? Mathf.Clamp01(_races / (float)_racesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _races = Mathf.Min(_races + 1, _racesNeeded);
            if (_races >= _racesNeeded)
            {
                int bonus = _bonusPerSpin;
                _spinCount++;
                _totalBonusAwarded += bonus;
                _races              = 0;
                _onBearingSpun?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _races = Mathf.Max(0, _races - _driftPerBot);
        }

        public void Reset()
        {
            _races             = 0;
            _spinCount         = 0;
            _totalBonusAwarded = 0;
        }
    }
}
