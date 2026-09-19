using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoastRun.Village
{
    /// 150차: 주울 것들 — 나뭇가지(장작), 조개껍데기(바닷가), 버섯(귤나무 그늘). 주마다 자리가 새로 정해지고(주 번호 시드),
    /// 주운 것은 SaveData.villagePickMask 비트로 기억해 그 주엔 다시 안 난다.
    public static class VillagePickups
    {
        public enum Kind { Branch, Shell, Mushroom }
        public class Pick { public int idx; public Kind kind; public Vector3 pos; public GameObject go; }
        public const int Branches = 10, Shells = 7, Mushrooms = 6;
        public static int Total => Branches + Shells + Mushrooms;

        public static void EnsureWeek(SaveData s) { if (s == null) return; if (s.villagePickWeek != s.week) { s.villagePickWeek = s.week; s.villagePickMask = 0; } }
        public static bool Taken(SaveData s, int idx) => s != null && (s.villagePickMask & (1L << idx)) != 0;
        public static void Take(SaveData s, int idx) { if (s != null) s.villagePickMask |= 1L << idx; }

        public static string ItemId(Kind k) => k == Kind.Branch ? "mat_wood" : k == Kind.Shell ? "gath_shell" : "gath_mushroom";
        public static string Title(Kind k) => k == Kind.Branch ? Loc.T("🪵 나뭇가지 줍기", "🪵 Pick up branch") : k == Kind.Shell ? Loc.T("🐚 조개껍데기 줍기", "🐚 Pick up shell") : Loc.T("🍄 버섯 따기", "🍄 Pick mushroom");
        public static string Got(Kind k) => k == Kind.Branch ? Loc.T("🪵 나뭇가지를 주웠다 → 장작 +1", "🪵 Picked a branch → Firewood +1") : k == Kind.Shell ? Loc.T("🐚 예쁜 조개껍데기! 가방에 넣었다", "🐚 A pretty shell! Into the bag") : Loc.T("🍄 버섯을 땄다 → 재료(볶음)", "🍄 Picked a mushroom → ingredient");

        /// 이번 주 자리 만들기 + 아직 안 주운 것만 세운다.
        public static List<Pick> Build(Transform root, SaveData s)
        {
            var old = root.Find("Pickups"); if (old != null) UnityEngine.Object.Destroy(old.gameObject);
            var host = new GameObject("Pickups").transform; host.SetParent(root, false);
            var list = new List<Pick>();
            if (s == null) return list;
            EnsureWeek(s);
            var rng = new System.Random(s.week * 7919 + 150);
            int idx = 0;
            // 나뭇가지: 귤나무·야자수 근처 풀밭
            var trees = new List<Vector3>(); foreach (var t in VillageWorld.Trees) if (t != null) trees.Add(t.position);
            for (int i = 0; i < Branches; i++, idx++)
            {
                Vector3 p;
                if (trees.Count > 0) { var t = trees[rng.Next(trees.Count)]; float a = (float)rng.NextDouble() * 6.28f, r = 1.6f + (float)rng.NextDouble() * 2.2f; p = new Vector3(t.x + Mathf.Cos(a) * r, 0f, t.z + Mathf.Sin(a) * r); }
                else p = new Vector3((float)rng.NextDouble() * 40f - 20f, 0f, (float)rng.NextDouble() * 30f - 15f);
                Add(list, host, s, idx, Kind.Branch, p);
            }
            // 조개: 모래사장(z -27 ~ -31, x -3 ~ 14)
            for (int i = 0; i < Shells; i++, idx++)
                Add(list, host, s, idx, Kind.Shell, new Vector3(-3f + (float)rng.NextDouble() * 17f, 0f, -26.5f - (float)rng.NextDouble() * 4f));
            // 버섯: 귤나무 그늘(바로 아래 0.9~1.5 m)
            for (int i = 0; i < Mushrooms; i++, idx++)
            {
                Vector3 p;
                if (trees.Count > 0) { var t = trees[rng.Next(trees.Count)]; float a = (float)rng.NextDouble() * 6.28f, r = 0.9f + (float)rng.NextDouble() * 0.7f; p = new Vector3(t.x + Mathf.Cos(a) * r, 0f, t.z + Mathf.Sin(a) * r); }
                else p = new Vector3((float)rng.NextDouble() * 30f - 15f, 0f, (float)rng.NextDouble() * 20f);
                Add(list, host, s, idx, Kind.Mushroom, p);
            }
            return list;
        }

        static void Add(List<Pick> list, Transform host, SaveData s, int idx, Kind k, Vector3 p)
        {
            if (Taken(s, idx)) return;
            if (VillageWorld.NearBuildingPublic(p.x, p.z, 2.0f) || VillageWorld.Height(p.x, p.z) < VillageWorld.SeaLevel + 0.05f) return;
            p.y = VillageWorld.Height(p.x, p.z);
            var pk = new Pick { idx = idx, kind = k, pos = p };
            var go = new GameObject("Pick_" + k + "_" + idx); go.transform.SetParent(host, false); go.transform.position = p; go.transform.rotation = Quaternion.Euler(0f, idx * 47f, 0f);
            switch (k)
            {
                case Kind.Branch:
                    Prim(go.transform, PrimitiveType.Cylinder, new Vector3(0f, 0.07f, 0f), new Vector3(0.09f, 0.42f, 0.09f), new Color(0.50f, 0.34f, 0.20f), Quaternion.Euler(84f, 0f, 12f));
                    Prim(go.transform, PrimitiveType.Cylinder, new Vector3(0.18f, 0.10f, 0.10f), new Vector3(0.06f, 0.18f, 0.06f), new Color(0.55f, 0.38f, 0.22f), Quaternion.Euler(70f, 40f, 0f));
                    break;
                case Kind.Shell:
                    Prim(go.transform, PrimitiveType.Sphere, new Vector3(0f, 0.06f, 0f), new Vector3(0.30f, 0.11f, 0.26f), new Color(1f, 0.88f, 0.80f), Quaternion.identity);
                    Prim(go.transform, PrimitiveType.Sphere, new Vector3(0f, 0.10f, 0.02f), new Vector3(0.18f, 0.05f, 0.14f), new Color(0.98f, 0.70f, 0.72f), Quaternion.identity);
                    break;
                default:
                    Prim(go.transform, PrimitiveType.Cylinder, new Vector3(0f, 0.12f, 0f), new Vector3(0.09f, 0.12f, 0.09f), new Color(0.96f, 0.92f, 0.82f), Quaternion.identity);
                    Prim(go.transform, PrimitiveType.Sphere, new Vector3(0f, 0.26f, 0f), new Vector3(0.34f, 0.20f, 0.34f), new Color(0.85f, 0.42f, 0.30f), Quaternion.identity);
                    Prim(go.transform, PrimitiveType.Sphere, new Vector3(0.08f, 0.33f, 0.05f), new Vector3(0.07f, 0.04f, 0.07f), Color.white, Quaternion.identity);
                    Prim(go.transform, PrimitiveType.Sphere, new Vector3(-0.09f, 0.31f, -0.06f), new Vector3(0.06f, 0.04f, 0.06f), Color.white, Quaternion.identity);
                    break;
            }
            go.AddComponent<PickBob>();
            pk.go = go; list.Add(pk);
        }

        static void Prim(Transform parent, PrimitiveType t, Vector3 pos, Vector3 scale, Color c, Quaternion rot)
        {
            var g = GameObject.CreatePrimitive(t); UnityEngine.Object.Destroy(g.GetComponent<Collider>()); g.transform.SetParent(parent, false);
            g.transform.localPosition = pos; g.transform.localScale = scale; g.transform.localRotation = rot;
            g.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateLit(c);
        }

        /// 살짝 반짝(주울 수 있다는 표시): 천천히 위아래 + 회전
        public class PickBob : MonoBehaviour
        {
            float _ph; Vector3 _p;
            void Start() { _p = transform.position; _ph = UnityEngine.Random.value * 6f; }
            void Update() { transform.position = _p + Vector3.up * (0.03f + Mathf.Sin(Time.time * 2.2f + _ph) * 0.03f); }
        }
    }
}
