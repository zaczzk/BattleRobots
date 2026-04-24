using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureDoldKanTests
    {
        private static ZoneControlCaptureDoldKanSO CreateSO(
            int degeneraciesNeeded    = 7,
            int breakPerBot           = 2,
            int bonusPerCorrespondence = 3790)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureDoldKanSO>();
            typeof(ZoneControlCaptureDoldKanSO)
                .GetField("_degeneraciesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, degeneraciesNeeded);
            typeof(ZoneControlCaptureDoldKanSO)
                .GetField("_breakPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, breakPerBot);
            typeof(ZoneControlCaptureDoldKanSO)
                .GetField("_bonusPerCorrespondence", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerCorrespondence);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureDoldKanController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureDoldKanController>();
        }

        [Test]
        public void SO_FreshInstance_Degeneracies_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Degeneracies, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_CorrespondCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CorrespondCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesDegeneracies()
        {
            var so = CreateSO(degeneraciesNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.Degeneracies, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(degeneraciesNeeded: 3, bonusPerCorrespondence: 3790);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,               Is.EqualTo(3790));
            Assert.That(so.CorrespondCount,  Is.EqualTo(1));
            Assert.That(so.Degeneracies,     Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(degeneraciesNeeded: 7);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_BreaksNormalization()
        {
            var so = CreateSO(degeneraciesNeeded: 7, breakPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Degeneracies, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(degeneraciesNeeded: 7, breakPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Degeneracies, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_DegeneracyProgress_Clamped()
        {
            var so = CreateSO(degeneraciesNeeded: 7);
            so.RecordPlayerCapture();
            Assert.That(so.DegeneracyProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnDoldKanCorresponded_FiresEvent()
        {
            var so    = CreateSO(degeneraciesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureDoldKanSO)
                .GetField("_onDoldKanCorresponded", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(degeneraciesNeeded: 2, bonusPerCorrespondence: 3790);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Degeneracies,      Is.EqualTo(0));
            Assert.That(so.CorrespondCount,   Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleCorrespondences_Accumulate()
        {
            var so = CreateSO(degeneraciesNeeded: 2, bonusPerCorrespondence: 3790);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.CorrespondCount,   Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(7580));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_DoldKanSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.DoldKanSO, Is.Null);
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
            typeof(ZoneControlCaptureDoldKanController)
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
