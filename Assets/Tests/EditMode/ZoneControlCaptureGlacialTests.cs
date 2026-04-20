using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureGlacialTests
    {
        private static ZoneControlCaptureGlacialSO CreateSO(
            int   icePerBotCapture = 25,
            int   maxIce           = 100,
            float meltMultiplier   = 3f)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureGlacialSO>();
            typeof(ZoneControlCaptureGlacialSO)
                .GetField("_icePerBotCapture", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, icePerBotCapture);
            typeof(ZoneControlCaptureGlacialSO)
                .GetField("_maxIce", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, maxIce);
            typeof(ZoneControlCaptureGlacialSO)
                .GetField("_meltMultiplier", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, meltMultiplier);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureGlacialController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureGlacialController>();
        }

        [Test]
        public void SO_FreshInstance_IceLevel_Zero()
        {
            var so = CreateSO();
            Assert.That(so.IceLevel, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_AccumulatesIce()
        {
            var so = CreateSO(icePerBotCapture: 25, maxIce: 100);
            so.RecordBotCapture();
            Assert.That(so.IceLevel, Is.EqualTo(25));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToMaxIce()
        {
            var so = CreateSO(icePerBotCapture: 40, maxIce: 100);
            so.RecordBotCapture();
            so.RecordBotCapture();
            so.RecordBotCapture();
            Assert.That(so.IceLevel, Is.EqualTo(100));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_NoIce_ReturnsZero()
        {
            var so    = CreateSO();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_WithIce_ReturnsBonus()
        {
            var so = CreateSO(icePerBotCapture: 100, maxIce: 100, meltMultiplier: 3f);
            so.RecordBotCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(300));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_WithIce_ClearsIce()
        {
            var so = CreateSO(icePerBotCapture: 50, maxIce: 100);
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            Assert.That(so.IceLevel, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MeltBonus_ScalesWithIceLevel()
        {
            var so = CreateSO(icePerBotCapture: 50, maxIce: 100, meltMultiplier: 2f);
            so.RecordBotCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(100));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_IceProgress_Clamped()
        {
            var so = CreateSO(icePerBotCapture: 25, maxIce: 100);
            Assert.That(so.IceProgress, Is.InRange(0f, 1f));
            so.RecordBotCapture();
            Assert.That(so.IceProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnGlacialMelt_FiresEvent()
        {
            var so    = CreateSO(icePerBotCapture: 50, maxIce: 100);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureGlacialSO)
                .GetField("_onGlacialMelt", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(icePerBotCapture: 50, maxIce: 100);
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            so.Reset();
            Assert.That(so.IceLevel,          Is.EqualTo(0));
            Assert.That(so.MeltCount,         Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleMelts_Accumulate()
        {
            var so = CreateSO(icePerBotCapture: 100, maxIce: 100, meltMultiplier: 1f);
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            Assert.That(so.MeltCount, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_TotalBonusAwarded_AccumulatesAcrossMelts()
        {
            var so = CreateSO(icePerBotCapture: 50, maxIce: 100, meltMultiplier: 2f);
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(100 + 200));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_GlacialSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.GlacialSO, Is.Null);
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
            typeof(ZoneControlCaptureGlacialController)
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
