using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureSigilTests
    {
        private static ZoneControlCaptureSigilSO CreateSO(
            int capturesForSigil  = 6,
            int botBreakThreshold = 2,
            int bonusPerSigil     = 350)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureSigilSO>();
            typeof(ZoneControlCaptureSigilSO)
                .GetField("_capturesForSigil",  BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, capturesForSigil);
            typeof(ZoneControlCaptureSigilSO)
                .GetField("_botBreakThreshold", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, botBreakThreshold);
            typeof(ZoneControlCaptureSigilSO)
                .GetField("_bonusPerSigil",     BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerSigil);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureSigilController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureSigilController>();
        }

        [Test]
        public void SO_FreshInstance_SigilCharges_Zero()
        {
            var so = CreateSO();
            Assert.That(so.SigilCharges, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_BelowThreshold_ReturnsZero()
        {
            var so    = CreateSO(capturesForSigil: 4);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_BelowThreshold_IncreasesCharges()
        {
            var so = CreateSO(capturesForSigil: 4);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.SigilCharges, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReachesThreshold_AwakensAndReturnsBonus()
        {
            var so    = CreateSO(capturesForSigil: 3, bonusPerSigil: 350);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,           Is.EqualTo(350));
            Assert.That(so.SigilCount,   Is.EqualTo(1));
            Assert.That(so.SigilCharges, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_Awaken_FiresEvent()
        {
            var so    = CreateSO(capturesForSigil: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureSigilSO)
                .GetField("_onSigilAwakened", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_RecordPlayerCapture_ResetsBotCapturesSinceBreak()
        {
            var so = CreateSO(capturesForSigil: 4, botBreakThreshold: 3);
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            Assert.That(so.BotCapturesSinceBreak, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_BelowBreakThreshold_DoesNotReduceCharges()
        {
            var so = CreateSO(capturesForSigil: 6, botBreakThreshold: 3);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            so.RecordBotCapture();
            Assert.That(so.SigilCharges, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ReachesBreakThreshold_ReducesOneCharge()
        {
            var so = CreateSO(capturesForSigil: 6, botBreakThreshold: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            so.RecordBotCapture();
            Assert.That(so.SigilCharges, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsChargesAtZero()
        {
            var so = CreateSO(botBreakThreshold: 1);
            so.RecordBotCapture();
            Assert.That(so.SigilCharges, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SigilProgress_ReflectsChargeRatio()
        {
            var so = CreateSO(capturesForSigil: 4);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.SigilProgress, Is.EqualTo(0.5f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(capturesForSigil: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.SigilCharges,          Is.EqualTo(0));
            Assert.That(so.SigilCount,            Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,     Is.EqualTo(0));
            Assert.That(so.BotCapturesSinceBreak, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_SigilSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.SigilSO, Is.Null);
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
            typeof(ZoneControlCaptureSigilController)
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
