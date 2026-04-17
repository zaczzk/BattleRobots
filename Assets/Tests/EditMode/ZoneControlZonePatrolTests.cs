using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for T375: <see cref="ZoneControlZonePatrolSO"/> and
    /// <see cref="ZoneControlZonePatrolController"/>.
    ///
    /// ZoneControlZonePatrolTests (12):
    ///   SO_FreshInstance_VisitedCount_Zero                       ×1
    ///   SO_FreshInstance_AllVisited_False                        ×1
    ///   SO_RecordVisit_NewIndex_IncrementsCount                  ×1
    ///   SO_RecordVisit_DuplicateIndex_NoDoubleCount              ×1
    ///   SO_RecordVisit_OutOfRange_Ignored                        ×1
    ///   SO_RecordVisit_AllZones_SetsAllVisited                   ×1
    ///   SO_Reset_ClearsAll                                       ×1
    ///   Controller_FreshInstance_PatrolSO_Null                   ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow               ×1
    ///   Controller_OnDisable_Unregisters_Channel                 ×1
    ///   Controller_Refresh_NullPatrolSO_HidesPanel               ×1
    /// </summary>
    public sealed class ZoneControlZonePatrolTests
    {
        private static ZoneControlZonePatrolSO CreateSO() =>
            ScriptableObject.CreateInstance<ZoneControlZonePatrolSO>();

        private static ZoneControlZonePatrolController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlZonePatrolController>();
        }

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_VisitedCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.VisitedCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_AllVisited_False()
        {
            var so = CreateSO();
            Assert.That(so.AllVisited, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordVisit_NewIndex_IncrementsCount()
        {
            var so = CreateSO();
            so.RecordVisit(0);
            Assert.That(so.VisitedCount, Is.EqualTo(1));
            Assert.That(so.HasVisited(0), Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordVisit_DuplicateIndex_NoDoubleCount()
        {
            var so = CreateSO();
            so.RecordVisit(0);
            so.RecordVisit(0);
            Assert.That(so.VisitedCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordVisit_OutOfRange_Ignored()
        {
            var so = CreateSO();
            so.RecordVisit(-1);
            so.RecordVisit(so.TotalZones);
            Assert.That(so.VisitedCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordVisit_AllZones_SetsAllVisited()
        {
            var so = CreateSO();
            for (int i = 0; i < so.TotalZones; i++)
                so.RecordVisit(i);

            Assert.That(so.AllVisited, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO();
            for (int i = 0; i < so.TotalZones; i++)
                so.RecordVisit(i);

            so.Reset();

            Assert.That(so.VisitedCount, Is.EqualTo(0));
            Assert.That(so.AllVisited,   Is.False);
            Object.DestroyImmediate(so);
        }

        // ── Controller tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_PatrolSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.PatrolSO, Is.Null);
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
            ctrl.gameObject.SetActive(true);
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(false));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channel()
        {
            var ctrl    = CreateController();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlZonePatrolController)
                .GetField("_onNewZoneVisited", BindingFlags.NonPublic | BindingFlags.Instance)
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
        public void Controller_Refresh_NullPatrolSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlZonePatrolController)
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
