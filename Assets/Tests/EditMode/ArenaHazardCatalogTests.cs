using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.Physics;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T237:
    ///   <see cref="ArenaHazardCatalogSO"/> and
    ///   <see cref="ArenaHazardActivationController"/>.
    ///
    /// ArenaHazardCatalogSOTests (10):
    ///   FreshInstance_EntryCountZero                            ×1
    ///   GetActivationDelay_OutOfRange_ReturnsZero               ×1
    ///   GetActivationDelay_NegativeIndex_ReturnsZero            ×1
    ///   GetActivationDelay_ValidIndex_ReturnsDelay              ×1
    ///   GetHazardId_OutOfRange_ReturnsEmpty                     ×1
    ///   GetHazardId_ValidIndex_ReturnsId                        ×1
    ///   EntryCount_MatchesEntriesLength                         ×1
    ///   RaiseAllActive_NullEvent_NoThrow                        ×1
    ///   RaiseAllActive_WithEvent_FiresEvent                     ×1
    ///   GetActivationDelay_FirstEntry_DelayZero_ReturnsZero     ×1
    ///
    /// ArenaHazardActivationControllerTests (8):
    ///   FreshInstance_CatalogNull                               ×1
    ///   FreshInstance_AllActivatedFalse                         ×1
    ///   OnEnable_NullRefs_DoesNotThrow                          ×1
    ///   OnDisable_Unregisters                                   ×1
    ///   HandleMatchStarted_SetsIsMatchRunning                   ×1
    ///   HandleMatchEnded_SetsIsMatchRunningFalse                ×1
    ///   Tick_NullHazards_NoThrow                                ×1
    ///   HandleMatchStarted_NoCatalog_ActivatesHazardImmediately ×1
    ///
    /// Total: 18 new EditMode tests.
    /// </summary>
    public class ArenaHazardCatalogTests
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

        private static VoidGameEvent CreateVoidEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static ArenaHazardCatalogSO CreateCatalogSO() =>
            ScriptableObject.CreateInstance<ArenaHazardCatalogSO>();

        private static ArenaHazardActivationController CreateController() =>
            new GameObject("HazardActivation_Test").AddComponent<ArenaHazardActivationController>();

        private static HazardZoneController CreateHazardZone() =>
            new GameObject("HazardZone_Test").AddComponent<HazardZoneController>();

        /// <summary>
        /// Sets a <see cref="ArenaHazardCatalogSO._entries"/> array via reflection
        /// using an array of <see cref="ArenaHazardCatalogSO.HazardCatalogEntry"/> values.
        /// </summary>
        private static void SetEntries(ArenaHazardCatalogSO so,
            ArenaHazardCatalogSO.HazardCatalogEntry[] entries)
        {
            SetField(so, "_entries", entries);
        }

        // ── ArenaHazardCatalogSOTests ─────────────────────────────────────────

        [Test]
        public void FreshInstance_EntryCountZero()
        {
            var so = CreateCatalogSO();
            Assert.AreEqual(0, so.EntryCount,
                "EntryCount must be 0 on a fresh instance (empty entries array).");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void GetActivationDelay_OutOfRange_ReturnsZero()
        {
            var so = CreateCatalogSO();
            Assert.AreEqual(0f, so.GetActivationDelay(0), 0.001f,
                "GetActivationDelay(0) must return 0 when entries is empty.");
            Assert.AreEqual(0f, so.GetActivationDelay(5), 0.001f,
                "GetActivationDelay(5) must return 0 when entries has fewer elements.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void GetActivationDelay_NegativeIndex_ReturnsZero()
        {
            var so = CreateCatalogSO();
            Assert.AreEqual(0f, so.GetActivationDelay(-1), 0.001f,
                "GetActivationDelay(-1) must return 0 (negative index is out-of-range).");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void GetActivationDelay_ValidIndex_ReturnsDelay()
        {
            var so = CreateCatalogSO();
            var entries = new[]
            {
                new ArenaHazardCatalogSO.HazardCatalogEntry { hazardId = "Lava",     activationDelay = 5f },
                new ArenaHazardCatalogSO.HazardCatalogEntry { hazardId = "Electric", activationDelay = 10f },
            };
            SetEntries(so, entries);

            Assert.AreEqual(5f,  so.GetActivationDelay(0), 0.001f,
                "GetActivationDelay(0) must return 5 seconds for the first entry.");
            Assert.AreEqual(10f, so.GetActivationDelay(1), 0.001f,
                "GetActivationDelay(1) must return 10 seconds for the second entry.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void GetHazardId_OutOfRange_ReturnsEmpty()
        {
            var so = CreateCatalogSO();
            Assert.AreEqual(string.Empty, so.GetHazardId(0),
                "GetHazardId(0) must return empty string when entries is empty.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void GetHazardId_ValidIndex_ReturnsId()
        {
            var so = CreateCatalogSO();
            var entries = new[]
            {
                new ArenaHazardCatalogSO.HazardCatalogEntry { hazardId = "Spikes", activationDelay = 0f },
            };
            SetEntries(so, entries);

            Assert.AreEqual("Spikes", so.GetHazardId(0),
                "GetHazardId(0) must return the configured hazardId.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void EntryCount_MatchesEntriesLength()
        {
            var so = CreateCatalogSO();
            var entries = new[]
            {
                new ArenaHazardCatalogSO.HazardCatalogEntry { hazardId = "A", activationDelay = 0f },
                new ArenaHazardCatalogSO.HazardCatalogEntry { hazardId = "B", activationDelay = 5f },
                new ArenaHazardCatalogSO.HazardCatalogEntry { hazardId = "C", activationDelay = 15f },
            };
            SetEntries(so, entries);

            Assert.AreEqual(3, so.EntryCount,
                "EntryCount must equal the length of the _entries array.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void RaiseAllActive_NullEvent_NoThrow()
        {
            var so = CreateCatalogSO();
            // _onAllHazardsActive is null by default
            Assert.DoesNotThrow(() => so.RaiseAllActive(),
                "RaiseAllActive with a null event channel must not throw.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void RaiseAllActive_WithEvent_FiresEvent()
        {
            var so = CreateCatalogSO();
            var ch = CreateVoidEvent();
            SetField(so, "_onAllHazardsActive", ch);

            int count = 0;
            ch.RegisterCallback(() => count++);

            so.RaiseAllActive();

            Assert.AreEqual(1, count,
                "RaiseAllActive must fire the _onAllHazardsActive event exactly once.");
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void GetActivationDelay_FirstEntry_DelayZero_ReturnsZero()
        {
            var so = CreateCatalogSO();
            var entries = new[]
            {
                new ArenaHazardCatalogSO.HazardCatalogEntry { hazardId = "Instant", activationDelay = 0f },
            };
            SetEntries(so, entries);

            Assert.AreEqual(0f, so.GetActivationDelay(0), 0.001f,
                "GetActivationDelay must return 0 for an entry configured with delay = 0.");
            Object.DestroyImmediate(so);
        }

        // ── ArenaHazardActivationControllerTests ──────────────────────────────

        [Test]
        public void FreshInstance_CatalogNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.Catalog,
                "Catalog must be null on a fresh instance.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void FreshInstance_AllActivatedFalse()
        {
            var ctrl = CreateController();
            Assert.IsFalse(ctrl.AllActivated,
                "AllActivated must be false on a fresh instance.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnEnable"),
                "OnEnable with all-null refs must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnDisable_Unregisters()
        {
            var ctrl    = CreateController();
            var started = CreateVoidEvent();
            var ended   = CreateVoidEvent();
            SetField(ctrl, "_onMatchStarted", started);
            SetField(ctrl, "_onMatchEnded",   ended);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            InvokePrivate(ctrl, "OnDisable");

            int startCount = 0, endCount = 0;
            started.RegisterCallback(() => startCount++);
            ended.RegisterCallback(() => endCount++);
            started.Raise();
            ended.Raise();

            Assert.AreEqual(1, startCount,
                "After OnDisable only the manual callback fires on _onMatchStarted.");
            Assert.AreEqual(1, endCount,
                "After OnDisable only the manual callback fires on _onMatchEnded.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(started);
            Object.DestroyImmediate(ended);
        }

        [Test]
        public void HandleMatchStarted_SetsIsMatchRunning()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");

            ctrl.HandleMatchStarted();

            Assert.IsTrue(ctrl.IsMatchRunning,
                "IsMatchRunning must be true after HandleMatchStarted.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void HandleMatchEnded_SetsIsMatchRunningFalse()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            ctrl.HandleMatchStarted();
            ctrl.HandleMatchEnded();

            Assert.IsFalse(ctrl.IsMatchRunning,
                "IsMatchRunning must be false after HandleMatchEnded.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Tick_NullHazards_NoThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            SetField(ctrl, "_matchRunning", true);
            // _hazards remains null

            Assert.DoesNotThrow(() => ctrl.Tick(1f),
                "Tick with null hazards must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void HandleMatchStarted_NoCatalog_ActivatesHazardImmediately()
        {
            var ctrl   = CreateController();
            var hazard = CreateHazardZone();
            hazard.IsActive = false;

            SetField(ctrl, "_hazards", new HazardZoneController[] { hazard });
            // _catalog remains null — default delay = 0
            InvokePrivate(ctrl, "Awake");

            ctrl.HandleMatchStarted();

            Assert.IsTrue(hazard.IsActive,
                "A hazard with no catalog (delay 0) must be activated immediately at match start.");
            Assert.IsTrue(ctrl.AllActivated,
                "AllActivated must be true once every hazard is active.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(hazard.gameObject);
        }
    }
}
