using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="KillFeedSO"/>.
    ///
    /// Covers:
    ///   • Fresh instance has Count = 0 and MaxEntries = 5.
    ///   • Add single entry → Count = 1, GetEntry(0) returns it.
    ///   • Add beyond MaxEntries → Count stays at MaxEntries, oldest entry evicted.
    ///   • Clear → Count resets to 0.
    ///   • GetEntry out-of-range → returns default (no exception).
    ///   • _onFeedUpdated fires on Add and Clear.
    ///   • KillFeedEntry constructor sets fields correctly.
    ///   • Newest-first order verified for two entries.
    /// </summary>
    public class KillFeedSOTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            var fi = target.GetType().GetField(
                name, System.Reflection.BindingFlags.Instance |
                      System.Reflection.BindingFlags.NonPublic |
                      System.Reflection.BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static void InvokePrivate(object target, string method)
        {
            var mi = target.GetType().GetMethod(
                method, System.Reflection.BindingFlags.Instance |
                         System.Reflection.BindingFlags.NonPublic |
                         System.Reflection.BindingFlags.Public);
            Assert.IsNotNull(mi, $"Method '{method}' not found on {target.GetType().Name}.");
            mi.Invoke(target, null);
        }

        private static KillFeedSO CreateFeed(int maxEntries = 3)
        {
            var so = ScriptableObject.CreateInstance<KillFeedSO>();
            SetField(so, "_maxEntries", maxEntries);
            InvokePrivate(so, "OnEnable");
            return so;
        }

        // ── KillFeedEntry tests ───────────────────────────────────────────────

        [Test]
        public void Entry_Constructor_SetsFields()
        {
            var e = new KillFeedSO.KillFeedEntry("Bot A", "Bot B", 50, 3);
            Assert.AreEqual("Bot A", e.AttackerName);
            Assert.AreEqual("Bot B", e.VictimName);
            Assert.AreEqual(50, e.Reward);
            Assert.AreEqual(3,  e.ComboCount);
        }

        [Test]
        public void Entry_NullNames_FallbackToEmpty()
        {
            var e = new KillFeedSO.KillFeedEntry(null, null);
            Assert.AreEqual(string.Empty, e.AttackerName);
            Assert.AreEqual(string.Empty, e.VictimName);
        }

        // ── KillFeedSO tests ──────────────────────────────────────────────────

        [Test]
        public void FreshInstance_Count_IsZero()
        {
            var so = CreateFeed();
            Assert.AreEqual(0, so.Count);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void FreshInstance_MaxEntries_IsFive()
        {
            var so = ScriptableObject.CreateInstance<KillFeedSO>();
            InvokePrivate(so, "OnEnable");
            Assert.AreEqual(5, so.MaxEntries);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Add_SingleEntry_CountIsOne_GetEntry0ReturnsIt()
        {
            var so    = CreateFeed(maxEntries: 3);
            var entry = new KillFeedSO.KillFeedEntry("A", "B", 100, 1);
            so.Add(entry);

            Assert.AreEqual(1, so.Count);
            var got = so.GetEntry(0);
            Assert.AreEqual("A",   got.AttackerName);
            Assert.AreEqual("B",   got.VictimName);
            Assert.AreEqual(100,   got.Reward);

            Object.DestroyImmediate(so);
        }

        [Test]
        public void Add_BeyondMax_CountStaysAtMax()
        {
            var so = CreateFeed(maxEntries: 2);
            so.Add(new KillFeedSO.KillFeedEntry("A", "B"));
            so.Add(new KillFeedSO.KillFeedEntry("C", "D"));
            so.Add(new KillFeedSO.KillFeedEntry("E", "F"));   // exceeds capacity

            Assert.AreEqual(2, so.Count);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Add_BeyondMax_OldestEvicted_GetEntry0_IsMostRecent()
        {
            var so = CreateFeed(maxEntries: 2);
            so.Add(new KillFeedSO.KillFeedEntry("Old", "Victim1"));
            so.Add(new KillFeedSO.KillFeedEntry("Mid", "Victim2"));
            so.Add(new KillFeedSO.KillFeedEntry("New", "Victim3"));   // "Old" evicted

            var newest = so.GetEntry(0);
            Assert.AreEqual("New", newest.AttackerName,
                "GetEntry(0) must return the most recently added entry.");

            Object.DestroyImmediate(so);
        }

        [Test]
        public void Add_TwoEntries_NewestFirst_Order()
        {
            var so = CreateFeed(maxEntries: 5);
            so.Add(new KillFeedSO.KillFeedEntry("First",  "V1", 10));
            so.Add(new KillFeedSO.KillFeedEntry("Second", "V2", 20));

            Assert.AreEqual("Second", so.GetEntry(0).AttackerName, "Index 0 = newest.");
            Assert.AreEqual("First",  so.GetEntry(1).AttackerName, "Index 1 = older.");

            Object.DestroyImmediate(so);
        }

        [Test]
        public void Clear_ResetsCountToZero()
        {
            var so = CreateFeed(maxEntries: 3);
            so.Add(new KillFeedSO.KillFeedEntry("A", "B"));
            so.Add(new KillFeedSO.KillFeedEntry("C", "D"));
            so.Clear();

            Assert.AreEqual(0, so.Count);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void GetEntry_OutOfRange_ReturnsDefault()
        {
            var so = CreateFeed(maxEntries: 3);
            so.Add(new KillFeedSO.KillFeedEntry("A", "B"));

            // Index out-of-range — must not throw, returns default.
            KillFeedSO.KillFeedEntry result = default;
            Assert.DoesNotThrow(() => result = so.GetEntry(99));
            Assert.AreEqual(string.Empty, result.AttackerName);

            Object.DestroyImmediate(so);
        }

        [Test]
        public void OnFeedUpdated_FiredOnAdd()
        {
            var so      = CreateFeed();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            SetField(so, "_onFeedUpdated", channel);

            int callCount = 0;
            channel.RegisterCallback(() => callCount++);

            so.Add(new KillFeedSO.KillFeedEntry("A", "B"));

            Assert.AreEqual(1, callCount, "_onFeedUpdated must fire once on Add.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void OnFeedUpdated_FiredOnClear()
        {
            var so      = CreateFeed();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            SetField(so, "_onFeedUpdated", channel);

            int callCount = 0;
            channel.RegisterCallback(() => callCount++);

            so.Clear();

            Assert.AreEqual(1, callCount, "_onFeedUpdated must fire once on Clear.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }
    }
}
