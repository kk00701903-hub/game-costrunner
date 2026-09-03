using UnityEngine;

/// Spawns the zone-2 scout fight once per run when the overpass ends.
public class PrerunnerTrigger : MonoBehaviour
{
    private bool _fired;

    private void Update()
    {
        if (_fired || GameManager.Instance == null || !GameManager.Instance.IsPlaying)
            return;

        // End of zone 2 (Overpass). Not during king arena boot.
        if (GameBootstrap.PendingMode == BootMode.KingArena)
            return;

        float distance = GameManager.Instance.TraveledDistance;
        float gate = Zones.StartDistance(Zone.Flooded) - 40f;
        if (distance < gate)
            return;

        _fired = true;

        // Returning players still get the wall-narrow cycle; veterans (5+) skip.
        int attempts = GameManager.Instance.RunCount + 1;
        if (attempts >= 5)
            return;

        GameObject go = GameObject.Find("KingFight");
        if (go == null)
            go = new GameObject("KingFight");

        KingFight fight = go.GetComponent<KingFight>();
        if (fight == null)
            fight = go.AddComponent<KingFight>();

        if (fight.Active)
            return;

        fight.BeginPrerunner();
        if (UIManager.Instance != null)
            UIManager.Instance.ShowBanner("정찰기", 1.8f);
    }
}
