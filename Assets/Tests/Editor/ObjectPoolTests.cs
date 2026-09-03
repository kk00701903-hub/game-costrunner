using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class ObjectPoolTests
{
    private Transform _root;

    [SetUp]
    public void SetUp()
    {
        _root = new GameObject("PoolRoot").transform;
    }

    [TearDown]
    public void TearDown()
    {
        if (_root != null)
            Object.DestroyImmediate(_root.gameObject);
    }

    [Test]
    public void Warm_FillsFreeCount()
    {
        var pool = new ObjectPool<Transform>(() => new GameObject("Pooled").transform, _root, 3);
        Assert.AreEqual(3, pool.FreeCount);
        Assert.AreEqual(0, pool.LiveCount);
    }

    [Test]
    public void AcquireRelease_RecyclesInstance()
    {
        var pool = new ObjectPool<Transform>(() => new GameObject("Pooled").transform, _root, 1);
        Transform a = pool.Acquire();
        Assert.AreEqual(1, pool.LiveCount);
        Assert.AreEqual(0, pool.FreeCount);

        pool.Release(a);
        Assert.AreEqual(0, pool.LiveCount);
        Assert.AreEqual(1, pool.FreeCount);

        Transform b = pool.Acquire();
        Assert.AreSame(a, b);
    }

    [Test]
    public void ReleaseAll_ReturnsLiveToFree()
    {
        var pool = new ObjectPool<Transform>(() => new GameObject("Pooled").transform, _root, 0);
        pool.Acquire();
        pool.Acquire();
        Assert.AreEqual(2, pool.LiveCount);
        pool.ReleaseAll();
        Assert.AreEqual(0, pool.LiveCount);
        Assert.AreEqual(2, pool.FreeCount);
    }
}
