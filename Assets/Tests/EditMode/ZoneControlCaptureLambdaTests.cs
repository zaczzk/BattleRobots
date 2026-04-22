using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureLambdaTests
    {
        private static ZoneControlCaptureLambdaSO CreateSO(
            int lambdasNeeded    = 5,
            int removePerBot     = 1,
            int bonusPerExecution = 2185)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureLambdaSO>();
            typeof(ZoneControlCaptureLambdaSO)
                .GetField("_lambdasNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, lambdasNeeded);
            typeof(ZoneControlCaptureLambdaSO)
                .GetField("_removePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, removePerBot);
            typeof(ZoneControlCaptureLambdaSO)
                .GetField("_bonusPerExecution", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerExecution);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureLambdaController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureLambdaController>();
        }

        [Test]
        public void SO_FreshInstance_Lambdas_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Lambdas, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_ExecutionCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.ExecutionCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesLambdas()
        {
            var so = CreateSO(lambdasNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Lambdas, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(lambdasNeeded: 3, bonusPerExecution: 2185);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,              Is.EqualTo(2185));
            Assert.That(so.ExecutionCount,  Is.EqualTo(1));
            Assert.That(so.Lambdas,         Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(lambdasNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesLambdas()
        {
            var so = CreateSO(lambdasNeeded: 5, removePerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Lambdas, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(lambdasNeeded: 5, removePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Lambdas, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_LambdaProgress_Clamped()
        {
            var so = CreateSO(lambdasNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.LambdaProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnLambdaExecuted_FiresEvent()
        {
            var so    = CreateSO(lambdasNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureLambdaSO)
                .GetField("_onLambdaExecuted", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(lambdasNeeded: 2, bonusPerExecution: 2185);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Lambdas,           Is.EqualTo(0));
            Assert.That(so.ExecutionCount,    Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleExecutions_Accumulate()
        {
            var so = CreateSO(lambdasNeeded: 2, bonusPerExecution: 2185);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.ExecutionCount,    Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(4370));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_LambdaSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.LambdaSO, Is.Null);
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
            typeof(ZoneControlCaptureLambdaController)
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
