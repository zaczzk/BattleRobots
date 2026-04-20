using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureArchTests
    {
        private static ZoneControlCaptureArchSO CreateSO(
            int keystonesNeeded = 6,
            int topplePerBot    = 2,
            int bonusPerArch    = 455)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureArchSO>();
            typeof(ZoneControlCaptureArchSO)
                .GetField("_keystonesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, keystonesNeeded);
            typeof(ZoneControlCaptureArchSO)
                .GetField("_topplePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, topplePerBot);
            typeof(ZoneControlCaptureArchSO)
                .GetField("_bonusPerArch", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerArch);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureArchController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureArchController>();
        }

        [Test]
        public void SO_FreshInstance_Keystones_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Keystones, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ArchCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ArchCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesKeystones()
        {
            var so = CreateSO(keystonesNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Keystones, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_CompletesAtThreshold()
        {
            var so = CreateSO(keystonesNeeded: 3, bonusPerArch: 455);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,        Is.EqualTo(455));
            Assert.That(so.ArchCount, Is.EqualTo(1));
            Assert.That(so.Keystones, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroBeforeComplete()
        {
            var so    = CreateSO(keystonesNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_TopplesKeystones()
        {
            var so = CreateSO(keystonesNeeded: 6, topplePerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Keystones, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(keystonesNeeded: 6, topplePerBot: 4);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Keystones, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_KeystoneProgress_Clamped()
        {
            var so = CreateSO(keystonesNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.KeystoneProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnArchComplete_FiresEvent()
        {
            var so    = CreateSO(keystonesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureArchSO)
                .GetField("_onArchComplete", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(keystonesNeeded: 2, bonusPerArch: 455);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Keystones,         Is.EqualTo(0));
            Assert.That(so.ArchCount,         Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleArches_Accumulate()
        {
            var so = CreateSO(keystonesNeeded: 2, bonusPerArch: 455);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ArchCount,         Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(910));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ArchSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ArchSO, Is.Null);
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
            typeof(ZoneControlCaptureArchController)
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
