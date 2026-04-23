using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureLimitTests
    {
        private static ZoneControlCaptureLimitSO CreateSO(
            int componentsNeeded = 6,
            int dissolvePerBot   = 2,
            int bonusPerLimit    = 2845)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureLimitSO>();
            typeof(ZoneControlCaptureLimitSO)
                .GetField("_componentsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, componentsNeeded);
            typeof(ZoneControlCaptureLimitSO)
                .GetField("_dissolvePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, dissolvePerBot);
            typeof(ZoneControlCaptureLimitSO)
                .GetField("_bonusPerLimit", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerLimit);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureLimitController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureLimitController>();
        }

        [Test]
        public void SO_FreshInstance_Components_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Components, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_LimitCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.LimitCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesComponents()
        {
            var so = CreateSO(componentsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Components, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(componentsNeeded: 3, bonusPerLimit: 2845);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,           Is.EqualTo(2845));
            Assert.That(so.LimitCount,   Is.EqualTo(1));
            Assert.That(so.Components,   Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(componentsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesComponents()
        {
            var so = CreateSO(componentsNeeded: 6, dissolvePerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Components, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(componentsNeeded: 6, dissolvePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Components, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ComponentProgress_Clamped()
        {
            var so = CreateSO(componentsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.ComponentProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnLimitComputed_FiresEvent()
        {
            var so    = CreateSO(componentsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureLimitSO)
                .GetField("_onLimitComputed", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(componentsNeeded: 2, bonusPerLimit: 2845);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Components,       Is.EqualTo(0));
            Assert.That(so.LimitCount,       Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleLimits_Accumulate()
        {
            var so = CreateSO(componentsNeeded: 2, bonusPerLimit: 2845);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.LimitCount,        Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(5690));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_LimitSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.LimitSO, Is.Null);
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
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlCaptureLimitController)
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
