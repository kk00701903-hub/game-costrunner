using System;
using UnityEngine;

public enum PickupKind
{
    /// Old currency, worth nothing. Doha picks it up anyway.
    Coin,

    /// A retrieved person's name tag. Score and risk in one object.
    Tag,

    /// Booster cell.
    BoosterCell,

    /// Drone scrap shield.
    Shield,

    /// Hacked tag that reads the drones' telegraphs early.
    ReverseScan,

    /// Emergency deck tape. Only appears when the deck is already cracked.
    DeckTape,

    /// Left by another rider. Permanent currency.
    DeckPiece,

    /// One per zone, hidden. Revive charge and true ending condition both.
    Letter
}

public class Pickup : MonoBehaviour
{
    private static readonly System.Collections.Generic.List<Pickup> Live =
        new System.Collections.Generic.List<Pickup>(64);

    [SerializeField] private PickupKind kind = PickupKind.Coin;
    [SerializeField] private Vector3 size = new Vector3(0.9f, 0.9f, 0.9f);

    private bool _collected;

    public PickupKind Kind
    {
        get { return kind; }
        set { kind = value; }
    }

    public Vector3 Center => transform.position;
    public Vector3 Size => size;

    private void OnEnable()
    {
        _collected = false;
        if (!Live.Contains(this))
            Live.Add(this);
    }

    private void OnDisable()
    {
        Live.Remove(this);
    }

    /// Physics-free collect. Called by the player after movement each frame.
    public static void CollectOverlaps(Vector3 center, Vector3 bodySize)
    {
        for (int i = Live.Count - 1; i >= 0; i--)
        {
            Pickup pickup = Live[i];
            if (pickup == null || !pickup.isActiveAndEnabled || pickup._collected)
            {
                if (pickup == null)
                    Live.RemoveAt(i);
                continue;
            }

            if (!Aabb.Overlaps(center, bodySize, pickup.Center, pickup.Size))
                continue;

            pickup._collected = true;
            PickupBurstVfx.Play(pickup.Center, pickup.kind);
            if (GameManager.Instance != null)
                GameManager.Instance.Collect(pickup.kind);
            pickup.gameObject.SetActive(false);
        }
    }

    /// Kinds are carried by prefab name so new art drops in without wiring.
    public static PickupKind KindFromName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return PickupKind.Coin;

        if (Has(name, "Letter"))
            return PickupKind.Letter;
        if (Has(name, "DeckPiece") || Has(name, "Deck_Piece"))
            return PickupKind.DeckPiece;
        if (Has(name, "Tape"))
            return PickupKind.DeckTape;
        if (Has(name, "Scan"))
            return PickupKind.ReverseScan;
        if (Has(name, "Shield") || Has(name, "Scrap"))
            return PickupKind.Shield;
        if (Has(name, "Battery") || Has(name, "Cell") || Has(name, "Booster"))
            return PickupKind.BoosterCell;
        if (Has(name, "Tag"))
            return PickupKind.Tag;
        if (Has(name, "Coin"))
            return PickupKind.Coin;

        // Legacy coastal supply crates, kept working until the city art lands.
        if (Has(name, "Crate_Med"))
            return PickupKind.DeckTape;
        if (Has(name, "Crate_Food"))
            return PickupKind.Coin;
        if (Has(name, "Crate"))
            return PickupKind.BoosterCell;

        return PickupKind.Coin;
    }

    private static bool Has(string name, string token)
    {
        return name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public static bool IsActiveItem(PickupKind kind)
    {
        return kind == PickupKind.BoosterCell || kind == PickupKind.Shield || kind == PickupKind.ReverseScan;
    }
}
