using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureLinearTypeTests
    {
        private static ZoneControlCaptureLinearTypeSO CreateSO(
            int linearUsesNeeded          = 6,
            int resourceDuplicationsPerBot = 1,
            int bonusPerLinearUse         = 5065)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureLinearTypeSO>();
            typeof(ZoneControlCaptureLinearTypeSO)
                .GetField("_linearUsesNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, linearUsesNeeded);
            typeof(ZoneControlCaptureLinearTypeSO)
                .GetField("_resourceDuplicationsPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, resourceDuplicationsPerBot);
            typeof(ZoneControlCaptureLinearTypeSO)
                .GetField("_bonusPerLinearUse", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerLinearUse);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureLinearTypeController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureLinearTypeController>();
        }

        [Test]
        public void SO_FreshInstance_LinearUses_Zero()
        {
            var so = CreateSO();
            Assert.That(so.LinearUses, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_LinearUseCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.LinearUseCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesLinearUses()
        {
            var so = CreateSO(linearUsesNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.LinearUses, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(linearUsesNeeded: 3, bonusPerLinearUse: 5065);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,              Is.EqualTo(5065));
            Assert.That(so.LinearUseCount,  Is.EqualTo(1));
            Assert.That(so.LinearUses,      Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(linearUsesNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesResourceDuplications()
        {
            var so = CreateSO(linearUsesNeeded: 6, resourceDuplicationsPerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.LinearUses, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(linearUsesNeeded: 6, resourceDuplicationsPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.LinearUses, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_LinearUseProgress_Clamped()
        {
            var so = CreateSO(linearUsesNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.LinearUseProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnLinearTypeCompleted_FiresEvent()
        {
            var so    = CreateSO(linearUsesNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureLinearTypeSO)
                .GetField("_onLinearTypeCompleted", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(linearUsesNeeded: 2, bonusPerLinearUse: 5065);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.LinearUses,        Is.EqualTo(0));
            Assert.That(so.LinearUseCount,    Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleLinearUses_Accumulate()
        {
            var so = CreateSO(linearUsesNeeded: 2, bonusPerLinearUse: 5065);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.LinearUseCount,    Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(10130));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_LinearTypeSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.LinearTypeSO, Is.Null);
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
            typeof(ZoneControlCaptureLinearTypeController)
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
