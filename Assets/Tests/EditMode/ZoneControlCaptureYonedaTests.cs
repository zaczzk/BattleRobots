using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureYonedaTests
    {
        private static ZoneControlCaptureYonedaSO CreateSO(
            int embedsNeeded  = 5,
            int dissolvePerBot = 1,
            int bonusPerEmbed = 2425)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureYonedaSO>();
            typeof(ZoneControlCaptureYonedaSO)
                .GetField("_embedsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, embedsNeeded);
            typeof(ZoneControlCaptureYonedaSO)
                .GetField("_dissolvePerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, dissolvePerBot);
            typeof(ZoneControlCaptureYonedaSO)
                .GetField("_bonusPerEmbed", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerEmbed);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureYonedaController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureYonedaController>();
        }

        [Test]
        public void SO_FreshInstance_Embeds_Zero()
        {
            var so = CreateSO();
            Assert.That(so.Embeds, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_EmbedCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.EmbedCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesEmbeds()
        {
            var so = CreateSO(embedsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.Embeds, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(embedsNeeded: 3, bonusPerEmbed: 2425);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,          Is.EqualTo(2425));
            Assert.That(so.EmbedCount,  Is.EqualTo(1));
            Assert.That(so.Embeds,      Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(embedsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_RemovesEmbeds()
        {
            var so = CreateSO(embedsNeeded: 5, dissolvePerBot: 1);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Embeds, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(embedsNeeded: 5, dissolvePerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.Embeds, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_EmbedProgress_Clamped()
        {
            var so = CreateSO(embedsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.EmbedProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnYonedaEmbedded_FiresEvent()
        {
            var so    = CreateSO(embedsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureYonedaSO)
                .GetField("_onYonedaEmbedded", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(embedsNeeded: 2, bonusPerEmbed: 2425);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.Embeds,            Is.EqualTo(0));
            Assert.That(so.EmbedCount,        Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleEmbeddings_Accumulate()
        {
            var so = CreateSO(embedsNeeded: 2, bonusPerEmbed: 2425);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.EmbedCount,        Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(4850));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_YonedaSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.YonedaSO, Is.Null);
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
            typeof(ZoneControlCaptureYonedaController)
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
