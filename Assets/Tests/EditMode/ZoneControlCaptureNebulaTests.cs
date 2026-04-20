using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureNebulaTests
    {
        private static ZoneControlCaptureNebulaSO CreateSO(
            float densityPerCapture  = 15f,
            float maxDensity         = 100f,
            float payoutFraction     = 0.8f,
            int   bonusPerDensityUnit = 4)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureNebulaSO>();
            typeof(ZoneControlCaptureNebulaSO)
                .GetField("_densityPerCapture",   BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, densityPerCapture);
            typeof(ZoneControlCaptureNebulaSO)
                .GetField("_maxDensity",          BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, maxDensity);
            typeof(ZoneControlCaptureNebulaSO)
                .GetField("_payoutFraction",      BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, payoutFraction);
            typeof(ZoneControlCaptureNebulaSO)
                .GetField("_bonusPerDensityUnit", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerDensityUnit);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureNebulaController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureNebulaController>();
        }

        [Test]
        public void SO_FreshInstance_CurrentDensity_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CurrentDensity, Is.EqualTo(0f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_IncreasesDensity()
        {
            var so = CreateSO(densityPerCapture: 20f, maxDensity: 100f);
            so.RecordPlayerCapture();
            Assert.That(so.CurrentDensity, Is.EqualTo(20f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ClampsDensityAtMax()
        {
            var so = CreateSO(densityPerCapture: 60f, maxDensity: 100f);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.CurrentDensity, Is.EqualTo(100f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_EmptyNebula_ReturnsZero()
        {
            var so     = CreateSO();
            int payout = so.RecordBotCapture();
            Assert.That(payout, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_WithDensity_ReturnsNonZeroPayout()
        {
            var so = CreateSO(densityPerCapture: 100f, maxDensity: 100f, payoutFraction: 1f, bonusPerDensityUnit: 4);
            so.RecordPlayerCapture();
            int payout = so.RecordBotCapture();
            Assert.That(payout, Is.EqualTo(400));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClearsDensity()
        {
            var so = CreateSO(densityPerCapture: 50f, maxDensity: 100f);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.CurrentDensity, Is.EqualTo(0f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_FiresEvent()
        {
            var so    = CreateSO(densityPerCapture: 100f, maxDensity: 100f);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureNebulaSO)
                .GetField("_onNebulaDispersed", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_RecordBotCapture_IncrementsDispersalCount()
        {
            var so = CreateSO(densityPerCapture: 50f, maxDensity: 100f);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.DispersalCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_NebulaProgress_ReflectsDensityRatio()
        {
            var so = CreateSO(densityPerCapture: 50f, maxDensity: 100f);
            so.RecordPlayerCapture();
            Assert.That(so.NebulaProgress, Is.EqualTo(0.5f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_TotalBonusAwarded_AccumulatesOnDispersal()
        {
            var so = CreateSO(densityPerCapture: 100f, maxDensity: 100f, payoutFraction: 1f, bonusPerDensityUnit: 4);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(800));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(densityPerCapture: 50f, maxDensity: 100f);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            so.Reset();
            Assert.That(so.CurrentDensity,    Is.EqualTo(0f).Within(0.001f));
            Assert.That(so.DispersalCount,    Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_NebulaSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.NebulaSO, Is.Null);
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
            typeof(ZoneControlCaptureNebulaController)
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
