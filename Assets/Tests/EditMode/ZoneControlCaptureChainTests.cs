using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T381: <see cref="ZoneControlCaptureChainSO"/> and
    /// <see cref="ZoneControlCaptureChainController"/>.
    ///
    /// ZoneControlCaptureChainTests (13):
    ///   SO_FreshInstance_CurrentChainLength_Zero                 ×1
    ///   SO_FreshInstance_CompletedChains_Zero                    ×1
    ///   SO_AddCapture_UniqueZones_BuildsChain                    ×1
    ///   SO_AddCapture_SameZone_ResetsChain                       ×1
    ///   SO_AddCapture_AtTarget_CompletesChain                    ×1
    ///   SO_AddCapture_AtTarget_FiresOnChainCompleted             ×1
    ///   SO_AddCapture_AfterComplete_StartsNewChain               ×1
    ///   SO_Reset_ClearsAll                                       ×1
    ///   Controller_FreshInstance_ChainSO_Null                    ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow               ×1
    ///   Controller_OnDisable_Unregisters_Channel                 ×1
    ///   Controller_Refresh_NullChainSO_HidesPanel                ×1
    /// </summary>
    public sealed class ZoneControlCaptureChainTests
    {
        private static ZoneControlCaptureChainSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlCaptureChainSO>();

        private static ZoneControlCaptureChainController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureChainController>();
        }

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_CurrentChainLength_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CurrentChainLength, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_CompletedChains_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CompletedChains, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AddCapture_UniqueZones_BuildsChain()
        {
            var so = CreateSO();
            so.AddCapture(0);
            so.AddCapture(1);
            Assert.That(so.CurrentChainLength, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AddCapture_SameZone_ResetsChain()
        {
            var so = CreateSO();
            so.AddCapture(0);
            so.AddCapture(1);
            so.AddCapture(1); // same as last — reset
            Assert.That(so.CurrentChainLength, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AddCapture_AtTarget_CompletesChain()
        {
            var so = CreateSO();
            for (int i = 0; i < so.ChainTarget; i++)
                so.AddCapture(i);

            Assert.That(so.CompletedChains,    Is.EqualTo(1));
            Assert.That(so.CurrentChainLength, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AddCapture_AtTarget_FiresOnChainCompleted()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureChainSO)
                .GetField("_onChainCompleted", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            for (int i = 0; i < so.ChainTarget; i++)
                so.AddCapture(i);

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_AddCapture_AfterComplete_StartsNewChain()
        {
            var so = CreateSO();
            for (int i = 0; i < so.ChainTarget; i++)
                so.AddCapture(i);

            // After completion chain resets; capture a new unique zone
            so.AddCapture(99);
            Assert.That(so.CurrentChainLength, Is.EqualTo(1));
            Assert.That(so.CompletedChains,    Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            for (int i = 0; i < so.ChainTarget; i++)
                so.AddCapture(i);
            so.AddCapture(99);

            so.Reset();

            Assert.That(so.CurrentChainLength, Is.EqualTo(0));
            Assert.That(so.CompletedChains,    Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        // ── Controller tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_ChainSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ChainSO, Is.Null);
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
            typeof(ZoneControlCaptureChainController)
                .GetField("_onChainCompleted", BindingFlags.NonPublic | BindingFlags.Instance)
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
        public void Controller_Refresh_NullChainSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlCaptureChainController)
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
