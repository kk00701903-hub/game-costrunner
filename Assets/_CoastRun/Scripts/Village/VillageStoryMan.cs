using System;
using UnityEngine;

namespace CoastRun.Village
{
    /// 198차(사용자: 「스토리모드에서 남자 주인공도 캐릭터 만들어줘 — 주인공이 다가가면 남자 주인공은 계속 멀어진다, 그러다가 사라진다」):
    /// 흰 셔츠에 우유 두 병을 든 키 큰 남자. 3장~19장 동안 마을 몇 군데(정류장·가게 앞·탑 길·방파제·등대 길)에 번갈아 서 있다.
    /// 9 m 안으로 다가가면 등을 돌려 걸어서 멀어지고, 5초 넘게 쫓거나 3 m 안까지 붙으면 흐려지며 사라진다. 1~2분 뒤 다른 곳에 다시 선다.
    /// 말은 하지 않는다(정체를 흘리지 않기 — 스타일 가이드).
    public class VillageStoryMan : MonoBehaviour
    {
        public Func<bool> Hidden;   // 집 안·시내·관광지·컷씬 중이면 true
        public Transform Player;
        public static VillageStoryMan I;
        Transform _root, _pivot; CharacterMotion _motion; Renderer[] _rends; Vector3 _vel;
        enum St { Wait, Idle, Leave, Fade }
        St _st = St.Wait; float _t, _leaveT, _fade = 1f; int _spotIdx;
        static readonly Vector2[] Spots = {
            new Vector2(66.0f, 9.5f),    // 버스 정류장(동쪽 차도) — VillageEast.Stop 근처
            new Vector2(-2.6f, -27.2f),  // 가게 앞
            new Vector2(8.2f, 37.6f),    // 송전탑 가는 길
            new Vector2(1.5f, -28.5f),   // 방파제 쪽 바닷가
            new Vector2(-8.5f, -51.5f),  // 등대 가는 길
        };

        public static VillageStoryMan Create(Transform parent, Transform player, Func<bool> hidden)
        {
            var go = new GameObject("StoryMan"); go.transform.SetParent(parent, false);
            var m = go.AddComponent<VillageStoryMan>(); m.Player = player; m.Hidden = hidden; I = m; m.Build(); return m;
        }

        void Build()
        {
            _root = new GameObject("ManRoot").transform; _root.SetParent(transform, false);
            _pivot = new GameObject("Pivot").transform; _pivot.SetParent(_root, false);
            var rig = SkaterRig.SpawnModel(ArtAssets.ResourceRoot + "Rig/Npc_Surfer", _pivot, 1.12f, true);
            if (rig != null)
            {
                var anim = rig.GetComponent<Animator>(); if (anim != null) { anim.SetBool("Grounded", true); anim.Play("Run", 0, 0.12f); anim.speed = 0f; }
                _motion = _pivot.gameObject.AddComponent<CharacterMotion>(); _motion.Anim = anim; _motion.WalkSpeed = 1.3f; _motion.RunSpeed = 3.2f; _motion.BobScale = 0.7f; _motion.FootDust = false;
                Recolor(rig.gameObject);
                foreach (var smr in rig.GetComponentsInChildren<SkinnedMeshRenderer>()) if (smr.GetComponent<CelOutlineHint>() == null) smr.gameObject.AddComponent<CelOutlineHint>();
            }
            else CoastFigureMesh.BuildHaneul(_pivot, 1.1f);
            // 우유 두 병(가슴 앞, 오른손 쪽)
            var milk = CoastMaterials.CreateLit(new Color(0.97f, 0.97f, 0.95f), 0.4f); var cap = CoastMaterials.CreateLit(new Color(0.35f, 0.55f, 0.85f), 0.2f);
            for (int k = 0; k < 2; k++)
            {
                var b = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(b.GetComponent<Collider>()); b.name = "Milk"; b.transform.SetParent(_root, false);
                b.transform.localPosition = new Vector3(0.20f + k * 0.09f, 0.72f, 0.18f); b.transform.localScale = new Vector3(0.07f, 0.09f, 0.07f); b.GetComponent<MeshRenderer>().sharedMaterial = milk;
                var c = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(c.GetComponent<Collider>()); c.transform.SetParent(b.transform, false); c.transform.localPosition = new Vector3(0f, 1.1f, 0f); c.transform.localScale = new Vector3(0.8f, 0.15f, 0.8f); c.GetComponent<MeshRenderer>().sharedMaterial = cap;
            }
            GroundBlob.Attach(_root, 0.42f, 0.34f, _pivot);
            _rends = System.Array.FindAll(_root.GetComponentsInChildren<Renderer>(true), x => x.enabled);   // 숨긴 서프보드는 빼고
            _root.gameObject.SetActive(false); _t = 3f;
        }

