using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureRicochetTests
    {
        private static ZoneControlCaptureRicochetSO CreateSO(int ricochetBonus = 200)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureRicochetSO>();
            typeof(ZoneControlCaptureRicochetSO)
                .GetField("_ricochetBonus", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, ricochetBonus);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureRicochetController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureRicochetController>();
        }

        [Test]
        public void SO_FreshInstance_IsArmed_False()
        {
            var so = CreateSO();
            Assert.That(so.IsArmed, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_BotCapture_ArmsRicochet()
        {
            var so = CreateSO();
            so.RecordBotCapture();
            Assert.That(so.IsArmed, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_PlayerCapture_WhenArmed_FiresRicochet()
        {
            var so    = CreateSO();
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureRicochetSO)
                .GetField("_onRicochet", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            Assert.That(fired, Is.EqualTo(1));
            Assert.That(so.RicochetCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_PlayerCapture_WhenNotArmed_NoRicochet()
        {
            var so = CreateSO();
            so.RecordPlayerCapture();
            Assert.That(so.RicochetCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Ricochet_AccumulatesBonus()
        {
            var so = CreateSO(ricochetBonus: 100);
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(100));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AfterRicochet_IsArmed_False()
        {
            var so = CreateSO();
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            Assert.That(so.IsArmed, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleRicochetCycles_Correct()
        {
            var so = CreateSO(ricochetBonus: 50);
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            Assert.That(so.RicochetCount,     Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(100));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.IsArmed,           Is.False);
            Assert.That(so.RicochetCount,     Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_RicochetSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.RicochetSO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
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
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlCaptureRicochetController)
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
