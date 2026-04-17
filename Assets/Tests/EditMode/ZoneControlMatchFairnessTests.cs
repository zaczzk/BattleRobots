using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlMatchFairnessTests
    {
        private static ZoneControlMatchFairnessSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlMatchFairnessSO>();

        private static ZoneControlMatchFairnessController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlMatchFairnessController>();
        }

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_IsCatchUpActive_False()
        {
            var so = CreateSO();
            Assert.That(so.IsCatchUpActive, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_GetCatchUpBonus_Zero()
        {
            var so = CreateSO();
            Assert.That(so.GetCatchUpBonus(), Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EvaluateFairness_BotLeadsBeyondThreshold_ActivatesCatchUp()
        {
            var so        = CreateSO();
            int threshold = so.GapThreshold;
            so.EvaluateFairness(0, threshold);
            Assert.That(so.IsCatchUpActive, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EvaluateFairness_BotLeadsBeyondThreshold_FiresActivatedEvent()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlMatchFairnessSO)
                .GetField("_onCatchUpActivated",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.EvaluateFairness(0, so.GapThreshold);

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_EvaluateFairness_GapClosedWhileActive_DeactivatesCatchUp()
        {
            var so = CreateSO();
            so.EvaluateFairness(0, so.GapThreshold);
            so.EvaluateFairness(5, 5);
            Assert.That(so.IsCatchUpActive, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EvaluateFairness_GapClosedWhileActive_FiresDeactivatedEvent()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlMatchFairnessSO)
                .GetField("_onCatchUpDeactivated",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.EvaluateFairness(0, so.GapThreshold);
            so.EvaluateFairness(5, 5);

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_GetCatchUpBonus_WhenActive_ReturnsBonus()
        {
            var so = CreateSO();
            so.EvaluateFairness(0, so.GapThreshold);
            Assert.That(so.GetCatchUpBonus(), Is.EqualTo(so.CatchUpBonusPerZone));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EvaluateFairness_Idempotent_WhenAlreadyActive()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlMatchFairnessSO)
                .GetField("_onCatchUpActivated",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.EvaluateFairness(0, so.GapThreshold);
            so.EvaluateFairness(0, so.GapThreshold + 1);

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Reset_ClearsCatchUpActive()
        {
            var so = CreateSO();
            so.EvaluateFairness(0, so.GapThreshold);
            so.Reset();
            Assert.That(so.IsCatchUpActive, Is.False);
            Object.DestroyImmediate(so);
        }

        // ── Controller tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_FairnessSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.FairnessSO, Is.Null);
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
        public void Controller_Refresh_NullFairnessSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            panel.SetActive(true);
            typeof(ZoneControlMatchFairnessController)
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
