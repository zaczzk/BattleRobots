using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that detects a capture frenzy — <see cref="_frenzyThreshold"/> or
    /// more captures within <see cref="_frenzyWindow"/> seconds.
    ///
    /// Call <see cref="RecordCapture(float)"/> on each zone capture, passing the
    /// current game-time.  Call <see cref="Tick(float)"/> every frame to prune stale
    /// entries and evaluate transitions.  Fires <c>_onFrenzyStarted</c> on the
    /// false→true transition and <c>_onFrenzyEnded</c> on the true→false transition.
    /// <see cref="Reset"/> clears all state silently; called from <c>OnEnable</c>.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlCaptureFrenzy.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureFrenzy", order = 86)]
    public sealed class ZoneControlCaptureFrenzySO : ScriptableObject
    {
        [Header("Frenzy Settings")]
        [Min(2)]
        [SerializeField] private int _frenzyThreshold = 4;

        [Min(0.5f)]
        [SerializeField] private float _frenzyWindow = 6f;

        [Min(0)]
        [SerializeField] private int _frenzyBonus = 175;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onFrenzyStarted;
        [SerializeField] private VoidGameEvent _onFrenzyEnded;

        private readonly List<float> _timestamps = new List<float>();
        private bool _isFrenzy;

        private void OnEnable() => Reset();

        public int   FrenzyThreshold => _frenzyThreshold;
        public float FrenzyWindow    => _frenzyWindow;
        public int   FrenzyBonus     => _frenzyBonus;
        public bool  IsFrenzy        => _isFrenzy;
        public int   CaptureCount    => _timestamps.Count;

        /// <summary>Records a capture at the given game-time and evaluates frenzy.</summary>
        public void RecordCapture(float currentTime)
        {
            Prune(currentTime);
            _timestamps.Add(currentTime);
            EvaluateFrenzy();
        }

        /// <summary>Prunes stale entries and re-evaluates frenzy state each frame.</summary>
        public void Tick(float currentTime)
        {
            Prune(currentTime);
            EvaluateFrenzy();
        }

        /// <summary>Clears all runtime state silently.</summary>
        public void Reset()
        {
            _timestamps.Clear();
            _isFrenzy = false;
        }

        private void Prune(float currentTime)
        {
            for (int i = _timestamps.Count - 1; i >= 0; i--)
            {
                if (currentTime - _timestamps[i] > _frenzyWindow)
                    _timestamps.RemoveAt(i);
            }
        }

        private void EvaluateFrenzy()
        {
            bool nowFrenzy = _timestamps.Count >= _frenzyThreshold;
            if (nowFrenzy && !_isFrenzy)
            {
                _isFrenzy = true;
                _onFrenzyStarted?.Raise();
            }
            else if (!nowFrenzy && _isFrenzy)
            {
                _isFrenzy = false;
                _onFrenzyEnded?.Raise();
            }
        }
    }
}