        /// 흰 셔츠 · 짙은 바지 · 검은 머리(모델 재질 이름으로 골라 칠함)
        static void Recolor(GameObject rig)
        {
            var names = new System.Text.StringBuilder();
            foreach (var r in rig.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var mats = r.materials; names.Append("[" + r.name + "]"); bool allBoard = true;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i]; if (m == null) continue; string n = m.name.ToLowerInvariant(); names.Append(m.name).Append(',');
                    Color? c = null; if (!n.Contains("board")) allBoard = false;
                    if (n.Contains("skin")) c = new Color(0.99f, 0.86f, 0.76f);   // 서퍼의 그을린 피부 → 밝게
                    else if (n.Contains("mintd")) c = new Color(0.16f, 0.18f, 0.24f);   // Npc_Surfer: 짙은 민트 = 바지
                    else if (n.Contains("mint")) c = new Color(0.97f, 0.97f, 0.98f);   // 민트 = 셔츠
                    else if (n.Contains("hair")) c = new Color(0.08f, 0.07f, 0.09f);
                    else if (n.Contains("shirt") || n.Contains("top") || n.Contains("tee") || n.Contains("jacket") || n.Contains("cloth") || n.Contains("hood")) c = new Color(0.97f, 0.97f, 0.98f);
                    else if (n.Contains("pant") || n.Contains("short") || n.Contains("bottom") || n.Contains("trouser") || n.Contains("jean")) c = new Color(0.16f, 0.18f, 0.24f);
                    else if (n.Contains("shoe") || n.Contains("sneaker")) c = new Color(0.92f, 0.92f, 0.94f);
                    if (c.HasValue) { if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c.Value); if (m.HasProperty("_Color")) m.SetColor("_Color", c.Value); if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", null); if (m.HasProperty("_MainTex")) m.mainTexture = null; }
                }
                r.materials = mats; if (allBoard && mats.Length > 0) r.enabled = false;   // 서프보드는 들지 않는다
                // 한 메시 안의 보드 부분(서브메시)은 삼각형을 비워서 지운다
                var sm = r.sharedMesh; bool cloned = false;
                for (int i = 0; i < mats.Length && sm != null && i < sm.subMeshCount; i++)
                {
                    if (mats[i] == null || !mats[i].name.ToLowerInvariant().Contains("board")) continue;
                    if (!cloned) { sm = Instantiate(sm); cloned = true; }
                    sm.SetTriangles(new int[0], i);
                }
                if (cloned) r.sharedMesh = sm;
            }
            Debug.LogWarning("[StoryMan] mats: " + names);
        }

        bool Active(SaveData s) => s != null && VillageStory.PlaceMode && s.prologueSeen && s.chapter >= 3 && s.chapter <= 19;

        void Update()
        {
            var hub = VillageHub.I; var s = hub != null ? hub.SaveRef : null;
            bool hide = !Active(s) || (Hidden != null && Hidden()) || Player == null;
            if (hide) { if (_root.gameObject.activeSelf) _root.gameObject.SetActive(false); _st = St.Wait; if (_t < 3f) _t = 3f; return; }
            float dt = Time.deltaTime; var p = _root.position; var pp = Player.position;
            var away = p - pp; away.y = 0f; float dist = away.magnitude;
            switch (_st)
            {
                case St.Wait:
                    _t -= dt;
                    if (_t <= 0f)
                    {
                        // 주인공에게서 20 m 넘게 떨어진 다음 자리에 선다
                        for (int k = 0; k < Spots.Length; k++)
                        {
                            _spotIdx = (_spotIdx + 1) % Spots.Length; var sp = Spots[_spotIdx];
                            if (Vector2.Distance(sp, new Vector2(pp.x, pp.z)) > 20f) break;
                        }
                        var g = VillageWorld.Ground(Spots[_spotIdx].x, Spots[_spotIdx].y);
                        _root.position = g; _root.rotation = Quaternion.Euler(0f, 180f, 0f); _vel = Vector3.zero;   // 바다(남쪽)를 본다
                        SetFade(1f); _root.localScale = Vector3.one; _root.gameObject.SetActive(true); _st = St.Idle;
                    }
                    break;
                case St.Idle:
                    if (dist < 9f) { _st = St.Leave; _leaveT = 0f; }
                    break;
                case St.Leave:
                {
                    _leaveT += dt;
                    var dir = dist > 0.05f ? away / dist : _root.forward; dir.y = 0f;
                    float speed = 2.1f;
                    var want = dir * speed;
                    _vel = Vector3.MoveTowards(_vel, want, dt * 6f);
                    var np = p + _vel * dt; float h = VillageWorld.Height(np.x, np.z);
                    if (h < VillageWorld.SeaLevel + 0.4f || Mathf.Abs(np.x) > 70f || np.z > 58f || np.z < -62f)
                    { _vel = Quaternion.Euler(0f, 70f, 0f) * _vel; np = p + _vel * dt; h = VillageWorld.Height(np.x, np.z); }   // 물가·경계면 옆으로 비켜 걷는다
                    _root.position = new Vector3(np.x, h, np.z);
                    if (_vel.sqrMagnitude > 0.01f) _root.rotation = Quaternion.Slerp(_root.rotation, Quaternion.LookRotation(_vel.normalized, Vector3.up), dt * 6f);
                    if (_leaveT > 5f || dist < 3f) { _st = St.Fade; _fade = 1f; }
                    else if (dist > 16f) _st = St.Idle;   // 멀어지면 다시 멈춰 선다(바다 쪽을 본다)
                    break;
                }
                case St.Fade:
                    _fade -= dt / 1.2f; SetFade(Mathf.Clamp01(_fade)); _root.position += _root.forward * 1.2f * dt;
                    if (_fade <= 0f) { _root.gameObject.SetActive(false); _st = St.Wait; _t = UnityEngine.Random.Range(60f, 120f); }
                    break;
            }
        }

        void SetFade(float a)
        {
            // 흐려지며 사라진다: 크기를 살짝 줄이고, 재질 알파를 내린다(투명을 못 하는 재질은 크기만)
            _root.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, a);
            if (_rends == null) return;
            foreach (var r in _rends)
            {
                if (r == null) continue; r.enabled = a > 0.02f;
                foreach (var m in r.materials) { if (m == null) continue; if (m.HasProperty("_BaseColor")) { var c = m.GetColor("_BaseColor"); c.a = a; m.SetColor("_BaseColor", c); } }
            }
        }

        public string DevState() { var d = Player != null ? Vector3.Distance(new Vector3(_root.position.x, 0f, _root.position.z), new Vector3(Player.position.x, 0f, Player.position.z)) : -1f; return $"st={_st} active={_root.gameObject.activeSelf} dist={d:F1} fade={_fade:F2} pos={_root.position}"; }
        /// 개발용: 주인공을 남자 6 m 앞에 세운다(다가간 것처럼)
        public void DevApproach(Action<Vector3> teleport) { if (Player == null || !_root.gameObject.activeSelf) return; var dir = Player.position - _root.position; dir.y = 0f; dir = dir.sqrMagnitude > 0.01f ? dir.normalized : Vector3.back; teleport(_root.position + dir * 6f); }
        // 개발용: 주인공 앞 8 m 에 세운다
        public void DevShow()
        {
            var sv = VillageHub.I != null ? VillageHub.I.SaveRef : null; Debug.LogWarning($"[StoryMan] dev active={Active(sv)} ch={(sv != null ? sv.chapter : -1)} place={VillageStory.PlaceMode} hidden={(Hidden != null && Hidden())} player={(Player != null)} rends={(_rends != null ? _rends.Length : 0)}");
            if (Player == null) return; var cf = Camera.main != null ? Camera.main.transform.forward : Player.forward; cf.y = 0f; cf.Normalize(); var pos = Player.position + cf * 13f;
            _root.position = VillageWorld.Ground(pos.x, pos.z); _root.rotation = Quaternion.LookRotation(-cf, Vector3.up); SetFade(1f); _root.gameObject.SetActive(true); _st = St.Idle;
        }
    }
}
