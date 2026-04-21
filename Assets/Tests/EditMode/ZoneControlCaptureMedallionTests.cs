using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureMedallionTests
    {
        private static ZoneControlCaptureMedallionSO CreateSO(
            int inscriptionsNeeded = 5,
            int erasePerBot        = 1,
            int bonusPerMedallion  = 715)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureMedallionSO>();
            typeof(ZoneControlCaptureMedallionSO)
                .GetField("_inscriptionsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, inscriptionsNeeded);
            typeof(ZoneControlCaptureMedallionSO)
                .GetField("_erasePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, erasePerBot);
            typeof(ZoneControlCaptureMedallionSO)
                .GetField("_bonusPerMedallion", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerMedallion);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureMedallionController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureMedallionController>();
        }

        [Test]
        public void SO_FreshInstance_Inscriptions_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Inscriptions, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_MedallionCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.MedallionCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesInscriptions()
        {
            var so = CreateSO(inscriptionsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Inscriptions, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_CompletesAtThreshold()
        {
            var so    = CreateSO(inscriptionsNeeded: 3, bonusPerMedallion: 715);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,              Is.EqualTo(715));
            Assert.That(so.MedallionCount,  Is.EqualTo(1));
            Assert.That(so.Inscriptions,    Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(inscriptionsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ErasesInscriptions()
        {
            var so = CreateSO(inscriptionsNeeded: 5, erasePerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Inscriptions, Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(inscriptionsNeeded: 5, erasePerBot: 5);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Inscriptions, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_InscriptionProgress_Clamped()
        {
            var so = CreateSO(inscriptionsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.InscriptionProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnMedallionComplete_FiresEvent()
        {
            var so    = CreateSO(inscriptionsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureMedallionSO)
                .GetField("_onMedallionComplete", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(inscriptionsNeeded: 2, bonusPerMedallion: 715);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Inscriptions,      Is.EqualTo(0));
            Assert.That(so.MedallionCount,    Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleMedallions_Accumulate()
        {
            var so = CreateSO(inscriptionsNeeded: 2, bonusPerMedallion: 715);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.MedallionCount,    Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(1430));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_MedallionSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.MedallionSO, Is.Null);
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
            typeof(ZoneControlCaptureMedallionController)
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
