using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureVortexTests
    {
        private static ZoneControlCaptureVortexSO CreateSO(
            int chargesForVortex       = 4,
            int vortexDurationCaptures = 3,
            int bonusPerVortexCapture  = 110)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureVortexSO>();
            typeof(ZoneControlCaptureVortexSO)
                .GetField("_chargesForVortex",       BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, chargesForVortex);
            typeof(ZoneControlCaptureVortexSO)
                .GetField("_vortexDurationCaptures", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, vortexDurationCaptures);
            typeof(ZoneControlCaptureVortexSO)
                .GetField("_bonusPerVortexCapture",  BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerVortexCapture);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureVortexController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureVortexController>();
        }

        [Test]
        public void SO_FreshInstance_IsActive_False()
        {
            var so = CreateSO();
            Assert.That(so.IsActive, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_BelowThreshold_DoesNotActivate()
        {
            var so = CreateSO(chargesForVortex: 4);
            so.RecordBotCapture();
            so.RecordBotCapture();
            so.RecordBotCapture();
            Assert.That(so.IsActive, Is.False);
            Assert.That(so.BotChargeCount, Is.EqualTo(3));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ReachesThreshold_ActivatesVortex()
        {
            var so = CreateSO(chargesForVortex: 3);
            so.RecordBotCapture();
            so.RecordBotCapture();
            so.RecordBotCapture();
            Assert.That(so.IsActive, Is.True);
            Assert.That(so.VortexCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_WhileActive_ClosesVortex()
        {
            var so = CreateSO(chargesForVortex: 2);
            so.RecordBotCapture();
            so.RecordBotCapture();
            Assert.That(so.IsActive, Is.True);
            so.RecordBotCapture();
            Assert.That(so.IsActive, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_Inactive_ReturnsZero()
        {
            var so    = CreateSO();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_Active_ReturnsBonus()
        {
            var so = CreateSO(chargesForVortex: 2, vortexDurationCaptures: 3, bonusPerVortexCapture: 110);
            so.RecordBotCapture();
            so.RecordBotCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(110));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ExhaustsVortex_ClosesIt()
        {
            var so = CreateSO(chargesForVortex: 2, vortexDurationCaptures: 2);
            so.RecordBotCapture();
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.IsActive, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_VortexProgress_ChargingMode_ReflectsRatio()
        {
            var so = CreateSO(chargesForVortex: 4);
            so.RecordBotCapture();
            so.RecordBotCapture();
            Assert.That(so.VortexProgress, Is.EqualTo(0.5f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_VortexProgress_ActiveMode_ReflectsCaptures()
        {
            var so = CreateSO(chargesForVortex: 2, vortexDurationCaptures: 4);
            so.RecordBotCapture();
            so.RecordBotCapture();
            Assert.That(so.VortexProgress, Is.EqualTo(1f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_VortexCount_IncrementsOnOpen()
        {
            var so = CreateSO(chargesForVortex: 2);
            so.RecordBotCapture();
            so.RecordBotCapture();
            Assert.That(so.VortexCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(chargesForVortex: 2);
            so.RecordBotCapture();
            so.RecordBotCapture();
            so.Reset();
            Assert.That(so.IsActive,          Is.False);
            Assert.That(so.BotChargeCount,    Is.EqualTo(0));
            Assert.That(so.VortexCount,       Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_VortexSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.VortexSO, Is.Null);
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
            typeof(ZoneControlCaptureVortexController)
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
