using UnityEngine;

/// Release-strippable logging. Prefer this over Debug.Log in gameplay code.
public static class GameLog
{
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void Verbose(string message)
    {
        Debug.Log(message);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void Warn(string message)
    {
        Debug.LogWarning(message);
    }

    public static void Error(string message)
    {
        Debug.LogError(message);
    }
}
