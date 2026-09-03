using UnityEngine;

[CreateAssetMenu(menuName = "347/KingPhase", fileName = "KingPhase")]
public class KingPhaseData : ScriptableObject
{
    public int hp = 3;
    public float cycleDuration = 4.0f;
    public float aimTime = 0.6f;
    public float throwTime = 0.5f;
    public float counterWindow = 1.4f;
    public float recoverTime = 1.0f;
    public int counterLanesPerCycle = 1;
    public bool hasFakeCounterLane = false;
    [Tooltip("반격 실패 시 다음 투척 속도 배수")]
    public float missPenaltySpeedMul = 1.30f;
    public float staggerSeconds = 0.8f;
    public float staggerCloseIn = 30f;

    private void OnValidate()
    {
        hp = Mathf.Max(1, hp);
        aimTime = Mathf.Max(0.05f, aimTime);
        throwTime = Mathf.Max(0.05f, throwTime);
        counterWindow = Mathf.Max(0.1f, counterWindow);
        recoverTime = Mathf.Max(0.05f, recoverTime);
        counterLanesPerCycle = Mathf.Clamp(counterLanesPerCycle, 1, 3);
        missPenaltySpeedMul = Mathf.Max(1f, missPenaltySpeedMul);
    }

    /// Sum of the four stages. Kept as a property so designers can set either
    /// the parts or the whole without the FSM drifting.
    public float ComputedCycle => aimTime + throwTime + counterWindow + recoverTime;
}
