using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests.EditMode
{
    public sealed class ZoneControlCaptureKolmogorovComplexityTests
    {
        private static ZoneControlCaptureKolmogorovComplexitySO CreateSO(
            int compressedDescriptionsNeeded = 5,
            int incompressibleStringsPerBot  = 2,
            int bonusPerDescription          = 4600)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlCaptureKolmogorovComplexitySO>();
            typeof(ZoneControlCaptureKolmogorovComplexitySO)
                .GetField("_compressedDescriptionsNeeded", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, compressedDescriptionsNeeded);
            typeof(ZoneControlCaptureKolmogorovComplexitySO)
                .GetField("_incompressibleStringsPerBot", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, incompressibleStringsPerBot);
            typeof(ZoneControlCaptureKolmogorovComplexitySO)
                .GetField("_bonusPerDescription", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(so, bonusPerDescription);
            so.Reset();
            return so;
        }

        private static ZoneControlCaptureKolmogorovComplexityController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlCaptureKolmogorovComplexityController>();
        }

        [Test]
        public void SO_FreshInstance_CompressedDescriptions_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CompressedDescriptions, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_CompressionCount_Zero()
        {
            var so = CreateSO();
            Assert.That(so.CompressionCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AccumulatesDescriptions()
        {
            var so = CreateSO(compressedDescriptionsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.CompressedDescriptions, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_AtThreshold_AwardsBonus()
        {
            var so    = CreateSO(compressedDescriptionsNeeded: 3, bonusPerDescription: 4600);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus,                 Is.EqualTo(4600));
            Assert.That(so.CompressionCount,   Is.EqualTo(1));
            Assert.That(so.CompressedDescriptions, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_ReturnsZeroWhileFilling()
        {
            var so    = CreateSO(compressedDescriptionsNeeded: 5);
            int bonus = so.RecordPlayerCapture();
            Assert.That(bonus, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IntroducesIncompressibleStrings()
        {
            var so = CreateSO(compressedDescriptionsNeeded: 5, incompressibleStringsPerBot: 2);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.CompressedDescriptions, Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_ClampsToZero()
        {
            var so = CreateSO(compressedDescriptionsNeeded: 5, incompressibleStringsPerBot: 10);
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.That(so.CompressedDescriptions, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_DescriptionProgress_Clamped()
        {
            var so = CreateSO(compressedDescriptionsNeeded: 5);
            so.RecordPlayerCapture();
            Assert.That(so.DescriptionProgress, Is.InRange(0f, 1f));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_OnDescriptionCompressed_FiresEvent()
        {
            var so    = CreateSO(compressedDescriptionsNeeded: 2);
            int fired = 0;
            var evt   = ScriptableObject.CreateInstance<VoidGameEvent>();
            typeof(ZoneControlCaptureKolmogorovComplexitySO)
                .GetField("_onDescriptionCompressed", BindingFlags.NonPublic | BindingFlags.Instance)
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
            var so = CreateSO(compressedDescriptionsNeeded: 2, bonusPerDescription: 4600);
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.Reset();
            Assert.That(so.CompressedDescriptions, Is.EqualTo(0));
            Assert.That(so.CompressionCount,       Is.EqualTo(0));
            Assert.That(so.TotalBonusAwarded,      Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_MultipleCompressions_Accumulate()
        {
            var so = CreateSO(compressedDescriptionsNeeded: 2, bonusPerDescription: 4600);
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            so.RecordPlayerCapture(); so.RecordPlayerCapture();
            Assert.That(so.CompressionCount,  Is.EqualTo(2));
            Assert.That(so.TotalBonusAwarded, Is.EqualTo(9200));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void Controller_FreshInstance_KolmogorovSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.KolmogorovSO, Is.Null);
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
            typeof(ZoneControlCaptureKolmogorovComplexityController)
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
