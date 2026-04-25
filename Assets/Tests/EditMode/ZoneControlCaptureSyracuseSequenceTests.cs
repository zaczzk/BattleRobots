using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureSyracuseSequenceTests
    {
        private static ZoneControlCaptureSyracuseSequenceSO CreateSO(
            int descentsNeeded     = 6,
            int ascentSpikesPerBot = 1,
            int bonusPerDescent    = 4750)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureSyracuseSequenceSO>();
            typeof(ZoneControlCaptureSyracuseSequenceSO)
                .GetField("_descentsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, descentsNeeded);
            typeof(ZoneControlCaptureSyracuseSequenceSO)
                .GetField("_ascentSpikesPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, ascentSpikesPerBot);
            typeof(ZoneControlCaptureSyracuseSequenceSO)
                .GetField("_bonusPerDescent", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerDescent);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureSyracuseSequenceController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureSyracuseSequenceController>();
        }

        [Test]
        public void SO_FreshInstance_Descents_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Descents, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_DescentCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.DescentCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesDescents()
        {
            var so = CreateSO(descentsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Descents, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(descentsNeeded: 3, bonusPerDescent: 4750);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,            Is.EqualTo(4750));
            Assert.That(so.DescentCount,  Is.EqualTo(1));
            Assert.That(so.Descents,      Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(descentsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesAscentSpikes()
        {
            var so = CreateSO(descentsNeeded: 6, ascentSpikesPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Descents, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(descentsNeeded: 6, ascentSpikesPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Descents, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_DescentProgress_Clamped()
        {
            var so = CreateSO(descentsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.DescentProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnSyracuseSequenceDescended_FiresEvent()
        {
            var so    = CreateSO(descentsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureSyracuseSequenceSO)
                .GetField("_onSyracuseSequenceDescended", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(descentsNeeded: 2, bonusPerDescent: 4750);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Descents,          Is.EqualTo(0));
            Assert.That(so.DescentCount,      Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleDescents_Accumulate()
        {
            var so = CreateSO(descentsNeeded: 2, bonusPerDescent: 4750);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.DescentCount,      Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(9500));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_SyracuseSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.SyracuseSO, Is.Null);
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
            typeof(ZoneControlCaptureSyracuseSequenceController)
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
