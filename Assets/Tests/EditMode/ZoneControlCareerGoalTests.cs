using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for <see cref="ZoneControlCareerGoalSO"/> and
    /// <see cref="ZoneControlCareerGoalController"/>.
    /// </summary>
    public sealed class ZoneControlCareerGoalTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static ZoneControlCareerGoalSO CreateGoalSO() =>
            ScriptableObject.CreateInstance<ZoneControlCareerGoalSO>();

        private static ZoneControlCareerGoalController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCareerGoalController>();
        }

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_IsAchieved_False()
        {
            var so = CreateGoalSO();
            Assert.That(so.IsAchieved, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_AccumulatedValue_Zero()
        {
            var so = CreateGoalSO();
            Assert.That(so.AccumulatedValue, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AddProgress_IncrementsValue()
        {
            var so = CreateGoalSO();
            so.AddProgress(5);
            Assert.That(so.AccumulatedValue, Is.EqualTo(5));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AddProgress_FiresEvent_WhenTargetMet()
        {
            var so      = CreateGoalSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            var field   = typeof(ZoneControlCareerGoalSO).GetField(
                "_onGoalAchieved", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(so, channel);
            var targetField = typeof(ZoneControlCareerGoalSO).GetField(
                "_targetValue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            targetField.SetValue(so, 10);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.AddProgress(10);

            Assert.That(fired, Is.EqualTo(1));
            Assert.That(so.IsAchieved, Is.True);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_AddProgress_Idempotent_WhenAlreadyAchieved()
        {
            var so      = CreateGoalSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            var field   = typeof(ZoneControlCareerGoalSO).GetField(
                "_onGoalAchieved", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(so, channel);
            var targetField = typeof(ZoneControlCareerGoalSO).GetField(
                "_targetValue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            targetField.SetValue(so, 5);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.AddProgress(10);
            so.AddProgress(10);

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_AddProgress_NegativeIgnored()
        {
            var so = CreateGoalSO();
            so.AddProgress(-5);
            Assert.That(so.AccumulatedValue, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_LoadSnapshot_RestoresState()
        {
            var so = CreateGoalSO();
            so.LoadSnapshot(30, true);
            Assert.That(so.AccumulatedValue, Is.EqualTo(30));
            Assert.That(so.IsAchieved, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsState()
        {
            var so = CreateGoalSO();
            so.LoadSnapshot(25, true);
            so.Reset();
            Assert.That(so.AccumulatedValue, Is.EqualTo(0));
            Assert.That(so.IsAchieved, Is.False);
            Object.DestroyImmediate(so);
        }

        // ── Controller tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_GoalSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.GoalSO, Is.Null);
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
        public void Controller_Refresh_NullGoalSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            panel.SetActive(true);
            var panelField = typeof(ZoneControlCareerGoalController).GetField(
                "_panel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            panelField.SetValue(ctrl, panel);

            ctrl.Refresh();
            Assert.That(panel.activeSelf, Is.False);

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }
    }
}
