using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for <see cref="ZoneControlPrestigeTrackSO"/> and
    /// <see cref="ZoneControlPrestigeTrackController"/>.
    /// </summary>
    public sealed class ZoneControlPrestigeTrackTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static ZoneControlPrestigeTrackSO CreateTrackSO() =>
            ScriptableObject.CreateInstance<ZoneControlPrestigeTrackSO>();

        private static ZoneControlPrestigeTrackController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlPrestigeTrackController>();
        }

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_TotalCaptures_Zero()
        {
            var so = CreateTrackSO();
            Assert.That(so.TotalCaptures, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_CurrentTierIndex_Zero()
        {
            var so = CreateTrackSO();
            Assert.That(so.CurrentTierIndex, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AddCaptures_Negative_Ignored()
        {
            var so = CreateTrackSO();
            so.AddCaptures(-10);
            Assert.That(so.TotalCaptures, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AddCaptures_BelowFirstThreshold_NoTierAdvance()
        {
            var so = CreateTrackSO();
            so.AddCaptures(50); // default first threshold is 100
            Assert.That(so.CurrentTierIndex, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AddCaptures_AtFirstThreshold_AdvancesTier()
        {
            var so = CreateTrackSO();
            so.AddCaptures(100);
            Assert.That(so.CurrentTierIndex, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AddCaptures_ExceedsMultipleThresholds_AdvancesMultipleTiers()
        {
            var so = CreateTrackSO();
            so.AddCaptures(500); // crosses 100 and 500
            Assert.That(so.CurrentTierIndex, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AddCaptures_FiresPrestigeGranted_OnTierAdvance()
        {
            var so      = CreateTrackSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlPrestigeTrackSO)
                .GetField("_onPrestigeGranted",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.AddCaptures(100); // crosses first threshold

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_GetPrestigeTitle_TierZero_ReturnsFirstTitle()
        {
            var so = CreateTrackSO();
            Assert.That(so.GetPrestigeTitle(), Is.EqualTo("Novice"));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateTrackSO();
            so.AddCaptures(500);
            so.Reset();
            Assert.That(so.TotalCaptures,    Is.EqualTo(0));
            Assert.That(so.CurrentTierIndex, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        // ── Controller tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_TrackSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.TrackSO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(false));
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(true));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(false));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullTrackSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            panel.SetActive(true);
            typeof(ZoneControlPrestigeTrackController)
                .GetField("_panel",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(ctrl, panel);

            ctrl.Refresh();
            Assert.That(panel.activeSelf, Is.False);

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }
    }
}
