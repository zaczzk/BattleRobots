using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// JSON-serialisable payload produced by <see cref="ZoneControlReplayExportSO"/>.
    ///
    /// Parallel arrays are used so <c>JsonUtility</c> can serialise them without
    /// the jagged-array limitation.  Each index <c>i</c> corresponds to one
    /// <see cref="ZoneControlSnapshot"/>:
    ///   - <c>timestamps[i]</c>     — snapshot timestamp.
    ///   - <c>captureStates[i]</c>  — comma-separated "true/false" values, one per zone.
    /// </summary>
    [Serializable]
    public sealed class ZoneControlReplayPayload
    {
        /// <summary>Match-elapsed timestamps for each snapshot (seconds).</summary>
        public List<float>  timestamps     = new List<float>();

        /// <summary>
        /// Per-snapshot zone capture states encoded as comma-separated bool strings.
        /// E.g. "true,false,true" for a 3-zone snapshot.
        /// </summary>
        public List<string> captureStates  = new List<string>();

        /// <summary>Number of snapshots in this payload.</summary>
        public int Count => timestamps != null ? timestamps.Count : 0;
    }

    /// <summary>
    /// Runtime ScriptableObject that serialises a <see cref="ZoneControlReplaySO"/>
    /// ring-buffer to JSON and caches the result.
    ///
    /// ── Export ─────────────────────────────────────────────────────────────────
    ///   Call <see cref="ExportToJson(ZoneControlReplaySO)"/> to serialise the
    ///   current replay buffer.  The result is cached in <see cref="LastExportJson"/>
    ///   and <see cref="_onExportCompleted"/> is raised.
    ///
    /// ── Import / round-trip ─────────────────────────────────────────────────────
    ///   <see cref="ParsePayload"/> deserialises a previously exported JSON string
    ///   back into a <see cref="ZoneControlReplayPayload"/>, returning null on failure.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Caches last export in <c>_lastExportJson</c> (runtime, not serialised).
    ///   - OnEnable calls <see cref="Reset"/> so the field starts empty on reload.
    ///   - Allocation occurs only during Export (list construction + JsonUtility).
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlReplayExport.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlReplayExport", order = 28)]
    public sealed class ZoneControlReplayExportSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Event Channels (optional)")]
        [Tooltip("Raised after a successful ExportToJson call.")]
        [SerializeField] private VoidGameEvent _onExportCompleted;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private string _lastExportJson;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>
        /// The JSON string produced by the most recent successful export.
        /// <see cref="string.Empty"/> when no export has been performed.
        /// </summary>
        public string LastExportJson => _lastExportJson ?? string.Empty;

        /// <summary>True when a non-empty export JSON is cached.</summary>
        public bool HasExport => !string.IsNullOrEmpty(_lastExportJson);

        /// <summary>Character length of the last exported JSON string (0 when none).</summary>
        public int LastExportLength => HasExport ? _lastExportJson.Length : 0;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Serialises the contents of <paramref name="replay"/> to a JSON string,
        /// caches it, fires <c>_onExportCompleted</c>, and returns the JSON.
        ///
        /// Returns <see cref="string.Empty"/> when <paramref name="replay"/> is null
        /// or has no snapshots.
        /// </summary>
        public string ExportToJson(ZoneControlReplaySO replay)
        {
            if (replay == null || replay.Count == 0)
                return string.Empty;

            var payload = new ZoneControlReplayPayload();

            for (int i = 0; i < replay.Count; i++)
            {
                ZoneControlSnapshot snap = replay.GetSnapshot(i);
                payload.timestamps.Add(snap.timestamp);
                payload.captureStates.Add(EncodeCaptureState(snap.captureState));
            }

            string json = JsonUtility.ToJson(payload);
            _lastExportJson = json;
            _onExportCompleted?.Raise();
            return json;
        }

        /// <summary>
        /// Attempts to deserialise a previously exported JSON string back into a
        /// <see cref="ZoneControlReplayPayload"/>.
        ///
        /// Returns <c>null</c> when <paramref name="json"/> is null, empty, or
        /// cannot be parsed.
        /// </summary>
        public ZoneControlReplayPayload ParsePayload(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;

            try
            {
                var payload = JsonUtility.FromJson<ZoneControlReplayPayload>(json);
                return payload;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Clears the cached export JSON.
        /// Does not fire any events.
        /// </summary>
        public void Reset()
        {
            _lastExportJson = string.Empty;
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private static string EncodeCaptureState(bool[] state)
        {
            if (state == null || state.Length == 0) return string.Empty;

            // Pre-size estimate: "true" = 4 chars, "false" = 5 chars, commas = (n-1)
            var sb = new System.Text.StringBuilder(state.Length * 5);
            for (int i = 0; i < state.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(state[i] ? "true" : "false");
            }
            return sb.ToString();
        }
    }
}
