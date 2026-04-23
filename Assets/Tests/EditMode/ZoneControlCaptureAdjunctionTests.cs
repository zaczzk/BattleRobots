using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureAdjunctionTests
    {
        private static ZoneControlCaptureAdjunctionSO CreateSO(
            int adjunctsNeeded = 5,
            int splitPerBot    = 1,
            int bonusPerAdjoin = 2455)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureAdjunctionSO>();
            typeof(ZoneControlCaptureAdjunctionSO)
                .GetField("_adjunctsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, adjunctsNeeded);
            typeof(ZoneControlCaptureAdjunctionSO)
                .GetField("_splitPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, splitPerBot);
            typeof(ZoneControlCaptureAdjunctionSO)
                .GetField("_bonusPerAdjoin", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerAdjoin);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureAdjunctionController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureAdjunctionController>();
        }

        [Test]
        public void SO_FreshInstance_Adjuncts_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Adjuncts, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_AdjoinCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.AdjoinCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesAdjuncts()
        {
            var so = CreateSO(adjunctsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Adjuncts, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(adjunctsNeeded: 3, bonusPerAdjoin: 2455);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,            Is.EqualTo(2455));
            Assert.That(so.AdjoinCount,   Is.EqualTo(1));
            Assert.That(so.Adjuncts,      Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(adjunctsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesAdjuncts()
        {
            var so = CreateSO(adjunctsNeeded: 5, splitPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Adjuncts, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(adjunctsNeeded: 5, splitPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Adjuncts, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AdjunctProgress_Clamped()
        {
            var so = CreateSO(adjunctsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.AdjunctProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnAdjunctionAdjoined_FiresEvent()
        {
            var so    = CreateSO(adjunctsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureAdjunctionSO)
                .GetField("_onAdjunctionAdjoined", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(adjunctsNeeded: 2, bonusPerAdjoin: 2455);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Adjuncts,          Is.EqualTo(0));
            Assert.That(so.AdjoinCount,       Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleAdjoins_Accumulate()
        {
            var so = CreateSO(adjunctsNeeded: 2, bonusPerAdjoin: 2455);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.AdjoinCount,       Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(4910));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_AdjunctionSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.AdjunctionSO, Is.Null);
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
            typeof(ZoneControlCaptureAdjunctionController)
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
