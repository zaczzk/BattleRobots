using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T193 — <see cref="PrestigeHistorySO"/>.
    ///
    /// PrestigeHistorySOTests (10):
    ///   SO_DefaultMaxHistory_IsTen                 ×1
    ///   SO_FreshCount_IsZero                       ×1
    ///   SO_AddEntry_IncrementsCount                ×1
    ///   SO_Count_CappedAtMax                       ×1
    ///   SO_GetEntry_Empty_ReturnsNull              ×1
    ///   SO_GetEntry_OneEntry_CorrectData           ×1
    ///   SO_GetLatest_IsNewestEntry                 ×1
    ///   SO_RingBuffer_OldestEntryEvicted           ×1
    ///   SO_Clear_ResetsCount                       ×1
    ///   SO_GetEntry_OutOfRange_ReturnsNull         ×1
    /// </summary>
    public class PrestigeHistorySOTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static PrestigeHistorySO CreateSO(int maxHistory = 10)
        {
            var so = ScriptableObject.CreateInstance<PrestigeHistorySO>();
            FieldInfo fi = typeof(PrestigeHistorySO)
                .GetField("_maxHistory", BindingFlags.Instance | BindingFlags.NonPublic);
            fi?.SetValue(so, maxHistory);
            // Trigger OnEnable-equivalent to init the buffer at the new max.
            so.Clear();
            return so;
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void SO_DefaultMaxHistory_IsTen()
        {
            var so = ScriptableObject.CreateInstance<PrestigeHistorySO>();
            Assert.AreEqual(10, so.MaxHistory,
                "Default MaxHistory must be 10.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshCount_IsZero()
        {
            var so = CreateSO();
            Assert.AreEqual(0, so.Count,
                "Fresh PrestigeHistorySO must have Count = 0.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AddEntry_IncrementsCount()
        {
            var so = CreateSO();
            so.AddEntry(1, "Bronze I");
            Assert.AreEqual(1, so.Count,
                "Count must increment to 1 after one AddEntry.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Count_CappedAtMax()
        {
            // Max = 3; add 5 entries → count stays at 3.
            var so = CreateSO(maxHistory: 3);
            for (int i = 1; i <= 5; i++)
                so.AddEntry(i, $"Rank {i}");
            Assert.AreEqual(3, so.Count,
                "Count must be capped at MaxHistory (3).");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GetEntry_Empty_ReturnsNull()
        {
            var so = CreateSO();
            PrestigeHistoryEntry? entry = so.GetEntry(0);
            Assert.IsFalse(entry.HasValue,
                "GetEntry(0) on an empty buffer must return null.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GetEntry_OneEntry_CorrectData()
        {
            var so = CreateSO();
            so.AddEntry(1, "Bronze I");
            PrestigeHistoryEntry? entry = so.GetEntry(0);
            Assert.IsTrue(entry.HasValue, "GetEntry(0) must return a value after one AddEntry.");
            Assert.AreEqual(1,          entry.Value.prestigeCount,
                "Entry prestigeCount must be 1.");
            Assert.AreEqual("Bronze I", entry.Value.rankLabel,
                "Entry rankLabel must be 'Bronze I'.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GetLatest_IsNewestEntry()
        {
            var so = CreateSO();
            so.AddEntry(1, "Bronze I");
            so.AddEntry(2, "Bronze II");

            PrestigeHistoryEntry? latest = so.GetLatest();
            Assert.IsTrue(latest.HasValue, "GetLatest must return a value after entries added.");
            Assert.AreEqual(2, latest.Value.prestigeCount,
                "GetLatest must return the most recently added entry (count=2).");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RingBuffer_OldestEntryEvicted()
        {
            // Max = 2; add entries 1, 2, 3 → oldest (1) is evicted; buffer = [3, 2].
            var so = CreateSO(maxHistory: 2);
            so.AddEntry(1, "Bronze I");
            so.AddEntry(2, "Bronze II");
            so.AddEntry(3, "Bronze III");

            Assert.AreEqual(2, so.Count, "Count must remain at max (2) after overflow.");

            PrestigeHistoryEntry? newest = so.GetEntry(0);
            Assert.IsTrue(newest.HasValue);
            Assert.AreEqual(3, newest.Value.prestigeCount,
                "Newest entry (index 0) must be the last added (count=3).");

            PrestigeHistoryEntry? oldest = so.GetEntry(1);
            Assert.IsTrue(oldest.HasValue);
            Assert.AreEqual(2, oldest.Value.prestigeCount,
                "Oldest entry (index 1) after overflow must be count=2 (first was evicted).");

            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Clear_ResetsCount()
        {
            var so = CreateSO();
            so.AddEntry(1, "Bronze I");
            so.AddEntry(2, "Bronze II");
            so.Clear();

            Assert.AreEqual(0, so.Count,
                "Clear must reset Count to 0.");
            Assert.IsFalse(so.GetEntry(0).HasValue,
                "GetEntry after Clear must return null.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GetEntry_OutOfRange_ReturnsNull()
        {
            var so = CreateSO();
            so.AddEntry(1, "Bronze I");

            PrestigeHistoryEntry? outOfRange = so.GetEntry(5);
            Assert.IsFalse(outOfRange.HasValue,
                "GetEntry with out-of-range index must return null.");
            Object.DestroyImmediate(so);
        }
    }
}
