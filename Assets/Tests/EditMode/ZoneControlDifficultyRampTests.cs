using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlDifficultyRampTests
    {
        private static ZoneControlDifficultyRampSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlDifficultyRampSO>();

        private static ZoneControlDifficultyRampController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlDifficultyRampController>();
        }

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_IsActive_False()
        {
            var so = CreateSO();
            Assert.That(so.IsActive, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_CurrentDifficulty_EqualsStartDifficulty()
        {
            var so = CreateSO();
            Assert.That(so.CurrentDifficulty, Is.EqualTo(so.StartDifficulty));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StartRamp_SetsIsActive_True()
        {
            var so = CreateSO();
            so.StartRamp();
            Assert.That(so.IsActive, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_NotActive_NoOp()
        {
            var so = CreateSO();
            so.Tick(10f);
            Assert.That(so.ElapsedTime, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_Active_IncreasesElapsedTime()
        {
            var so = CreateSO();
            so.StartRamp();
            so.Tick(5f);
            Assert.That(so.ElapsedTime, Is.EqualTo(5f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_FullDuration_ReachesEndDifficulty()
        {
            var so = CreateSO();
            so.StartRamp();
            so.Tick(so.RampDuration + 10f);
            Assert.That(so.CurrentDifficulty, Is.EqualTo(so.EndDifficulty).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StartRamp_FiresEvent()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlDifficultyRampSO)
                .GetField("_onDifficultyChanged", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.StartRamp();

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Reset_ClearsState()
        {
            var so = CreateSO();
            so.StartRamp();
            so.Tick(30f);
            so.Reset();
            Assert.That(so.IsActive, Is.False);
            Assert.That(so.ElapsedTime, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        // ── Controller tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_RampSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.RampSO, Is.Null);
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
            typeof(ZoneControlDifficultyRampController)
                .GetField("_onDifficultyChanged", BindingFlags.NonPublic | BindingFlags.Instance)
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
    }
}
