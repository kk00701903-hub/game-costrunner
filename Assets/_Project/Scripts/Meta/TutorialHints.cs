using UnityEngine;
using UnityEngine.UI;

/// Max four gesture hints in the whole game. Never used during the king fight.
public static class TutorialHints
{
    public enum Id
    {
        None,
        Steer,
        Jump,
        Slide,
        Item
    }

    public static bool SuppressAll { get; set; }

    private static Id _current;
    private static float _until;
    private static readonly bool[] ShownThisRun = new bool[5];

    public static void ResetRun()
    {
        for (int i = 0; i < ShownThisRun.Length; i++)
            ShownThisRun[i] = false;
        Hide();
    }

    public static void Show(Id id)
    {
        if (SuppressAll || id == Id.None)
            return;
        if (ShownThisRun[(int)id])
            return;

        ShownThisRun[(int)id] = true;
        _current = id;
        _until = Time.time + 1.6f;

        if (UIManager.Instance != null)
            UIManager.Instance.ShowTutorialHint(Label(id), 1.6f);
    }

    public static void Hide()
    {
        _current = Id.None;
        _until = 0f;
        if (UIManager.Instance != null)
            UIManager.Instance.HideTutorialHint();
    }

    public static void Tick()
    {
        if (_current == Id.None)
            return;
        if (Time.time < _until)
            return;
        Hide();
    }

    private static string Label(Id id)
    {
        // Max 12 Korean characters. Gesture ghost is drawn by UI.
        switch (id)
        {
            case Id.Steer:
                return "← → 피하기";
            case Id.Jump:
                return "↑ 점프";
            case Id.Slide:
                return "↓ 숙이기";
            case Id.Item:
                return "탭 아이템";
            default:
                return "";
        }
    }
}
