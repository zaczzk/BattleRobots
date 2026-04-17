using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlReplayAnnotationTests
    {
        private static ZoneControlReplayAnnotationSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlReplayAnnotationSO>();

        private static ZoneControlReplayAnnotationController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlReplayAnnotationController>();
        }

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_AnnotationCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.AnnotationCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AddAnnotation_IncrementsCount()
        {
            var so = CreateSO();
            so.AddAnnotation(1f, "Note A");
            Assert.That(so.AnnotationCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AddAnnotation_EmptyNote_Ignored()
        {
            var so = CreateSO();
            so.AddAnnotation(1f, "");
            so.AddAnnotation(1f, null);
            Assert.That(so.AnnotationCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AddAnnotation_EvictsOldest_WhenFull()
        {
            var so  = CreateSO();
            int max = so.MaxAnnotations;
            for (int i = 0; i < max; i++) so.AddAnnotation(i, $"Note {i}");
            so.AddAnnotation(99f, "New Note");
            Assert.That(so.AnnotationCount, Is.EqualTo(max));
            Assert.That(so.GetAnnotations()[max - 1].Note, Is.EqualTo("New Note"));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AddAnnotation_FiresEvent()
        {
            var so      = CreateSO();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlReplayAnnotationSO)
                .GetField("_onAnnotationAdded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, channel);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            so.AddAnnotation(1f, "Test");

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void SO_GetAnnotations_ReturnsAllEntries()
        {
            var so = CreateSO();
            so.AddAnnotation(1f, "A");
            so.AddAnnotation(2f, "B");
            var list = so.GetAnnotations();
            Assert.That(list.Count, Is.EqualTo(2));
            Assert.That(list[0].Note, Is.EqualTo("A"));
            Assert.That(list[1].Note, Is.EqualTo("B"));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            so.AddAnnotation(1f, "Note");
            so.Reset();
            Assert.That(so.AnnotationCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        // ── Controller tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_AnnotationSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.AnnotationSO, Is.Null);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(false));
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(true));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(false));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channel()
        {
            var ctrl    = CreateController();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlReplayAnnotationController)
                .GetField("_onAnnotationAdded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, channel);

            ctrl.gameObject.SetActive(true);
            ctrl.gameObject.SetActive(false);

            int fired = 0;
            channel.RegisterCallback(() => fired++);
            channel.Raise();

            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void Controller_Refresh_NullAnnotationSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            panel.SetActive(true);
            typeof(ZoneControlReplayAnnotationController)
                .GetField("_panel", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(ctrl, panel);

            ctrl.Refresh();
            Assert.That(panel.activeSelf, Is.False);

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }
    }
}
