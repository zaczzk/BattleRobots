using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlFirstCaptureBonus", order = 94)]
    public sealed class ZoneControlFirstCaptureBonusSO : ScriptableObject
    {
        [Header("Bonus Settings")]
        [Min(0)]
        [SerializeField] private int _bonusAmount = 500;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onFirstCapture;

        private bool _hasFired;

        private void OnEnable() => Reset();

        public int  BonusAmount => _bonusAmount;
        public bool HasFired    => _hasFired;

        public void RecordCapture()
        {
            if (_hasFired) return;
            _hasFired = true;
            _onFirstCapture?.Raise();
        }

        public void Reset()
        {
            _hasFired = false;
        }
    }
}
