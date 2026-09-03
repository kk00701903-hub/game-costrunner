using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class AabbTests
{
    [Test]
    public void Overlaps_WhenBoxesTouch_ReturnsTrue()
    {
        Assert.IsTrue(Aabb.Overlaps(
            Vector3.zero, Vector3.one,
            new Vector3(0.9f, 0f, 0f), Vector3.one));
    }

    [Test]
    public void Overlaps_WhenSeparated_ReturnsFalse()
    {
        Assert.IsFalse(Aabb.Overlaps(
            Vector3.zero, Vector3.one,
            new Vector3(5f, 0f, 0f), Vector3.one));
    }

    [Test]
    public void OverlapsXZ_IgnoresHeightDifference()
    {
        Assert.IsTrue(Aabb.OverlapsXZ(
            new Vector3(0f, 0f, 0f), new Vector3(2f, 1f, 2f),
            new Vector3(0f, 50f, 0f), new Vector3(2f, 1f, 2f)));
    }
}
