using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class GameConfigTests
{
    [Test]
    public void Active_NeverNull()
    {
        Assert.IsNotNull(GameConfig.Active);
    }

    [Test]
    public void Defaults_MatchRunnerConstraints()
    {
        GameConfig cfg = ScriptableObject.CreateInstance<GameConfig>();
        Assert.AreEqual(3, cfg.maxHp);
        Assert.GreaterOrEqual(cfg.minTelegraphSec, 0.45f);
        Assert.AreEqual(0.18f, cfg.laneChangeSeconds, 0.0001f);
        Assert.IsFalse(cfg.use3DCharacter);
    }

    [Test]
    public void TelegraphLeadMetres_ScalesWithSpeed()
    {
        GameConfig cfg = ScriptableObject.CreateInstance<GameConfig>();
        cfg.minTelegraphSec = 0.45f;
        float slow = cfg.TelegraphLeadMetres(10f, 1f / 60f);
        float fast = cfg.TelegraphLeadMetres(20f, 1f / 60f);
        Assert.AreEqual(4.5f, slow, 0.01f);
        Assert.AreEqual(9f, fast, 0.01f);
        Assert.Greater(fast, slow);
    }

    [Test]
    public void MinTelegraph_FloorIsPointFourFive()
    {
        GameConfig cfg = ScriptableObject.CreateInstance<GameConfig>();
        Assert.GreaterOrEqual(cfg.minTelegraphSec, 0.45f);
        float lead = cfg.TelegraphLeadMetres(1f, 1f / 60f);
        Assert.GreaterOrEqual(lead, 0.45f);
    }
}
