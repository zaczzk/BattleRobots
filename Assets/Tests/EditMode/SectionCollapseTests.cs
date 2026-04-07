using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode unit tests for T058 — Section collapse/expand toggle per ping tier.
    ///
    /// Coverage (12 cases):
    ///
    /// SectionHeaderUI — setup and initial state
    ///   [01] DefaultState_IsNotCollapsed
    ///   [02] Setup_SetsTier
    ///   [03] Setup_SetsIsCollapsed_True
    ///   [04] HandleClicked_TogglesIsCollapsed
    ///   [05] HandleClicked_FiresOnToggleCallback_WithCorrectTier
    ///   [06] HandleClicked_NullCallback_DoesNotThrow
    ///   [07] SetupTwice_SecondCallOverwritesState
    ///
    /// RoomListUI.IsTierCollapsed / ToggleTierCollapse — collapse state logic
    ///   [08] IsTierCollapsed_DefaultState_AllTiersExpanded
    ///   [09] ToggleTierCollapse_FirstCall_TierBecomesCollapsed
    ///   [10] ToggleTierCollapse_SecondCall_TierBecomesExpanded
    ///   [11] ToggleTierCollapse_MultipleTiers_IndependentState
    ///   [12] ToggleTierCollapse_AllFourTiers_CollapseThenExpand
    /// </summary>
    [TestFixture]
    public sealed class SectionCollapseTests
    {
        // ── SectionHeaderUI ───────────────────────────────────────────────────

        // [01] Default IsCollapsed is false before any Setup call.
        [Test]
        public void DefaultState_IsNotCollapsed()
        {
            var go = new GameObject();
            var header = go.AddComponent<SectionHeaderUI>();

            Assert.IsFalse(header.IsCollapsed,
                "A freshly-added SectionHeaderUI must not start collapsed.");

            Object.DestroyImmediate(go);
        }

        // [02] Setup assigns the correct Tier.
        [Test]
        public void Setup_SetsTier()
        {
            var go = new GameObject();
            var header = go.AddComponent<SectionHeaderUI>();

            header.Setup("Excellent (≤80 ms)", PingTier.Excellent, false, null);

            Assert.AreEqual(PingTier.Excellent, header.Tier,
                "Tier must match the value passed to Setup.");

            Object.DestroyImmediate(go);
        }

        // [03] Setup can initialise the header as collapsed.
        [Test]
        public void Setup_SetsIsCollapsed_True()
        {
            var go = new GameObject();
            var header = go.AddComponent<SectionHeaderUI>();

            header.Setup("High (>150 ms)", PingTier.High, true, null);

            Assert.IsTrue(header.IsCollapsed,
                "IsCollapsed must be true when initiallyCollapsed is passed as true.");

            Object.DestroyImmediate(go);
        }

        // [04] Clicking toggles IsCollapsed (false → true → false).
        [Test]
        public void HandleClicked_TogglesIsCollapsed()
        {
            var go = new GameObject();
            var header = go.AddComponent<SectionHeaderUI>();
            header.Setup("Good (≤150 ms)", PingTier.Good, false, null);

            header.HandleClicked();
            Assert.IsTrue(header.IsCollapsed,
                "IsCollapsed must become true after the first click.");

            header.HandleClicked();
            Assert.IsFalse(header.IsCollapsed,
                "IsCollapsed must revert to false after the second click.");

            Object.DestroyImmediate(go);
        }

        // [05] HandleClicked fires the onToggle callback with the correct tier.
        [Test]
        public void HandleClicked_FiresOnToggleCallback_WithCorrectTier()
        {
            var go = new GameObject();
            var header = go.AddComponent<SectionHeaderUI>();
            PingTier received = (PingTier)(-1);

            header.Setup("Unknown", PingTier.Unknown, false, t => received = t);
            header.HandleClicked();

            Assert.AreEqual(PingTier.Unknown, received,
                "The onToggle callback must receive the header's assigned tier.");

            Object.DestroyImmediate(go);
        }

        // [06] A null onToggle callback must not cause an exception.
        [Test]
        public void HandleClicked_NullCallback_DoesNotThrow()
        {
            var go = new GameObject();
            var header = go.AddComponent<SectionHeaderUI>();
            header.Setup("Excellent (≤80 ms)", PingTier.Excellent, false, null);

            Assert.DoesNotThrow(() => header.HandleClicked(),
                "Clicking a header with a null callback must not throw.");

            Object.DestroyImmediate(go);
        }

        // [07] A second Setup call overwrites the state from the first.
        [Test]
        public void SetupTwice_SecondCallOverwritesState()
        {
            var go = new GameObject();
            var header = go.AddComponent<SectionHeaderUI>();

            header.Setup("Excellent (≤80 ms)", PingTier.Excellent, false, null);
            header.Setup("High (>150 ms)", PingTier.High, true, null);

            Assert.AreEqual(PingTier.High, header.Tier,
                "Tier must reflect the second Setup call.");
            Assert.IsTrue(header.IsCollapsed,
                "IsCollapsed must reflect the second Setup call.");

            Object.DestroyImmediate(go);
        }

        // ── RoomListUI ────────────────────────────────────────────────────────

        // [08] All tiers start expanded (none collapsed).
        [Test]
        public void IsTierCollapsed_DefaultState_AllTiersExpanded()
        {
            var go = new GameObject();
            var ui = go.AddComponent<RoomListUI>();

            foreach (PingTier tier in System.Enum.GetValues(typeof(PingTier)))
            {
                Assert.IsFalse(ui.IsTierCollapsed(tier),
                    $"{tier} must not be collapsed on a freshly-created RoomListUI.");
            }

            Object.DestroyImmediate(go);
        }

        // [09] First ToggleTierCollapse call collapses the tier.
        [Test]
        public void ToggleTierCollapse_FirstCall_TierBecomesCollapsed()
        {
            var go = new GameObject();
            var ui = go.AddComponent<RoomListUI>();

            ui.ToggleTierCollapse(PingTier.Good);

            Assert.IsTrue(ui.IsTierCollapsed(PingTier.Good),
                "Good tier must be collapsed after one ToggleTierCollapse call.");

            Object.DestroyImmediate(go);
        }

        // [10] A second ToggleTierCollapse call on the same tier expands it again.
        [Test]
        public void ToggleTierCollapse_SecondCall_TierBecomesExpanded()
        {
            var go = new GameObject();
            var ui = go.AddComponent<RoomListUI>();

            ui.ToggleTierCollapse(PingTier.Excellent);
            ui.ToggleTierCollapse(PingTier.Excellent);

            Assert.IsFalse(ui.IsTierCollapsed(PingTier.Excellent),
                "Excellent tier must be expanded again after two ToggleTierCollapse calls.");

            Object.DestroyImmediate(go);
        }

        // [11] Each tier's collapsed state is tracked independently.
        [Test]
        public void ToggleTierCollapse_MultipleTiers_IndependentState()
        {
            var go = new GameObject();
            var ui = go.AddComponent<RoomListUI>();

            ui.ToggleTierCollapse(PingTier.High);
            ui.ToggleTierCollapse(PingTier.Unknown);

            Assert.IsTrue(ui.IsTierCollapsed(PingTier.High),    "High must be collapsed.");
            Assert.IsTrue(ui.IsTierCollapsed(PingTier.Unknown), "Unknown must be collapsed.");
            Assert.IsFalse(ui.IsTierCollapsed(PingTier.Excellent), "Excellent must remain expanded.");
            Assert.IsFalse(ui.IsTierCollapsed(PingTier.Good),      "Good must remain expanded.");

            Object.DestroyImmediate(go);
        }

        // [12] All four tiers can be collapsed, then all expanded, in sequence.
        [Test]
        public void ToggleTierCollapse_AllFourTiers_CollapseThenExpand()
        {
            var go = new GameObject();
            var ui = go.AddComponent<RoomListUI>();

            // Collapse all tiers.
            foreach (PingTier tier in System.Enum.GetValues(typeof(PingTier)))
                ui.ToggleTierCollapse(tier);

            foreach (PingTier tier in System.Enum.GetValues(typeof(PingTier)))
                Assert.IsTrue(ui.IsTierCollapsed(tier), $"{tier} must be collapsed.");

            // Expand all tiers.
            foreach (PingTier tier in System.Enum.GetValues(typeof(PingTier)))
                ui.ToggleTierCollapse(tier);

            foreach (PingTier tier in System.Enum.GetValues(typeof(PingTier)))
                Assert.IsFalse(ui.IsTierCollapsed(tier), $"{tier} must be expanded again.");

            Object.DestroyImmediate(go);
        }
    }
}
