using System.Collections.Generic;
using UnityEngine;

namespace CoastRun.Village
{
    /// 195차(사용자: 「동물의 숲 급 계절 이벤트·연출」): 계절마다 3주짜리 마을 이벤트 — 장식·부스(한정 옷·먹거리·체험)·첫 방문 연출.
    public static class VillageSeason
    {
        public class Ev { public int id, from, to; public string ko, en, boothKo, foodKo, outfit; public int foodPrice, foodHp, foodStress; public Vector3 booth; public Color col; }
        public static readonly Ev[] All = {
            new Ev { id = 0, from = 5, to = 7, ko = "유채꽃 축제", en = "Canola Festival", boothKo = "유채꽃 축제 부스", foodKo = "유채꿀 호떡", foodPrice = 90, foodHp = 30, foodStress = 8, outfit = "pin_canola", booth = new Vector3(52.5f, 0f, 40f), col = new Color(1f, 0.85f, 0.2f) },
            new Ev { id = 1, from = 18, to = 20, ko = "중몬 여름밤 축제", en = "Jungmon Summer Night", boothKo = "여름밤 축제 부스", foodKo = "수박 화채", foodPrice = 150, foodHp = 50, foodStress = 10, outfit = "hat_straw", booth = new Vector3(VillageZones.TourC.x + 6f, 0.3f, VillageZones.TourC.z + 12f), col = new Color(0.35f, 0.70f, 0.95f) },
            new Ev { id = 2, from = 31, to = 33, ko = "감귤 따기 체험", en = "Tangerine Picking", boothKo = "감귤 따기 체험 부스", foodKo = "귤 찐빵", foodPrice = 80, foodHp = 25, foodStress = 8, outfit = "scarf_orange", booth = new Vector3(40f, 0f, 19f), col = new Color(1f, 0.55f, 0.15f) },
            new Ev { id = 3, from = 44, to = 46, ko = "눈꽃 축제", en = "Snowflake Festival", boothKo = "붕어빵 수레", foodKo = "붕어빵", foodPrice = 80, foodHp = 25, foodStress = 8, outfit = "hat_knit", booth = new Vector3(21f, 0f, -2.5f), col = new Color(0.80f, 0.90f, 1f) },
        };
        public static Ev Now(SaveData s) { if (s == null) return null; foreach (var e in All) if (s.week >= e.from && s.week <= e.to) return e; return null; }
        public static Ev Next(SaveData s) { if (s == null) return All[0]; foreach (var e in All) if (e.from > s.week) return e; return All[0]; }
        public static Transform Booth;

