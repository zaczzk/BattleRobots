using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureLoomTests
    {
        private static ZoneControlCaptureLoomSO CreateSO(
            int threadsNeeded = 6,
            int tanglePerBot  = 2,
            int bonusPerWeave = 940)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureLoomSO>();
            typeof(ZoneControlCaptureLoomSO)
                .GetField("_threadsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, threadsNeeded);
            typeof(ZoneControlCaptureLoomSO)
                .GetField("_tanglePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, tanglePerBot);
            typeof(ZoneControlCaptureLoomSO)
                .GetField("_bonusPerWeave", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerWeave);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureLoomController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureLoomController>();
        }

        [Test]
        public void SO_FreshInstance_Threads_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Threads, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_WeaveCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.WeaveCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesThreads()
        {
            var so = CreateSO(threadsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Threads, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ThreadsAtThreshold()
        {
            var so    = CreateSO(threadsNeeded: 3, bonusPerWeave: 940);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,         Is.EqualTo(940));
            Assert.That(so.WeaveCount, Is.EqualTo(1));
            Assert.That(so.Threads,    Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(threadsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_TanglesThreads()
        {
            var so = CreateSO(threadsNeeded: 6, tanglePerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Threads, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(threadsNeeded: 6, tanglePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Threads, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ThreadProgress_Clamped()
        {
            var so = CreateSO(threadsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.ThreadProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnLoomWoven_FiresEvent()
        {
            var so    = CreateSO(threadsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureLoomSO)
                .GetField("_onLoomWoven", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(threadsNeeded: 2, bonusPerWeave: 940);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Threads,           Is.EqualTo(0));
            Assert.That(so.WeaveCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleWeaves_Accumulate()
        {
            var so = CreateSO(threadsNeeded: 2, bonusPerWeave: 940);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.WeaveCount,        Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(1880));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_LoomSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.LoomSO, Is.Null);
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
            typeof(ZoneControlCaptureLoomController)
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
