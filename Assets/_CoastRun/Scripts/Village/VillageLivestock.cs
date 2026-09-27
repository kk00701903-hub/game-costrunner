using System.Collections.Generic;
using UnityEngine;

namespace CoastRun.Village
{
    /// 171차(사용자: 「집 근처에 농장 — 산에서 돌아다니는 닭·토끼를 잠자리채로 잡으면 키운다. 닭은 알을 낳고 토끼는 다 크면 때려서 고기」).
    /// 우리집 서쪽(−10, 34.5) 울타리 안. 데이터는 SaveData.farmChickens / farmEggs / farmRabbitAge(잠 횟수). 잠(SleepWeek)마다 SleepTick.
    public static class VillageLivestock
    {
        public const float CX = -10f, CZ = 34.5f, HW = 3.4f, HD = 2.6f;
        public const int Cap = 6, GrownAge = 3, EggCap = 8;

        public static void Ensure(SaveData s) { if (s != null && s.farmRabbitAge == null) s.farmRabbitAge = new int[0]; }
        public static bool AddChicken(SaveData s) { Ensure(s); if (s.farmChickens >= Cap) return false; s.farmChickens++; return true; }
        public static bool AddRabbit(SaveData s)
        {
            Ensure(s); if (s.farmRabbitAge.Length >= Cap) return false;
            var n = new int[s.farmRabbitAge.Length + 1]; s.farmRabbitAge.CopyTo(n, 0); n[n.Length - 1] = 0; s.farmRabbitAge = n; return true;
        }
        public static int Rabbits(SaveData s) { Ensure(s); return s.farmRabbitAge.Length; }
        public static int GrownRabbits(SaveData s) { Ensure(s); int n = 0; foreach (var a in s.farmRabbitAge) if (a >= GrownAge) n++; return n; }
        /// 잠 한 번 = 하루: 닭마다 달걀 1(최대 8 쌓임), 토끼는 한 살 더
        public static void SleepTick(SaveData s)
        {
            Ensure(s);
            // 205차(사용자: 「동물 돌보기 — 애정 하트」): ♥3 이상이면 달걀이 가끔 황금 달걀, ♥5 면 하루 한 개 더. 그날 쓰다듬지 않았으면 애정 −3
            int eggs = s.farmChickens; int hh = Hearts(s.loveHen);
            if (s.farmChickens > 0 && hh >= 5) eggs++;
            int gold = 0; if (hh >= 3) for (int k = 0; k < eggs; k++) if (Random.value < (hh >= 5 ? 0.35f : 0.2f)) gold++;
            s.farmEggs = Mathf.Min(EggCap, s.farmEggs + eggs - gold); s.farmGoldEggs = Mathf.Min(EggCap, s.farmGoldEggs + gold);
            for (int i = 0; i < s.farmRabbitAge.Length; i++) s.farmRabbitAge[i]++;
            if (Rabbits(s) > 0 && Hearts(s.loveRabbit) >= 5) { s.starShards++; s.starShardsTotal++; }   // 205차: 토끼 ♥5 — 네잎클로버(별조각 +1)
            bool petHen = s.carePetWeek == s.week && (s.carePetMask & 1) != 0, petRab = s.carePetWeek == s.week && (s.carePetMask & 2) != 0;
            if (s.farmChickens > 0 && !petHen) s.loveHen = Mathf.Max(0, s.loveHen - 3);
            if (Rabbits(s) > 0 && !petRab) s.loveRabbit = Mathf.Max(0, s.loveRabbit - 3);
            if (s.horsePetWeek != s.week) s.loveHorse = Mathf.Max(0, s.loveHorse - 2);
        }
        public static int Hearts(int love) => Mathf.Clamp(love / 20, 0, 5);
        public static string HeartText(int love) { int h = Hearts(love); return new string('♥', h) + new string('♡', 5 - h); }
        public static int CollectEggs(SaveData s) { int n = s.farmEggs; if (n > 0) LifeItems.Add(s, "ing_egg", n); s.farmEggs = 0; return n; }
        public static int CollectGoldEggs(SaveData s) { int n = s.farmGoldEggs; if (n > 0) LifeItems.Add(s, "ing_egg_gold", n); s.farmGoldEggs = 0; return n; }
        /// 다 큰 토끼 한 마리 → 고기 2
        public static bool Butcher(SaveData s)
        {
            Ensure(s); int idx = -1; for (int i = 0; i < s.farmRabbitAge.Length && idx < 0; i++) if (s.farmRabbitAge[i] >= GrownAge) idx = i;
            if (idx < 0) return false;
            var n = new List<int>(s.farmRabbitAge); n.RemoveAt(idx); s.farmRabbitAge = n.ToArray();
            LifeItems.Add(s, "ing_meat", 2); return true;
        }
        public static bool Inside(Vector3 p) => Mathf.Abs(p.x - CX) < HW + 0.6f && Mathf.Abs(p.z - CZ) < HD + 0.6f;
        public static Vector3 Gate => VillageWorld.Ground(CX, CZ - HD - 1.3f);

