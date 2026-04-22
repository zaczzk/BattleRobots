using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureCommutator", order = 318)]
    public sealed class ZoneControlCaptureCommutatorSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _contactsNeeded       = 7;
        [SerializeField, Min(1)] private int _wearPerBot           = 2;
        [SerializeField, Min(0)] private int _bonusPerCommutation  = 1510;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onCommutatorFired;

        private int _contacts;
        private int _commutationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ContactsNeeded      => _contactsNeeded;
        public int   WearPerBot          => _wearPerBot;
        public int   BonusPerCommutation => _bonusPerCommutation;
        public int   Contacts            => _contacts;
        public int   CommutationCount    => _commutationCount;
        public int   TotalBonusAwarded   => _totalBonusAwarded;
        public float ContactProgress     => _contactsNeeded > 0
            ? Mathf.Clamp01(_contacts / (float)_contactsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _contacts = Mathf.Min(_contacts + 1, _contactsNeeded);
            if (_contacts >= _contactsNeeded)
            {
                int bonus = _bonusPerCommutation;
                _commutationCount++;
                _totalBonusAwarded += bonus;
                _contacts           = 0;
                _onCommutatorFired?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _contacts = Mathf.Max(0, _contacts - _wearPerBot);
        }

        public void Reset()
        {
            _contacts          = 0;
            _commutationCount  = 0;
            _totalBonusAwarded = 0;
        }
    }
}
