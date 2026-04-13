using System;
using System.Collections.Generic;
using System.Text;

namespace BattleRobots.Core
{
    /// <summary>
    /// Encodes and decodes a player's loadout part-ID list as a compact, shareable
    /// alphanumeric string (Base64 over a pipe-separated part-ID list).
    ///
    /// ── Encoding format ──────────────────────────────────────────────────────────
    ///   1. Part IDs are joined with '|' as a separator.
    ///      Example: "chassis_mk1|arm_left_mk2|arm_right_mk1|legs_mk1"
    ///   2. The joined string is encoded as UTF-8 bytes.
    ///   3. Those bytes are converted to a standard Base64 string.
    ///
    ///   An empty or null part-ID list produces an empty string.
    ///   A null or empty code string decodes to null.
    ///   A malformed Base64 string (FormatException) also decodes to null.
    ///
    /// ── Usage ────────────────────────────────────────────────────────────────────
    ///   string code    = BuildCodeEncoder.Encode(playerLoadout.EquippedPartIds);
    ///   List<string> ids = BuildCodeEncoder.Decode(code);
    ///   if (ids != null) playerLoadout.SetLoadout(ids);
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; static helper — no MonoBehaviour, no SO.
    ///   - No UnityEngine dependency — fully testable in EditMode without a scene.
    ///   - Allocates only on the encode/decode call path (cold path).
    /// </summary>
    public static class BuildCodeEncoder
    {
        private const char Separator = '|';

        /// <summary>
        /// Encodes <paramref name="partIds"/> as a Base64 build code string.
        /// Returns <see cref="string.Empty"/> when <paramref name="partIds"/> is null or empty.
        /// Null or whitespace-only entries are skipped.
        /// </summary>
        /// <param name="partIds">Ordered list of equipped part IDs. May be null.</param>
        /// <returns>Base64 build code, or <see cref="string.Empty"/> when the list is empty.</returns>
        public static string Encode(IReadOnlyList<string> partIds)
        {
            if (partIds == null || partIds.Count == 0)
                return string.Empty;

            // Build the pipe-separated ID string, skipping null/whitespace entries.
            var sb = new StringBuilder();
            bool first = true;
            for (int i = 0; i < partIds.Count; i++)
            {
                string id = partIds[i];
                if (string.IsNullOrWhiteSpace(id)) continue;

                if (!first) sb.Append(Separator);
                sb.Append(id);
                first = false;
            }

            if (sb.Length == 0) return string.Empty;

            byte[] bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Decodes a Base64 build code back into an ordered list of part IDs.
        /// Returns <c>null</c> when:
        /// <list type="bullet">
        ///   <item><description><paramref name="code"/> is null or empty.</description></item>
        ///   <item><description><paramref name="code"/> is not valid Base64 (FormatException).</description></item>
        ///   <item><description>The decoded string contains no non-empty segments.</description></item>
        /// </list>
        /// </summary>
        /// <param name="code">Base64 build code produced by <see cref="Encode"/>. May be null.</param>
        /// <returns>Ordered list of part IDs, or <c>null</c> on failure.</returns>
        public static List<string> Decode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return null;

            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(code);
            }
            catch (FormatException)
            {
                return null;
            }

            string joined = Encoding.UTF8.GetString(bytes);
            string[] segments = joined.Split(Separator);

            var result = new List<string>(segments.Length);
            for (int i = 0; i < segments.Length; i++)
            {
                string seg = segments[i];
                if (!string.IsNullOrWhiteSpace(seg))
                    result.Add(seg);
            }

            return result.Count > 0 ? result : null;
        }
    }
}
