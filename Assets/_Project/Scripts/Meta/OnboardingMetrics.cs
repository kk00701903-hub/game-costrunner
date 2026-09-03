using System.Collections.Generic;
using UnityEngine;

/// Lightweight telemetry for the onboarding brief §11. Local only for now.
public static class OnboardingMetrics
{
    private static readonly Dictionary<string, int> Counts = new Dictionary<string, int>();
    private static float _firstInputAt = -1f;
    private static float _prologueShownAt = -1f;
    private static float _firstDeathAt = -1f;
    private static int _kingCycle2Counters;
    private static int _kingHintFired;

    public static void Note(string key)
    {
        int n;
        Counts.TryGetValue(key, out n);
        Counts[key] = n + 1;
        GameLog.Verbose("metric:" + key + "=" + Counts[key]);
    }

    public static void PrologueShown()
    {
        _prologueShownAt = Time.unscaledTime;
        Note("prologue_shown");
    }

    public static void FirstInput()
    {
        if (_firstInputAt > 0f)
            return;
        _firstInputAt = Time.unscaledTime;
        Note("first_input");
    }

    public static void TutorialComplete()
    {
        Note("tutorial_complete");
    }

    public static void FirstDeath()
    {
        if (_firstDeathAt < 0f)
            _firstDeathAt = Time.unscaledTime;
        Note("first_death");
    }

    public static void KingCounter(int onboardingCycle)
    {
        Note("king_counter_c" + onboardingCycle);
        if (onboardingCycle == 2)
            _kingCycle2Counters++;
    }

    public static void KingHint()
    {
        _kingHintFired++;
        Note("king_hint");
    }

    public static float SecondsToFirstInput =>
        _prologueShownAt > 0f && _firstInputAt > 0f ? _firstInputAt - _prologueShownAt : -1f;

    public static float SecondsToFirstDeath =>
        _prologueShownAt > 0f && _firstDeathAt > 0f ? _firstDeathAt - _prologueShownAt : -1f;
}
