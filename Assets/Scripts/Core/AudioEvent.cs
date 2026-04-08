using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// SO event channel for audio signals.
    ///
    /// Holds an array of <see cref="AudioClip"/> variants; each call to <see cref="Raise"/>
    /// notifies registered listeners (typically <see cref="AudioManager"/>) with this SO
    /// as the payload so they can pick a clip, apply volume/pitch variation, and play it.
    ///
    /// Follows the same RegisterCallback / UnregisterCallback pattern as
    /// <see cref="GameEvent{T}"/> and <see cref="VoidGameEvent"/>.
    ///
    /// Create via Assets ▶ BattleRobots ▶ Events ▶ AudioEvent.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Events/AudioEvent", order = 5)]
    public sealed class AudioEvent : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("One or more clips to choose from randomly each time this event fires.")]
        [SerializeField] private AudioClip[] _clips;

        [Tooltip("Master volume for this sound (0–1).")]
        [SerializeField, Range(0f, 1f)] private float _volume = 1f;

        [Tooltip("Minimum random pitch multiplier.")]
        [SerializeField, Range(0.5f, 2f)] private float _pitchMin = 0.9f;

        [Tooltip("Maximum random pitch multiplier.")]
        [SerializeField, Range(0.5f, 2f)] private float _pitchMax = 1.1f;

        // ── Callbacks (transient — not serialised) ─────────────────────────────
        private readonly List<Action<AudioEvent>> _callbacks = new List<Action<AudioEvent>>();

        // ── Public accessors (read-only; SO immutable at runtime) ─────────────

        public float Volume   => _volume;
        public float PitchMin => _pitchMin;
        public float PitchMax => _pitchMax;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Selects a random <see cref="AudioClip"/> from the pool.
        /// Returns null if no clips are assigned.
        /// </summary>
        public AudioClip PickClip()
        {
            if (_clips == null || _clips.Length == 0) return null;
            return _clips[Random.Range(0, _clips.Length)];
        }

        /// <summary>
        /// Broadcast this event to all registered listeners.
        /// Call from game code when the sound should play.
        /// </summary>
        public void Raise()
        {
            // Iterate in reverse so listeners can safely unregister during invocation.
            for (int i = _callbacks.Count - 1; i >= 0; i--)
                _callbacks[i]?.Invoke(this);
        }

        /// <summary>Register a callback to be invoked when this event is raised.</summary>
        public void RegisterCallback(Action<AudioEvent> callback)
        {
            if (!_callbacks.Contains(callback))
                _callbacks.Add(callback);
        }

        /// <summary>Unregister a previously registered callback.</summary>
        public void UnregisterCallback(Action<AudioEvent> callback)
        {
            _callbacks.Remove(callback);
        }

#if UNITY_EDITOR
        [ContextMenu("Raise (Editor Only)")]
        private void RaiseEditor() => Raise();
#endif
    }
}
