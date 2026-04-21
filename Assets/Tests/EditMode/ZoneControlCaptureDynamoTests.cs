using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureDynamoTests
    {
        private static ZoneControlCaptureDynamoSO CreateSO(
            int fieldsNeeded      = 6,
            int slipPerBot        = 2,
            int bonusPerGeneration = 1420)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureDynamoSO>();
            typeof(ZoneControlCaptureDynamoSO)
                .GetField("_fieldsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, fieldsNeeded);
            typeof(ZoneControlCaptureDynamoSO)
                .GetField("_slipPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, slipPerBot);
            typeof(ZoneControlCaptureDynamoSO)
                .GetField("_bonusPerGeneration", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerGeneration);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureDynamoController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureDynamoController>();
        }

        [Test]
        public void SO_FreshInstance_Fields_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Fields, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_GenerationCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.GenerationCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesFields()
        {
            var so = CreateSO(fieldsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.Fields, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_FieldsAtThreshold()
        {
            var so    = CreateSO(fieldsNeeded: 3, bonusPerGeneration: 1420);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,               Is.EqualTo(1420));
            Assert.That(so.GenerationCount,  Is.EqualTo(1));
            Assert.That(so.Fields,           Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(fieldsNeeded: 6);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesFields()
        {
            var so = CreateSO(fieldsNeeded: 6, slipPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Fields, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(fieldsNeeded: 6, slipPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Fields, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FieldProgress_Clamped()
        {
            var so = CreateSO(fieldsNeeded: 6);
            so.RecordPlayerCapture();
            Assert.That(so.FieldProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnDynamoGenerated_FiresEvent()
        {
            var so    = CreateSO(fieldsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureDynamoSO)
                .GetField("_onDynamoGenerated", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(fieldsNeeded: 2, bonusPerGeneration: 1420);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Fields,            Is.EqualTo(0));
            Assert.That(so.GenerationCount,   Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleGenerations_Accumulate()
        {
            var so = CreateSO(fieldsNeeded: 2, bonusPerGeneration: 1420);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.GenerationCount,   Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(2840));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_DynamoSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.DynamoSO, Is.Null);
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
            typeof(ZoneControlCaptureDynamoController)
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
