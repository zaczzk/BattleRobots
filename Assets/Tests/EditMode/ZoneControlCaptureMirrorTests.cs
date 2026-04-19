using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureMirrorTests
    {
        private static ZoneControlCaptureMirrorSO CreateSO(int bonus = 175)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureMirrorSO>();
            typeof(ZoneControlCaptureMirrorSO)
                .GetField("_bonusPerMirror", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonus);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureMirrorController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureMirrorController>();
        }

        [Test]
        public void SO_FreshInstance_MirrorCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.MirrorCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_IsTied_False()
        {
            var so = CreateSO();
            Assert.That(so.IsTied, Is.False);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_IncreasesCount()
        {
            var so = CreateSO();
            so.RecordPlayerCapture();
            Assert.That(so.PlayerCaptures, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IncreasesCount()
        {
            var so = CreateSO();
            so.RecordBotCapture();
            Assert.That(so.BotCaptures, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_IsTied_True_WhenCapturesEqual()
        {
            var so = CreateSO();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.IsTied, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EvaluateMirror_FiresOnFreshTie()
        {
            var so    = CreateSO(bonus: 175);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureMirrorSO)
                .GetField("_onMirrorHit", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(fired,          Is.EqualTo(1));
            Assert.That(so.MirrorCount, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_EvaluateMirror_Idempotent_WhenAlreadyTied()
        {
            var so    = CreateSO();
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureMirrorSO)
                .GetField("_onMirrorHit", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            so.RecordBotCapture();
            Assert.That(fired, Is.EqualTo(1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_EvaluateMirror_AccumulatesBonus()
        {
            var so = CreateSO(bonus: 100);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(100));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EvaluateMirror_ReTriggers_OnNewTie()
        {
            var so    = CreateSO();
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureMirrorSO)
                .GetField("_onMirrorHit", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, evt);
            evt.RegisterCallback(() => fired++);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(fired,          Is.EqualTo(2));
            Assert.That(so.MirrorCount, Is.EqualTo(2));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_Reset_ClearsAll()
        {
            var so = CreateSO(bonus: 100);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            so.Reset();
            Assert.That(so.PlayerCaptures,    Is.EqualTo(0));
            Assert.That(so.BotCaptures,       Is.EqualTo(0));
            Assert.That(so.MirrorCount,       Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_MirrorSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.MirrorSO, Is.Null);
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
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            ctrl.gameObject.SetActive(true);
            Assert.DoesNotThrow(() => ctrl.gameObject.SetActive(false));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_Refresh_NullSO_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject();
            typeof(ZoneControlCaptureMirrorController)
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
