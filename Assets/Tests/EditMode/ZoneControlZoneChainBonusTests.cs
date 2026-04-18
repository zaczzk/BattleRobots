using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T439: <see cref="ZoneControlZoneChainBonusSO"/> and
    /// <see cref="ZoneControlZoneChainBonusController"/>.
    ///
    /// ZoneControlZoneChainBonusTests (12):
    ///   SO_FreshInstance_ChainLength_Zero                           x1
    ///   SO_FreshInstance_ChainCount_Zero                            x1
    ///   SO_RecordPlayerCapture_AdvancesChainLength                  x1
    ///   SO_RecordPlayerCapture_AtTarget_CompletesChain              x1
    ///   SO_RecordPlayerCapture_AtTarget_ResetsChainLength           x1
    ///   SO_RecordPlayerCapture_FiresOnChainCompleted                x1
    ///   SO_RecordBotCapture_ResetsChainLength                       x1
    ///   SO_RecordBotCapture_FiresOnChainBroken_WhenChainActive      x1
    ///   SO_RecordBotCapture_NoEvent_WhenChainEmpty                  x1
    ///   SO_Reset_ClearsAll                                          x1
    ///   Controller_FreshInstance_ChainBonusSO_Null                 x1
    ///   Controller_Refresh_NullSO_HidesPanel                       x1
    /// </summary>
    public sealed class ZoneControlZoneChainBonusTests
    {
        private static ZoneControlZoneChainBonusSO CreateSO(int target = 3, int bonus = 150)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlZoneChainBonusSO>();
            typeof(ZoneControlZoneChainBonusSO)
                .GetField("_chainTarget", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, target);
            typeof(ZoneControlZoneChainBonusSO)
                .GetField("_bonusPerChain", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonus);
            so.Reset();
            return so;
        }

        private static ZoneControlZoneChainBonusController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlZoneChainBonusController>();
        }

        [Test]
        public void SO_FreshInstance_ChainLength_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ChainLength, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ChainCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ChainCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AdvancesChainLength()
        {
            var so = CreateSO(target: 3);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.ChainLength, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtTarget_CompletesChain()
        {
            var so = CreateSO(target: 3);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.ChainCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtTarget_ResetsChainLength()
        {
            var so = CreateSO(target: 3);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.ChainLength, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_FiresOnChainCompleted()
        {
            var so      = CreateSO(target: 2);
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlZoneChainBonusSO)
                .GetField("_onChainCompleted", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);
            so.Reset();

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            so.RecordPlayerCapture();
            Assert.That(fired, Is.EqualTo(0));
            so.RecordPlayerCapture();
            Assert.That(fired, Is.EqualTo(1));

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_RecordBotCapture_ResetsChainLength()
        {
            var so = CreateSO(target: 5);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.ChainLength, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_FiresOnChainBroken_WhenChainActive()
        {
            var so      = CreateSO(target: 5);
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlZoneChainBonusSO)
                .GetField("_onChainBroken", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);
            so.Reset();

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(fired, Is.EqualTo(1));

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_RecordBotCapture_NoEvent_WhenChainEmpty()
        {
            var so      = CreateSO(target: 5);
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlZoneChainBonusSO)
                .GetField("_onChainBroken", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);
            so.Reset();

            int fired = 0;
            channel.RegisterCallback(() => fired++);

            so.RecordBotCapture(); // chain is empty — no event
            Assert.That(fired, Is.EqualTo(0));

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(target: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture(); // completes chain
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.ChainLength,       Is.EqualTo(0));
            Assert.That(so.ChainCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Assert.That(so.ChainProgress,     Is.EqualTo(0f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ChainBonusSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ChainBonusSO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(false));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlZoneChainBonusController)
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
