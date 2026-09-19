using UnityEngine;

namespace CoastRun.Village
{
    /// 147차: 집 안 — 마을 밖(원점 300,300)에 작은 방을 짓고 주인공을 옮긴다. 남쪽(카메라 쪽)은 열려 있어 디오라마처럼 들여다본다.
    /// VillageWorld.Height 는 방 안에서 FloorY 를 돌려준다(Interior 정적 참조).
    public class VillageInterior : MonoBehaviour
    {
        public const float OX = 300f, OZ = 300f, RW = 7f, RD = 5.5f, WallH = 3f;
        public float FloorY = 0f;
        public string HouseName; public Vector3 ReturnPos; public float ReturnYaw = 180f;
        public Vector3 Entry => new Vector3(OX, FloorY, OZ - RD * 0.5f + 1.2f);
        public Vector3 ExitSpot => new Vector3(OX, FloorY, OZ - RD * 0.5f + 0.6f);
        /// 방 + 카메라가 서는 남쪽 여유(14 m)까지 평평하게 — 바깥 지형 높이로 카메라가 치솟지 않게(147차 캡처에서 탑다운 되던 원인)
        public bool Contains(float x, float z) => Mathf.Abs(x - OX) < RW * 0.5f + 14f && Mathf.Abs(z - OZ) < RD * 0.5f + 14f;

        public static VillageInterior Create(Transform parent, string houseName, Color wall, Color accent, Vector3 returnPos, float returnYaw)
        {
            var go = new GameObject("Interior_" + houseName); go.transform.SetParent(parent, false);
            var it = go.AddComponent<VillageInterior>(); it.HouseName = houseName; it.ReturnPos = returnPos; it.ReturnYaw = returnYaw;
            it.Build(wall, accent);
            VillageWorld.Interior = it;
            return it;
        }

        void OnDestroy() { if (VillageWorld.Interior == this) VillageWorld.Interior = null; }

