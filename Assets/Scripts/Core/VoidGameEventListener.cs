using UnityEngine;
using UnityEngine.Events;

namespace BattleRobots.Core
{
    /// <summary>
    /// MonoBehaviour listener for a VoidGameEvent channel.
    /// </summary>
    public sealed class VoidGameEventListener : MonoBehaviour
    {
        [SerializeField] private VoidGameEvent _event;
        [SerializeField] private UnityEvent _response;

        private void OnEnable()
        {
            if (_event != null)
                _event.RegisterListener(this);
        }

        private void OnDisable()
        {
            if (_event != null)
                _event.UnregisterListener(this);
        }

        public void OnEventRaised() => _response?.Invoke();
    }
}
