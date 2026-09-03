using UnityEngine;

public enum PartSlot
{
    Deck = 0,
    Truck = 1,
    Wheel = 2,
    Bearing = 3,
    Grip = 4
}

public enum PartRarity
{
    N = 0,
    R = 1,
    SR = 2,
    SSR = 3
}

[CreateAssetMenu(menuName = "347/PartDefinition", fileName = "Part_")]
public class PartDefinition : ScriptableObject
{
    public string partId;
    public string displayName;
    public PartSlot slot;
    public PartRarity rarity = PartRarity.N;
    [TextArea] public string description;
    [Tooltip("SSR rule-changers only. Never a pure stat stick.")]
    public bool changesRules;
    [TextArea] public string ruleUpside;
    [TextArea] public string ruleDownside;
}
