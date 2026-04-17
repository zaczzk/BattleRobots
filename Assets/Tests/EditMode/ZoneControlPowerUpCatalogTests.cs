using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for <see cref="ZoneControlPowerUpCatalogSO"/> and
    /// <see cref="ZoneControlPowerUpCatalogController"/>.
    /// </summary>
    public sealed class ZoneControlPowerUpCatalogTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static ZoneControlPowerUpCatalogSO CreateCatalogSO() =>
            ScriptableObject.CreateInstance<ZoneControlPowerUpCatalogSO>();

        private static ZoneControlPowerUpSO CreatePowerUpSO() =>
            ScriptableObject.CreateInstance<ZoneControlPowerUpSO>();

        private static ZoneControlPowerUpCatalogController CreateController()
        {
            var go = new GameObject();
            return go.AddComponent<ZoneControlPowerUpCatalogController>();
        }

        // ── SO tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_EntryCount_Zero()
        {
            var so = CreateCatalogSO();
            Assert.That(so.EntryCount, Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_GetPowerUp_ValidIndex_ReturnsEntry()
        {
            var so    = CreateCatalogSO();
            var entry = CreatePowerUpSO();
            // Inject via reflection
            var field = typeof(ZoneControlPowerUpCatalogSO).GetField(
                "_powerUps", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(so, new ZoneControlPowerUpSO[] { entry });

            Assert.That(so.GetPowerUp(0), Is.EqualTo(entry));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(entry);
        }

        [Test]
        public void SO_GetPowerUp_OutOfRange_ReturnsNull()
        {
            var so = CreateCatalogSO();
            Assert.That(so.GetPowerUp(0), Is.Null);
            Assert.That(so.GetPowerUp(-1), Is.Null);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SelectRandom_NullArray_ReturnsNull()
        {
            var so = CreateCatalogSO();
            Assert.That(so.SelectRandom(), Is.Null);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_SelectRandom_ValidArray_ReturnsEntry()
        {
            var so    = CreateCatalogSO();
            var entry = CreatePowerUpSO();
            var field = typeof(ZoneControlPowerUpCatalogSO).GetField(
                "_powerUps", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(so, new ZoneControlPowerUpSO[] { entry });

            var result = so.SelectRandom();
            Assert.That(result, Is.EqualTo(entry));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(entry);
        }

        [Test]
        public void SO_Reset_SetsLastIndexToMinusOne()
        {
            var so    = CreateCatalogSO();
            var entry = CreatePowerUpSO();
            var field = typeof(ZoneControlPowerUpCatalogSO).GetField(
                "_powerUps", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(so, new ZoneControlPowerUpSO[] { entry });
            so.SelectRandom();

            so.Reset();

            Assert.That(so.LastSelectedIndex, Is.EqualTo(-1));
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(entry);
        }

        // ── Controller tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_CatalogSO_Null()
        {
            var ctrl = CreateController();
            Assert.That(ctrl.CatalogSO, Is.Null);
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
        public void Controller_OnDisable_Unregisters_Channels()
        {
            var ctrl    = CreateController();
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            var field   = typeof(ZoneControlPowerUpCatalogController).GetField(
                "_onSpawnPowerUp", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(ctrl, channel);

            ctrl.gameObject.SetActive(true);

            int count = 0;
            channel.RegisterCallback(() => count++);
            ctrl.gameObject.SetActive(false);
            channel.Raise();

            Assert.That(count, Is.EqualTo(1)); // only the external listener fires
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(channel);
        }

        [Test]
        public void Controller_HandleSpawnPowerUp_NullCatalog_DoesNotThrow()
        {
            var ctrl = CreateController();
            Assert.DoesNotThrow(() => ctrl.HandleSpawnPowerUp());
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_HandleMatchStarted_NullCatalog_DoesNotThrow()
        {
            var ctrl = CreateController();
            Assert.DoesNotThrow(() => ctrl.HandleMatchStarted());
            Object.DestroyImmediate(ctrl.gameObject);
        }
    }
}
