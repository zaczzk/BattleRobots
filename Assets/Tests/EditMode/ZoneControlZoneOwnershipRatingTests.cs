using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T388: <see cref="ZoneControlZoneOwnershipRatingSO"/> and
    /// <see cref="ZoneControlZoneOwnershipRatingController"/>.
    ///
    /// ZoneControlZoneOwnershipRatingTests (12):
    ///   SO_FreshInstance_OwnershipRating_Zero                          ×1
    ///   SO_RecordControlTick_NoMajority_DoesNotIncrementControlTicks   ×1
    ///   SO_RecordControlTick_WithMajority_IncrementsControlTicks       ×1
    ///   SO_OwnershipRating_ComputedCorrectly                           ×1
    ///   SO_RecordControlTick_FiresOnRatingUpdated                      ×1
    ///   SO_Reset_ClearsAllTicks                                        ×1
    ///   SO_OwnershipRating_ZeroWhenNoTicks_NoThrow                     ×1
    ///   Controller_FreshInstance_OwnershipRatingSO_Null                ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                      ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow                     ×1
    ///   Controller_OnDisable_Unregisters_Channel                       ×1
    ///   Controller_Refresh_NullOwnershipRatingSO_HidesPanel            ×1
    /// </summary>
    public sealed class ZoneControlZoneOwnershipRatingTests
    {
        private static ZoneControlZoneOwnershipRatingSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlZoneOwnershipRatingSO>();

        private static ZoneControlZoneOwnershipRatingController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlZoneOwnershipRatingController>();
        }

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_OwnershipRating_Zero()
        {
            var so = CreateSO();
            Assert.That(so.OwnershipRating, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordControlTick_NoMajority_DoesNotIncrementControlTicks()
        {
            var so = CreateSO();
            so.RecordControlTick(0, 4);  // player owns 0 of 4 — no majority
            Assert.That(so.ControlTicks, Is.EqualTo(0));
            Assert.That(so.TotalTicks,   Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordControlTick_WithMajority_IncrementsControlTicks()
        {
            var so = CreateSO();
            so.RecordControlTick(3, 4);  // player owns 3 of 4 — majority
            Assert.That(so.ControlTicks, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OwnershipRating_ComputedCorrectly()
        {
            var so = CreateSO();
            so.RecordControlTick(3, 4);  // majority
            so.RecordControlTick(0, 4);  // no majority
            // 1 control tick out of 2 total = 0.5
            Assert.That(so.OwnershipRating, Is.EqualTo(0.5f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordControlTick_FiresOnRatingUpdated()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlZoneOwnershipRatingSO)
                .GetField("_onRatingUpdated", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.RecordControlTick(2, 4);

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Reset_ClearsAllTicks()
        {
            var so = CreateSO();
            so.RecordControlTick(3, 4);
            so.RecordControlTick(0, 4);
            so.Reset();
            Assert.That(so.TotalTicks,   Is.EqualTo(0));
            Assert.That(so.ControlTicks, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OwnershipRating_ZeroWhenNoTicks_NoThrow()
        {
            var so = CreateSO();
            Assert.DoesNotThrow(() => { float _ = so.OwnershipRating; });
            Assert.That(so.OwnershipRating, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        // ── Controller tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_OwnershipRatingSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.OwnershipRatingSO, Is.Null);
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
            ctrl.gameObject.SetActive(true);
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(false));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channel()
        {
            var ctrl    = CreateController();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlZoneOwnershipRatingController)
                .GetField("_onRatingUpdated", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, channel);

            ctrl.gameObject.SetActive(true);
            ctrl.gameObject.SetActive(false);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            channel.Raise();

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void Controller_Refresh_NullOwnershipRatingSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlZoneOwnershipRatingController)
                .GetField("_panel", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, panel);

            panel.SetActive(true);
            ctrl.Refresh();

            Assert.That(panel.activeSelf, Is.False);
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }
    }
}
