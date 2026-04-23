using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureInternalTests
    {
        private static ZoneControlCaptureInternalSO CreateSO(
            int objectsNeeded     = 6,
            int destructionPerBot = 2,
            int bonusPerInternal  = 3040)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureInternalSO>();
            typeof(ZoneControlCaptureInternalSO)
                .GetField("_objectsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, objectsNeeded);
            typeof(ZoneControlCaptureInternalSO)
                .GetField("_destructionPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, destructionPerBot);
            typeof(ZoneControlCaptureInternalSO)
                .GetField("_bonusPerInternal", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerInternal);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureInternalController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureInternalController>();
        }

        [Test]
        public void SO_FreshInstance_Objects_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Objects, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_InternalCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.InternalCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesObjects()
        {
            var so = CreateSO(objectsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Objects, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(objectsNeeded: 3, bonusPerInternal: 3040);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,              Is.EqualTo(3040));
            Assert.That(so.InternalCount,   Is.EqualTo(1));
            Assert.That(so.Objects,         Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(objectsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesObjects()
        {
            var so = CreateSO(objectsNeeded: 6, destructionPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Objects, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(objectsNeeded: 6, destructionPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Objects, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_ObjectProgress_Clamped()
        {
            var so = CreateSO(objectsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.ObjectProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnInternalCategoryBuilt_FiresEvent()
        {
            var so    = CreateSO(objectsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureInternalSO)
                .GetField("_onInternalCategoryBuilt", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(objectsNeeded: 2, bonusPerInternal: 3040);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Objects,           Is.EqualTo(0));
            Assert.That(so.InternalCount,     Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleInternal_Accumulate()
        {
            var so = CreateSO(objectsNeeded: 2, bonusPerInternal: 3040);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.InternalCount,     Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(6080));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_InternalSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.InternalSO, Is.Null);
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
            typeof(ZoneControlCaptureInternalController)
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
