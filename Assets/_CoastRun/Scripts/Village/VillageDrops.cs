using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoastRun.Village
{
    /// 159차(사용자: 「부순 나무·바위의 조각은 근처 가면 자동으로 흡수해서 아이템에」):
    /// 나무 패기·돌 캐기로 튀어나온 조각(장작·돌)을 바닥에 흩뿌리고, 주인공이 2 m 안에 오면 빨려 들어가듯 날아와 가방에 들어간다.
    public class VillageDrops : MonoBehaviour
    {
        class Drop { public Transform t; public string item; public Vector3 vel; public bool landed, flying; public float bob, fly; public Color col; }
        readonly List<Drop> _drops = new List<Drop>();
        Transform _player; Func<bool> _locked; Action<string, int> _onAbsorb;
        public float Magnet = 2.7f;

        public static VillageDrops Create(Transform parent, Transform player, Func<bool> locked, Action<string, int> onAbsorb)
        {
            var go = new GameObject("VillageDrops"); go.transform.SetParent(parent, false);
            var d = go.AddComponent<VillageDrops>(); d._player = player; d._locked = locked; d._onAbsorb = onAbsorb; return d;
        }

        public int Count => _drops.Count;

        /// 조각 n개를 from 에서 사방으로 튀긴다.
        public void Spawn(Vector3 from, string item, int n, Color col)
        {
            for (int i = 0; i < n; i++)
            {
                GameObject g;
                if (item == "mat_wood") { g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.transform.localScale = new Vector3(0.34f, 0.14f, 0.14f); }
                else { g = GameObject.CreatePrimitive(PrimitiveType.Sphere); g.transform.localScale = new Vector3(0.24f, 0.19f, 0.22f); }
                Destroy(g.GetComponent<Collider>()); g.name = "Drop_" + item; g.transform.SetParent(transform, false);
                g.transform.position = from; g.transform.rotation = UnityEngine.Random.rotation;
                g.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateLit(col);
                var a = UnityEngine.Random.value * Mathf.PI * 2f; float sp = 0.7f + UnityEngine.Random.value * 1.1f;
                _drops.Add(new Drop { t = g.transform, item = item, col = col, vel = new Vector3(Mathf.Cos(a) * sp, 3.2f + UnityEngine.Random.value * 1.2f, Mathf.Sin(a) * sp), bob = UnityEngine.Random.value * 6f });
            }
        }

        /// 남은 조각을 전부 가방에(씬을 떠날 때).
        public void AbsorbAll()
        {
            var sum = new Dictionary<string, int>();
            foreach (var d in _drops) { sum[d.item] = (sum.ContainsKey(d.item) ? sum[d.item] : 0) + 1; if (d.t != null) Destroy(d.t.gameObject); }
            _drops.Clear();
            foreach (var kv in sum) _onAbsorb?.Invoke(kv.Key, kv.Value);
        }

        void Update()
        {
            if (_player == null) return;
            float dt = Time.deltaTime; bool locked = _locked != null && _locked();
            var pp = _player.position + Vector3.up * 0.7f;
            for (int i = _drops.Count - 1; i >= 0; i--)
            {
                var d = _drops[i]; if (d.t == null) { _drops.RemoveAt(i); continue; }
                if (d.flying)
                {
                    d.fly += dt * 2.6f;
                    d.t.position = Vector3.Lerp(d.t.position, pp, Mathf.Clamp01(d.fly * d.fly * 0.5f + dt * 6f));
                    d.t.localScale *= 1f - dt * 1.4f;
                    d.t.Rotate(0f, 540f * dt, 0f, Space.World);
                    if (Vector3.Distance(d.t.position, pp) < 0.25f || d.fly > 1.6f)
                    {
                        _onAbsorb?.Invoke(d.item, 1); Destroy(d.t.gameObject); _drops.RemoveAt(i);
                    }
                    continue;
                }
                if (!d.landed)
                {
                    d.vel.y -= 11f * dt; var p = d.t.position + d.vel * dt;
                    float gy = VillageWorld.Height(p.x, p.z) + 0.10f;
                    if (p.y <= gy && d.vel.y < 0f) { p.y = gy; d.vel = Vector3.zero; d.landed = true; }
                    d.t.position = p; d.t.Rotate(new Vector3(240f, 160f, 0f) * dt);
                }
                else
                {
                    float gy = VillageWorld.Height(d.t.position.x, d.t.position.z) + 0.12f + Mathf.Sin(Time.time * 3f + d.bob) * 0.04f;
                    d.t.position = new Vector3(d.t.position.x, gy, d.t.position.z);
                    d.t.Rotate(0f, 60f * dt, 0f, Space.World);
                    if (!locked && Vector3.Distance(new Vector3(pp.x, 0f, pp.z), new Vector3(d.t.position.x, 0f, d.t.position.z)) < Magnet) { d.flying = true; d.fly = 0f; }
                }
            }
        }
    }
}