        /// 마을을 세울 때(씬 로드) — 이벤트 주간이면 장식·부스
        public static void Build(Transform root, SaveData s)
        {
            Booth = null; var e = Now(s); if (e == null) return;
            var host = new GameObject("SeasonEvent_" + e.id).transform; host.SetParent(root, false);
            var p = e.id == 1 ? e.booth : VillageWorld.Ground(e.booth.x, e.booth.z);
            Booth = new GameObject("EventBooth").transform; Booth.SetParent(host, false); Booth.position = p;
            // 부스: 줄무늬 천막 + 판매대 + 간판
            var a = CoastMaterials.CreateLit(e.col); var w = CoastMaterials.CreateLit(Color.white); var wood = CoastMaterials.CreateLit(new Color(0.62f, 0.44f, 0.30f));
            GameObject B(string n, Vector3 lp, Vector3 sc, Material m, bool col = false) { var g = GameObject.CreatePrimitive(PrimitiveType.Cube); if (!col) Object.Destroy(g.GetComponent<Collider>()); g.name = n; g.transform.SetParent(Booth, false); g.transform.localPosition = lp; g.transform.localScale = sc; g.GetComponent<MeshRenderer>().sharedMaterial = m; return g; }
            B("BoothCounter", new Vector3(0f, 0.5f, 0f), new Vector3(3f, 1f, 1f), wood, true);
            for (int s2 = -1; s2 <= 1; s2 += 2) B("BoothPole", new Vector3(s2 * 1.5f, 1.4f, 0.4f), new Vector3(0.1f, 2.8f, 0.1f), wood);
            for (int i = 0; i < 6; i++) { var r = B("BoothAwning", new Vector3(-1.5f + 0.3f + i * 0.5f, 2.8f, 0f), new Vector3(0.5f, 0.06f, 1.6f), i % 2 == 0 ? a : w); r.transform.localRotation = Quaternion.Euler(-15f, 0f, 0f); }
            VillageZones.Sign(Booth, p + new Vector3(0f, 3.3f, -0.5f), 180f, "🎪 " + e.ko, "🎪 " + e.en, Color.Lerp(e.col, Color.black, 0.25f), 3.4f, 0.7f);
            // 등불 줄
            for (int i = 0; i < 9; i++)
            {
                var l = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.Destroy(l.GetComponent<Collider>()); l.name = "FestLantern"; l.transform.SetParent(host, false);
                l.transform.position = p + new Vector3(-6f + i * 1.5f, 3.4f - Mathf.Sin(i / 8f * Mathf.PI) * 0.5f, 2.2f); l.transform.localScale = Vector3.one * 0.35f;
                l.GetComponent<MeshRenderer>().sharedMaterial = CoastMaterials.CreateUnlit(i % 2 == 0 ? e.col : new Color(1f, 0.6f, 0.7f));
            }
            // 계절별 장식
            var rng = new System.Random(e.id * 13 + 5);
            if (e.id == 0)   // 유채꽃: 꽃밭 앞 사진 액자
            {
                B("PhotoFrame", new Vector3(3.2f, 1.3f, 1.5f), new Vector3(1.8f, 1.8f, 0.1f), a);
                B("PhotoHole", new Vector3(3.2f, 1.3f, 1.45f), new Vector3(1.3f, 1.3f, 0.1f), CoastMaterials.CreateLit(new Color(0.75f, 0.90f, 1f)));
            }
            else if (e.id == 2)   // 감귤: 귤 상자 더미
            {
                for (int i = 0; i < 6; i++) { var c = B("OrangeCrate", new Vector3(-3f + (i % 3) * 0.7f, 0.25f + (i / 3) * 0.5f, 1.5f), new Vector3(0.6f, 0.45f, 0.45f), wood); for (int k = 0; k < 3; k++) { var o = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.Destroy(o.GetComponent<Collider>()); o.transform.SetParent(c.transform, false); o.transform.localPosition = new Vector3(-0.3f + k * 0.3f, 0.6f, 0f); o.transform.localScale = new Vector3(0.4f, 0.5f, 0.6f); o.GetComponent<MeshRenderer>().sharedMaterial = CoastMaterials.CreateLit(new Color(1f, 0.55f, 0.12f), 0.3f); } }
            }
            else if (e.id == 3)   // 눈꽃: 눈사람 · 동백 · 눈 내림
            {
                var snow = CoastMaterials.CreateLit(new Color(0.97f, 0.98f, 1f));
                var sm = new GameObject("Snowman").transform; sm.SetParent(host, false); sm.position = VillageWorld.Ground(24f, -9f);
                float[] rr = { 1.1f, 0.8f, 0.55f }; float yy = 0f;
                for (int i = 0; i < 3; i++) { var b = GameObject.CreatePrimitive(PrimitiveType.Sphere); if (i > 0) Object.Destroy(b.GetComponent<Collider>()); b.transform.SetParent(sm, false); yy += rr[i] * (i == 0 ? 0.5f : 0.85f); b.transform.localPosition = new Vector3(0f, yy, 0f); b.transform.localScale = Vector3.one * rr[i]; b.GetComponent<MeshRenderer>().sharedMaterial = snow; yy += rr[i] * 0.35f; }
                var nose = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Object.Destroy(nose.GetComponent<Collider>()); nose.transform.SetParent(sm, false); nose.transform.localPosition = new Vector3(0f, yy - 0.3f, 0.3f); nose.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); nose.transform.localScale = new Vector3(0.08f, 0.18f, 0.08f); nose.GetComponent<MeshRenderer>().sharedMaterial = CoastMaterials.CreateLit(new Color(1f, 0.5f, 0.1f));
                for (int i = 0; i < 16; i++)
                {
                    float x = (float)(rng.NextDouble() * 70 - 30), z = (float)(rng.NextDouble() * 60 - 20); if (VillageWorld.PathDist(x, z) < 2.5f) continue;
                    var cam = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.Destroy(cam.GetComponent<Collider>()); cam.name = "Camellia"; cam.transform.SetParent(host, false);
                    cam.transform.position = VillageWorld.Ground(x, z) + Vector3.up * 0.9f; cam.transform.localScale = new Vector3(1.2f, 1.1f, 1.2f); cam.GetComponent<MeshRenderer>().sharedMaterial = CoastMaterials.CreateLit(new Color(0.20f, 0.45f, 0.25f));
                    for (int k = 0; k < 4; k++) { var f = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.Destroy(f.GetComponent<Collider>()); f.transform.SetParent(cam.transform, false); f.transform.localPosition = Random.insideUnitSphere * 0.45f; f.transform.localScale = Vector3.one * 0.22f; f.GetComponent<MeshRenderer>().sharedMaterial = CoastMaterials.CreateLit(new Color(0.92f, 0.15f, 0.25f)); }
                }
                host.gameObject.AddComponent<SnowFall>();
            }
            BuildingOutline.Attach(Booth, 0.02f);
        }
    }

    /// 눈 내림 — 주인공 주변 흰 점들이 천천히 떨어진다
    public class SnowFall : MonoBehaviour
    {
        readonly List<Transform> _f = new List<Transform>(); Material _m;
        void Start()
        {
            _m = CoastMaterials.CreateUnlit(Color.white);
            for (int i = 0; i < 90; i++) { var q = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(q.GetComponent<Collider>()); q.name = "Snow"; q.transform.SetParent(transform, false); q.transform.localScale = Vector3.one * 0.08f; q.GetComponent<MeshRenderer>().sharedMaterial = _m; _f.Add(q.transform); Reset(q.transform, true); }
        }
        void Reset(Transform t, bool anyY)
        {
            var cam = Camera.main; var c = cam != null ? cam.transform.position : Vector3.zero;
            t.position = c + new Vector3(Random.Range(-14f, 14f), anyY ? Random.Range(-4f, 10f) : 10f, Random.Range(0f, 22f));
        }
        void Update()
        {
            var cam = Camera.main; if (cam == null) return;
            foreach (var t in _f)
            {
                t.position += new Vector3(Mathf.Sin(Time.time + t.position.x * 1.7f) * 0.3f, -1.2f, 0f) * Time.deltaTime;
                if (t.position.y < cam.transform.position.y - 12f || Vector3.Distance(t.position, cam.transform.position) > 30f) Reset(t, false);
            }
        }
    }
}
