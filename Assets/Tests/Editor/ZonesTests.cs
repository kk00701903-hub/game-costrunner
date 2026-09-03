using NUnit.Framework;

[TestFixture]
public class ZonesTests
{
    [Test]
    public void At_Zero_IsArcade()
    {
        Assert.AreEqual(Zone.Arcade, Zones.At(0f));
    }

    [Test]
    public void At_JustBeforeBoundary_StaysArcade()
    {
        Assert.AreEqual(Zone.Arcade, Zones.At(Zones.MetresPerZone - 0.01f));
    }

    [Test]
    public void At_Boundary_IsOverpass()
    {
        Assert.AreEqual(Zone.Overpass, Zones.At(Zones.MetresPerZone));
    }

    [Test]
    public void At_Far_ClampsToDepot()
    {
        Assert.AreEqual(Zone.Depot, Zones.At(Zones.MetresPerZone * 99f));
    }

    [Test]
    public void StartDistance_MatchesIndex()
    {
        Assert.AreEqual(0f, Zones.StartDistance(Zone.Arcade));
        Assert.AreEqual(Zones.MetresPerZone, Zones.StartDistance(Zone.Overpass));
        Assert.AreEqual(Zones.MetresPerZone * 4f, Zones.StartDistance(Zone.Depot));
    }

    [Test]
    public void Progress_AtStartOfZone_IsZero()
    {
        Assert.AreEqual(0f, Zones.Progress(0f), 0.0001f);
        Assert.AreEqual(0f, Zones.Progress(Zones.MetresPerZone), 0.0001f);
    }

    [Test]
    public void Label_ContainsZoneIndex()
    {
        StringAssert.Contains("구역 1", Zones.Label(Zone.Arcade));
        StringAssert.Contains("구역 5", Zones.Label(Zone.Depot));
    }
}
