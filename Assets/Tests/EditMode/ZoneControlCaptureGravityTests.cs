using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureGravityTests
    {
        private static ZoneControlCaptureGravitySO CreateSO(float rise = 20f, float fall = 15f, float max = 100f, int bonus = 300)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureGravitySO>();
            typeof(ZoneControlCaptureGravitySO)
                .GetField("_gravityRisePerBotCapture", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, rise);
            typeof(ZoneControlCaptureGravitySO)
                .GetField("_gravityFallPerPlayerCapture", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, fall);
            typeof(ZoneControlCaptureGravitySO)
                .GetField("_maxGravity", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, max);
            typeof(ZoneControlCaptureGravitySO)
                .GetField("_bonusAtMaxGravity", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonus);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureGravityController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureGravityController>();
        }

        [Test]
        public void SO_FreshInstance_Gravity_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CurrentGravity, Is.EqualTo(0f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_IsAtPeak_False()
        {
            var so = CreateSO();
            Assert.That(so.IsAtPeak, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RaisesGravity()
        {
            var so = CreateSO(rise: 25f, max: 100f);
            so.RecordBotCapture();
            Assert.That(so.CurrentGravity, Is.EqualTo(25f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsAtMax()
        {
            var so = CreateSO(rise: 60f, max: 100f);
            so.RecordBotCapture();
            so.RecordBotCapture();
            Assert.That(so.CurrentGravity, Is.EqualTo(100f).Within(0.001f));
            Assert.That(so.IsAtPeak, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_FiresEvent_AtMax()
        {
            var so    = CreateSO(rise: 100f, max: 100f);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureGravitySO)
                .GetField("_onGravityPeak", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordBotCapture();
            Assert.That(fired, Is.EqualTo(1));
            so.RecordBotCapture();
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtPeak_ReturnsBonus()
        {
            var so = CreateSO(rise: 100f, max: 100f, bonus: 500);
            so.RecordBotCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(500));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(500));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_BelowPeak_ReturnsZero()
        {
            var so    = CreateSO(rise: 20f, max: 100f, bonus: 500);
            so.RecordBotCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReducesGravity()
        {
            var so = CreateSO(rise: 50f, fall: 20f, max: 100f);
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            Assert.That(so.CurrentGravity, Is.EqualTo(30f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GravityProgress_ScalesCorrectly()
        {
            var so = CreateSO(rise: 50f, max: 100f);
            so.RecordBotCapture();
            Assert.That(so.GravityProgress, Is.EqualTo(0.5f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(rise: 100f, max: 100f, bonus: 300);
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.CurrentGravity,    Is.EqualTo(0f).Within(0.001f));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Assert.That(so.IsAtPeak,          Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_GravitySO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.GravitySO, Is.Null);
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
            typeof(ZoneControlCaptureGravityController)
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
