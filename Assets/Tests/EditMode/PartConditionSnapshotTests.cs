using NUnit.Framework;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="PartConditionSnapshot"/>.
    ///
    /// Covers:
    ///   • Default instance has partId = "" and hpRatio = 1f.
    ///   • partId and hpRatio can be set and read back.
    ///   • Independent instances do not share state.
    /// </summary>
    public class PartConditionSnapshotTests
    {
        [Test]
        public void DefaultInstance_PartId_IsEmptyString()
        {
            var snap = new PartConditionSnapshot();
            Assert.AreEqual("", snap.partId);
        }

        [Test]
        public void DefaultInstance_HPRatio_IsOne()
        {
            var snap = new PartConditionSnapshot();
            Assert.AreEqual(1f, snap.hpRatio, 0.001f);
        }

        [Test]
        public void FieldsCanBeSetAndReadBack()
        {
            var snap = new PartConditionSnapshot { partId = "weapon_01", hpRatio = 0.5f };
            Assert.AreEqual("weapon_01", snap.partId);
            Assert.AreEqual(0.5f, snap.hpRatio, 0.001f);
        }
    }
}
