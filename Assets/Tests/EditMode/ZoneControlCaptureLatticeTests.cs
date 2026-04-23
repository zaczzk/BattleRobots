using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureLatticeTests
    {
        private static ZoneControlCaptureLatticeSO CreateSO(
            int joinsNeeded    = 5,
            int collapsePerBot = 2,
            int bonusPerJoin   = 3220)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureLatticeSO>();
            typeof(ZoneControlCaptureLatticeSO)
                .GetField("_joinsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, joinsNeeded);
            typeof(ZoneControlCaptureLatticeSO)
                .GetField("_collapsePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, collapsePerBot);
            typeof(ZoneControlCaptureLatticeSO)
                .GetField("_bonusPerJoin", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerJoin);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureLatticeController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureLatticeController>();
        }

        [Test]
        public void SO_FreshInstance_Joins_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Joins, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_JoinCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.JoinCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesJoins()
        {
            var so = CreateSO(joinsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Joins, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(joinsNeeded: 3, bonusPerJoin: 3220);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,         Is.EqualTo(3220));
            Assert.That(so.JoinCount,  Is.EqualTo(1));
            Assert.That(so.Joins,      Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(joinsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesJoins()
        {
            var so = CreateSO(joinsNeeded: 5, collapsePerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Joins, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(joinsNeeded: 5, collapsePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Joins, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_JoinProgress_Clamped()
        {
            var so = CreateSO(joinsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.JoinProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnJoinFormed_FiresEvent()
        {
            var so    = CreateSO(joinsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureLatticeSO)
                .GetField("_onJoinFormed", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(joinsNeeded: 2, bonusPerJoin: 3220);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Joins,             Is.EqualTo(0));
            Assert.That(so.JoinCount,         Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleJoins_Accumulate()
        {
            var so = CreateSO(joinsNeeded: 2, bonusPerJoin: 3220);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.JoinCount,         Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(6440));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_LatticeSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.LatticeSO, Is.Null);
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
            typeof(ZoneControlCaptureLatticeController)
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
