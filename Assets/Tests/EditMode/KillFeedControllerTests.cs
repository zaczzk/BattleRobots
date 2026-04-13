using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="KillFeedController"/>.
    ///
    /// Covers:
    ///   • Fresh instance FeedSO is null.
    ///   • OnEnable / OnDisable with all-null refs — no exception.
    ///   • Refresh with null feed — no exception.
    ///   • ClearFeed with null feed — no exception.
    ///   • MatchEnd channel raise triggers ClearFeed (verifies via feed.Count reset).
    ///   • OnDisable unregisters from both channels (no crash after unsubscribe).
    /// </summary>
    public class KillFeedControllerTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static void InvokePrivate(object target, string method, object[] args = null)
        {
            MethodInfo mi = target.GetType()
                .GetMethod(method,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(mi, $"Method '{method}' not found on {target.GetType().Name}.");
            mi.Invoke(target, args ?? System.Array.Empty<object>());
        }

        private static KillFeedSO CreateFeedSO(int maxEntries = 5)
        {
            var so = ScriptableObject.CreateInstance<KillFeedSO>();
            SetField(so, "_maxEntries", maxEntries);
            InvokePrivate(so, "OnEnable");
            return so;
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_FeedSO_IsNull()
        {
            var go  = new GameObject();
            var kfc = go.AddComponent<KillFeedController>();
            Assert.IsNull(kfc.FeedSO);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnEnable_AllNullRefs_DoesNotThrow()
        {
            var go  = new GameObject();
            var kfc = go.AddComponent<KillFeedController>();
            SetField(kfc, "_feedSO",         null);
            SetField(kfc, "_onFeedUpdated",  null);
            SetField(kfc, "_onMatchEnd",     null);
            SetField(kfc, "_entryContainer", null);

            InvokePrivate(kfc, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(kfc, "OnEnable"));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_AllNullRefs_DoesNotThrow()
        {
            var go  = new GameObject();
            var kfc = go.AddComponent<KillFeedController>();
            SetField(kfc, "_feedSO",        null);
            SetField(kfc, "_onFeedUpdated", null);
            SetField(kfc, "_onMatchEnd",    null);

            InvokePrivate(kfc, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(kfc, "OnDisable"));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Refresh_NullFeed_DoesNotThrow()
        {
            var go  = new GameObject();
            var kfc = go.AddComponent<KillFeedController>();
            SetField(kfc, "_feedSO", null);

            InvokePrivate(kfc, "Awake");
            Assert.DoesNotThrow(() => kfc.Refresh());
            Object.DestroyImmediate(go);
        }

        [Test]
        public void ClearFeed_NullFeed_DoesNotThrow()
        {
            var go  = new GameObject();
            var kfc = go.AddComponent<KillFeedController>();
            SetField(kfc, "_feedSO", null);

            InvokePrivate(kfc, "Awake");
            Assert.DoesNotThrow(() => kfc.ClearFeed());
            Object.DestroyImmediate(go);
        }

        [Test]
        public void MatchEndChannel_Raise_ClearsFeed()
        {
            var go         = new GameObject();
            var kfc        = go.AddComponent<KillFeedController>();
            var feed       = CreateFeedSO();
            var endChannel = ScriptableObject.CreateInstance<VoidGameEvent>();

            SetField(kfc, "_feedSO",     feed);
            SetField(kfc, "_onMatchEnd", endChannel);

            feed.Add(new KillFeedSO.KillFeedEntry("A", "B"));
            feed.Add(new KillFeedSO.KillFeedEntry("C", "D"));
            Assert.AreEqual(2, feed.Count, "Pre-condition: feed must have 2 entries.");

            InvokePrivate(kfc, "Awake");
            InvokePrivate(kfc, "OnEnable");

            endChannel.Raise();

            Assert.AreEqual(0, feed.Count,
                "Raising _onMatchEnd must clear the kill feed.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(feed);
            Object.DestroyImmediate(endChannel);
        }

        [Test]
        public void OnDisable_UnregistersFromBothChannels()
        {
            var go          = new GameObject();
            var kfc         = go.AddComponent<KillFeedController>();
            var feed        = CreateFeedSO();
            var feedChannel = ScriptableObject.CreateInstance<VoidGameEvent>();
            var endChannel  = ScriptableObject.CreateInstance<VoidGameEvent>();

            SetField(kfc, "_feedSO",        feed);
            SetField(kfc, "_onFeedUpdated", feedChannel);
            SetField(kfc, "_onMatchEnd",    endChannel);

            InvokePrivate(kfc, "Awake");
            InvokePrivate(kfc, "OnEnable");
            InvokePrivate(kfc, "OnDisable");

            // Both channels must be safe to raise after unsubscribe.
            Assert.DoesNotThrow(() => feedChannel.Raise());
            Assert.DoesNotThrow(() => endChannel.Raise());
            // Feed should NOT have been cleared by a raise after unsubscribe.
            feed.Add(new KillFeedSO.KillFeedEntry("X", "Y"));
            Assert.AreEqual(1, feed.Count,
                "Match-end channel must not clear the feed after controller is disabled.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(feed);
            Object.DestroyImmediate(feedChannel);
            Object.DestroyImmediate(endChannel);
        }
    }
}
