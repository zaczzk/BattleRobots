using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T393: <see cref="ZoneControlRewardMultiplierStackSO"/> and
    /// <see cref="ZoneControlRewardMultiplierStackController"/>.
    ///
    /// ZoneControlRewardMultiplierStackTests (13):
    ///   SO_FreshInstance_CurrentStacks_Zero                     ×1
    ///   SO_FreshInstance_CurrentMultiplier_One                  ×1
    ///   SO_AddStack_IncrementsStacks                            ×1
    ///   SO_AddStack_ClampsToMaxStacks                           ×1
    ///   SO_AddStack_FiresStacksChanged                          ×1
    ///   SO_Tick_DecaysStack_AfterInterval                       ×1
    ///   SO_Tick_NoDecay_BeforeInterval                          ×1
    ///   SO_Tick_ZeroStacks_DoesNotDecay                         ×1
    ///   SO_ComputeBonus_ScalesByMultiplier                      ×1
    ///   SO_Reset_ClearsAll                                      ×1
    ///   Controller_FreshInstance_StackSO_Null                  ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow               ×1
    ///   Controller_Refresh_NullStackSO_HidesPanel               ×1
    /// </summary>
    public sealed class ZoneControlRewardMultiplierStackTests
    {
        private static ZoneControlRewardMultiplierStackSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlRewardMultiplierStackSO>();

        private static ZoneControlRewardMultiplierStackController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlRewardMultiplierStackController>();
        }

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_CurrentStacks_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CurrentStacks, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_CurrentMultiplier_One()
        {
            var so = CreateSO();
            Assert.That(so.CurrentMultiplier, Is.EqualTo(1f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AddStack_IncrementsStacks()
        {
            var so = CreateSO();
            so.AddStack();
            so.AddStack();
            Assert.That(so.CurrentStacks, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AddStack_ClampsToMaxStacks()
        {
            var so = CreateSO();
            for (int i = 0; i < so.MaxStacks + 5; i++) so.AddStack();
            Assert.That(so.CurrentStacks, Is.EqualTo(so.MaxStacks));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AddStack_FiresStacksChanged()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlRewardMultiplierStackSO)
                .GetField("_onStacksChanged", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.AddStack();
            so.AddStack();

            Assert.That(fired, Is.EqualTo(2));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Tick_DecaysStack_AfterInterval()
        {
            var so = CreateSO();
            so.AddStack();
            so.Tick(so.StackDecayInterval);
            Assert.That(so.CurrentStacks, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_NoDecay_BeforeInterval()
        {
            var so = CreateSO();
            so.AddStack();
            so.Tick(so.StackDecayInterval * 0.5f);
            Assert.That(so.CurrentStacks, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_ZeroStacks_DoesNotDecay()
        {
            var so = CreateSO();
            Assert.DoesNotThrow(() => so.Tick(so.StackDecayInterval * 10f));
            Assert.That(so.CurrentStacks, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ComputeBonus_ScalesByMultiplier()
        {
            var so = CreateSO();
            so.AddStack();
            float expected = 100f * so.CurrentMultiplier;
            Assert.That(so.ComputeBonus(100), Is.EqualTo(Mathf.RoundToInt(expected)));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            so.AddStack();
            so.AddStack();
            so.Reset();
            Assert.That(so.CurrentStacks, Is.EqualTo(0));
            Assert.That(so.CurrentMultiplier, Is.EqualTo(1f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        // ── Controller tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_StackSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.StackSO, Is.Null);
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
        public void Controller_Refresh_NullStackSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlRewardMultiplierStackController)
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
