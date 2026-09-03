using System;
using System.Collections.Generic;
using UnityEngine;

/// Pre-warmed pool. Acquire/Release only — never Instantiate or Destroy during a run.
public sealed class ObjectPool<T> where T : Component
{
    private readonly T _prefab;
    private readonly Transform _parent;
    private readonly Stack<T> _free = new Stack<T>(32);
    private readonly HashSet<T> _live = new HashSet<T>();
    private readonly Action<T> _onAcquire;
    private readonly Action<T> _onRelease;
    private readonly Func<T> _factory;

    public int LiveCount => _live.Count;
    public int FreeCount => _free.Count;

    public ObjectPool(T prefab, Transform parent, int warm, Action<T> onAcquire = null, Action<T> onRelease = null)
    {
        _prefab = prefab;
        _parent = parent;
        _onAcquire = onAcquire;
        _onRelease = onRelease;
        Warm(warm);
    }

    /// Prefab-free pool for procedurally built objects.
    public ObjectPool(Func<T> factory, Transform parent, int warm, Action<T> onAcquire = null, Action<T> onRelease = null)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _parent = parent;
        _onAcquire = onAcquire;
        _onRelease = onRelease;
        Warm(warm);
    }

    public void Warm(int count)
    {
        for (int i = 0; i < count; i++)
        {
            T item = Create();
            item.gameObject.SetActive(false);
            _free.Push(item);
        }
    }

    public T Acquire()
    {
        T item = _free.Count > 0 ? _free.Pop() : Create();
        item.gameObject.SetActive(true);
        _live.Add(item);
        _onAcquire?.Invoke(item);
        return item;
    }

    public void Release(T item)
    {
        if (item == null || !_live.Remove(item))
            return;

        _onRelease?.Invoke(item);
        item.gameObject.SetActive(false);
        if (_parent != null)
            item.transform.SetParent(_parent, false);
        _free.Push(item);
    }

    public void ReleaseAll()
    {
        if (_live.Count == 0)
            return;

        _scratch.Clear();
        foreach (T item in _live)
            _scratch.Add(item);

        for (int i = 0; i < _scratch.Count; i++)
            Release(_scratch[i]);
    }

    private readonly List<T> _scratch = new List<T>(32);

    private T Create()
    {
        T item;
        if (_factory != null)
        {
            item = _factory();
        }
        else
        {
            item = UnityEngine.Object.Instantiate(_prefab, _parent);
        }

        item.gameObject.name = (_prefab != null ? _prefab.name : typeof(T).Name) + "_pooled";
        return item;
    }
}
