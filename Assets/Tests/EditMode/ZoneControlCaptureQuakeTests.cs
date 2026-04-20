using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureQuakeTests
    {
        private static ZoneControlCaptureQuakeSO CreateSO(
            int capturesPerQuake = 5,
            int bonusPerQuake    = 300,
            int coolingPerBot    = 1)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureQuakeSO>();
            typeof(ZoneControlCaptureQuakeSO)
                .GetField("_capturesPerQuake", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, capturesPerQuake);
            typeof(ZoneControlCaptureQuakeSO)
                .GetField("_bonusPerQuake", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerQuake);
            typeof(ZoneControlCaptureQuakeSO)
                .GetField("_coolingPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, coolingPerBot);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureQuakeController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureQuakeController>();
        }

        [Test]
        public void SO_FreshInstance_TremorCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.TremorCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_QuakeCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.QuakeCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_BelowThreshold_ReturnsZero()
        {
            var so    = CreateSO(capturesPerQuake: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_BelowThreshold_BuildsTremor()
        {
            var so = CreateSO(capturesPerQuake: 5);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.TremorCount, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReachesThreshold_TriggersQuake()
        {
            var so = CreateSO(capturesPerQuake: 3, bonusPerQuake: 300);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(so.QuakeCount,  Is.EqualTo(1));
            Assert.That(bonus,          Is.EqualTo(300));
            Assert.That(so.TremorCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_Quake_FiresEvent()
        {
            var so    = CreateSO(capturesPerQuake: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureQuakeSO)
                .GetField("_onQuake", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_RecordPlayerCapture_MultipleQuakes_CountAccumulates()
        {
            var so = CreateSO(capturesPerQuake: 2);
            for (int i = 0; i < 6; i++) so.RecordPlayerCapture();
            Assert.That(so.QuakeCount, Is.EqualTo(3));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_CoolsTremor()
        {
            var so = CreateSO(capturesPerQuake: 5, coolingPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.TremorCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsAtZero()
        {
            var so = CreateSO();
            so.RecordBotCapture();
            Assert.That(so.TremorCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_TremorProgress_ReflectsRatio()
        {
            var so = CreateSO(capturesPerQuake: 4);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.TremorProgress, Is.EqualTo(0.5f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(capturesPerQuake: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.TremorCount,       Is.EqualTo(0));
            Assert.That(so.QuakeCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_QuakeSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.QuakeSO, Is.Null);
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
            typeof(ZoneControlCaptureQuakeController)
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
