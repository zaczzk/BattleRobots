using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureComonadTests
    {
        private static ZoneControlCaptureComonadSO CreateSO(
            int contextsNeeded  = 6,
            int collapsePerBot  = 2,
            int bonusPerExtract = 2260)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureComonadSO>();
            typeof(ZoneControlCaptureComonadSO)
                .GetField("_contextsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, contextsNeeded);
            typeof(ZoneControlCaptureComonadSO)
                .GetField("_collapsePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, collapsePerBot);
            typeof(ZoneControlCaptureComonadSO)
                .GetField("_bonusPerExtract", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerExtract);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureComonadController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureComonadController>();
        }

        [Test]
        public void SO_FreshInstance_Contexts_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Contexts, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ExtractCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ExtractCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesContexts()
        {
            var so = CreateSO(contextsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Contexts, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(contextsNeeded: 3, bonusPerExtract: 2260);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,           Is.EqualTo(2260));
            Assert.That(so.ExtractCount, Is.EqualTo(1));
            Assert.That(so.Contexts,     Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(contextsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesContexts()
        {
            var so = CreateSO(contextsNeeded: 6, collapsePerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Contexts, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(contextsNeeded: 6, collapsePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Contexts, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ContextProgress_Clamped()
        {
            var so = CreateSO(contextsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.ContextProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnComonadExtracted_FiresEvent()
        {
            var so    = CreateSO(contextsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureComonadSO)
                .GetField("_onComonadExtracted", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(contextsNeeded: 2, bonusPerExtract: 2260);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Contexts,          Is.EqualTo(0));
            Assert.That(so.ExtractCount,      Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleExtracts_Accumulate()
        {
            var so = CreateSO(contextsNeeded: 2, bonusPerExtract: 2260);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ExtractCount,      Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(4520));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_ComonadSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.ComonadSO, Is.Null);
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
            typeof(ZoneControlCaptureComonadController)
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
