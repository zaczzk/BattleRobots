using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureProofNetsTests
    {
        private static ZoneControlCaptureProofNetsSO CreateSO(
            int netLinksNeeded           = 6,
            int cyclicObstructionsPerBot = 1,
            int bonusPerNetLink          = 5095)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureProofNetsSO>();
            typeof(ZoneControlCaptureProofNetsSO)
                .GetField("_netLinksNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, netLinksNeeded);
            typeof(ZoneControlCaptureProofNetsSO)
                .GetField("_cyclicObstructionsPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, cyclicObstructionsPerBot);
            typeof(ZoneControlCaptureProofNetsSO)
                .GetField("_bonusPerNetLink", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerNetLink);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureProofNetsController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureProofNetsController>();
        }

        [Test]
        public void SO_FreshInstance_NetLinks_Zero()
        {
            var so = CreateSO();
            Assert.That(so.NetLinks, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_NetLinkCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.NetLinkCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesNetLinks()
        {
            var so = CreateSO(netLinksNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.NetLinks, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(netLinksNeeded: 3, bonusPerNetLink: 5095);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,           Is.EqualTo(5095));
            Assert.That(so.NetLinkCount, Is.EqualTo(1));
            Assert.That(so.NetLinks,     Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(netLinksNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesCyclicObstructions()
        {
            var so = CreateSO(netLinksNeeded: 6, cyclicObstructionsPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.NetLinks, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(netLinksNeeded: 6, cyclicObstructionsPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.NetLinks, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_NetLinkProgress_Clamped()
        {
            var so = CreateSO(netLinksNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.NetLinkProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnProofNetsCompleted_FiresEvent()
        {
            var so    = CreateSO(netLinksNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureProofNetsSO)
                .GetField("_onProofNetsCompleted", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(netLinksNeeded: 2, bonusPerNetLink: 5095);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.NetLinks,          Is.EqualTo(0));
            Assert.That(so.NetLinkCount,      Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleNetLinks_Accumulate()
        {
            var so = CreateSO(netLinksNeeded: 2, bonusPerNetLink: 5095);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.NetLinkCount,      Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(10190));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ProofNetsSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ProofNetsSO, Is.Null);
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
            typeof(ZoneControlCaptureProofNetsController)
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
