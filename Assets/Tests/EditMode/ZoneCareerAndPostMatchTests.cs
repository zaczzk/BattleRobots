using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T278:
    ///   <see cref="ZoneCareerPersistenceController"/> and
    ///   <see cref="PostMatchZoneStatsController"/>.
    ///
    /// ZoneCareerPersistenceControllerTests (6):
    ///   FreshInstance_Tracker_Null                                         ×1
    ///   OnEnable_NullRefs_DoesNotThrow                                     ×1
    ///   OnDisable_NullRefs_DoesNotThrow                                    ×1
    ///   OnDisable_Unregisters                                              ×1
    ///   HandleMatchEnded_NullTracker_NoThrow                               ×1
    ///   HandleMatchEnded_AccumulatesTracker                                ×1
    ///
    /// PostMatchZoneStatsControllerTests (6):
    ///   FreshInstance_Tracker_Null                                         ×1
    ///   OnEnable_NullRefs_DoesNotThrow                                     ×1
    ///   OnDisable_NullRefs_DoesNotThrow                                    ×1
    ///   Refresh_NullTracker_HidesPanel                                     ×1
    ///   Refresh_WithTracker_SetsLabels                                     ×1
    ///   OnMatchEnded_Raise_Refreshes                                       ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneCareerAndPostMatchTests
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

        private static ZoneCareerPersistenceController CreatePersistenceCtrl() =>
            new GameObject("ZoneCareerPersistence_Test")
                .AddComponent<ZoneCareerPersistenceController>();

        private static PostMatchZoneStatsController CreateStatsCtrl() =>
            new GameObject("PostMatchZoneStats_Test")
                .AddComponent<PostMatchZoneStatsController>();

        private static ZoneScoreTrackerSO CreateTrackerSO() =>
            ScriptableObject.CreateInstance<ZoneScoreTrackerSO>();

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        // ══ ZoneCareerPersistenceController ══════════════════════════════════

        [Test]
        public void ZoneCareerPersistence_FreshInstance_Tracker_Null()
        {
            var ctrl = CreatePersistenceCtrl();
            Assert.IsNull(ctrl.Tracker,
                "Tracker must be null on a fresh ZoneCareerPersistenceController.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void ZoneCareerPersistence_OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreatePersistenceCtrl();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnEnable"),
                "OnEnable with null refs must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void ZoneCareerPersistence_OnDisable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreatePersistenceCtrl();
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnDisable"),
                "OnDisable with null refs must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void ZoneCareerPersistence_OnDisable_Unregisters()
        {
            var ctrl    = CreatePersistenceCtrl();
            var tracker = CreateTrackerSO();
            var evt     = CreateEvent();

            SetField(ctrl, "_tracker",     tracker);
            SetField(ctrl, "_onMatchEnded", evt);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            InvokePrivate(ctrl, "OnDisable");

            tracker.AddPlayerScore(50f);
            float careerBefore = tracker.CareerPlayerScore;
            evt.Raise(); // must not call HandleMatchEnded
            Assert.AreEqual(careerBefore, tracker.CareerPlayerScore, 0.001f,
                "After OnDisable, raising _onMatchEnded must not accumulate career scores.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(tracker);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void ZoneCareerPersistence_HandleMatchEnded_NullTracker_NoThrow()
        {
            var ctrl = CreatePersistenceCtrl();
            Assert.DoesNotThrow(() => ctrl.HandleMatchEnded(),
                "HandleMatchEnded with null Tracker must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void ZoneCareerPersistence_HandleMatchEnded_AccumulatesTracker()
        {
            var ctrl    = CreatePersistenceCtrl();
            var tracker = CreateTrackerSO();

            SetField(ctrl, "_tracker", tracker);

            tracker.AddPlayerScore(40f);
            tracker.AddEnemyScore(20f);

            ctrl.HandleMatchEnded();

            Assert.AreEqual(40f, tracker.CareerPlayerScore, 0.001f,
                "HandleMatchEnded must call AccumulateToCareer, adding PlayerScore to CareerPlayerScore.");
            Assert.AreEqual(20f, tracker.CareerEnemyScore, 0.001f,
                "HandleMatchEnded must call AccumulateToCareer, adding EnemyScore to CareerEnemyScore.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(tracker);
        }

        // ══ PostMatchZoneStatsController ═════════════════════════════════════

        [Test]
        public void PostMatchZoneStats_FreshInstance_Tracker_Null()
        {
            var ctrl = CreateStatsCtrl();
            Assert.IsNull(ctrl.Tracker,
                "Tracker must be null on a fresh PostMatchZoneStatsController.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void PostMatchZoneStats_OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateStatsCtrl();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnEnable"),
                "OnEnable with null refs must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void PostMatchZoneStats_OnDisable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateStatsCtrl();
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnDisable"),
                "OnDisable with null refs must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void PostMatchZoneStats_Refresh_NullTracker_HidesPanel()
        {
            var ctrl    = CreateStatsCtrl();
            var panelGO = new GameObject("Panel");
            SetField(ctrl, "_panel", panelGO);
            panelGO.SetActive(true);

            ctrl.Refresh();

            Assert.IsFalse(panelGO.activeSelf,
                "Refresh with null Tracker must hide the panel.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panelGO);
        }

        [Test]
        public void PostMatchZoneStats_Refresh_WithTracker_SetsLabels()
        {
            var ctrl    = CreateStatsCtrl();
            var tracker = CreateTrackerSO();

            // Set up all four labels.
            var currentPlayerLabelGO = new GameObject("CurP"); var cpLabel = currentPlayerLabelGO.AddComponent<Text>();
            var currentEnemyLabelGO  = new GameObject("CurE"); var ceLabel = currentEnemyLabelGO.AddComponent<Text>();
            var careerPlayerLabelGO  = new GameObject("CarP"); var capLabel = careerPlayerLabelGO.AddComponent<Text>();
            var careerEnemyLabelGO   = new GameObject("CarE"); var caeLabel = careerEnemyLabelGO.AddComponent<Text>();
            var panelGO              = new GameObject("Panel");

            SetField(ctrl, "_tracker",              tracker);
            SetField(ctrl, "_currentPlayerLabel",   cpLabel);
            SetField(ctrl, "_currentEnemyLabel",    ceLabel);
            SetField(ctrl, "_careerPlayerLabel",    capLabel);
            SetField(ctrl, "_careerEnemyLabel",     caeLabel);
            SetField(ctrl, "_panel",                panelGO);

            tracker.AddPlayerScore(30f);
            tracker.AddEnemyScore(15f);
            tracker.LoadSnapshot(130f, 65f); // pre-loaded career totals

            ctrl.Refresh();

            Assert.IsTrue(panelGO.activeSelf, "Panel must be active when tracker is assigned.");
            Assert.AreEqual("Match P: 30", cpLabel.text,   "Current player label must show 'Match P: 30'.");
            Assert.AreEqual("Match E: 15", ceLabel.text,   "Current enemy label must show 'Match E: 15'.");
            Assert.AreEqual("Career P: 130", capLabel.text, "Career player label must show 'Career P: 130'.");
            Assert.AreEqual("Career E: 65",  caeLabel.text, "Career enemy label must show 'Career E: 65'.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(tracker);
            Object.DestroyImmediate(currentPlayerLabelGO);
            Object.DestroyImmediate(currentEnemyLabelGO);
            Object.DestroyImmediate(careerPlayerLabelGO);
            Object.DestroyImmediate(careerEnemyLabelGO);
            Object.DestroyImmediate(panelGO);
        }

        [Test]
        public void PostMatchZoneStats_OnMatchEnded_Raise_Refreshes()
        {
            var ctrl    = CreateStatsCtrl();
            var tracker = CreateTrackerSO();
            var evt     = CreateEvent();
            var panelGO = new GameObject("Panel");

            SetField(ctrl, "_tracker",     tracker);
            SetField(ctrl, "_onMatchEnded", evt);
            SetField(ctrl, "_panel",        panelGO);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            panelGO.SetActive(false);
            evt.Raise();

            Assert.IsTrue(panelGO.activeSelf,
                "Raising _onMatchEnded must trigger Refresh and activate the panel.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(tracker);
            Object.DestroyImmediate(evt);
            Object.DestroyImmediate(panelGO);
        }
    }
}
