using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCapturePullbackTests
    {
        private static ZoneControlCapturePullbackSO CreateSO(
            int morphismsNeeded  = 5,
            int unravelPerBot    = 1,
            int bonusPerPullback = 2755)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCapturePullbackSO>();
            typeof(ZoneControlCapturePullbackSO)
                .GetField("_morphismsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, morphismsNeeded);
            typeof(ZoneControlCapturePullbackSO)
                .GetField("_unravelPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, unravelPerBot);
            typeof(ZoneControlCapturePullbackSO)
                .GetField("_bonusPerPullback", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerPullback);
            so.Reset();
            return so;
        }

        private static ZoneControlCapturePullbackController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCapturePullbackController>();
        }

        [Test]
        public void SO_FreshInstance_Morphisms_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Morphisms, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_PullbackCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.PullbackCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesMorphisms()
        {
            var so = CreateSO(morphismsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Morphisms, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(morphismsNeeded: 3, bonusPerPullback: 2755);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,            Is.EqualTo(2755));
            Assert.That(so.PullbackCount, Is.EqualTo(1));
            Assert.That(so.Morphisms,     Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(morphismsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesMorphisms()
        {
            var so = CreateSO(morphismsNeeded: 5, unravelPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Morphisms, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(morphismsNeeded: 5, unravelPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Morphisms, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MorphismProgress_Clamped()
        {
            var so = CreateSO(morphismsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.MorphismProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnPullbackPulled_FiresEvent()
        {
            var so    = CreateSO(morphismsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCapturePullbackSO)
                .GetField("_onPullbackPulled", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(morphismsNeeded: 2, bonusPerPullback: 2755);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Morphisms,         Is.EqualTo(0));
            Assert.That(so.PullbackCount,     Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultiplePullbacks_Accumulate()
        {
            var so = CreateSO(morphismsNeeded: 2, bonusPerPullback: 2755);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.PullbackCount,     Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(5510));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_PullbackSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.PullbackSO, Is.Null);
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
            typeof(ZoneControlCapturePullbackController)
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
