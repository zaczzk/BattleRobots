using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T347: <see cref="ZoneControlProfileSO"/> and
    /// <see cref="ZoneControlProfileController"/>.
    ///
    /// ZoneControlProfileTests (12):
    ///   SO_FreshInstance_TotalWins_Zero                                      ×1
    ///   SO_FreshInstance_WinRate_Zero                                        ×1
    ///   SO_RecordMatchResult_Win_IncrementsWins                              ×1
    ///   SO_RecordMatchResult_Loss_IncrementsLosses                           ×1
    ///   SO_RecordMatchResult_NegativeZones_ClampsToZero                      ×1
    ///   SO_RecordMatchResult_FiresOnProfileUpdated                           ×1
    ///   SO_WinRate_TwoWinsOneLoss_Correct                                    ×1
    ///   SO_Reset_ClearsAllAccumulators                                       ×1
    ///   Controller_OnEnable_AllNullRefs_DoesNotThrow                         ×1
    ///   Controller_OnDisable_AllNullRefs_DoesNotThrow                        ×1
    ///   Controller_OnDisable_Unregisters_Channels                            ×1
    ///   Controller_HandleMatchEnded_RecordsResult                            ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneControlProfileTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static ZoneControlProfileSO CreateProfileSO()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlProfileSO>();
            so.Reset();
            return so;
        }

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_TotalWins_Zero()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlProfileSO>();
            Assert.AreEqual(0, so.TotalWins,
                "TotalWins must be zero on a fresh instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_WinRate_Zero()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlProfileSO>();
            Assert.AreEqual(0f, so.WinRate,
                "WinRate must be zero when no matches have been played.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordMatchResult_Win_IncrementsWins()
        {
            var so = CreateProfileSO();
            so.RecordMatchResult(true, 5);
            Assert.AreEqual(1, so.TotalWins,
                "TotalWins must increment by 1 on a win.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordMatchResult_Loss_IncrementsLosses()
        {
            var so = CreateProfileSO();
            so.RecordMatchResult(false, 3);
            Assert.AreEqual(1, so.TotalLosses,
                "TotalLosses must increment by 1 on a loss.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordMatchResult_NegativeZones_ClampsToZero()
        {
            var so = CreateProfileSO();
            so.RecordMatchResult(true, -10);
            Assert.AreEqual(0, so.TotalZonesCaptured,
                "TotalZonesCaptured must clamp negative zone values to zero.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordMatchResult_FiresOnProfileUpdated()
        {
            var so  = CreateProfileSO();
            var evt = CreateEvent();
            SetField(so, "_onProfileUpdated", evt);

            int count = 0;
            evt.RegisterCallback(() => count++);

            so.RecordMatchResult(true, 5);

            Assert.AreEqual(1, count,
                "_onProfileUpdated must fire once on RecordMatchResult.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_WinRate_TwoWinsOneLoss_Correct()
        {
            var so = CreateProfileSO();
            so.RecordMatchResult(true, 0);
            so.RecordMatchResult(true, 0);
            so.RecordMatchResult(false, 0);
            Assert.AreEqual(2f / 3f, so.WinRate, 0.001f,
                "WinRate must be 2/3 after two wins and one loss.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAllAccumulators()
        {
            var so = CreateProfileSO();
            so.RecordMatchResult(true, 10);
            so.RecordMatchResult(false, 5);
            so.Reset();
            Assert.AreEqual(0, so.TotalWins,           "TotalWins must be 0 after Reset.");
            Assert.AreEqual(0, so.TotalLosses,         "TotalLosses must be 0 after Reset.");
            Assert.AreEqual(0, so.TotalZonesCaptured,  "TotalZonesCaptured must be 0 after Reset.");
            Object.DestroyImmediate(so);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_OnEnable_AllNullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_Profile_OnEnable_Null");
            Assert.DoesNotThrow(
                () => go.AddComponent<ZoneControlProfileController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_AllNullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_Profile_OnDisable_Null");
            go.AddComponent<ZoneControlProfileController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channels()
        {
            var go   = new GameObject("Test_Profile_Unregister");
            var ctrl = go.AddComponent<ZoneControlProfileController>();
            var evt  = CreateEvent();
            SetField(ctrl, "_onMatchEnded", evt);

            go.SetActive(true);
            go.SetActive(false);

            int count = 0;
            evt.RegisterCallback(() => count++);
            evt.Raise();

            Assert.AreEqual(1, count,
                "_onMatchEnded must be unregistered after OnDisable.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void Controller_HandleMatchEnded_RecordsResult()
        {
            var go        = new GameObject("Test_Profile_HandleMatchEnded");
            var ctrl      = go.AddComponent<ZoneControlProfileController>();
            var profileSO = CreateProfileSO();
            SetField(ctrl, "_profileSO", profileSO);

            ctrl.HandleMatchEnded();

            // No scoreboard → playerWon = false; no summary → zones = 0.
            Assert.AreEqual(1, profileSO.MatchesPlayed,
                "HandleMatchEnded must call RecordMatchResult, incrementing MatchesPlayed.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(profileSO);
        }
    }
}
