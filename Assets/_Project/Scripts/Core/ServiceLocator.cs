using System;
using System.Collections.Generic;

/// Tiny service map so gameplay systems stop FindObjectOfType-ing each other.
public static class ServiceLocator
{
    private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>(16);

    public static void Register<T>(T instance) where T : class
    {
        if (instance == null)
            return;
        _services[typeof(T)] = instance;
    }

    public static void Unregister<T>(T instance) where T : class
    {
        if (instance == null)
            return;

        object existing;
        if (_services.TryGetValue(typeof(T), out existing) && ReferenceEquals(existing, instance))
            _services.Remove(typeof(T));
    }

    public static T Get<T>() where T : class
    {
        object value;
        return _services.TryGetValue(typeof(T), out value) ? value as T : null;
    }

    public static bool TryGet<T>(out T service) where T : class
    {
        object value;
        if (_services.TryGetValue(typeof(T), out value))
        {
            service = value as T;
            return service != null;
        }

        service = null;
        return false;
    }

    public static void Clear()
    {
        _services.Clear();
    }
}
