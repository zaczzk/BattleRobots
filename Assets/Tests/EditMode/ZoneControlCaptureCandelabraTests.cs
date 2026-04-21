using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureCandelabraTests
    {
        private static ZoneControlCaptureCandelabraSO CreateSO(
            int flamesNeeded         = 6,
            int snuffPerBot          = 2,
            int bonusPerIllumination = 760)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureCandelabraSO>();
            typeof(ZoneControlCaptureCandelabraSO)
                .GetField("_flamesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, flamesNeeded);
            typeof(ZoneControlCaptureCandelabraSO)
                .GetField("_snuffPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, snuffPerBot);
            typeof(ZoneControlCaptureCandelabraSO)
                .GetField("_bonusPerIllumination", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerIllumination);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureCandelabraController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureCandelabraController>();
        }

        [Test]
        public void SO_FreshInstance_Flames_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Flames, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_IlluminationCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.IlluminationCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesFlames()
        {
            var so = CreateSO(flamesNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Flames, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_IlluminatesAtThreshold()
        {
            var so    = CreateSO(flamesNeeded: 3, bonusPerIllumination: 760);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                Is.EqualTo(760));
            Assert.That(so.IlluminationCount, Is.EqualTo(1));
            Assert.That(so.Flames,            Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(flamesNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_SnuffsFlames()
        {
            var so = CreateSO(flamesNeeded: 6, snuffPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Flames, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(flamesNeeded: 6, snuffPerBot: 5);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Flames, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FlameProgress_Clamped()
        {
            var so = CreateSO(flamesNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.FlameProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnCandelabraIlluminated_FiresEvent()
        {
            var so    = CreateSO(flamesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureCandelabraSO)
                .GetField("_onCandelabraIlluminated", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(flamesNeeded: 2, bonusPerIllumination: 760);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Flames,            Is.EqualTo(0));
            Assert.That(so.IlluminationCount, Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleIlluminations_Accumulate()
        {
            var so = CreateSO(flamesNeeded: 2, bonusPerIllumination: 760);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.IlluminationCount, Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(1520));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_CandelabraSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.CandelabraSO, Is.Null);
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
            typeof(ZoneControlCaptureCandelabraController)
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
