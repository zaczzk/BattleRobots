using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureShadowTests
    {
        private static ZoneControlCaptureShadowSO CreateSO(int bonusPerClear = 150)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureShadowSO>();
            typeof(ZoneControlCaptureShadowSO)
                .GetField("_bonusPerClear", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerClear);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureShadowController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureShadowController>();
        }

        [Test]
        public void SO_FreshInstance_ShadowDebt_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ShadowDebt, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_BotCapture_IncreasesShadowDebt()
        {
            var so = CreateSO();
            so.RecordBotCapture();
            so.RecordBotCapture();
            Assert.That(so.ShadowDebt, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_PlayerCapture_NoDebt_NoEffect()
        {
            var so = CreateSO();
            so.RecordPlayerCapture();
            Assert.That(so.ClearedCount, Is.EqualTo(0));
            Assert.That(so.ShadowDebt,   Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_PlayerCaptures_PayOffDebt_FiresOnClear()
        {
            var so    = CreateSO(bonusPerClear: 150);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureShadowSO)
                .GetField("_onShadowCleared", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordBotCapture();
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(fired, Is.EqualTo(1));
            Assert.That(so.ClearedCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_Clear_AccumulatesBonus()
        {
            var so = CreateSO(bonusPerClear: 100);
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(100));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleClearCycles_AccumulatesCorrectly()
        {
            var so = CreateSO(bonusPerClear: 50);
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            Assert.That(so.ClearedCount,      Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(100));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            so.RecordBotCapture();
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.ShadowDebt,        Is.EqualTo(0));
            Assert.That(so.ClearedCount,      Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ShadowSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ShadowSO, Is.Null);
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
            typeof(ZoneControlCaptureShadowController)
                .GetField("_panel", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, panel);
            panel.SetActive(true);
            ctrl.Refresh();
            Assert.That(panel.activeSelf, Is.False);
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Controller_Refresh_WithSO_ShowsPanel()
        {
            var ctrl  = CreateController();
            var so    = CreateSO();
            var panel = new GameObject();
            typeof(ZoneControlCaptureShadowController)
                .GetField("_shadowSO", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, so);
            typeof(ZoneControlCaptureShadowController)
                .GetField("_panel", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, panel);
            panel.SetActive(false);
            ctrl.Refresh();
            Assert.That(panel.activeSelf, Is.True);
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(so);
        }
    }
}
