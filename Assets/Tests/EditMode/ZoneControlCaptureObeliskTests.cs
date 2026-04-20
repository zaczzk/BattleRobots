using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureObeliskTests
    {
        private static ZoneControlCaptureObeliskSO CreateSO(
            int runesNeeded        = 5,
            int erosionPerBot      = 1,
            int bonusPerInscription = 510)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureObeliskSO>();
            typeof(ZoneControlCaptureObeliskSO)
                .GetField("_runesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, runesNeeded);
            typeof(ZoneControlCaptureObeliskSO)
                .GetField("_erosionPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, erosionPerBot);
            typeof(ZoneControlCaptureObeliskSO)
                .GetField("_bonusPerInscription", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerInscription);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureObeliskController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureObeliskController>();
        }

        [Test]
        public void SO_FreshInstance_Runes_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Runes, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_InscriptionCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.InscriptionCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesRunes()
        {
            var so = CreateSO(runesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Runes, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_InscribesAtThreshold()
        {
            var so    = CreateSO(runesNeeded: 3, bonusPerInscription: 510);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,               Is.EqualTo(510));
            Assert.That(so.InscriptionCount, Is.EqualTo(1));
            Assert.That(so.Runes,            Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileInscribing()
        {
            var so    = CreateSO(runesNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ErodesRunes()
        {
            var so = CreateSO(runesNeeded: 5, erosionPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Runes, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(runesNeeded: 5, erosionPerBot: 4);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Runes, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RuneProgress_Clamped()
        {
            var so = CreateSO(runesNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.RuneProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnObeliskInscribed_FiresEvent()
        {
            var so    = CreateSO(runesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureObeliskSO)
                .GetField("_onObeliskInscribed", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(runesNeeded: 2, bonusPerInscription: 510);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Runes,             Is.EqualTo(0));
            Assert.That(so.InscriptionCount,  Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleInscriptions_Accumulate()
        {
            var so = CreateSO(runesNeeded: 2, bonusPerInscription: 510);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.InscriptionCount,  Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(1020));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ObeliskSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ObeliskSO, Is.Null);
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
            typeof(ZoneControlCaptureObeliskController)
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
