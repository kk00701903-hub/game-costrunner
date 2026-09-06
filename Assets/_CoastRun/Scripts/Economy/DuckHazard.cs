using UnityEngine;

namespace CoastRun
{
    /// Overhead bar / clothesline — SoftHit only when the player is standing.
    /// Crouch (swipe down / S) to pass underneath.
    public class DuckHazard : MonoBehaviour
    {
        [SerializeField] private NearMissZone nearMiss;

        public void BindNearMiss(NearMissZone zone) => nearMiss = zone;

        private void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponentInParent<PlayerController>();
            if (player == null && !other.CompareTag("Player"))
                return;
            if (player == null)
                player = other.GetComponentInParent<PlayerController>();
            if (player == null)
                return;

            if (player.IsCrouching)
                return;

            nearMiss?.NotifyHardHit();
            player.SoftHit();
        }

        public static GameObject Create(Transform parent, Vector3 worldPos, int lane, DuckStyle style)
        {
            var root = new GameObject(style == DuckStyle.Clothesline ? "Obstacle_Clothesline" : style == DuckStyle.LanternString ? "Obstacle_LanternString" : "Obstacle_OverheadBar");
            root.transform.SetParent(parent, false);
            root.transform.position = worldPos;
            root.transform.rotation = DownhillPath.Rotation;
            float width = 1.7f;
            string painted = style == DuckStyle.Clothesline ? "Clothesline" : style == DuckStyle.LanternString ? "Lantern" : "OverheadBar";
            bool hasPainting = PaintedProp.Available(painted);
            if (hasPainting)
            {
                // Whole gate as one painting (posts + bar/clothes); the duck trigger
                // below is unchanged, so gameplay stays identical.
                PaintedProp.Attach(root.transform, painted, 1.55f, replace: false);
            }
            else
            {
                CreatePole(root.transform, new Vector3(-width * 0.5f, 0f, 0f));
                CreatePole(root.transform, new Vector3(width * 0.5f, 0f, 0f));
            }

            if (!hasPainting && style == DuckStyle.LanternString)
            {
                // 등불 줄: 줄 하나에 등불 4개(폴백).
                for (int i = 0; i < 4; i++)
                {
                    var lamp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    lamp.name = "Lantern" + i;
                    lamp.transform.SetParent(root.transform, false);
                    lamp.transform.localPosition = new Vector3(-width * 0.375f + i * width * 0.25f, 1.2f - (i == 1 || i == 2 ? 0.08f : 0f), 0f);
                    lamp.transform.localScale = new Vector3(0.22f, 0.3f, 0.22f);
                    Object.Destroy(lamp.GetComponent<Collider>());
                    lamp.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateUnlit(i % 2 == 0 ? new Color(1f, 0.45f, 0.25f) : new Color(1f, 0.85f, 0.35f));
                }
            }
            if (!hasPainting && style == DuckStyle.Clothesline)
            {
                var cloth = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cloth.name = "Cloth";
                cloth.transform.SetParent(root.transform, false);
                cloth.transform.localPosition = new Vector3(0f, 1.15f, 0f);
                cloth.transform.localScale = new Vector3(width * 0.95f, 0.38f, 0.1f);
                Object.Destroy(cloth.GetComponent<Collider>());
                cloth.GetComponent<Renderer>().sharedMaterial =
                    CoastMaterials.CreateLit(() => Color.Lerp(CoastPalette.AccentOrange, CoastPalette.TownCream, 0.35f));

                var line = GameObject.CreatePrimitive(PrimitiveType.Cube);
                line.name = "Line";
                line.transform.SetParent(root.transform, false);
                line.transform.localPosition = new Vector3(0f, 1.35f, 0f);
                line.transform.localScale = new Vector3(width, 0.03f, 0.03f);
                Object.Destroy(line.GetComponent<Collider>());
                line.GetComponent<Renderer>().sharedMaterial =
                    CoastMaterials.CreateUnlit(() => CoastPalette.Pole);
            }
            else if (!hasPainting)
            {
                var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bar.name = "Bar";
                bar.transform.SetParent(root.transform, false);
                bar.transform.localPosition = new Vector3(0f, 1.1f, 0f);
                bar.transform.localScale = new Vector3(width, 0.16f, 0.16f);
                Object.Destroy(bar.GetComponent<Collider>());
                bar.GetComponent<Renderer>().sharedMaterial =
                    CoastMaterials.CreateLit(() => Color.Lerp(CoastPalette.RoadGrey, CoastPalette.AccentOrange, 0.4f));

                var sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
                sign.name = "Sign";
                sign.transform.SetParent(root.transform, false);
                sign.transform.localPosition = new Vector3(0f, 1.25f, 0f);
                sign.transform.localScale = new Vector3(0.95f, 0.38f, 0.06f);
                Object.Destroy(sign.GetComponent<Collider>());
                sign.GetComponent<Renderer>().sharedMaterial =
                    CoastMaterials.CreateLit(() => CoastPalette.CoinYellow);
            }

            // Hit volume sits at head height — crouching collider drops below this.
            var hard = new GameObject("DuckHit");
            hard.transform.SetParent(root.transform, false);
            hard.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            var hardCol = hard.AddComponent<BoxCollider>();
            hardCol.isTrigger = true;
            hardCol.size = new Vector3(width * 0.9f, 0.55f, 0.4f);
            var duck = hard.AddComponent<DuckHazard>();

            var near = new GameObject("NearMiss");
            near.transform.SetParent(root.transform, false);
            near.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            var nearCol = near.AddComponent<BoxCollider>();
            nearCol.isTrigger = true;
            nearCol.size = new Vector3(width * 1.1f, 1.6f, 1.2f);
            var zone = near.AddComponent<NearMissZone>();
            zone.Configure(15, lane);
            duck.BindNearMiss(zone);

            BlobShadow.Attach(root.transform, width * 0.55f);
            return root;
        }

        private static void CreatePole(Transform parent, Vector3 localPos)
        {
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Pole";
            pole.transform.SetParent(parent, false);
            pole.transform.localPosition = localPos + new Vector3(0f, 0.75f, 0f);
            pole.transform.localScale = new Vector3(0.09f, 0.75f, 0.09f);
            Object.Destroy(pole.GetComponent<Collider>());
            pole.GetComponent<Renderer>().sharedMaterial =
                CoastMaterials.CreateLit(() => CoastPalette.Pole);
        }
    }

    public enum DuckStyle
    {
        OverheadBar,
        Clothesline,
        LanternString
    }
}
