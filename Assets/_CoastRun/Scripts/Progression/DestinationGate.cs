using UnityEngine;

namespace CoastRun
{
    /// Transmission tower gate — only reachable when MaxSpeed grind is high enough.
    public class DestinationGate : MonoBehaviour
    {
        [SerializeField] private UpgradeManager upgrades;
        [SerializeField] private PlayerController player;
        [SerializeField] private GameSession session;
        [SerializeField] private UI_FeedbackController feedback;
        [SerializeField] private float checkRadius = 8f;

        private bool _reached;
        private bool _warnedLocked;

        public void Bind(UpgradeManager upgradeManager, PlayerController playerController,
            GameSession gameSession, UI_FeedbackController ui)
        {
            upgrades = upgradeManager;
            player = playerController;
            session = gameSession;
            feedback = ui;
        }

        private void LateUpdate()
        {
            if (_reached || player == null || upgrades == null)
                return;

            float towerDist = upgrades.TowerDistance;
            if (player.PathDistance < towerDist - checkRadius)
            {
                _warnedLocked = false;
                return;
            }

            if (!upgrades.MeetsTowerRequirement())
            {
                if (!_warnedLocked && player.PathDistance >= towerDist - checkRadius)
                {
                    _warnedLocked = true;
                    feedback?.ShowWatchMessage("송전탑 LOCKED",
                        "MaxSpeed Lv." + upgrades.GetLevel(UpgradeStat.MaxSpeed) +
                        " → need Lv." + upgrades.TowerRequiredMaxSpeedLevel);
                }

                // Soft ceiling: clamp progress near locked tower so grind is required.
                if (player.PathDistance > towerDist - 1f)
                    player.SoftHit();
                return;
            }

            if (player.PathDistance >= towerDist)
            {
                _reached = true;
                feedback?.ShowWatchMessage("DESTINATION", "송전탑 도착!");
                session?.EndRun();
            }
        }

        public static GameObject CreateVisual(Transform parent, float z)
        {
            var prefab = ArtAssets.LoadPrefabOrNull("TransmissionTower");
            if (prefab != null)
            {
                var inst = Object.Instantiate(prefab, parent);
                inst.name = "TransmissionTower";
                inst.transform.SetPositionAndRotation(
                    DownhillPath.Point(z, -6f), DownhillPath.Rotation);
                return inst;
            }

            var root = new GameObject("TransmissionTower");
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(
                DownhillPath.Point(z, -6f), DownhillPath.Rotation);

            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Mast";
            pole.transform.SetParent(root.transform, false);
            pole.transform.localPosition = new Vector3(0f, 12f, 0f);
            pole.transform.localScale = new Vector3(0.8f, 12f, 0.8f);
            Object.Destroy(pole.GetComponent<Collider>());
            pole.GetComponent<Renderer>().sharedMaterial =
                CoastMaterials.CreateLit(new Color(0.45f, 0.48f, 0.5f));

            var cross = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cross.name = "CrossArm";
            cross.transform.SetParent(root.transform, false);
            cross.transform.localPosition = new Vector3(0f, 20f, 0f);
            cross.transform.localScale = new Vector3(10f, 0.4f, 0.4f);
            Object.Destroy(cross.GetComponent<Collider>());
            cross.GetComponent<Renderer>().sharedMaterial =
                CoastMaterials.CreateLit(new Color(0.35f, 0.36f, 0.38f));

            return root;
        }
    }
}
