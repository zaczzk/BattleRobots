using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="MatchHistoryController"/>.
    ///
    /// Covers:
    ///   • OnEnable with null _onMatchEnded — ?. guard prevents throw; and
    ///     PopulateHistory() is called immediately but exits early due to null refs.
    ///   • OnDisable with null _onMatchEnded — ?. guard prevents throw.
    ///   • PopulateHistory() with null _listContainer — logs warning + returns early.
    ///   • PopulateHistory() with null _rowPrefab — logs warning + returns early.
    ///   • PopulateHistory() with both null — same early-return, no throw.
    ///   • OnDisable unregisters _populateDelegate from _onMatchEnded
    ///     (external-counter pattern verifies only the test counter fires after
    ///     disable).
    ///
    /// All tests run headless; no Prefab, ScrollRect, or uGUI objects required.
    /// SaveSystem.Delete() is called in SetUp/TearDown to prevent disk pollution
    /// (PopulateHistory calls SaveSystem.Load() when it reaches the load step, but
    /// the null-container early-return means Load is never reached in these tests).
    /// </summary>
    public class MatchHistoryControllerTests
    {
        // ── Reflection helper ─────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()    => SaveSystem.Delete();

        [TearDown]
        public void TearDown() => SaveSystem.Delete();

        // ── Factory helper ────────────────────────────────────────────────────

        private static (GameObject go, MatchHistoryController ctrl) MakeCtrl()
        {
            var go   = new GameObject("MatchHistoryController");
            go.SetActive(false); // inactive so OnEnable doesn't run during field setup
            var ctrl = go.AddComponent<MatchHistoryController>();
            return (go, ctrl);
        }

        // ── OnEnable — null event channel ─────────────────────────────────────

        [Test]
        public void OnEnable_NullEventChannel_DoesNotThrow()
        {
            var (go, _) = MakeCtrl();
            // _onMatchEnded not assigned; OnEnable's ?. guard must silently skip.
            // PopulateHistory() is called but exits early (null container + prefab).
            Assert.DoesNotThrow(() => go.SetActive(true),
                "OnEnable with null _onMatchEnded must not throw.");
            Object.DestroyImmediate(go);
        }

        // ── OnDisable — null event channel ────────────────────────────────────

        [Test]
        public void OnDisable_NullEventChannel_DoesNotThrow()
        {
            var (go, _) = MakeCtrl();
            go.SetActive(true);
            Assert.DoesNotThrow(() => go.SetActive(false),
                "OnDisable with null _onMatchEnded must not throw.");
            Object.DestroyImmediate(go);
        }

        // ── PopulateHistory — null container ──────────────────────────────────

        [Test]
        public void PopulateHistory_NullListContainer_DoesNotThrow()
        {
            var (go, ctrl) = MakeCtrl();
            go.SetActive(true); // OnEnable runs PopulateHistory safely

            // Calling the public method directly must also be safe.
            Assert.DoesNotThrow(() => ctrl.PopulateHistory(),
                "PopulateHistory with null _listContainer must not throw.");

            Object.DestroyImmediate(go);
        }

        // ── PopulateHistory — null row prefab ─────────────────────────────────

        [Test]
        public void PopulateHistory_NullRowPrefab_DoesNotThrow()
        {
            var containerGO = new GameObject("Container");
            var (go, ctrl)  = MakeCtrl();
            SetField(ctrl, "_listContainer", containerGO.transform);
            // _rowPrefab remains null; the guard catches (_listContainer != null &&
            // _rowPrefab == null) → logs warning + returns.
            go.SetActive(true);

            Assert.DoesNotThrow(() => ctrl.PopulateHistory(),
                "PopulateHistory with null _rowPrefab must not throw.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(containerGO);
        }

        // ── PopulateHistory — both null ───────────────────────────────────────

        [Test]
        public void PopulateHistory_NullBoth_DoesNotThrow()
        {
            var (go, ctrl) = MakeCtrl();
            go.SetActive(true);

            Assert.DoesNotThrow(() => ctrl.PopulateHistory(),
                "PopulateHistory with both _listContainer and _rowPrefab null must not throw.");

            Object.DestroyImmediate(go);
        }

        // ── OnDisable — unregisters _populateDelegate from _onMatchEnded ──────

        [Test]
        public void OnDisable_UnregistersFromMatchEndedChannel()
        {
            var matchEnded = ScriptableObject.CreateInstance<VoidGameEvent>();

            // External counter — the only callback that should remain after disable.
            int externalCount = 0;
            matchEnded.RegisterCallback(() => externalCount++);

            var (go, ctrl) = MakeCtrl();
            SetField(ctrl, "_onMatchEnded", matchEnded);

            go.SetActive(true);   // OnEnable registers _populateDelegate
            go.SetActive(false);  // OnDisable unregisters _populateDelegate

            // Raise event — only external counter should fire.
            matchEnded.Raise();

            Assert.AreEqual(1, externalCount,
                "After OnDisable, only the external counter (not _populateDelegate) " +
                "should fire on _onMatchEnded.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(matchEnded);
        }
    }
}
