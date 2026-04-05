using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Parameter-less SO event channel for signals that carry no data
    /// (e.g., MatchStarted, RobotDestroyed).
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Events/VoidGameEvent", order = 1)]
    public sealed class VoidGameEvent : ScriptableObject
    {
        private readonly List<VoidGameEventListener> _listeners = new List<VoidGameEventListener>();

        public void Raise()
        {
            for (int i = _listeners.Count - 1; i >= 0; i--)
                _listeners[i].OnEventRaised();
        }

        public void RegisterListener(VoidGameEventListener listener)
        {
            if (!_listeners.Contains(listener))
                _listeners.Add(listener);
        }

        public void UnregisterListener(VoidGameEventListener listener)
        {
            _listeners.Remove(listener);
        }

#if UNITY_EDITOR
        [ContextMenu("Raise (Editor Only)")]
        private void RaiseEditor() => Raise();
#endif
    }
}
