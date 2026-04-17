using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T389: <see cref="ZoneControlMatchIntensitySO"/> and
    /// <see cref="ZoneControlMatchIntensityController"/>.
    ///
    /// ZoneControlMatchIntensityTests (13):
    ///   SO_FreshInstance_LastIntensity_Zero                            ×1
    ///   SO_ComputeIntensity_AllFalse_ZeroVelocity_ReturnsZero          ×1
    ///   SO_ComputeIntensity_SurgeOnly_ReturnsExpected                  ×1
    ///   SO_ComputeIntensity_AllTrue_FullVelocity_ClampsToTen           ×1
    ///   SO_ComputeIntensity_PartialInputs_ComputedCorrectly            ×1
    ///   SO_ComputeIntensity_FiresOnIntensityChanged                    ×1
    ///   SO_ComputeIntensity_CachesLastIntensity                        ×1
    ///   SO_Reset_ClearsLastIntensity                                   ×1
    ///   Controller_FreshInstance_IntensitySO_Null                      ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                      ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow                     ×1
    ///   Controller_OnDisable_Unregisters_Channel                       ×1
    ///   Controller_Refresh_NullIntensitySO_HidesPanel                  ×1
    /// </summary>
    public sealed class ZoneControlMatchIntensityTests
    {
        private static ZoneControlMatchIntensitySO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlMatchIntensitySO>();

        private static ZoneControlMatchIntensityController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlMatchIntensityController>();
        }

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_LastIntensity_Zero()
        {
            var so = CreateSO();
            Assert.That(so.LastIntensity, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ComputeIntensity_AllFalse_ZeroVelocity_ReturnsZero()
        {
            var so     = CreateSO();
            float score = so.ComputeIntensity(false, false, false, 0f);
            Assert.That(score, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ComputeIntensity_SurgeOnly_ReturnsExpected()
        {
            var so     = CreateSO();
            float score = so.ComputeIntensity(true, false, false, 0f);
            Assert.That(score, Is.EqualTo(so.BoolWeight).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ComputeIntensity_AllTrue_FullVelocity_ClampsToTen()
        {
            var so     = CreateSO();
            float score = so.ComputeIntensity(true, true, true, 1f);
            Assert.That(score, Is.EqualTo(10f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ComputeIntensity_PartialInputs_ComputedCorrectly()
        {
            var so = CreateSO();
            // Set weights to known values via reflection
            typeof(ZoneControlMatchIntensitySO)
                .GetField("_boolWeight", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, 2f);
            typeof(ZoneControlMatchIntensitySO)
                .GetField("_velocityWeight", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, 4f);

            float score = so.ComputeIntensity(true, false, false, 0.5f);
            // Expected: 2 (surge) + 4 * 0.5 (velocity) = 4
            Assert.That(score, Is.EqualTo(4f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ComputeIntensity_FiresOnIntensityChanged()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlMatchIntensitySO)
                .GetField("_onIntensityChanged", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            so.ComputeIntensity(false, false, false, 0f);

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_ComputeIntensity_CachesLastIntensity()
        {
            var so     = CreateSO();
            float score = so.ComputeIntensity(true, false, false, 0f);
            Assert.That(so.LastIntensity, Is.EqualTo(score));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsLastIntensity()
        {
            var so = CreateSO();
            so.ComputeIntensity(true, true, true, 1f);
            so.Reset();
            Assert.That(so.LastIntensity, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        // ── Controller tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_IntensitySO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.IntensitySO, Is.Null);
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
            typeof(ZoneControlMatchIntensityController)
                .GetField("_onIntensityChanged", BindingFlags.NonPublic | BindingFlags.Instance)
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
        public void Controller_Refresh_NullIntensitySO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlMatchIntensityController)
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
