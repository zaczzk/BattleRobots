using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureEchoChainTests
    {
        private static ZoneControlCaptureEchoChainSO CreateSO(
            float echoWindow = 6f, int bonusPerEcho = 60, int maxMultiplier = 5)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureEchoChainSO>();
            typeof(ZoneControlCaptureEchoChainSO)
                .GetField("_echoWindowSeconds", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, echoWindow);
            typeof(ZoneControlCaptureEchoChainSO)
                .GetField("_bonusPerEcho", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerEcho);
            typeof(ZoneControlCaptureEchoChainSO)
                .GetField("_maxEchoMultiplier", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, maxMultiplier);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureEchoChainController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureEchoChainController>();
        }

        [Test]
        public void SO_FreshInstance_EchoCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.EchoCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_CurrentMultiplier_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CurrentMultiplier, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_TotalBonusAwarded_Zero()
        {
            var so = CreateSO();
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_FirstCapture_MultiplierOne()
        {
            var so = CreateSO(echoWindow: 6f);
            so.RecordPlayerCapture(0f);
            Assert.That(so.CurrentMultiplier, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_FirstCapture_EarnsBaseBonus()
        {
            var so    = CreateSO(bonusPerEcho: 60);
            int bonus = so.RecordPlayerCapture(0f);
            Assert.That(bonus, Is.EqualTo(60));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_WithinWindow_IncrementsMultiplier()
        {
            var so = CreateSO(echoWindow: 6f, bonusPerEcho: 60);
            so.RecordPlayerCapture(0f);
            int bonus = so.RecordPlayerCapture(3f);
            Assert.That(so.CurrentMultiplier, Is.EqualTo(2));
            Assert.That(bonus, Is.EqualTo(120));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_BeyondWindow_ResetsMultiplierToOne()
        {
            var so = CreateSO(echoWindow: 6f, bonusPerEcho: 60);
            so.RecordPlayerCapture(0f);
            so.RecordPlayerCapture(3f);
            so.RecordPlayerCapture(100f);
            Assert.That(so.CurrentMultiplier, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtMaxMultiplier_ClampsToMax()
        {
            var so = CreateSO(echoWindow: 10f, bonusPerEcho: 10, maxMultiplier: 2);
            so.RecordPlayerCapture(0f);
            so.RecordPlayerCapture(1f);
            int bonus = so.RecordPlayerCapture(2f);
            Assert.That(so.CurrentMultiplier, Is.EqualTo(2));
            Assert.That(bonus, Is.EqualTo(20));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_FiresEvent()
        {
            var so    = CreateSO();
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureEchoChainSO)
                .GetField("_onEchoHit", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordPlayerCapture(0f);
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_RecordBotCapture_ResetsMultiplier()
        {
            var so = CreateSO(echoWindow: 6f);
            so.RecordPlayerCapture(0f);
            so.RecordPlayerCapture(1f);
            so.RecordBotCapture();
            Assert.That(so.CurrentMultiplier, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ThenPlayerCapture_MultiplierRestartAtOne()
        {
            var so = CreateSO(echoWindow: 6f);
            so.RecordPlayerCapture(0f);
            so.RecordPlayerCapture(1f);
            so.RecordBotCapture();
            so.RecordPlayerCapture(2f);
            Assert.That(so.CurrentMultiplier, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(echoWindow: 6f, bonusPerEcho: 60);
            so.RecordPlayerCapture(0f);
            so.RecordPlayerCapture(1f);
            so.Reset();
            Assert.That(so.EchoCount,         Is.EqualTo(0));
            Assert.That(so.CurrentMultiplier, Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_EchoChainSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.EchoChainSO, Is.Null);
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
            typeof(ZoneControlCaptureEchoChainController)
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
