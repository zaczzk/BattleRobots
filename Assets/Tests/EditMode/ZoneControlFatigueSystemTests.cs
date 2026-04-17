using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlFatigueSystemTests
    {
        private static ZoneControlFatigueSystemSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlFatigueSystemSO>();

        private static ZoneControlFatigueSystemController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlFatigueSystemController>();
        }

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_IsFatigued_False()
        {
            var so = CreateSO();
            Assert.That(so.IsFatigued, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ConsecutiveCaptures_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ConsecutiveCaptures, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IncreasesConsecutiveCaptures()
        {
            var so = CreateSO();
            so.RecordBotCapture();
            Assert.That(so.ConsecutiveCaptures, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_AtThreshold_SetsFatigued()
        {
            var so = CreateSO();
            int threshold = so.FatigueThreshold;
            for (int i = 0; i < threshold; i++) so.RecordBotCapture();
            Assert.That(so.IsFatigued, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_AtThreshold_FiresEvent()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlFatigueSystemSO)
                .GetField("_onFatigueTriggered",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            int threshold = so.FatigueThreshold;
            for (int i = 0; i < threshold; i++) so.RecordBotCapture();

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_RecordBotCapture_AlreadyFatigued_DoesNotFireEventAgain()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlFatigueSystemSO)
                .GetField("_onFatigueTriggered",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            int threshold = so.FatigueThreshold;
            for (int i = 0; i < threshold + 2; i++) so.RecordBotCapture();

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_RecoverFromFatigue_ClearsState()
        {
            var so = CreateSO();
            int threshold = so.FatigueThreshold;
            for (int i = 0; i < threshold; i++) so.RecordBotCapture();
            so.RecoverFromFatigue();
            Assert.That(so.IsFatigued, Is.False);
            Assert.That(so.ConsecutiveCaptures, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecoverFromFatigue_FiresEvent_WhenWasFatigued()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlFatigueSystemSO)
                .GetField("_onFatigueRecovered",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            int threshold = so.FatigueThreshold;
            for (int i = 0; i < threshold; i++) so.RecordBotCapture();
            so.RecoverFromFatigue();

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_RecoverFromFatigue_NotFatigued_DoesNotFireEvent()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlFatigueSystemSO)
                .GetField("_onFatigueRecovered",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.RecoverFromFatigue();

            Assert.That(fired, Is.EqualTo(0));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            int threshold = so.FatigueThreshold;
            for (int i = 0; i < threshold; i++) so.RecordBotCapture();
            so.Reset();
            Assert.That(so.IsFatigued, Is.False);
            Assert.That(so.ConsecutiveCaptures, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        // ── Controller tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_FatigueSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.FatigueSO, Is.Null);
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
        public void Controller_Refresh_NullFatigueSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            panel.SetActive(true);
            typeof(ZoneControlFatigueSystemController)
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
