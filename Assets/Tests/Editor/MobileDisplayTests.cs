using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class MobileDisplayTests
{
    [Test]
    public void GalaxyS26_ResolutionIs1080x2340()
    {
        Assert.AreEqual(1080, MobileDisplay.Width);
        Assert.AreEqual(2340, MobileDisplay.Height);
        Assert.AreEqual("Galaxy S26", MobileDisplay.DeviceName);
    }

    [Test]
    public void Aspect_Is19_5To9()
    {
        float ratio = MobileDisplay.Height / (float)MobileDisplay.Width;
        Assert.AreEqual(MobileDisplay.AspectHeight / MobileDisplay.AspectWidth, ratio, 0.001f);
    }

    [Test]
    public void Reference_MatchesWidthHeight()
    {
        Assert.AreEqual(new Vector2(1080f, 2340f), MobileDisplay.Reference);
    }

    [Test]
    public void PortraitViewport_PillarboxesLandscapeWindow()
    {
        Rect r = PortraitViewport.ComputeNormalizedRect(1920, 1080);
        Assert.Less(r.width, 1f);
        Assert.AreEqual(0f, r.y, 0.001f);
        Assert.AreEqual(0.5f, r.x + r.width * 0.5f, 0.02f);
    }

    [Test]
    public void PortraitViewport_FullScreenOnTargetAspect()
    {
        Rect r = PortraitViewport.ComputeNormalizedRect(MobileDisplay.Width, MobileDisplay.Height);
        Assert.AreEqual(1f, r.width, 0.001f);
        Assert.AreEqual(1f, r.height, 0.001f);
    }
}
