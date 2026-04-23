using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureSemilatticeTests
    {
        private static ZoneControlCaptureSemilatticeSO CreateSO(
            int meetsNeeded    = 6,
            int dissolvePerBot = 2,
            int bonusPerMeet   = 3205)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureSemilatticeSO>();
            typeof(ZoneControlCaptureSemilatticeSO)
                .GetField("_meetsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, meetsNeeded);
            typeof(ZoneControlCaptureSemilatticeSO)
                .GetField("_dissolvePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, dissolvePerBot);
            typeof(ZoneControlCaptureSemilatticeSO)
                .GetField("_bonusPerMeet", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerMeet);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureSemilatticeController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureSemilatticeController>();
        }

        [Test]
        public void SO_FreshInstance_Meets_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Meets, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_MeetCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.MeetCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesMeets()
        {
            var so = CreateSO(meetsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Meets, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(meetsNeeded: 3, bonusPerMeet: 3205);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,          Is.EqualTo(3205));
            Assert.That(so.MeetCount,   Is.EqualTo(1));
            Assert.That(so.Meets,       Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(meetsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesMeets()
        {
            var so = CreateSO(meetsNeeded: 6, dissolvePerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Meets, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(meetsNeeded: 6, dissolvePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Meets, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MeetProgress_Clamped()
        {
            var so = CreateSO(meetsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.MeetProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnMeetFormed_FiresEvent()
        {
            var so    = CreateSO(meetsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureSemilatticeSO)
                .GetField("_onMeetFormed", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(meetsNeeded: 2, bonusPerMeet: 3205);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Meets,             Is.EqualTo(0));
            Assert.That(so.MeetCount,         Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleMeets_Accumulate()
        {
            var so = CreateSO(meetsNeeded: 2, bonusPerMeet: 3205);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.MeetCount,         Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(6410));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_SemilatticeSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.SemilatticeSO, Is.Null);
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
            typeof(ZoneControlCaptureSemilatticeController)
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