        GameObject Box(string n, Vector3 p, Vector3 s, Color c, bool collide = false)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube); if (!collide) Destroy(g.GetComponent<Collider>()); g.name = n;
            g.transform.SetParent(transform, false); g.transform.position = new Vector3(OX, FloorY, OZ) + p; g.transform.localScale = s;
            g.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateLit(c, 0.05f); return g;
        }
        GameObject Ball(string n, Vector3 p, Vector3 s, Color c)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(g.GetComponent<Collider>()); g.name = n;
            g.transform.SetParent(transform, false); g.transform.position = new Vector3(OX, FloorY, OZ) + p; g.transform.localScale = s;
            g.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateLit(c, 0.05f); return g;
        }

        void Build(Color wall, Color accent)
        {
            var floor = new Color(0.86f, 0.70f, 0.50f); var floorD = new Color(0.78f, 0.62f, 0.44f);
            Box("Floor", new Vector3(0f, -0.1f, 0f), new Vector3(RW, 0.2f, RD), floor);
            for (float x = -RW * 0.5f + 0.5f; x < RW * 0.5f; x += 1f) Box("Plank", new Vector3(x, 0.001f, 0f), new Vector3(0.03f, 0.01f, RD), floorD);
            // 벽 3면(남쪽 열림) + 굽도리
            Box("WallN", new Vector3(0f, WallH * 0.5f, RD * 0.5f), new Vector3(RW, WallH, 0.2f), wall, true);
            Box("WallW", new Vector3(-RW * 0.5f, WallH * 0.5f, 0f), new Vector3(0.2f, WallH, RD), wall, true);
            Box("WallE", new Vector3(RW * 0.5f, WallH * 0.5f, 0f), new Vector3(0.2f, WallH, RD), wall, true);
            Box("Skirt", new Vector3(0f, 0.12f, RD * 0.5f - 0.12f), new Vector3(RW, 0.24f, 0.06f), Color.Lerp(wall, Color.black, 0.2f));
            // 창(북벽) + 햇빛 사각
            Box("WinFrame", new Vector3(1.6f, 1.7f, RD * 0.5f - 0.12f), new Vector3(1.3f, 1.1f, 0.08f), Color.white);
            Box("Win", new Vector3(1.6f, 1.7f, RD * 0.5f - 0.16f), new Vector3(1.1f, 0.9f, 0.06f), new Color(0.72f, 0.88f, 1f));
            Box("Curtain", new Vector3(0.85f, 1.7f, RD * 0.5f - 0.2f), new Vector3(0.28f, 1.3f, 0.06f), accent);
            Box("Curtain", new Vector3(2.35f, 1.7f, RD * 0.5f - 0.2f), new Vector3(0.28f, 1.3f, 0.06f), accent);
            // 남쪽 보이지 않는 벽(카메라 쪽) — 밖으로 못 나가게, 출구 스팟만
            var b = new GameObject("Bound", typeof(BoxCollider)); b.transform.SetParent(transform, false); b.transform.position = new Vector3(OX, FloorY + 1f, OZ - RD * 0.5f - 0.3f); b.GetComponent<BoxCollider>().size = new Vector3(RW, 3f, 0.4f);
            // 가구: 러그·침대·탁자·의자·책장·램프·화분
            Box("Rug", new Vector3(0f, 0.015f, -0.3f), new Vector3(2.8f, 0.03f, 2.0f), accent);
            Box("RugIn", new Vector3(0f, 0.025f, -0.3f), new Vector3(2.2f, 0.02f, 1.5f), Color.Lerp(accent, Color.white, 0.4f));
            Box("Bed", new Vector3(-2.2f, 0.28f, 1.3f), new Vector3(1.4f, 0.5f, 2.2f), new Color(0.75f, 0.55f, 0.40f), true);
            Box("Mattress", new Vector3(-2.2f, 0.58f, 1.3f), new Vector3(1.3f, 0.16f, 2.1f), Color.white);
            Box("Blanket", new Vector3(-2.2f, 0.68f, 1.0f), new Vector3(1.32f, 0.08f, 1.4f), accent);
            Box("Pillow", new Vector3(-2.2f, 0.72f, 2.0f), new Vector3(0.9f, 0.16f, 0.5f), new Color(1f, 0.96f, 0.9f));
            Box("Headboard", new Vector3(-2.2f, 0.85f, 2.35f), new Vector3(1.4f, 1.1f, 0.1f), new Color(0.62f, 0.44f, 0.30f));
            Box("Table", new Vector3(1.8f, 0.72f, -0.4f), new Vector3(1.4f, 0.08f, 0.9f), new Color(0.80f, 0.60f, 0.42f), true);
            foreach (var d in new[] { new Vector3(-0.6f, 0.35f, -0.35f), new Vector3(0.6f, 0.35f, -0.35f), new Vector3(-0.6f, 0.35f, 0.35f), new Vector3(0.6f, 0.35f, 0.35f) }) Box("Leg", new Vector3(1.8f, 0f, -0.4f) + d, new Vector3(0.08f, 0.7f, 0.08f), new Color(0.62f, 0.44f, 0.30f));
            Box("Chair", new Vector3(1.8f, 0.24f, -1.3f), new Vector3(0.5f, 0.08f, 0.5f), new Color(0.75f, 0.55f, 0.40f));
            Box("ChairBack", new Vector3(1.8f, 0.6f, -1.52f), new Vector3(0.5f, 0.7f, 0.06f), new Color(0.75f, 0.55f, 0.40f));
            Ball("Cup", new Vector3(1.6f, 0.86f, -0.4f), new Vector3(0.18f, 0.22f, 0.18f), Color.white);
            Box("Shelf", new Vector3(2.9f, 1.0f, 2.2f), new Vector3(1.2f, 2.0f, 0.4f), new Color(0.62f, 0.44f, 0.30f), true);
            Color[] bk = { new Color(0.95f, 0.45f, 0.45f), new Color(0.45f, 0.65f, 0.95f), new Color(0.98f, 0.85f, 0.45f), new Color(0.55f, 0.80f, 0.55f) };
            for (int s = 0; s < 3; s++) for (int k = 0; k < 5; k++) Box("Book", new Vector3(2.45f + k * 0.2f, 0.45f + s * 0.6f, 2.2f), new Vector3(0.14f, 0.36f + (k % 2) * 0.06f, 0.26f), bk[(s + k) % bk.Length]);
            Box("LampBase", new Vector3(-0.2f, 0.9f, 2.3f), new Vector3(0.12f, 1.8f, 0.12f), new Color(0.62f, 0.44f, 0.30f));
            Ball("LampShade", new Vector3(-0.2f, 1.95f, 2.3f), new Vector3(0.6f, 0.4f, 0.6f), new Color(1f, 0.92f, 0.7f));
            Box("Pot", new Vector3(-3.0f, 0.2f, -1.6f), new Vector3(0.4f, 0.4f, 0.4f), new Color(0.80f, 0.48f, 0.32f));
            Ball("Plant", new Vector3(-3.0f, 0.75f, -1.6f), new Vector3(0.8f, 0.9f, 0.8f), new Color(0.40f, 0.68f, 0.32f));
            // 문(남쪽 왼편, 장식) — 출구 스팟 안내
            Box("DoorMat", new Vector3(0f, 0.01f, -RD * 0.5f + 0.5f), new Vector3(0.9f, 0.02f, 0.5f), new Color(0.75f, 0.55f, 0.40f));
            BuildingOutline.Attach(transform, 0.018f);
            var l = new GameObject("RoomLight").AddComponent<Light>(); l.transform.SetParent(transform, false); l.transform.position = new Vector3(OX - 0.2f, FloorY + 2.2f, OZ + 2.3f);
            l.type = LightType.Point; l.range = 7f; l.intensity = 1.2f; l.color = new Color(1f, 0.93f, 0.78f);
        }
    }
}
