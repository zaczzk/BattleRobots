using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlMatchQualityTests
    {
        private static ZoneControlMatchQualitySO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlMatchQualitySO>();

        private static ZoneControlMatchQualityController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlMatchQualityController>();
        }

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_LastQuality_Zero()
        {
            var so = CreateSO();
            Assert.That(so.LastQuality, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ComputeQuality_ZeroWeights_ReturnsZero()
        {
            var so = CreateSO();
            typeof(ZoneControlMatchQualitySO)
                .GetField("_zoneWeight", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, 0f);
            typeof(ZoneControlMatchQualitySO)
                .GetField("_paceWeight", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, 0f);
            typeof(ZoneControlMatchQualitySO)
                .GetField("_ratingWeight", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, 0f);

            int result = so.ComputeQuality(10, 3f, 4);
            Assert.That(result, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ComputeQuality_MaxValues_Returns100()
        {
            var so = CreateSO();
            int result = so.ComputeQuality(so.ZoneScaleDivisor, so.PaceScaleDivisor, so.MaxRating);
            Assert.That(result, Is.EqualTo(100));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ComputeQuality_HalfValues_Returns50()
        {
            var so = CreateSO();
            int result = so.ComputeQuality(
                so.ZoneScaleDivisor / 2,
                so.PaceScaleDivisor / 2f,
                so.MaxRating / 2);
            Assert.That(result, Is.InRange(48, 52));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ComputeQuality_ClampsNegativeInputs()
        {
            var so = CreateSO();
            int result = so.ComputeQuality(-5, -1f, -2);
            Assert.That(result, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ComputeQuality_FiresEvent()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlMatchQualitySO)
                .GetField("_onQualityComputed", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.ComputeQuality(5, 1f, 3);

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_ComputeQuality_CachesLastQuality()
        {
            var so = CreateSO();
            so.ComputeQuality(so.ZoneScaleDivisor, so.PaceScaleDivisor, so.MaxRating);
            Assert.That(so.LastQuality, Is.EqualTo(100));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsLastQuality()
        {
            var so = CreateSO();
            so.ComputeQuality(so.ZoneScaleDivisor, so.PaceScaleDivisor, so.MaxRating);
            so.Reset();
            Assert.That(so.LastQuality, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        // ── Controller tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_QualitySO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.QualitySO, Is.Null);
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
        public void Controller_OnDisable_Unregisters_Channel()
        {
            var ctrl    = CreateController();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlMatchQualityController)
                .GetField("_onQualityComputed", BindingFlags.NonPublic | BindingFlags.Instance)
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
        public void Controller_Refresh_NullQualitySO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            panel.SetActive(true);
            typeof(ZoneControlMatchQualityController)
                .GetField("_panel", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, panel);

            ctrl.Refresh();
            Assert.That(panel.activeSelf, Is.False);

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }
    }
}
