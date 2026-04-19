using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureMirageTests
    {
        private static ZoneControlCaptureMirageSO CreateSO(int stackBonus = 25, int maxStacks = 8)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureMirageSO>();
            typeof(ZoneControlCaptureMirageSO)
                .GetField("_stackBonus", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, stackBonus);
            typeof(ZoneControlCaptureMirageSO)
                .GetField("_maxStacks", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, maxStacks);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureMirageController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureMirageController>();
        }

        [Test]
        public void SO_FreshInstance_CurrentStacks_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CurrentStacks, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_TotalMirageBonus_Zero()
        {
            var so = CreateSO();
            Assert.That(so.TotalMirageBonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_IncrementsStack()
        {
            var so = CreateSO(maxStacks: 8);
            so.RecordPlayerCapture();
            Assert.That(so.CurrentStacks, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsStackTimesBonus()
        {
            var so    = CreateSO(stackBonus: 10, maxStacks: 8);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(30));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ClampsAtMaxStacks()
        {
            var so = CreateSO(maxStacks: 3);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.CurrentStacks, Is.EqualTo(3));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtMaxStacks_FiresEventOnce()
        {
            var so    = CreateSO(maxStacks: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureMirageSO)
                .GetField("_onMirageStack", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int prevFired = fired;
            so.RecordPlayerCapture();
            Assert.That(fired, Is.EqualTo(prevFired));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesTotal()
        {
            var so = CreateSO(stackBonus: 10, maxStacks: 8);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.TotalMirageBonus, Is.EqualTo(30));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_FiresEvent_OnNewStack()
        {
            var so    = CreateSO(maxStacks: 8);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureMirageSO)
                .GetField("_onMirageStack", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordPlayerCapture();
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_RecordBotCapture_ClearsStacks()
        {
            var so = CreateSO();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.CurrentStacks, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_StackProgress_Computed()
        {
            var so = CreateSO(maxStacks: 4);
            Assert.That(so.StackProgress, Is.EqualTo(0f).Within(0.001f));
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.That(so.StackProgress, Is.EqualTo(0.5f).Within(0.001f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(stackBonus: 10, maxStacks: 8);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.CurrentStacks,    Is.EqualTo(0));
            Assert.That(so.TotalMirageBonus, Is.EqualTo(0));
            Assert.That(so.MirageCaptures,   Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_MirageSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.MirageSO, Is.Null);
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
            typeof(ZoneControlCaptureMirageController)
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