        // ── 3D ─────────────────────────────────────────────────────────
        public static void Build(Transform root, SaveData s)
        {
            var old = root.Find("Livestock"); if (old != null) Object.Destroy(old.gameObject);
            var host = new GameObject("Livestock").transform; host.SetParent(root, false);
            // 울타리(남쪽 가운데 1.6 m 는 문)
            var a = new Vector3(CX - HW, 0f, CZ - HD); var b = new Vector3(CX + HW, 0f, CZ - HD); var c = new Vector3(CX + HW, 0f, CZ + HD); var d = new Vector3(CX - HW, 0f, CZ + HD);
            VillageHouses.LogFence(host, a, new Vector3(CX - 0.9f, 0f, CZ - HD)); VillageHouses.LogFence(host, new Vector3(CX + 0.9f, 0f, CZ - HD), b);
            VillageHouses.LogFence(host, b, c); VillageHouses.LogFence(host, c, d); VillageHouses.LogFence(host, d, a);
            foreach (var seg in new[] { (a, b, true), (b, c, false), (c, d, true), (d, a, false) })
            {
                var w = new GameObject("FenceCol", typeof(BoxCollider)); w.transform.SetParent(host, false);
                var mid = (seg.Item1 + seg.Item2) * 0.5f; w.transform.position = VillageWorld.Ground(mid.x, mid.z) + Vector3.up * 0.5f;
                var bc = w.GetComponent<BoxCollider>(); bc.size = seg.Item3 ? new Vector3(HW * 2f, 1f, 0.25f) : new Vector3(0.25f, 1f, HD * 2f);
                if (seg.Item3 && seg.Item1 == a) { bc.size = new Vector3(HW - 0.9f, 1f, 0.25f); w.transform.position = VillageWorld.Ground(CX - HW * 0.5f - 0.45f, CZ - HD) + Vector3.up * 0.5f; var w2 = Object.Instantiate(w, host); w2.transform.position = VillageWorld.Ground(CX + HW * 0.5f + 0.45f, CZ - HD) + Vector3.up * 0.5f; }
            }
            // 닭장(작은 집) + 물그릇 + 여물통
            var coop = VillageWorld.Ground(CX + HW - 1.1f, CZ + HD - 0.9f);
            var wood = CoastMaterials.CreateLit(new Color(0.72f, 0.52f, 0.30f), 0.05f); var roofM = CoastMaterials.CreateLit(new Color(0.86f, 0.36f, 0.30f), 0.1f);
            Box(host, coop + new Vector3(0f, 0.55f, 0f), new Vector3(1.6f, 1.1f, 1.3f), wood);
            var roof = Box(host, coop + new Vector3(0f, 1.25f, 0f), new Vector3(1.9f, 0.16f, 1.6f), roofM); roof.transform.rotation = Quaternion.Euler(-14f, 0f, 0f);
            Box(host, coop + new Vector3(0f, 0.35f, -0.66f), new Vector3(0.5f, 0.7f, 0.05f), CoastMaterials.CreateLit(new Color(0.25f, 0.18f, 0.12f)));   // 문
            var bowl = VillageWorld.Ground(CX - HW + 1.0f, CZ + HD - 0.8f);
            var bw = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Object.Destroy(bw.GetComponent<Collider>()); bw.transform.SetParent(host, false); bw.transform.position = bowl + Vector3.up * 0.1f; bw.transform.localScale = new Vector3(0.7f, 0.1f, 0.7f);
            bw.GetComponent<MeshRenderer>().sharedMaterial = CoastMaterials.CreateLit(new Color(0.55f, 0.75f, 0.95f), 0.5f);
            Box(host, VillageWorld.Ground(CX - HW + 1.0f, CZ - HD + 0.9f) + Vector3.up * 0.2f, new Vector3(0.9f, 0.35f, 0.5f), wood);
            // 팻말
            var sign = VillageWorld.Ground(CX - 1.6f, CZ - HD - 0.3f);
            Box(host, sign + Vector3.up * 0.6f, new Vector3(0.1f, 1.2f, 0.1f), wood); Box(host, sign + Vector3.up * 1.25f, new Vector3(1.0f, 0.42f, 0.08f), wood);
            // 가축
            var tick = host.gameObject.AddComponent<LivestockTick>();
            if (s != null)
            {
                Ensure(s); var rng = new System.Random(s.week * 7 + 171);
                for (int i = 0; i < s.farmChickens; i++) tick.Add(Spawn(host, "VAnimal_Chicken", rng, 1.0f), true, 1f);
                for (int i = 0; i < s.farmRabbitAge.Length; i++) { float sc = Mathf.Lerp(0.75f, 1.45f, Mathf.Clamp01(s.farmRabbitAge[i] / (float)GrownAge)); tick.Add(Spawn(host, "VAnimal_Rabbit", rng, sc), false, sc); }
            }
        }
        static GameObject Box(Transform p, Vector3 pos, Vector3 size, Material m)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube); Object.Destroy(g.GetComponent<Collider>()); g.transform.SetParent(p, false); g.transform.position = pos; g.transform.localScale = size; g.GetComponent<MeshRenderer>().sharedMaterial = m; return g;
        }
        static Transform Spawn(Transform host, string model, System.Random rng, float scale)
        {
            var t = new GameObject(model).transform; t.SetParent(host, false);
            var p = new Vector3(CX + ((float)rng.NextDouble() - 0.5f) * (HW * 2f - 1.6f), 0f, CZ + ((float)rng.NextDouble() - 0.5f) * (HD * 2f - 1.6f));
            t.position = VillageWorld.Ground(p.x, p.z);
            var kit = JejuKit.Spawn(model, t, Vector3.zero, (float)rng.NextDouble() * 360f, scale);
            if (kit == null)
            {
                var g = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.Destroy(g.GetComponent<Collider>()); g.transform.SetParent(t, false); g.transform.localPosition = new Vector3(0f, 0.2f * scale, 0f); g.transform.localScale = Vector3.one * 0.4f * scale;
                g.GetComponent<MeshRenderer>().sharedMaterial = CoastMaterials.CreateLit(model.Contains("Chicken") ? Color.white : new Color(0.72f, 0.68f, 0.66f));
            }
            return t;
        }

        /// 울타리 안에서 어슬렁 — 닭은 쪼고, 토끼는 깡충
        public class LivestockTick : MonoBehaviour
        {
            class A { public Transform t; public bool chicken; public float scale, ph, wait; public Vector3 tgt; }
            readonly List<A> _l = new List<A>();
            public void Add(Transform t, bool chicken, float scale) { _l.Add(new A { t = t, chicken = chicken, scale = scale, ph = Random.value * 6f, wait = Random.Range(0.5f, 2f), tgt = t.position }); }
            void Update()
            {
                float dt = Time.deltaTime;
                foreach (var a in _l)
                {
                    if (a.t == null) continue; a.ph += dt; a.wait -= dt;
                    if (a.wait <= 0f) { a.wait = Random.Range(1.5f, 4f); a.tgt = new Vector3(CX + Random.Range(-HW + 0.9f, HW - 0.9f), 0f, CZ + Random.Range(-HD + 0.9f, HD - 0.9f)); }
                    var p = a.t.position; var d = a.tgt - p; d.y = 0f;
                    bool moving = d.magnitude > 0.15f;
                    if (moving)
                    {
                        float sp = a.chicken ? 0.55f : 0.9f;
                        var np = p + d.normalized * sp * dt; a.t.position = new Vector3(np.x, VillageWorld.Height(np.x, np.z), np.z);
                        a.t.rotation = Quaternion.Slerp(a.t.rotation, Quaternion.LookRotation(d.normalized, Vector3.up), dt * 6f);
                    }
                    float hop = a.chicken ? 0f : (moving ? Mathf.Abs(Mathf.Sin(a.ph * 9f)) * 0.14f * a.scale : 0f);
                    float peck = a.chicken && !moving ? Mathf.Max(0f, Mathf.Sin(a.ph * 5f)) * 18f : 0f;
                    var kid = a.t.childCount > 0 ? a.t.GetChild(0) : null;
                    if (kid != null) { kid.localPosition = new Vector3(0f, hop, 0f); kid.localRotation = Quaternion.Euler(peck, kid.localEulerAngles.y, 0f); }
                }
            }
        }
    }
}
