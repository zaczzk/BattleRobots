using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T449: <see cref="ZoneControlMatchControllerRatingSO"/> and
    /// <see cref="ZoneControlMatchControllerRatingController"/>.
    ///
    /// ZoneControlMatchControllerRatingTests (12):
    ///   SO_FreshInstance_LastRating_Zero                                 x1
    ///   SO_ComputeRating_PerfectInputs_Returns100                        x1
    ///   SO_ComputeRating_ZeroInputs_ReturnsZero                          x1
    ///   SO_ComputeRating_HalfInputs_Returns50                            x1
    ///   SO_ComputeRating_ZeroTotalWeight_ReturnsZero                     x1
    ///   SO_ComputeRating_NegativeLeadDelta_ClampsToZero                  x1
    ///   SO_ComputeRating_FiresOnRatingComputed                           x1
    ///   SO_GetGradeLabel_A_At90Plus                                      x1
    ///   SO_GetGradeLabel_F_BelowThirtyFive                               x1
    ///   SO_Reset_SetsLastRatingZero                                      x1
    ///   Controller_FreshInstance_RatingSO_Null                           x1
    ///   Controller_Refresh_NullSO_HidesPanel                             x1
    /// </summary>
    public sealed class ZoneControlMatchControllerRatingTests
    {
        private static ZoneControlMatchControllerRatingSO CreateSO(
            float holdWeight   = 1f,
            float effWeight    = 1f,
            float leadWeight   = 1f,
            int   maxLeadDelta = 10)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlMatchControllerRatingSO>();
            typeof(ZoneControlMatchControllerRatingSO)
                .GetField("_holdRatioWeight", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, holdWeight);
            typeof(ZoneControlMatchControllerRatingSO)
                .GetField("_efficiencyWeight", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, effWeight);
            typeof(ZoneControlMatchControllerRatingSO)
                .GetField("_leadDeltaWeight", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, leadWeight);
            typeof(ZoneControlMatchControllerRatingSO)
                .GetField("_maxLeadDelta", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, maxLeadDelta);
            so.Reset();
            return so;
        }

        private static ZoneControlMatchControllerRatingController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlMatchControllerRatingController>();
        }

        [Test]
        public void SO_FreshInstance_LastRating_Zero()
        {
            var so = CreateSO();
            Assert.That(so.LastRating, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ComputeRating_PerfectInputs_Returns100()
        {
            var so     = CreateSO(maxLeadDelta: 5);
            int rating = so.ComputeRating(1f, 1f, 5);
            Assert.That(rating, Is.EqualTo(100));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ComputeRating_ZeroInputs_ReturnsZero()
        {
            var so     = CreateSO();
            int rating = so.ComputeRating(0f, 0f, 0);
            Assert.That(rating, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ComputeRating_HalfInputs_Returns50()
        {
            var so     = CreateSO(maxLeadDelta: 10);
            int rating = so.ComputeRating(0.5f, 0.5f, 5);
            Assert.That(rating, Is.EqualTo(50));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ComputeRating_ZeroTotalWeight_ReturnsZero()
        {
            var so     = CreateSO(holdWeight: 0f, effWeight: 0f, leadWeight: 0f);
            int rating = so.ComputeRating(1f, 1f, 10);
            Assert.That(rating, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ComputeRating_NegativeLeadDelta_ClampsToZero()
        {
            var so = CreateSO(holdWeight: 0f, effWeight: 0f, leadWeight: 1f, maxLeadDelta: 10);
            // lead only; negative lead → 0
            int rating = so.ComputeRating(0f, 0f, -5);
            Assert.That(rating, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ComputeRating_FiresOnRatingComputed()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlMatchControllerRatingSO)
                .GetField("_onRatingComputed", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);
            so.Reset();

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.ComputeRating(1f, 1f, 5);

            Assert.That(fired, Is.EqualTo(1));

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_GetGradeLabel_A_At90Plus()
        {
            Assert.That(ZoneControlMatchControllerRatingSO.GetGradeLabel(90),  Is.EqualTo("A"));
            Assert.That(ZoneControlMatchControllerRatingSO.GetGradeLabel(100), Is.EqualTo("A"));
        }

        [Test]
        public void SO_GetGradeLabel_F_BelowThirtyFive()
        {
            Assert.That(ZoneControlMatchControllerRatingSO.GetGradeLabel(34), Is.EqualTo("F"));
            Assert.That(ZoneControlMatchControllerRatingSO.GetGradeLabel(0),  Is.EqualTo("F"));
        }

        [Test]
        public void SO_Reset_SetsLastRatingZero()
        {
            var so = CreateSO(maxLeadDelta: 10);
            so.ComputeRating(1f, 1f, 10);
            so.Reset();
            Assert.That(so.LastRating, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_RatingSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.RatingSO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlMatchControllerRatingController)
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
