using UnityEngine;
using UnityEngine.Events;

namespace BattleRobots.Core
{
    /// <summary>
    /// MonoBehaviour listener for a typed GameEvent channel.
    /// Wires SO → UnityEvent so designers can hook up responses in the Inspector.
    /// </summary>
    public abstract class GameEventListener<T> : MonoBehaviour
    {
        [SerializeField] private GameEvent<T> _event;
        [SerializeField] private UnityEvent<T> _response;

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

        public void OnEventRaised(T value) => _response?.Invoke(value);
    }
}
