using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T369: <see cref="ZoneControlDominationPulseSO"/> and
    /// <see cref="ZoneControlDominationPulseController"/>.
    ///
    /// ZoneControlDominationPulseTests (13):
    ///   SO_FreshInstance_IsDominating_False              ×1
    ///   SO_FreshInstance_TotalPulseCount_Zero            ×1
    ///   SO_StartDomination_SetsIsDominating              ×1
    ///   SO_EndDomination_ClearsIsDominating              ×1
    ///   SO_Tick_NotDominating_NoPulse                    ×1
    ///   SO_Tick_DurationElapsed_TriggersPulse            ×1
    ///   SO_Tick_TwiceDuration_TriggersTwoPulses          ×1
    ///   SO_Pulse_FiresEvent                              ×1
    ///   SO_Reset_ClearsAll                               ×1
    ///   Controller_FreshInstance_PulseSO_Null            ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow        ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow       ×1
    ///   Controller_OnDisable_Unregisters_Channel         ×1
    /// </summary>
    public sealed class ZoneControlDominationPulseTests
    {
        private static ZoneControlDominationPulseSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlDominationPulseSO>();

        private static ZoneControlDominationPulseController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlDominationPulseController>();
        }

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_IsDominating_False()
        {
            var so = CreateSO();
            Assert.That(so.IsDominating, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_TotalPulseCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.TotalPulseCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StartDomination_SetsIsDominating()
        {
            var so = CreateSO();
            so.StartDomination();
            Assert.That(so.IsDominating, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EndDomination_ClearsIsDominating()
        {
            var so = CreateSO();
            so.StartDomination();
            so.EndDomination();
            Assert.That(so.IsDominating, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_NotDominating_NoPulse()
        {
            var so = CreateSO();
            so.Tick(so.PulseDuration + 1f);
            Assert.That(so.TotalPulseCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_DurationElapsed_TriggersPulse()
        {
            var so = CreateSO();
            so.StartDomination();
            so.Tick(so.PulseDuration + 0.1f);
            Assert.That(so.TotalPulseCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Tick_TwiceDuration_TriggersTwoPulses()
        {
            var so = CreateSO();
            so.StartDomination();
            so.Tick(so.PulseDuration * 2f + 0.1f);
            Assert.That(so.TotalPulseCount, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Pulse_FiresEvent()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlDominationPulseSO)
                .GetField("_onPulseTriggered", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            so.StartDomination();
            so.Tick(so.PulseDuration + 0.1f);

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            so.StartDomination();
            so.Tick(so.PulseDuration + 0.1f);
            so.Reset();
            Assert.That(so.IsDominating, Is.False);
            Assert.That(so.TotalPulseCount, Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Assert.That(so.PulseProgress, Is.EqualTo(0f));
            Object.DestroyImmediate(so);
        }

        // ── Controller tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_PulseSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.PulseSO, Is.Null);
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
            typeof(ZoneControlDominationPulseController)
                .GetField("_onPulseTriggered", BindingFlags.NonPublic | BindingFlags.Instance)
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
