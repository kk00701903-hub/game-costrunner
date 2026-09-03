using UnityEngine;

/// Animator parameter names/hashes kept in sync with PlayerController FSM.
public static class CharacterAnimParams
{
    public static readonly int Speed = Animator.StringToHash("Speed");
    public static readonly int Grounded = Animator.StringToHash("Grounded");
    public static readonly int Slide = Animator.StringToHash("Slide");
    public static readonly int Jump = Animator.StringToHash("Jump");
    public static readonly int Lean = Animator.StringToHash("Lean");
    public static readonly int Hurt = Animator.StringToHash("Hurt");
    public static readonly int Counter = Animator.StringToHash("Counter");
    public static readonly int Grind = Animator.StringToHash("Grind");
    public static readonly int WallRun = Animator.StringToHash("WallRun");
    public static readonly int Dead = Animator.StringToHash("Dead");
}
