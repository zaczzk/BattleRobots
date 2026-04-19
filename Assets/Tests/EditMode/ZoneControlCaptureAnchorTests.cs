using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureAnchorTests
    {
        private static ZoneControlCaptureAnchorSO CreateSO(int anchorBonus = 80)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureAnchorSO>();
            typeof(ZoneControlCaptureAnchorSO)
                .GetField("_anchorBonus", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, anchorBonus);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureAnchorController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureAnchorController>();
        }

        [Test]
        public void SO_FreshInstance_IsAnchored_False()
        {
            var so = CreateSO();
            Assert.That(so.IsAnchored, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_AnchorsSet_Zero()
        {
            var so = CreateSO();
            Assert.That(so.AnchorsSet, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_FirstCapture_SetsAnchor()
        {
            var so = CreateSO();
            so.RecordPlayerCapture();
            Assert.That(so.IsAnchored,  Is.True);
            Assert.That(so.AnchorsSet,  Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_FirstCapture_ReturnsZero()
        {
            var so    = CreateSO(anchorBonus: 80);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_Anchored_ReturnsBonus()
        {
            var so = CreateSO(anchorBonus: 80);
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(80));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_Anchored_AccumulatesChain()
        {
            var so = CreateSO(anchorBonus: 50);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.AnchorChainLength, Is.EqualTo(2));
            Assert.That(so.TotalAnchorBonus,  Is.EqualTo(100));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_Anchored_FiresEvent()
        {
            var so    = CreateSO();
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureAnchorSO)
                .GetField("_onAnchorBonus", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_RecordBotCapture_BreaksAnchor()
        {
            var so = CreateSO();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.IsAnchored,        Is.False);
            Assert.That(so.AnchorChainLength, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_WhenNotAnchored_NoEffect()
        {
            var so = CreateSO();
            Assert.DoesNotThrow(() => so.RecordBotCapture());
            Assert.That(so.IsAnchored, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_FiresEvent()
        {
            var so    = CreateSO();
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureAnchorSO)
                .GetField("_onAnchorBroken", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(anchorBonus: 100);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.IsAnchored,        Is.False);
            Assert.That(so.AnchorChainLength, Is.EqualTo(0));
            Assert.That(so.TotalAnchorBonus,  Is.EqualTo(0));
            Assert.That(so.AnchorsSet,        Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_AnchorSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.AnchorSO, Is.Null);
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
            typeof(ZoneControlCaptureAnchorController)
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
