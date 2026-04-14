using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T231:
    ///   <see cref="HazardDamageTrackerSO"/> and <see cref="HazardDamageHUDController"/>.
    ///
    /// HazardDamageTrackerSOTests (10):
    ///   FreshInstance_TotalDamage_Zero              ×1
    ///   AddDamage_ZeroAmount_NoChange               ×1
    ///   AddDamage_NegativeAmount_NoChange           ×1
    ///   AddDamage_Lava_Accumulates                  ×1
    ///   GetDamageForType_UnknownCast_ReturnsZero    ×1
    ///   GetHitCountForType_AfterAddDamage           ×1
    ///   GetMostFrequentType_NoHits_Null             ×1
    ///   GetMostFrequentType_ReturnsHighestCount     ×1
    ///   Reset_ClearsAll                             ×1
    ///   AddDamage_FiresEvent                        ×1
    ///
    /// HazardDamageHUDControllerTests (6):
    ///   FreshInstance_TrackerNull                   ×1
    ///   OnEnable_NullRefs_DoesNotThrow              ×1
    ///   OnDisable_NullRefs_DoesNotThrow             ×1
    ///   OnDisable_Unregisters                       ×1
    ///   Refresh_NullTracker_HidesPanel              ×1
    ///   Refresh_WithTracker_ShowsPanel              ×1
    ///
    /// Total: 16 new EditMode tests.
    /// </summary>
    public class HazardDamageTrackerTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static void InvokePrivate(object target, string method)
        {
            MethodInfo mi = target.GetType()
                .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(mi, $"Method '{method}' not found on {target.GetType().Name}.");
            mi.Invoke(target, null);
        }

        private static HazardDamageTrackerSO CreateTracker()
        {
            var so = ScriptableObject.CreateInstance<HazardDamageTrackerSO>();
            InvokePrivate(so, "OnEnable"); // triggers Reset()
            return so;
        }

        private static VoidGameEvent CreateVoidEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static HazardDamageHUDController CreateHUD() =>
            new GameObject("HazardHUD_Test").AddComponent<HazardDamageHUDController>();

        // ── HazardDamageTrackerSOTests ────────────────────────────────────────

        [Test]
        public void FreshInstance_TotalDamage_Zero()
        {
            var tracker = CreateTracker();
            Assert.AreEqual(0f, tracker.GetTotalDamage(), 0.001f,
                "GetTotalDamage must be 0 on a fresh instance.");
            Object.DestroyImmediate(tracker);
        }

        [Test]
        public void AddDamage_ZeroAmount_NoChange()
        {
            var tracker = CreateTracker();
            tracker.AddDamage(HazardZoneType.Lava, 0f);
            Assert.AreEqual(0f, tracker.GetTotalDamage(), 0.001f,
                "Adding 0 damage must not change total.");
            Object.DestroyImmediate(tracker);
        }

        [Test]
        public void AddDamage_NegativeAmount_NoChange()
        {
            var tracker = CreateTracker();
            tracker.AddDamage(HazardZoneType.Electric, -5f);
            Assert.AreEqual(0f, tracker.GetTotalDamage(), 0.001f,
                "Adding negative damage must not change total.");
            Object.DestroyImmediate(tracker);
        }

        [Test]
        public void AddDamage_Lava_Accumulates()
        {
            var tracker = CreateTracker();
            tracker.AddDamage(HazardZoneType.Lava, 10f);
            tracker.AddDamage(HazardZoneType.Lava, 5f);
            Assert.AreEqual(15f, tracker.GetDamageForType(HazardZoneType.Lava), 0.001f,
                "Lava damage must accumulate across multiple calls.");
            Assert.AreEqual(15f, tracker.GetTotalDamage(), 0.001f,
                "GetTotalDamage must reflect accumulated lava damage.");
            Object.DestroyImmediate(tracker);
        }

        [Test]
        public void GetDamageForType_UnknownCast_ReturnsZero()
        {
            var tracker = CreateTracker();
            // Cast an out-of-range int to HazardZoneType to simulate unknown value
            float result = tracker.GetDamageForType((HazardZoneType)99);
            Assert.AreEqual(0f, result, 0.001f,
                "Unknown HazardZoneType must return 0 from GetDamageForType.");
            Object.DestroyImmediate(tracker);
        }

        [Test]
        public void GetHitCountForType_AfterAddDamage()
        {
            var tracker = CreateTracker();
            tracker.AddDamage(HazardZoneType.Acid, 8f);
            tracker.AddDamage(HazardZoneType.Acid, 3f);
            Assert.AreEqual(2, tracker.GetHitCountForType(HazardZoneType.Acid),
                "Hit count must increment once per AddDamage call.");
            Object.DestroyImmediate(tracker);
        }

        [Test]
        public void GetMostFrequentType_NoHits_Null()
        {
            var tracker = CreateTracker();
            Assert.IsNull(tracker.GetMostFrequentType(),
                "GetMostFrequentType must return null when no damage has been recorded.");
            Object.DestroyImmediate(tracker);
        }

        [Test]
        public void GetMostFrequentType_ReturnsHighestCount()
        {
            var tracker = CreateTracker();
            tracker.AddDamage(HazardZoneType.Lava,     5f);   // 1 hit
            tracker.AddDamage(HazardZoneType.Electric, 3f);   // 1 hit
            tracker.AddDamage(HazardZoneType.Electric, 2f);   // 2nd hit — winner
            tracker.AddDamage(HazardZoneType.Spikes,   1f);   // 1 hit

            HazardZoneType? most = tracker.GetMostFrequentType();
            Assert.IsTrue(most.HasValue, "GetMostFrequentType must return a value when hits exist.");
            Assert.AreEqual(HazardZoneType.Electric, most.Value,
                "Electric must be the most frequent type (2 hits vs 1 for others).");
            Object.DestroyImmediate(tracker);
        }

        [Test]
        public void Reset_ClearsAll()
        {
            var tracker = CreateTracker();
            tracker.AddDamage(HazardZoneType.Lava,     20f);
            tracker.AddDamage(HazardZoneType.Spikes,   10f);
            tracker.Reset();

            Assert.AreEqual(0f, tracker.GetTotalDamage(), 0.001f,
                "Reset must zero total damage.");
            Assert.AreEqual(0, tracker.GetHitCountForType(HazardZoneType.Lava),
                "Reset must zero Lava hit count.");
            Assert.IsNull(tracker.GetMostFrequentType(),
                "Reset must clear most-frequent tracking.");
            Object.DestroyImmediate(tracker);
        }

        [Test]
        public void AddDamage_FiresEvent()
        {
            var tracker = CreateTracker();
            var ch      = CreateVoidEvent();
            SetField(tracker, "_onDamageAdded", ch);

            int count = 0;
            ch.RegisterCallback(() => count++);

            tracker.AddDamage(HazardZoneType.Acid, 5f);

            Assert.AreEqual(1, count,
                "_onDamageAdded must fire once per valid AddDamage call.");
            Object.DestroyImmediate(tracker);
            Object.DestroyImmediate(ch);
        }

        // ── HazardDamageHUDControllerTests ────────────────────────────────────

        [Test]
        public void FreshInstance_TrackerNull()
        {
            var hud = CreateHUD();
            Assert.IsNull(hud.Tracker,
                "Tracker must be null on a fresh instance.");
            Object.DestroyImmediate(hud.gameObject);
        }

        [Test]
        public void OnEnable_NullRefs_DoesNotThrow()
        {
            var hud = CreateHUD();
            InvokePrivate(hud, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(hud, "OnEnable"),
                "OnEnable with all-null refs must not throw.");
            Object.DestroyImmediate(hud.gameObject);
        }

        [Test]
        public void OnDisable_NullRefs_DoesNotThrow()
        {
            var hud = CreateHUD();
            InvokePrivate(hud, "Awake");
            InvokePrivate(hud, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(hud, "OnDisable"),
                "OnDisable with all-null refs must not throw.");
            Object.DestroyImmediate(hud.gameObject);
        }

        [Test]
        public void OnDisable_Unregisters()
        {
            var hud = CreateHUD();
            var ch  = CreateVoidEvent();
            SetField(hud, "_onMatchEnded", ch);
            InvokePrivate(hud, "Awake");
            InvokePrivate(hud, "OnEnable");
            InvokePrivate(hud, "OnDisable");

            int count = 0;
            ch.RegisterCallback(() => count++);
            ch.Raise();

            Assert.AreEqual(1, count,
                "After OnDisable only the manually registered callback must fire.");
            Object.DestroyImmediate(hud.gameObject);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void Refresh_NullTracker_HidesPanel()
        {
            var hud   = CreateHUD();
            var panel = new GameObject("Panel");
            panel.SetActive(true);
            SetField(hud, "_panel", panel);
            InvokePrivate(hud, "Awake");

            hud.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden when Tracker is null.");
            Object.DestroyImmediate(hud.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Refresh_WithTracker_ShowsPanel()
        {
            var hud     = CreateHUD();
            var tracker = CreateTracker();
            var panel   = new GameObject("Panel");
            panel.SetActive(false);
            SetField(hud, "_tracker", tracker);
            SetField(hud, "_panel",   panel);
            InvokePrivate(hud, "Awake");

            hud.Refresh();

            Assert.IsTrue(panel.activeSelf,
                "Panel must be shown when a valid Tracker is assigned.");
            Object.DestroyImmediate(hud.gameObject);
            Object.DestroyImmediate(tracker);
            Object.DestroyImmediate(panel);
        }
    }
}
