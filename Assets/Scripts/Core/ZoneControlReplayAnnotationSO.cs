using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    [Serializable]
    public sealed class ZoneControlReplayAnnotationEntry
    {
        public float  Timestamp;
        public string Note;

        public ZoneControlReplayAnnotationEntry(float timestamp, string note)
        {
            Timestamp = timestamp;
            Note      = note ?? string.Empty;
        }
    }

    /// <summary>
    /// Runtime SO that stores player-authored annotations on match replay events.
    /// Each entry pairs a timestamp with a short note string.
    /// Oldest entries are evicted when <c>_maxAnnotations</c> is reached.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlReplayAnnotation.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlReplayAnnotation", order = 77)]
    public sealed class ZoneControlReplayAnnotationSO : ScriptableObject
    {
        [Header("Settings")]
        [Min(1)]
        [SerializeField] private int _maxAnnotations = 10;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onAnnotationAdded;

        private readonly List<ZoneControlReplayAnnotationEntry> _entries = new List<ZoneControlReplayAnnotationEntry>();

        private void OnEnable() => Reset();

        public int AnnotationCount  => _entries.Count;
        public int MaxAnnotations   => _maxAnnotations;

        /// <summary>
        /// Adds an annotation.  Empty or null notes are ignored.
        /// Evicts the oldest entry when the buffer is full.
        /// </summary>
        public void AddAnnotation(float timestamp, string note)
        {
            if (string.IsNullOrEmpty(note)) return;
            if (_entries.Count >= _maxAnnotations)
                _entries.RemoveAt(0);
            _entries.Add(new ZoneControlReplayAnnotationEntry(timestamp, note));
            _onAnnotationAdded?.Raise();
        }

        public IReadOnlyList<ZoneControlReplayAnnotationEntry> GetAnnotations() => _entries;

        /// <summary>Clears all annotations silently.</summary>
        public void Reset() => _entries.Clear();
    }
}
