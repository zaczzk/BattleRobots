using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureSpringboardTests
    {
        private static ZoneControlCaptureSpringboardSO CreateSO(
            int bouncesNeeded  = 5,
            int dampPerBot     = 1,
            int bonusPerLaunch = 1165)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureSpringboardSO>();
            typeof(ZoneControlCaptureSpringboardSO)
                .GetField("_bouncesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bouncesNeeded);
            typeof(ZoneControlCaptureSpringboardSO)
                .GetField("_dampPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, dampPerBot);
            typeof(ZoneControlCaptureSpringboardSO)
                .GetField("_bonusPerLaunch", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerLaunch);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureSpringboardController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureSpringboardController>();
        }

        [Test]
        public void SO_FreshInstance_Bounces_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Bounces, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_LaunchCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.LaunchCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesBounces()
        {
            var so = CreateSO(bouncesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Bounces, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_BouncesAtThreshold()
        {
            var so    = CreateSO(bouncesNeeded: 3, bonusPerLaunch: 1165);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,           Is.EqualTo(1165));
            Assert.That(so.LaunchCount,  Is.EqualTo(1));
            Assert.That(so.Bounces,      Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(bouncesNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesBounces()
        {
            var so = CreateSO(bouncesNeeded: 5, dampPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Bounces, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(bouncesNeeded: 5, dampPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Bounces, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_BounceProgress_Clamped()
        {
            var so = CreateSO(bouncesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.BounceProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnSpringboardLaunched_FiresEvent()
        {
            var so    = CreateSO(bouncesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureSpringboardSO)
                .GetField("_onSpringboardLaunched", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(bouncesNeeded: 2, bonusPerLaunch: 1165);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Bounces,           Is.EqualTo(0));
            Assert.That(so.LaunchCount,       Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleLaunches_Accumulate()
        {
            var so = CreateSO(bouncesNeeded: 2, bonusPerLaunch: 1165);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.LaunchCount,       Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(2330));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_SpringboardSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.SpringboardSO, Is.Null);
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
            typeof(ZoneControlCaptureSpringboardController)
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
