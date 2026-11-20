using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun.Village
{
    /// 137차(사용자): 마을 NPC(치비 리그 재활용, 길 따라 산책, 주인공을 보면 「누구세요?」「음…」만 하고 말을 안 걸어줌),
    /// 주인공을 따라다니는 꼬마, 정령·나비(잠자리채로 잡음), 큰 벌레(왕사슴벌레·왕말벌 — 방망이로 때림, 부딪히면 HP 깎임).
    /// 170차(사용자): 「정령이 잘 안 보인다 → 크게. 정령은 잠자리채로 잡고, 대형 벌레는 방망이로 때린다」 — 정령은 이제 다치게 하지 않고 곁에서 맴돈다.
    public class VillageCreatures : MonoBehaviour
    {
        /// 208차(사용자: 「모션 부드러움」): 몸 돌리기를 한 프레임에 확 꺾지 않고 스르륵 — 진행 방향으로 지수 감쇠 회전
        /// 209차: 움직임 점검(DevMotionProbe)용 — 지금 살아 있는 NPC·산적·벌레·귀신·정령·꼬마
        public void DevTracked(List<KeyValuePair<string, Transform>> l)
        {
            foreach (var n in _npcs) if (n.root != null) l.Add(new KeyValuePair<string, Transform>("npc", n.root));
            foreach (var b in _bandits) if (b.root != null && !b.down) l.Add(new KeyValuePair<string, Transform>(b.police ? "police" : "bandit", b.root));
            foreach (var c in _crit) if (c.t != null) l.Add(new KeyValuePair<string, Transform>(c.ghost ? "ghost" : c.spirit ? "spirit" : c.big ? "bigbug" : "bug", c.t));
            if (_kid != null) l.Add(new KeyValuePair<string, Transform>("kid", _kid));
        }
        static void BrakeNpc(Npc n, Vector3 p, float dt) { n.vel = Vector3.SmoothDamp(n.vel, Vector3.zero, ref n.acc, 0.15f, 100f, dt); if (n.vel.sqrMagnitude < 1e-6f) { n.vel = Vector3.zero; return; } var bp = p + n.vel * dt; bp.y = VillageWorld.Height(bp.x, bp.z); n.root.position = bp; }
        static void Face(Transform t, Vector3 dir, float k = 12f) { if (t == null || dir.sqrMagnitude < 1e-8f) return; t.rotation = Quaternion.Slerp(t.rotation, Quaternion.LookRotation(dir, Vector3.up), 1f - Mathf.Exp(-Time.deltaTime * k)); }
        public Transform Player; public System.Func<bool> Locked;
        public System.Action<int> OnSpiritHit;           // HP 깎기 (170차: 큰 벌레 접촉)
        public System.Action<int> OnGhostHit;            // 155차: 밤 귀신(엄청 강함) 접촉
        public System.Action<string, int, int> OnCaught; // (종류, 별조각, 코인)
        public static int NetTier, BatTier;   // 198차: 도구 등급(VillageHub.PushToolTiers)
        // 175차(사용자: 「포켓몬스터처럼, 펫 상점에서 파는 펫을 몬스터로 잡는 것도 — 밤에만, 5% 확률」)
        public bool Night;                              // VillageHub 가 매 프레임 알려 준다
        public System.Action<int> OnMonsterCaught;      // 잡은 몬스터가 어떤 펫인지(PetKind 값)

        // ── NPC ──
        class Npc { public Transform root; public Animator anim; public CharacterMotion motion; public Vector3 home, target, vel, acc; public float wait; public SpeechBubble bubble; public float said; public int line; public string name; public bool noticed; public float idleAct; }
        readonly List<Npc> _npcs = new List<Npc>();
        static readonly string[] NpcKo = { "누구세요?", "음…", "…?", "처음 보는 얼굴인데.", "(고개를 갸웃한다)" };
        static readonly string[] NpcEn = { "Who are you?", "Hmm…", "…?", "Never seen you before.", "(tilts head)" };
        static readonly Color[] Shirts = { new Color(0.55f, 0.75f, 0.98f), new Color(0.98f, 0.70f, 0.45f), new Color(0.72f, 0.88f, 0.55f), new Color(0.90f, 0.60f, 0.90f) };

        // ── 꼬마 ──
        Transform _kid; Vector3 _kidVel; int _kidLine;

        // ── 171차: 산적 ──
        class Bandit { public Transform root, pivot; public CharacterMotion motion; public int hp; public float stun, ph, said, life; public SpeechBubble bubble; public bool down, police; }
        readonly List<Bandit> _bandits = new List<Bandit>();
        float _banditT = 90f;   // 195차(사용자: 「산적이 너무 자주」): 첫 출몰 25 → 90 초
        public int BanditsAlive { get { int n = 0; foreach (var b in _bandits) if (!b.down && b.root != null && !b.police) n++; return n; } }
        public int PoliceAlive { get { int n = 0; foreach (var b in _bandits) if (!b.down && b.root != null && b.police) n++; return n; } }

        // ── 정령·나비 ──
        class Critter { public Vector3 kb; public float v; public bool rare; public Transform t; public bool spirit; public bool ghost; public bool big; public float stun; public int kind; public int pet; public float life, phase; public Vector3 anchor; }   // kind: 0 나비 1 잠자리 2 무당벌레 4 닭 5 토끼 6 몬스터 · big: 0 왕사슴벌레 1 왕말벌
        static bool BatTarget(Critter c) => c.big || c.ghost;   // 170차: 방망이 = 큰 벌레·귀신, 잠자리채 = 정령·작은 벌레
        readonly List<Critter> _crit = new List<Critter>();
        float _spawnT = 4f;

        public static VillageCreatures Create(Transform parent, Transform player)
        {
            var go = new GameObject("VillageCreatures"); go.transform.SetParent(parent, false);
            var c = go.AddComponent<VillageCreatures>(); c.Player = player;
            c.BuildNpcs(); c.BuildKid();
            return c;
        }

        // ── NPC 만들기 ─────────────────────────────────────────────────
        // 140차: 첨부 캐릭터 시트를 블렌더(npc_chibi_rig.py)로 모델링한 6종 — 역할별 자리
        struct NpcDef { public string model, ko, en; public Vector2 home; }
        static readonly NpcDef[] Defs = {
            new NpcDef { model = "Npc_Haenyeo", ko = "해녀 할머니", en = "Haenyeo", home = new Vector2(-2f, -29f) },
            new NpcDef { model = "Npc_Keeper", ko = "등대지기", en = "Lighthouse keeper", home = new Vector2(-19.5f, -57f) },
            new NpcDef { model = "Npc_Florist", ko = "꽃집 언니", en = "Florist", home = new Vector2(-8f, -9f) },
            new NpcDef { model = "Npc_FisherBoy", ko = "낚시 소년", en = "Fisher boy", home = new Vector2(0.5f, -28.5f) },
            new NpcDef { model = "Npc_Cafe", ko = "카페 알바", en = "Cafe clerk", home = new Vector2(-9f, -37f) },
            new NpcDef { model = "Npc_Surfer", ko = "서퍼", en = "Surfer", home = new Vector2(-2f, -24f) },
        };

        void BuildNpcs()
        {
            for (int i = 0; i < Defs.Length; i++)
            {
                var d = Defs[i]; var n = new Npc();
                var root = new GameObject("Npc_" + d.model).transform; root.SetParent(transform, false);
                n.home = VillageWorld.Ground(d.home.x, d.home.y); root.position = n.home; n.root = root; n.name = Loc.T(d.ko, d.en);
                var pivot = new GameObject("Pivot").transform; pivot.SetParent(root, false);
                var rig = SkaterRig.SpawnModel(ArtAssets.ResourceRoot + "Rig/" + d.model, pivot, 1.0f, true) ?? SkaterRig.Spawn(pivot, 1.05f, true);
                if (rig == null) { CoastFigureMesh.BuildHaneul(pivot, 1.05f); }
                else
                {
                    n.anim = rig.GetComponent<Animator>();
                    if (n.anim != null) { n.anim.SetBool("Grounded", true); n.anim.Play("Run", 0, 0.12f); n.anim.speed = 0f; }
                    n.motion = pivot.gameObject.AddComponent<CharacterMotion>(); n.motion.Anim = n.anim; n.motion.WalkSpeed = 1.2f; n.motion.RunSpeed = 3f; n.motion.BobScale = 0.8f;
                    foreach (var smr in rig.GetComponentsInChildren<SkinnedMeshRenderer>()) if (smr.GetComponent<CelOutlineHint>() == null) smr.gameObject.AddComponent<CelOutlineHint>();
                    StartCoroutine(AttachNpcFace(n, rig.gameObject, d.model));   // 161차: NPC 도 주인공과 같은 그림 얼굴(UI_Face_Npc_*)
                }
                                var col = root.gameObject.AddComponent<CapsuleCollider>(); col.center = new Vector3(0f, 0.6f, 0f); col.radius = 0.3f; col.height = 1.2f;
                // 146차: 월드 스페이스 말풍선(꼬리·둥실) + 발밑 블롭 그림자
                n.bubble = SpeechBubble.Create(root, 1.55f);
                GroundBlob.Attach(root, 0.42f, 0.34f, pivot);
                n.target = n.home; n.wait = Random.Range(1f, 3f); n.idleAct = Random.Range(3f, 8f);
                _npcs.Add(n);
            }
        }

        /// 161차(사용자: 「다른 NPC 들도 얼굴 확인」): 3D 눈알 대신 그림 얼굴 데칼 — 모델별 UI_Face_Npc_<이름>, 없으면 주인공 얼굴로 폴백
        System.Collections.IEnumerator AttachNpcFace(Npc n, GameObject rig, string model)
        {
            yield return null;
            var tex = Resources.Load<Texture2D>("CoastRun/Textures/Village/UI_Face_" + model) ?? Resources.Load<Texture2D>("CoastRun/Textures/Village/UI_Face_Haneul");
            if (tex != null && n.anim != null && rig != null) FaceDecal.Attach(n.anim, rig, tex, n.root.forward);
        }

        /// 옷·머리 색 바꾸기(같은 치비 리그를 마을 사람으로) — 머티리얼 이름에 Jacket/Shirt/Top/Pants/Skirt/Hair 가 있으면 그 부분만.
        static void Tint(GameObject go, Color shirt, int seed)
        {
            var rng = new System.Random(seed * 7 + 3);
            Color hair = new Color(0.25f + (float)rng.NextDouble() * 0.4f, 0.18f + (float)rng.NextDouble() * 0.25f, 0.12f + (float)rng.NextDouble() * 0.2f);
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials; bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i]; if (m == null) continue; string nm = m.name.ToLowerInvariant();
                    var mt = m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") : m.mainTexture; if (mt != null) nm += " " + mt.name.ToLowerInvariant();
                    Color? c = null;
                    if (nm.Contains("jacket") || nm.Contains("shirt") || nm.Contains("top") || nm.Contains("cloth")) c = shirt;
                    else if (nm.Contains("pants") || nm.Contains("skirt") || nm.Contains("short") || nm.Contains("leg")) c = Color.Lerp(shirt, new Color(0.25f, 0.28f, 0.40f), 0.7f);
                    else if (nm.Contains("hair")) c = hair;
                    if (c == null) continue;
                    var m2 = new Material(m); if (m2.HasProperty("_BaseColor")) m2.SetColor("_BaseColor", c.Value); else m2.color = c.Value;
                    mats[i] = m2; changed = true;
                }
                if (changed) r.sharedMaterials = mats;
            }
        }

        void TickNpcs(float dt)
        {
            foreach (var n in _npcs)
            {
                if (n.root == null) continue;
                var p = n.root.position; var pp = Player != null ? Player.position : p;
                float dp = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(pp.x, pp.z));
                bool sees = dp < 3.2f;
                // 141차: 6 m 안이면 고개만 돌려 주인공을 쳐다본다(몸은 그대로), 처음 눈에 띄면 「!」
                if (n.motion != null) n.motion.LookTarget = dp < 6.5f ? Player : null;
                if (dp < 5f && !n.noticed) { n.noticed = true; n.said = Time.time - 2.6f; n.bubble.Show("!"); if (n.motion != null) n.motion.Hop(); }
                // 171차: 부탁이 있으면(제안 📜 / 완료 🎁) 8 m 안에서 7 s 마다 알린다
                if (RequestMark != null && dp < 8f && Time.time - n.said > 7f) { int mk = RequestMark(_npcs.IndexOf(n)); if (mk == 1) { n.said = Time.time; n.bubble.Show(n.name + ": " + Loc.T("📜 저기, 부탁이 있는데…", "📜 Um, I have a favor…")); } else if (mk == 2) { n.said = Time.time; n.bubble.Show(n.name + ": " + Loc.T("🎁 다 했어? 고마워!", "🎁 All done? Thank you!")); } }
                else if (dp > 9f) n.noticed = false;
                if (sees)
                {
                    // 주인공을 보면 멈춰서 쳐다보고, 「누구세요?」「음…」만 — 말은 안 걸어준다
                    var f = pp - p; f.y = 0f; if (f.sqrMagnitude > 0.01f) n.root.rotation = Quaternion.Slerp(n.root.rotation, Quaternion.LookRotation(f, Vector3.up), 1f - Mathf.Exp(-dt * 5f));
                    BrakeNpc(n, p, dt);   // 209차: 딱 멈추던 것 → 0.15 s 에 걸쳐 서서히
                    if (Time.time - n.said > 3.5f) { n.said = Time.time; n.bubble.Show(n.name + ": " + Loc.T(NpcKo[n.line % NpcKo.Length], NpcEn[n.line % NpcEn.Length])); n.line++; if (n.motion != null && n.line % 2 == 0) n.motion.Nod(); }
                }
                else
                {
                    if (n.bubble.Visible && Time.time - n.said > 2.5f) n.bubble.Hide();
                    var d = n.target - p; d.y = 0f;
                    if (d.magnitude < 0.35f)
                    {
                        n.wait -= dt; BrakeNpc(n, p, dt);
                        // 서 있을 때 가끔 끄덕·두리번·콩 뛰기
                        n.idleAct -= dt; if (n.idleAct <= 0f) { n.idleAct = Random.Range(3f, 8f); if (n.motion != null) { if (Random.value < 0.5f) n.motion.Nod(); else n.motion.Hop(); } }
                        if (n.wait <= 0f)
                        {
                            // 길 위 임의 지점(집 근처 10m 안)으로
                            var cand = n.home + new Vector3(Random.Range(-6f, 6f), 0f, Random.Range(-6f, 6f));
                            cand.x = Mathf.Clamp(cand.x, -42f, 42f); cand.z = Mathf.Clamp(cand.z, VillageWorld.Peninsula(cand.x, -40f) > 0.5f ? -57f : -29.5f, 42f);
                            if (VillageWorld.Bay(cand.x, cand.z - 1.5f) > 0.05f) cand.z = Mathf.Max(cand.z, -24f);   // 만(바다) 안으로는 안 감
                            cand.y = VillageWorld.Height(cand.x, cand.z); n.target = cand; n.wait = Random.Range(2f, 5f);
                        }
                    }
                    else
                    {
                        // 부드럽게 가속·감속(목표 앞에서 느려짐), 몸은 진행 방향으로
                        float want = Mathf.Min(1.4f, 0.5f + d.magnitude * 0.6f);
                        n.vel = Vector3.SmoothDamp(n.vel, d.normalized * want, ref n.acc, 0.25f, 100f, dt);
                        var np = p + n.vel * dt; np.y = VillageWorld.Height(np.x, np.z);
                        n.root.position = np; n.root.rotation = Quaternion.Slerp(n.root.rotation, Quaternion.LookRotation(d, Vector3.up), 1f - Mathf.Exp(-dt * 6f));
                    }
                }
            }
        }

        /// 가까운 NPC 에게 말 걸기 — 대답은 「누구세요?」「음…」뿐.
        // 171차: 부탁 시스템용 — 가장 가까운 NPC 인덱스 / 이름 / 말풍선
        public int NearestNpcIndex(float r) { if (Player == null) return -1; int best = -1; float bd = r; for (int i = 0; i < _npcs.Count; i++) { var n = _npcs[i]; if (n.root == null) continue; float d = Vector3.Distance(n.root.position, Player.position); if (d < bd) { bd = d; best = i; } } return best; }
        public string NpcName(int i) => i >= 0 && i < _npcs.Count ? _npcs[i].name : "";
        public Vector3 NpcPos(int i) => i >= 0 && i < _npcs.Count && _npcs[i].root != null ? _npcs[i].root.position : Vector3.zero;   // 172차
        public void NpcSay(int i, string line) { if (i < 0 || i >= _npcs.Count || _npcs[i].root == null) return; var n = _npcs[i]; n.said = Time.time; n.bubble.Show(n.name + ": " + line); if (n.motion != null) n.motion.Nod(); }
        public System.Func<int, int> RequestMark;   // NPC 인덱스 → 0 없음 1 📜 2 🎁 3 진행

        /// 187차: 화면 좌표에서 가장 가까운 마을 사람(머리 위치 투영, maxPx 안)
        public int NpcAtScreen(Camera cam, Vector2 screen, float maxPx)
        {
            if (cam == null) return -1; int best = -1; float bd = maxPx;
            for (int i = 0; i < _npcs.Count; i++)
            {
                var n = _npcs[i]; if (n.root == null) continue;
                var sp = cam.WorldToScreenPoint(n.root.position + Vector3.up * 0.9f); if (sp.z <= 0f) continue;
                float d = Vector2.Distance(new Vector2(sp.x, sp.y), screen); if (d < bd) { bd = d; best = i; }
            }
            return best;
        }
        /// 187차: 그 사람의 다음 대사를 말풍선으로(토스트 없이)
        public void TalkNpc(int i)
        {
            if (i < 0 || i >= _npcs.Count || _npcs[i].root == null) return; var n = _npcs[i];
            n.said = Time.time; n.bubble.Show(n.name + ": " + Loc.T(NpcKo[n.line % NpcKo.Length], NpcEn[n.line % NpcEn.Length])); n.line++;
            if (n.motion != null) n.motion.Nod();
        }
        public bool TalkNearestNpc()
        {
            if (Player == null) return false;
            foreach (var n in _npcs)
            {
                if (n.root == null) continue;
                if (Vector3.Distance(n.root.position, Player.position) < 3.2f)
                {
                    n.said = Time.time; n.bubble.Show(n.name + ": " + Loc.T(NpcKo[n.line % NpcKo.Length], NpcEn[n.line % NpcEn.Length])); n.line++;
                    CoastToast.Show(Loc.T("…마을 사람은 낯선 나에게 말을 걸어주지 않는다.", "…The villager doesn't talk to a stranger."));
                    return true;
                }
            }
            return false;
        }

        // ── 꼬마 동행 ──────────────────────────────────────────────────
        void BuildKid()
        {
            // 138차(사용자: 「꼬마가 떠다닌다 — 모델로 걷게」): 빌보드 대신 KidChibi 리그(블렌더 kid_chibi_rig.py, Humanoid) + 러닝 클립
            var root = new GameObject("Kid").transform; root.SetParent(transform, false); _kid = root;
            var pivot = new GameObject("Pivot").transform; pivot.SetParent(root, false);
            var rig = SkaterRig.SpawnModel(ArtAssets.ResourceRoot + "Rig/KidChibi", pivot, 0.92f, true);
            if (rig != null)
            {
                FaceDecal.HideBlush(rig.gameObject);   // 167차: 꼬마 볼터치 제거
                _kidAnim = rig.GetComponent<Animator>();
                if (_kidAnim != null) { _kidAnim.SetBool("Grounded", true); _kidAnim.Play("Run", 0, 0.12f); _kidAnim.speed = 0f; }
                _kidMotion = pivot.gameObject.AddComponent<CharacterMotion>(); _kidMotion.Anim = _kidAnim; _kidMotion.WalkSpeed = 1.8f; _kidMotion.RunSpeed = 4.2f; _kidMotion.Stride = 0.62f; _kidMotion.BobScale = 1.25f; _kidMotion.LookTarget = Player;
                foreach (var smr in rig.GetComponentsInChildren<SkinnedMeshRenderer>()) if (smr.GetComponent<CelOutlineHint>() == null) smr.gameObject.AddComponent<CelOutlineHint>();
            }
            else
            {
                // 모델이 아직 임포트 전이면 임시 스프라이트
                var tex = ArtAssets.LoadTexture("Raise_Kid");
                if (tex != null)
                {
                    var q = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(q.GetComponent<Collider>()); q.transform.SetParent(root, false);
                    float h = 1.4f; q.transform.localPosition = new Vector3(0f, h * 0.5f, 0f); q.transform.localScale = new Vector3(h * tex.width / Mathf.Max(1, tex.height), h, 1f);
                    q.GetComponent<MeshRenderer>().sharedMaterial = CoastMaterials.CreateTexturedTransparent(tex, Color.white); _kidBillboard = q.transform;
                }
            }
            GroundBlob.Attach(root, 0.34f, 0.32f, pivot);
            if (Player != null) { var p = Player.position - Player.forward * 1.2f + Player.right * 0.7f; p.y = VillageWorld.Height(p.x, p.z); root.position = p; root.rotation = Player.rotation; }
        }
        Animator _kidAnim; Transform _kidBillboard; bool _kidMoving; CharacterMotion _kidMotion; float _kidHopT = 4f;
        // 140차: 순간이동 시 꼬마도 같이 — 주인공 옆(앞쪽 오른편)에 바로 세운다
        public void SnapKid()
        {
            if (_kid == null || Player == null) return;
            var want = Player.position - Player.forward * 1.3f + Player.right * 0.8f;
            _kid.position = new Vector3(want.x, VillageWorld.Height(want.x, want.z), want.z); _kidVel = Vector3.zero; _kid.rotation = Player.rotation;
        }

        /// 196차: 이야기 도입(송전탑 위 기상 → CS1 전)엔 꼬마가 아직 없다 · 이야기 장소가 있으면 앞장서서 그쪽으로
        public bool KidHidden; public Vector3? LeadTo;
        void TickKid(float dt)
        {
            if (_kid == null || Player == null) return;
            if (_kid.gameObject.activeSelf == KidHidden) { _kid.gameObject.SetActive(!KidHidden); if (!KidHidden) SnapKid(); }
            if (KidHidden) return;
            var want = Player.position - Player.forward * 1.3f + Player.right * 0.8f;
            if (LeadTo.HasValue) { var ld = LeadTo.Value - Player.position; ld.y = 0f; if (ld.magnitude > 4f) want = Player.position + ld.normalized * 2.3f + Vector3.Cross(Vector3.up, ld.normalized) * 0.7f; }
            var cur = _kid.position; cur.y = 0f; want.y = 0f;
            float far = Vector3.Distance(cur, want);
            var next = far < 0.25f ? cur : Vector3.SmoothDamp(cur, want, ref _kidVel, 0.28f, 4.2f, dt);
            if (far < 0.25f) _kidVel = Vector3.zero;
            float g = VillageWorld.Height(next.x, next.z);
            bool moving = _kidVel.magnitude > 0.25f;
            _kid.position = new Vector3(next.x, g, next.z);
            // 걷는 방향으로 몸을 돌리고, 멈추면 주인공 쪽을 본다
            Vector3 face = moving ? _kidVel : (Player.position - _kid.position); face.y = 0f;
            if (face.sqrMagnitude > 0.001f) _kid.rotation = Quaternion.Slerp(_kid.rotation, Quaternion.LookRotation(face, Vector3.up), dt * 9f);
            // 141차: 애니는 CharacterMotion 이 맡고, 따라 뛸 때 가끔 콩콩 뛴다
            if (_kidMotion == null && _kidAnim != null)
            {
                if (moving) { _kidAnim.speed = Mathf.Clamp(_kidVel.magnitude / 3.2f, 0.55f, 1.25f); _kidAnim.SetFloat("Speed", 1f); }
                else if (_kidMoving) { _kidAnim.Play("Run", 0, 0.12f); _kidAnim.speed = 0f; _kidAnim.SetFloat("Speed", 0f); }
            }
            _kidHopT -= dt; if (_kidHopT <= 0f) { _kidHopT = Random.Range(3.5f, 8f); if (_kidMotion != null && (moving || Random.value < 0.4f)) _kidMotion.Hop(); }
            _kidMoving = moving;
            if (_kidBillboard != null && Camera.main != null) { var f = Camera.main.transform.forward; f.y = 0f; _kidBillboard.rotation = Quaternion.LookRotation(f, Vector3.up); }
        }

        public string KidLine()
        {
            string[] ko = { "누나! 오늘은 바다가 조용해. 낚시하기 좋은 날이야.", "우리집에 들어가면 이번 주 할 일을 정할 수 있어.", "엄마 집에서 낮잠 자면 기운이 나. 펫도 거기 있어!", "상점 아줌마가 오늘 생선을 싸게 판대.", "텃밭에 물 줬어? 주가 바뀌면 쑥쑥 자라.", "귤나무를 흔들면 귤이 떨어져. 일주일에 한 번만!", "마을 사람들은 아직 누나를 몰라서 말을 안 걸어. 곧 친해질 거야.", "정령은 잠자리채로 살짝! 큰 벌레가 오면 방망이로 톡, 부딪히면 아파." };
            string[] en = { "Sis! The sea is calm — good for fishing.", "Go in our house to set this week's plan.", "A nap at Mom's brings energy back. The pets are there!", "The shop has cheap fish today.", "Watered the garden? It grows when the week turns.", "Shake the orange tree for oranges — once a week!", "The villagers don't know you yet, so they won't talk. Soon!", "When a spirit comes, bop it with the bat! Bumping hurts." };
            int i = _kidLine++ % ko.Length; return Loc.T(ko[i], en[i]);
        }

        // ── 정령·나비 ──────────────────────────────────────────────────
        void TickCritters(float dt)
        {
            _spawnT -= dt;
            if (_spawnT <= 0f && Player != null)
            {
                // 154차(사용자: 「언덕에 벌레 자주」): 언덕(z>12)에서는 더 자주·더 많이(벌레 위주)
                bool hill = Player != null && Player.position.z > 12f; bool grass = InGrass(Player.position);   // 199차
                _spawnT = hill ? Random.Range(2.5f, 4.5f) : Random.Range(6f, 10f);
                int spirits = 0, bugs = 0, bigs = 0; foreach (var c in _crit) if (c.big) bigs++; else if (c.spirit) spirits++; else bugs++;
                int bugCap = grass ? 8 : 0;   // 199차: 초원 밖에서는 벌레 없음
                // 171차(사용자: 「바닷가 모래에 꽃게나 다른 생물, 망치(방망이)로 때리면 잡힌다」): 모래밭(z<−15, 해수면 근처)에서는 게 우선
                bool sand = Player.position.z < -15f && VillageWorld.Height(Player.position.x, Player.position.z) < VillageWorld.SeaLevel + 1.6f;
                int crabs = 0; foreach (var c in _crit) if (c.big && c.kind >= 2) crabs++;
                int wild = 0; foreach (var c in _crit) if (!c.big && !c.spirit && c.kind >= 4) wild++;
                int monsters = 0; foreach (var c in _crit) if (c.kind == 6) monsters++;
                if (NearHome(Player.position)) { }   // 203차: 집 둘레에선 아무것도 안 나온다
                else if (Night && monsters == 0 && Random.value < 0.05f) SpawnMonster();   // 175차: 밤에만, 한 마리씩
                else if (sand && crabs < 3 && Random.value < 0.6f) SpawnCrab();
                // 171차(사용자: 「산 쪽에 돌아다니는 닭·토끼를 잠자리채로 잡으면 농장에서 키운다」): 언덕(z>12)에서 최대 3마리
                else if (hill && wild < 3 && Random.value < 0.45f) SpawnWild();
                // 170차: 큰 벌레(방망이 표적)는 최대 2마리, 언덕에서 더 자주
                // 195차(사용자: 「벌레는 산 쪽에 가면 있게」): 큰 벌레는 언덕에서만, 작은 벌레도 언덕 밖에선 2마리까지
                else if (grass && Random.value < 0.30f && bigs - crabs < 2) SpawnBig();   // 199차: 초원에서만   // 197차: 우리집 마당에선 안 나옴
                else
                {
                    bool spirit = Random.value < (hill ? 0.25f : 0.6f) ? spirits < 2 : bugs >= bugCap;
                    if (spirit && spirits < 2) Spawn(true); else if (bugs < bugCap) Spawn(false);
                }
            }
            for (int i = _crit.Count - 1; i >= 0; i--)
            {
                var c = _crit[i]; if (c.t == null) { _crit.RemoveAt(i); continue; }
                c.life -= dt; c.phase += dt;
                if (!c.ghost && (IsBug(c) ? !InGrass(c.t.position) : NearHome(c.t.position)) && c.life > 1.2f) c.life = 1.2f;   // 199·203차: 초원 밖 벌레, 집 둘레의 정령·몬스터는 곧 사라진다   // 199차: 초원 밖(집 근처)으로 나온 벌레는 곧 사라진다
                var p = c.t.position;
                if (c.ghost)
                {
                    // 155차: 귀신 — 빠르게 쫓아옴(2.4 m/s, 달리면 도망칠 수 있음). 방망이로 맞으면 3 s 기절, 닿으면 HP −25.
                    c.stun -= dt;
                    var d = Player.position - p; d.y = 0f; float dist = d.magnitude;
                    float sp = c.stun > 0f ? 0f : 2.4f;
                    c.v = Mathf.MoveTowards(c.v, sp, dt * 5f);   // 209차: 0 ↔ 2.4 m/s 로 딱 바뀌던 것 → 0.5 s 가속
                    var step = (dist > 0.05f ? d.normalized * c.v * dt : Vector3.zero) + c.kb * dt; c.kb *= Mathf.Exp(-dt * 6f);   // 튕겨 나가기(미끄러지듯)
                    float g = VillageWorld.Height(p.x + step.x, p.z + step.z) + 1.0f + Mathf.Sin(c.phase * 2.2f) * 0.3f;
                    c.t.position = new Vector3(p.x + step.x, g, p.z + step.z);
                    if (d.sqrMagnitude > 0.01f) Face(c.t, d.normalized);
                    c.t.localScale = Vector3.one * (0.62f + Mathf.Sin(c.phase * 4f) * 0.05f) * (c.stun > 0f && Mathf.Repeat(c.phase * 8f, 1f) < 0.5f ? 0.7f : 1f);
                    if (c.stun <= 0f && dist < 0.95f)
                    {
                        OnGhostHit?.Invoke(25); c.stun = 1.6f; c.anchor = p - d.normalized * 3.5f;
                        c.kb = -d.normalized * 21f; c.v = 0f;   // 209차: 3.5 m 뒤로 순간이동 → 같은 거리를 0.4 s 동안 밀려남
                    }
                    continue;
                }
                if (c.big && c.kind >= 2)
                {
                    // 171차: 게 — 옆으로 종종걸음(1~2 s 마다 방향 바꿈), 2.6 m 안이면 주인공 반대쪽으로 도망(꽃게 2.2 · 소라게 0.7 m/s). 물지 않는다.
                    c.stun -= dt;
                    var d = Player.position - p; d.y = 0f; float dist = d.magnitude;
                    if (c.stun <= 0f) { c.stun = Random.Range(1f, 2.2f); c.anchor = new Vector3(Mathf.Cos(Random.value * 6.28f), 0f, Mathf.Sin(Random.value * 6.28f)); }
                    var dir = dist < 2.6f && dist > 0.05f ? -d.normalized : c.anchor;
                    float sp = (c.kind == 2 ? (dist < 2.6f ? 2.2f : 0.7f) : (dist < 2.6f ? 0.7f : 0.3f));
                    var np = p + dir * sp * dt;
                    // 모래 밖(풀·물속)으로는 안 나간다
                    float hn = VillageWorld.Height(np.x, np.z);
                    if (np.z > -14f || hn > VillageWorld.SeaLevel + 1.7f || hn < VillageWorld.SeaLevel - 0.3f) { c.anchor = -c.anchor; np = p; }
                    c.t.position = new Vector3(np.x, VillageWorld.Height(np.x, np.z) + 0.05f, np.z);
                    // 게는 옆으로 걷는다: 몸의 앞은 진행 방향과 90°
                    if (dir.sqrMagnitude > 0.01f) Face(c.t, Quaternion.Euler(0f, 90f, 0f) * dir);
                    c.t.localScale = Vector3.one * (1f + Mathf.Abs(Mathf.Sin(c.phase * (sp > 1f ? 22f : 8f))) * 0.04f);
                    if (c.life <= 0f) { Destroy(c.t.gameObject); _crit.RemoveAt(i); }
                    continue;
                }
                if (c.big)
                {
                    // 170차: 큰 벌레 — 주인공을 향해 온다. 왕사슴벌레는 땅을 기고(0.9 m/s), 왕말벌은 1 m 높이에서 비틀비틀 난다(1.5 m/s). 닿으면 HP −5, 1.2 s 물러남.
                    c.stun -= dt;
                    var d = Player.position - p; d.y = 0f; float dist = d.magnitude;
                    float sp = c.stun > 0f ? 0f : (c.kind == 1 ? 1.5f : 0.9f);
                    bool yard = InHomeYard(Player.position);   // 197차(사용자: 「집 앞에서는 벌레에 물리지 않게」): 마당이면 물러난다
                    c.v = Mathf.MoveTowards(c.v, yard ? -sp : sp, dt * 4f);   // 209차: 속도 부드럽게
                    var step = (dist > 0.05f ? d.normalized * c.v * dt : Vector3.zero) + c.kb * dt; c.kb *= Mathf.Exp(-dt * 6f);
                    if (c.kind == 1) step += new Vector3(Mathf.Sin(c.phase * 5f), 0f, Mathf.Cos(c.phase * 4.1f)) * 0.9f * dt;
                    float g = VillageWorld.Height(p.x + step.x, p.z + step.z) + (c.kind == 1 ? 1.0f + Mathf.Sin(c.phase * 6f) * 0.18f : 0.16f);
                    c.t.position = new Vector3(p.x + step.x, g, p.z + step.z);
                    if (d.sqrMagnitude > 0.01f) Face(c.t, d.normalized);
                    if (c.kind == 1) for (int w = 0; w < c.t.childCount; w++) { var ch = c.t.GetChild(w); if (ch.name == "Wing") ch.localRotation = Quaternion.Euler(0f, 0f, (ch.localPosition.x < 0f ? -1f : 1f) * (20f + Mathf.Sin(c.phase * 40f) * 35f)); }
                    if (c.stun <= 0f && dist < 0.9f && !yard)
                    {
                        OnSpiritHit?.Invoke(5); c.stun = 1.2f;
                        c.kb = -d.normalized * 13.2f; c.v = 0f;   // 209차: 2.2 m 순간이동 → 미끄러지듯 물러남
                    }
                    if (c.life <= 0f) { Destroy(c.t.gameObject); _crit.RemoveAt(i); }
                    continue;
                }
                if (c.spirit)
                {
                    // 주인공 쪽으로 천천히 다가와 1.3 m 앞에서 맴돈다(170차: 부딪혀도 안 다침 — 잠자리채로 잡는다). 크기 0.34→0.62
                    var d = Player.position + Vector3.up * 0.9f - p; d.y = 0f; float dist = d.magnitude;
                    var step = dist > 1.3f ? d.normalized * 1.1f * dt : new Vector3(Mathf.Cos(c.phase * 1.4f), 0f, Mathf.Sin(c.phase * 1.4f)) * 0.5f * dt;
                    float g = VillageWorld.Height(p.x + step.x, p.z + step.z) + 1.05f + Mathf.Sin(c.phase * 3f) * 0.28f;
                    c.t.position = new Vector3(p.x + step.x, g, p.z + step.z);
                    c.t.localScale = Vector3.one * (0.36f + Mathf.Sin(c.phase * 5f) * 0.035f) * (c.rare ? 1.15f : 1f);   // 203차: 0.52 → 0.36   // 198차: 0.62 → 0.52(조금 작게)
                    if (dist > 0.05f) Face(c.t, d.normalized);
                }
                else if (c.kind == 6)
                {
                    // 175차: 밤 몬스터 — 1 m 높이에서 둥둥, 2.5 m 안이면 슬금슬금 물러난다(잠자리채로 잡는다)
                    c.stun -= dt;
                    var dm = Player.position - p; dm.y = 0f; float distm = dm.magnitude;
                    if (c.stun <= 0f) { c.stun = Random.Range(1.5f, 3f); c.anchor = new Vector3(Mathf.Cos(Random.value * 6.28f), 0f, Mathf.Sin(Random.value * 6.28f)); }
                    // 175차: 너무 빨리 도망가면 못 잡는다 — 1.2 m 안에서만 살짝 물러난다
                    bool fleem = distm < 1.2f && distm > 0.05f;
                    var dirm = fleem ? -dm.normalized : c.anchor;
                    var npm = p + dirm * (fleem ? 1.1f : 0.45f) * dt;
                    npm.x = Mathf.Clamp(npm.x, -42f, 42f); npm.z = Mathf.Clamp(npm.z, -30f, 44f);
                    c.t.position = new Vector3(npm.x, VillageWorld.Height(npm.x, npm.z) + 0.95f + Mathf.Sin(c.phase * 2.6f) * 0.18f, npm.z);
                }
                else if (c.kind >= 4)
                {
                    // 171차: 야생 닭(4)·토끼(5) — 어슬렁거리다 2.8 m 안이면 도망(토끼 2.6 m/s 깡충, 닭 1.4 m/s 종종). 언덕 밖·집 마당 안으론 안 간다.
                    c.stun -= dt;
                    var d = Player.position - p; d.y = 0f; float dist = d.magnitude;
                    if (c.stun <= 0f) { c.stun = Random.Range(1.2f, 3f); c.anchor = Random.value < 0.35f ? Vector3.zero : new Vector3(Mathf.Cos(Random.value * 6.28f), 0f, Mathf.Sin(Random.value * 6.28f)); }
                    bool flee = dist < 2.8f && dist > 0.05f;
                    var dir = flee ? -d.normalized : c.anchor;
                    float sp = c.kind == 5 ? (flee ? 2.6f : 0.8f) : (flee ? 1.4f : 0.45f);
                    var np = p + dir * sp * dt;
                    if (np.z < 12f || Mathf.Abs(np.x) > 42f || np.z > 44f || VillageLivestock.Inside(np) || (Mathf.Abs(np.x) < 5f && np.z > 31f)) { c.anchor = -c.anchor; np = p; }
                    float hop = c.kind == 5 && dir.sqrMagnitude > 0.01f ? Mathf.Abs(Mathf.Sin(c.phase * (flee ? 12f : 7f))) * 0.18f : 0f;
                    c.t.position = new Vector3(np.x, VillageWorld.Height(np.x, np.z) + hop, np.z);
                    if (dir.sqrMagnitude > 0.01f) c.t.rotation = Quaternion.Slerp(c.t.rotation, Quaternion.LookRotation(dir, Vector3.up), dt * 8f);
                    if (c.kind == 4 && dir.sqrMagnitude < 0.01f && c.t.childCount > 0) c.t.GetChild(0).localRotation = Quaternion.Euler(Mathf.Max(0f, Mathf.Sin(c.phase * 5f)) * 18f, 0f, 0f);   // 닭 쪼기
                }
                else
                {
                    if (c.kind == 1)
                    {
                        // 잠자리: 넓고 빠른 8자, 방향은 진행 방향
                        var o = new Vector3(Mathf.Sin(c.phase * 2.2f) * 3.2f, 0f, Mathf.Sin(c.phase * 4.4f) * 1.6f);
                        var np = c.anchor + o; np.y = VillageWorld.Height(np.x, np.z) + 1.1f + Mathf.Sin(c.phase * 5f) * 0.2f;
                        var dv = np - p; c.t.position = np; if (dv.sqrMagnitude > 1e-4f) Face(c.t, dv.normalized);
                    }
                    else if (c.kind == 2)
                    {
                        // 무당벌레: 땅 위를 천천히 돌며 기어다님
                        var o = new Vector3(Mathf.Cos(c.phase * 0.5f) * 1.0f, 0f, Mathf.Sin(c.phase * 0.5f) * 1.0f);
                        var np = c.anchor + o; np.y = VillageWorld.Height(np.x, np.z) + 0.10f;
                        var dv = np - p; c.t.position = np; if (dv.sqrMagnitude > 1e-5f) Face(c.t, dv.normalized);
                    }
                    else
                    {
                        // 나비: 닻 주변을 8자로 팔랑
                        var o = new Vector3(Mathf.Sin(c.phase * 1.3f) * 2.2f, 0f, Mathf.Sin(c.phase * 2.6f) * 1.2f);
                        var np = c.anchor + o; np.y = VillageWorld.Height(np.x, np.z) + 0.9f + Mathf.Sin(c.phase * 7f) * 0.15f;
                        var dv = np - p; dv.y = 0f; c.t.position = np;   // 209차: 제자리에서 빙빙 돌던 몸 → 날아가는 쪽을 보고, 날갯짓에 맞춰 살짝 기울기
                        if (dv.sqrMagnitude > 1e-6f) { var yawQ = Quaternion.Slerp(Quaternion.Euler(0f, c.t.eulerAngles.y, 0f), Quaternion.LookRotation(dv.normalized, Vector3.up), 1f - Mathf.Exp(-dt * 8f)); c.t.rotation = yawQ * Quaternion.Euler(0f, 0f, Mathf.Sin(c.phase * 14f) * 18f); }
                    }
                }
                if (c.life <= 0f) { Destroy(c.t.gameObject); _crit.RemoveAt(i); }
            }
        }

        /// 175차: 밤 몬스터 — 펫 그림을 빌보드로 띄우고 보랏빛 무리를 두른다. 잠자리채로 잡으면 그 펫을 얻는다.
        static readonly string[] MonsterPetName = { "", "Sparrow", "BikerThug", "WildGoose", "BlackPig" };
        public void DevSpawnMonster() => SpawnMonster(true);
        public void DevSpawnSpirit() { Spawn(true); var c = _crit[_crit.Count - 1]; var p = Player.position + Player.forward * 1.2f; p.y = VillageWorld.Height(p.x, p.z) + 0.9f; c.anchor = p; if (c.t != null) c.t.position = p; }   // 183차 개발용
        void SpawnMonster(bool front = false)
        {
            if (Player == null) return;
            var pp = Player.position; float ang = Random.Range(0f, Mathf.PI * 2f), r = Random.Range(7f, 11f);
            var pos = front ? pp + Player.forward * 1.7f : new Vector3(pp.x + Mathf.Cos(ang) * r, 0f, pp.z + Mathf.Sin(ang) * r);
            pos.x = Mathf.Clamp(pos.x, -42f, 42f); pos.z = Mathf.Clamp(pos.z, -28f, 43f);
            pos.y = VillageWorld.Height(pos.x, pos.z) + 0.95f;
            int pet = Random.Range(1, 5);
            var g = new GameObject("NightMonster"); g.transform.SetParent(transform, false); g.transform.position = pos;
            // 보랏빛 무리
            var halo = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(halo.GetComponent<Collider>()); halo.name = "Halo";
            halo.transform.SetParent(g.transform, false); halo.transform.localScale = Vector3.one * 1.5f;
            halo.GetComponent<MeshRenderer>().sharedMaterial = CoastMaterials.CreateTransparent(new Color(0.72f, 0.52f, 1f, 0.26f));
            // 펫 그림(빌보드)
            var tex = Resources.Load<Texture2D>("CoastRun/Obs_Pet_" + MonsterPetName[pet]);
            if (tex != null)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(q.GetComponent<Collider>()); q.name = "PetArt";
                q.transform.SetParent(g.transform, false); q.transform.localScale = Vector3.one * 1.15f;
                q.GetComponent<MeshRenderer>().sharedMaterial = CoastMaterials.CreateTexturedTransparent(tex, Color.white);
                q.AddComponent<YawBillboard>();
            }
            else P(g.transform, PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.8f, CoastMaterials.CreateUnlit(new Color(0.78f, 0.58f, 1f)));
            _crit.Add(new Critter { kind = 6, pet = pet, life = 46f, phase = Random.value * 6f, anchor = Vector3.zero, t = g.transform });
        }

        /// 170차: 큰 벌레 — 왕사슴벌레(땅, 검은 갈색, 큰 턱)·왕말벌(공중, 노랑·검정 줄무늬). 방망이로만 잡힌다.
        /// 197차: 우리집 마당(집 중심 13 m) — 큰 벌레가 나오지도, 물지도 않는다
        public const float HomeYardR = 13f;
        public static bool InHomeYard(Vector3 p)
        {
            var h = VillageWorld.HeroHouse; var c = h != null ? h.position : new Vector3(0f, 0f, 36f);
            return new Vector2(p.x - c.x, p.z - c.z).magnitude < HomeYardR;
        }
        /// 199차(사용자: 「벌레는 집 근처에는 없고 초원 쪽에서만」): 초원 = 언덕 풀밭(z>12) 중 우리집에서 20 m 넘게 떨어진 곳
        public const float GrassHomeR = 26f;   // 203차(사용자: 「집 근처에 벌레가 나타난다」): 20 → 26 m (방목장 초원 중심 31.6 m 는 초원으로 남김)
        public static bool InGrass(Vector3 p)
        {
            if (p.z <= 12f) return false;
            var h = VillageWorld.HeroHouse; var c = h != null ? h.position : new Vector3(0f, 0f, 36f);
            return new Vector2(p.x - c.x, p.z - c.z).magnitude >= GrassHomeR;
        }
        /// 203차: 우리집 둘레(26 m) — 벌레·정령·밤 몬스터 모두 안 나오고, 들어오면 사라진다(귀신·산적·경찰은 따로)
        public static bool NearHome(Vector3 p)
        {
            var h = VillageWorld.HeroHouse; var c = h != null ? h.position : new Vector3(0f, 0f, 36f);
            return new Vector2(p.x - c.x, p.z - c.z).magnitude < GrassHomeR;
        }
        public string DevCritLog() { var sb = new System.Text.StringBuilder(); foreach (var c in _crit) if (c.t != null) sb.Append($"[{c.t.name} big={c.big} spirit={c.spirit} ghost={c.ghost} kind={c.kind} grass={InGrass(c.t.position)} d={(Player != null ? Vector3.Distance(c.t.position, Player.position) : -1f):F1}] "); return sb.ToString(); }
        public string DevBugLog() { int b = 0, far = 0; foreach (var c in _crit) if (c.t != null && IsBug(c)) { b++; if (!InGrass(c.t.position)) far++; } return $"bugs={b} outsideGrass={far} playerGrass={(Player != null && InGrass(Player.position))} pos={(Player != null ? Player.position.ToString() : "-")}"; }
        static bool IsBug(Critter c) => !c.spirit && !c.ghost && (c.big ? c.kind < 2 : c.kind < 4);
        void SpawnBig()
        {
            var pp = Player.position; float ang = Random.Range(0f, Mathf.PI * 2f), r = Random.Range(7f, 10f);
            var pos = new Vector3(Mathf.Clamp(pp.x + Mathf.Cos(ang) * r, -44f, 44f), 0f, Mathf.Clamp(pp.z + Mathf.Sin(ang) * r, -30f, 44f));
            int kind = Random.value < 0.5f ? 0 : 1;
            pos.y = VillageWorld.Height(pos.x, pos.z) + (kind == 1 ? 1.0f : 0.16f);
            var g = new GameObject(kind == 1 ? "GiantHornet" : "GiantBeetle"); g.transform.SetParent(transform, false); g.transform.position = pos;
            Material dark = CoastMaterials.CreateLit(new Color(0.16f, 0.10f, 0.08f), 0.45f), eye = CoastMaterials.CreateUnlit(new Color(0.95f, 0.25f, 0.2f));
            if (kind == 0)
            {
                var body = P(g.transform, PrimitiveType.Sphere, new Vector3(0f, 0.1f, -0.05f), new Vector3(0.55f, 0.30f, 0.75f), dark);
                P(g.transform, PrimitiveType.Sphere, new Vector3(0f, 0.12f, 0.38f), new Vector3(0.36f, 0.24f, 0.30f), dark);                  // 머리
                for (int k = -1; k <= 1; k += 2)
                {
                    var jaw = P(g.transform, PrimitiveType.Cube, new Vector3(k * 0.12f, 0.14f, 0.66f), new Vector3(0.06f, 0.06f, 0.42f), dark); jaw.transform.localRotation = Quaternion.Euler(0f, k * -22f, 0f);   // 큰 턱
                    P(g.transform, PrimitiveType.Sphere, new Vector3(k * 0.12f, 0.2f, 0.5f), Vector3.one * 0.08f, eye);
                    for (int l = 0; l < 3; l++) { var leg = P(g.transform, PrimitiveType.Cube, new Vector3(k * 0.32f, 0.02f, 0.22f - l * 0.24f), new Vector3(0.36f, 0.05f, 0.06f), dark); leg.transform.localRotation = Quaternion.Euler(0f, 0f, k * 28f); leg.name = "Leg"; }
                }
                var shell = P(g.transform, PrimitiveType.Sphere, new Vector3(0f, 0.17f, -0.1f), new Vector3(0.5f, 0.2f, 0.6f), CoastMaterials.CreateLit(new Color(0.42f, 0.22f, 0.12f), 0.6f));   // 등딱지 광택
            }
            else
            {
                Material yel = CoastMaterials.CreateLit(new Color(0.98f, 0.78f, 0.2f), 0.3f);
                P(g.transform, PrimitiveType.Sphere, new Vector3(0f, 0f, 0.26f), new Vector3(0.34f, 0.30f, 0.36f), yel);                          // 가슴
                P(g.transform, PrimitiveType.Sphere, new Vector3(0f, 0f, 0.55f), new Vector3(0.26f, 0.24f, 0.26f), dark);                       // 머리
                for (int k = -1; k <= 1; k += 2) P(g.transform, PrimitiveType.Sphere, new Vector3(k * 0.09f, 0.06f, 0.66f), Vector3.one * 0.09f, eye);
                for (int st = 0; st < 4; st++) P(g.transform, PrimitiveType.Sphere, new Vector3(0f, -0.02f, -0.02f - st * 0.16f), new Vector3(0.36f - st * 0.04f, 0.32f - st * 0.04f, 0.2f), st % 2 == 0 ? yel : dark);   // 줄무늬 배
                var sting = P(g.transform, PrimitiveType.Cube, new Vector3(0f, -0.02f, -0.72f), new Vector3(0.06f, 0.06f, 0.22f), dark);
                for (int k = -1; k <= 1; k += 2)
                {
                    var w = P(g.transform, PrimitiveType.Sphere, new Vector3(k * 0.4f, 0.16f, 0.18f), new Vector3(0.78f, 0.03f, 0.3f), CoastMaterials.CreateTransparent(new Color(0.9f, 0.95f, 1f, 0.55f))); w.name = "Wing";
                }
            }
            g.transform.localScale = Vector3.one * 0.72f;   // 203차(사용자: 「벌레가 너무 크다」): 1.1 → 0.72
            g.AddComponent<LegWiggle>();   // 198차: 다리 움직임
            var bc = new Critter { big = true, kind = kind, life = 40f, phase = Random.value * 6f, anchor = pos, t = g.transform };
            if (kind == 0 && Random.value < 0.06f + 0.02f * BatTier) { bc.rare = true; MakeRare(g.transform, new Color(1f, 0.80f, 0.18f)); }   // 198차: 황금사슴벌레
            _crit.Add(bc);
        }
        /// 171차: 야생 닭(kind 4)·토끼(kind 5) — 언덕에서 주인공 5~9 m 밖. 블렌더 키트 VAnimal_*.
        void SpawnWild()
        {
            var pp = Player.position; Vector3 pos = pp; bool ok = false;
            for (int k = 0; k < 8 && !ok; k++)
            {
                float ang = Random.Range(0f, Mathf.PI * 2f), r = Random.Range(5f, 9f);
                pos = new Vector3(pp.x + Mathf.Cos(ang) * r, 0f, pp.z + Mathf.Sin(ang) * r);
                ok = pos.z > 12.5f && pos.z < 44f && Mathf.Abs(pos.x) < 42f && !VillageLivestock.Inside(pos) && !(Mathf.Abs(pos.x) < 5f && pos.z > 31f);
            }
            if (!ok) return;
            int kind = Random.value < 0.5f ? 4 : 5;
            var g = new GameObject(kind == 4 ? "WildChicken" : "WildRabbit"); g.transform.SetParent(transform, false); g.transform.position = VillageWorld.Ground(pos.x, pos.z);
            var kit = JejuKit.Spawn(kind == 4 ? "VAnimal_Chicken" : "VAnimal_Rabbit", g.transform, Vector3.zero, Random.Range(0f, 360f), kind == 4 ? 1.0f : 0.9f);
            if (kit == null) { var b = P(g.transform, PrimitiveType.Sphere, new Vector3(0f, 0.2f, 0f), Vector3.one * 0.4f, CoastMaterials.CreateLit(kind == 4 ? Color.white : new Color(0.72f, 0.68f, 0.66f))); }
            _crit.Add(new Critter { kind = kind, life = 50f, phase = Random.value * 6f, anchor = Vector3.zero, t = g.transform });
        }

        /// 171차: 바닷가 생물 — 꽃게(kind 2, 주황, 집게 2·다리 6·눈자루) · 소라게(kind 3, 소라 껍데기 + 다리). 방망이로 잡는다.
        void SpawnCrab()
        {
            var pp = Player.position; Vector3 pos = pp; bool ok = false;
            for (int k = 0; k < 8 && !ok; k++)
            {
                float ang = Random.Range(0f, Mathf.PI * 2f), r = Random.Range(3.5f, 7f);
                pos = new Vector3(pp.x + Mathf.Cos(ang) * r, 0f, pp.z + Mathf.Sin(ang) * r);
                float h = VillageWorld.Height(pos.x, pos.z);
                ok = pos.z < -14f && h < VillageWorld.SeaLevel + 1.6f && h > VillageWorld.SeaLevel - 0.3f;
            }
            if (!ok) return;
            pos.y = VillageWorld.Height(pos.x, pos.z) + 0.05f;
            int kind = Random.value < 0.65f ? 2 : 3;
            var g = new GameObject(kind == 2 ? "Crab" : "HermitCrab"); g.transform.SetParent(transform, false); g.transform.position = pos;
            var eye = CoastMaterials.CreateUnlit(new Color(0.12f, 0.10f, 0.12f));
            if (kind == 2)
            {
                var m = CoastMaterials.CreateLit(new Color(0.95f, 0.42f, 0.22f), 0.35f); var md = CoastMaterials.CreateLit(new Color(0.78f, 0.28f, 0.14f), 0.3f);
                P(g.transform, PrimitiveType.Sphere, new Vector3(0f, 0.12f, 0f), new Vector3(0.62f, 0.22f, 0.44f), m);                      // 몸통(옆으로 넓게)
                for (int k = -1; k <= 1; k += 2)
                {
                    var cl = P(g.transform, PrimitiveType.Sphere, new Vector3(k * 0.36f, 0.16f, 0.22f), new Vector3(0.22f, 0.16f, 0.26f), md);   // 집게
                    P(g.transform, PrimitiveType.Sphere, new Vector3(k * 0.36f, 0.19f, 0.36f), new Vector3(0.12f, 0.08f, 0.14f), md);
                    for (int l = 0; l < 3; l++) { var leg = P(g.transform, PrimitiveType.Cube, new Vector3(k * 0.36f, 0.06f, 0.05f - l * 0.13f), new Vector3(0.26f, 0.04f, 0.05f), md); leg.transform.localRotation = Quaternion.Euler(0f, 0f, k * 30f); leg.name = "Leg"; }
                    var stalk = P(g.transform, PrimitiveType.Cylinder, new Vector3(k * 0.10f, 0.26f, 0.16f), new Vector3(0.03f, 0.06f, 0.03f), md);     // 눈자루
                    P(g.transform, PrimitiveType.Sphere, new Vector3(k * 0.10f, 0.33f, 0.16f), Vector3.one * 0.07f, eye);
                }
            }
            else
            {
                var shell = CoastMaterials.CreateLit(new Color(0.85f, 0.70f, 0.50f), 0.4f); var shellD = CoastMaterials.CreateLit(new Color(0.62f, 0.46f, 0.32f), 0.3f); var body = CoastMaterials.CreateLit(new Color(0.80f, 0.45f, 0.35f), 0.2f);
                P(g.transform, PrimitiveType.Sphere, new Vector3(0f, 0.22f, -0.08f), new Vector3(0.42f, 0.40f, 0.44f), shell);               // 소라
                P(g.transform, PrimitiveType.Sphere, new Vector3(-0.08f, 0.34f, -0.18f), new Vector3(0.24f, 0.22f, 0.24f), shellD);
                P(g.transform, PrimitiveType.Sphere, new Vector3(-0.14f, 0.42f, -0.24f), new Vector3(0.12f, 0.12f, 0.12f), shellD);
                P(g.transform, PrimitiveType.Sphere, new Vector3(0f, 0.10f, 0.16f), new Vector3(0.26f, 0.16f, 0.22f), body);                 // 몸
                for (int k = -1; k <= 1; k += 2)
                {
                    for (int l = 0; l < 2; l++) { var leg = P(g.transform, PrimitiveType.Cube, new Vector3(k * 0.18f, 0.05f, 0.18f - l * 0.1f), new Vector3(0.18f, 0.035f, 0.045f), body); leg.transform.localRotation = Quaternion.Euler(0f, 0f, k * 28f); leg.name = "Leg"; }
                    P(g.transform, PrimitiveType.Sphere, new Vector3(k * 0.07f, 0.2f, 0.26f), Vector3.one * 0.05f, eye);
                }
            }
            g.transform.localScale = Vector3.one * 1.0f;   // 198차: 1.15 → 1.0
            g.AddComponent<LegWiggle>();
            _crit.Add(new Critter { big = true, kind = kind, life = 45f, phase = Random.value * 6f, anchor = new Vector3(1f, 0f, 0f), t = g.transform });
        }

        static GameObject P(Transform parent, PrimitiveType t, Vector3 pos, Vector3 scale, Material m)
        {
            var g = GameObject.CreatePrimitive(t); Destroy(g.GetComponent<Collider>()); g.transform.SetParent(parent, false);
            g.transform.localPosition = pos; g.transform.localScale = scale; g.GetComponent<MeshRenderer>().sharedMaterial = m; return g;
        }

        void Spawn(bool spirit)
        {
            var pp = Player.position; float ang = Random.Range(0f, Mathf.PI * 2f), r = spirit ? Random.Range(6f, 9f) : Random.Range(3f, 6f);
            var pos = new Vector3(pp.x + Mathf.Cos(ang) * r, 0f, pp.z + Mathf.Sin(ang) * r);
            pos.x = Mathf.Clamp(pos.x, -44f, 44f); pos.z = Mathf.Clamp(pos.z, -30f, 44f);
            pos.y = VillageWorld.Height(pos.x, pos.z) + 0.9f;
            var c = new Critter { spirit = spirit, life = spirit ? 28f : 22f, phase = Random.value * 6f, anchor = pos };
            if (spirit)
            {
                var g = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(g.GetComponent<Collider>()); g.name = "Spirit"; g.transform.SetParent(transform, false); g.transform.position = pos;
                Color col = Random.value < 0.5f ? new Color(0.55f, 0.90f, 1f) : new Color(0.85f, 0.70f, 1f);
                g.GetComponent<MeshRenderer>().sharedMaterial = CoastMaterials.CreateUnlit(col);
                var halo = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(halo.GetComponent<Collider>()); halo.transform.SetParent(g.transform, false); halo.transform.localScale = Vector3.one * 1.9f;
                halo.GetComponent<MeshRenderer>().sharedMaterial = CoastMaterials.CreateTransparent(new Color(col.r, col.g, col.b, 0.22f));
                for (int k = 0; k < 2; k++)
                {
                    var e = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(e.GetComponent<Collider>()); e.transform.SetParent(g.transform, false);
                    e.transform.localPosition = new Vector3(k == 0 ? -0.22f : 0.22f, 0.12f, 0.42f); e.transform.localScale = Vector3.one * 0.16f;
                    e.GetComponent<MeshRenderer>().sharedMaterial = CoastMaterials.CreateUnlit(new Color(0.15f, 0.15f, 0.25f));
                }
                c.t = g.transform;
            }
            else
            {
                // 150차: 벌레 3종 — 나비(어디나) · 잠자리(바닷가 z<-20, 빠름) · 무당벌레(풀밭 땅 위, 느림)
                float rv = Random.value; c.kind = rv < 0.5f ? 0 : rv < 0.72f && pos.z < -20f ? 1 : rv < 0.72f ? 0 : 2;
                var g = new GameObject(c.kind == 1 ? "Dragonfly" : c.kind == 2 ? "Ladybug" : "Butterfly"); g.transform.SetParent(transform, false); g.transform.position = pos;
                if (c.kind == 1)
                {
                    var body = GameObject.CreatePrimitive(PrimitiveType.Capsule); Destroy(body.GetComponent<Collider>()); body.transform.SetParent(g.transform, false);
                    body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); body.transform.localScale = new Vector3(0.06f, 0.26f, 0.06f);
                    body.GetComponent<MeshRenderer>().sharedMaterial = CoastMaterials.CreateUnlit(new Color(0.30f, 0.60f, 0.95f));
                    for (int k = 0; k < 4; k++)
                    {
                        var w = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(w.GetComponent<Collider>()); w.transform.SetParent(g.transform, false);
                        w.transform.localPosition = new Vector3(k % 2 == 0 ? -0.24f : 0.24f, 0.02f, k < 2 ? 0.06f : -0.06f); w.transform.localScale = new Vector3(0.42f, 0.02f, 0.10f);
                        w.GetComponent<MeshRenderer>().sharedMaterial = CoastMaterials.CreateTransparent(new Color(0.85f, 0.95f, 1f, 0.55f));
                    }
                }
                else if (c.kind == 2)
                {
                    var body = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(body.GetComponent<Collider>()); body.transform.SetParent(g.transform, false);
                    body.transform.localScale = new Vector3(0.22f, 0.14f, 0.26f);
                    body.GetComponent<MeshRenderer>().sharedMaterial = CoastMaterials.CreateUnlit(new Color(0.92f, 0.22f, 0.20f));
                    var head = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(head.GetComponent<Collider>()); head.transform.SetParent(g.transform, false);
                    head.transform.localPosition = new Vector3(0f, 0.02f, 0.13f); head.transform.localScale = Vector3.one * 0.10f;
                    head.GetComponent<MeshRenderer>().sharedMaterial = CoastMaterials.CreateUnlit(new Color(0.12f, 0.10f, 0.12f));
                    for (int k = 0; k < 4; k++)
                    {
                        var d = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(d.GetComponent<Collider>()); d.transform.SetParent(g.transform, false);
                        d.transform.localPosition = new Vector3(k % 2 == 0 ? -0.06f : 0.06f, 0.06f, k < 2 ? 0.04f : -0.05f); d.transform.localScale = Vector3.one * 0.05f;
                        d.GetComponent<MeshRenderer>().sharedMaterial = CoastMaterials.CreateUnlit(new Color(0.12f, 0.10f, 0.12f));
                    }
                    // 198차: 다리 6개(움직임)
                    for (int k = 0; k < 6; k++)
                    {
                        var lg = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(lg.GetComponent<Collider>()); lg.name = "Leg"; lg.transform.SetParent(g.transform, false);
                        float sx = k % 2 == 0 ? -1f : 1f; lg.transform.localPosition = new Vector3(sx * 0.12f, -0.04f, 0.08f - (k / 2) * 0.08f); lg.transform.localRotation = Quaternion.Euler(0f, 0f, sx * 25f); lg.transform.localScale = new Vector3(0.10f, 0.015f, 0.02f);
                        lg.GetComponent<MeshRenderer>().sharedMaterial = CoastMaterials.CreateUnlit(new Color(0.12f, 0.10f, 0.12f));
                    }
                    g.AddComponent<LegWiggle>();
                }
                else
                {
                    Color col = Random.value < 0.5f ? new Color(1f, 0.75f, 0.30f) : Random.value < 0.5f ? new Color(1f, 0.55f, 0.75f) : new Color(0.65f, 0.80f, 1f);
                    for (int k = 0; k < 2; k++)
                    {
                        var w = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(w.GetComponent<Collider>()); w.transform.SetParent(g.transform, false);
                        w.transform.localPosition = new Vector3(k == 0 ? -0.16f : 0.16f, 0f, 0f); w.transform.localScale = new Vector3(0.28f, 0.06f, 0.2f);
                        w.GetComponent<MeshRenderer>().sharedMaterial = CoastMaterials.CreateUnlit(col);
                    }
                }
                c.t = g.transform;
                // 198차: 희귀 벌레(무지개나비·황금잠자리·칠보무당벌레) — 잠자리채 등급이 높을수록 자주
                if (Random.value < 0.05f + 0.025f * NetTier) { c.rare = true; MakeRare(g.transform, c.kind == 0 ? Color.HSVToRGB(Random.value, 0.7f, 1f) : c.kind == 1 ? new Color(1f, 0.82f, 0.22f) : new Color(0.35f, 0.95f, 0.85f)); }
            }
            _crit.Add(c);
        }

        /// 198차: 희귀 벌레 — 몸 색을 바꾸고 반짝이 구슬을 둘러 준다
        static void MakeRare(Transform g, Color col)
        {
            var m = CoastMaterials.CreateUnlit(col);
            foreach (var r in g.GetComponentsInChildren<MeshRenderer>()) { if (r.gameObject.name == "Leg" || r.gameObject.name == "Wing") continue; r.sharedMaterial = m; }
            var sp = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(sp.GetComponent<Collider>()); sp.name = "RareGlow"; sp.transform.SetParent(g, false); sp.transform.localScale = Vector3.one * 0.9f;
            sp.GetComponent<MeshRenderer>().sharedMaterial = CoastMaterials.CreateTransparent(new Color(1f, 0.95f, 0.6f, 0.25f)); sp.AddComponent<Twinkle198>();
        }
        void Pop(Critter c, Color col)
        {
            if (c.t == null) return;
            StartCoroutine(PopCo(c.t, col));
        }
        System.Collections.IEnumerator PopCo(Transform t, Color col)
        {
            float k = 0f; var s0 = t.localScale;
            while (k < 0.25f) { k += Time.deltaTime; if (t == null) yield break; t.localScale = s0 * (1f + k * 0.8f); yield return null; }   // 198차: 잡을 때 커지는 폭 줄임(×1.75 → ×1.2)
            if (t != null) Destroy(t.gameObject);
        }

        /// 도구 휘두르기: bat=true 면 큰 벌레(·귀신 밀어내기), false 면 정령·작은 벌레를 2.4m 안에서 잡는다. 결과 메시지 반환(null = 아무것도 없음).
        public string Swing(bool bat)
        {
            if (Player == null) return null;
            if (bat && SwingBandit(out bool wasPolice)) return wasPolice ? (PoliceAlive == 0 ? Loc.T("🚓 경찰을 모두 쓰러뜨렸다… 수배가 풀렸다", "🚓 Beat all the police… wanted level cleared") : Loc.T("🏏 경찰을 때렸다! 아직 버틴다", "🏏 Hit the police! Still standing")) : (BanditsAlive == 0 ? Loc.T("🏏 산적 무리를 물리쳤다! 40G 씩 떨어뜨렸다", "🏏 Beat the bandits! They dropped 40G each") : Loc.T("🏏 산적을 때렸다!", "🏏 Hit a bandit!"));
            // 183차(사용자: 「말도 때려서 식량으로」): 방망이 세 방이면 말이 쓰러지고 고기가 된다 — 계속 잡으면 경찰(VillageHub)
            if (bat) { var hm = SwingHorse(); if (hm != null) return hm; }
            Critter best = null; float bd = 2.4f; bool wrong = false;
            foreach (var c in _crit)
            {
                if (c.t == null) continue;
                float d = Vector3.Distance(new Vector3(c.t.position.x, 0f, c.t.position.z), new Vector3(Player.position.x, 0f, Player.position.z));
                bool ok = BatTarget(c) == bat || (bat && c.spirit && !c.ghost && c.kind != 6) || (bat && !c.big && !c.spirit && !c.ghost && c.kind <= 2);   // 198차: 방망이로 작은 벌레도 처치   // 183차: 방망이로 정령도 칠 수 있다(잡기는 잠자리채만)
                if (d < bd) { if (ok) { bd = d; best = c; } else wrong = true; }
            }
            if (best == null) return wrong ? (bat ? Loc.T("정령·나비는 잠자리채로!", "Use the net for spirits and bugs!") : Loc.T("큰 벌레는 방망이로!", "Use the bat for big bugs!")) : null;
            if (bat && best.ghost)
            {
                // 155차: 귀신은 못 잡는다 — 밀어내고 3 s 기절
                best.stun = 3f; var away = (best.t.position - Player.position); away.y = 0f; away = away.normalized * 5f;
                var np = best.t.position + away; best.t.position = new Vector3(np.x, VillageWorld.Height(np.x, np.z) + 1f, np.z);
                return Loc.T("👻 귀신을 밀어냈다! 잡을 순 없다 — 집으로 도망치자!", "👻 Pushed the ghost back! Can't catch it — run home!");
            }
            if (bat && best.spirit && !best.ghost)
            {
                // 183차(사용자: 「방망이로 정령도 죽일 수 있게, 다만 잡으려면 잠자리채」): 흩어져 사라질 뿐 — 별조각·코인은 없다
                _crit.Remove(best); if (best.t != null) VillagePang.Burst(best.t.position, new Color(0.75f, 0.70f, 0.60f), new Color(1f, 0.95f, 0.7f), 1.1f); Pop(best, Color.white);
                return Loc.T("💥 정령을 방망이로 쳐서 흩어 버렸다… (잡으려면 잠자리채!)", "💥 Scattered the spirit with the bat… (use the net to catch it!)");
            }
            if (bat && !best.big && !best.spirit && !best.ghost)
            {
                // 198차(사용자: 「벌레는 죽이거나 팔 수 있게」): 방망이로 치면 처치 — 가방엔 안 들어가고 2G
                _crit.Remove(best); if (best.t != null) VillagePang.Burst(best.t.position, new Color(0.8f, 0.8f, 0.7f), Color.white, 0.8f); Pop(best, Color.white);
                OnCaught?.Invoke("kill", 0, 2);
                return Loc.T("탁— 벌레를 처치했다 (+2G). 팔려면 잠자리채로 잡자.", "Whack — squashed a bug (+2G). Use the net to keep it.");
            }
            if (bat)
            {
                // 170차: 큰 벌레 — 방망이 한 방. 팡 + 별조각 2 + 15G, 가방(도감)에 들어간다
                _crit.Remove(best); if (best.t != null) VillagePang.Burst(best.t.position, new Color(1f, 0.85f, 0.35f), Color.white, 1.3f); Pop(best, Color.white);
                if (best.kind == 2) { OnCaught?.Invoke("crab", 1, 12); return Loc.T("🦀 꽃게를 잡았다! 별조각 +1 · 12G · 가방에 넣었다", "🦀 Caught a crab! Shard +1 · 12G"); }
                if (best.kind == 3) { OnCaught?.Invoke("hermit", 1, 8); return Loc.T("🐚 소라게를 잡았다! 별조각 +1 · 8G · 가방에 넣었다", "🐚 Caught a hermit crab! Shard +1 · 8G"); }
                if (best.kind == 1) { OnCaught?.Invoke("hornet", 2, 15); return Loc.T("🐝 왕말벌을 때려잡았다! 별조각 +2 · 15G", "🐝 Smacked a giant hornet! Shards +2 · 15G"); }
                if (best.rare) { OnCaught?.Invoke("rare_beetle", 6, 20 + BatTier * 5); return Loc.T("★ 황금사슴벌레를 잡았다! 전설의 벌레 — 가방에 넣었다", "★ Golden stag beetle! Legendary — into the bag"); }
                OnCaught?.Invoke("bigbeetle", 2, 15 + BatTier * 5); return Loc.T("🪲 왕사슴벌레를 때려잡았다! 별조각 +2 · 15G", "🪲 Smacked a giant beetle! Shards +2 · 15G");
            }
            if (best.kind == 6)
            {
                // 175차: 몬스터를 잡으면 펫을 얻는다(펫 상점과 같은 취급) — 메시지는 VillageHub 가 띄운다
                _crit.Remove(best);
                if (best.t != null) VillagePang.Burst(best.t.position, new Color(0.85f, 0.65f, 1f), Color.white, 1.4f);
                Pop(best, Color.white);
                OnMonsterCaught?.Invoke(best.pet);
                return null;
            }
            if (best.spirit) { _crit.Remove(best); if (best.t != null) VillagePang.Burst(best.t.position, new Color(1f, 0.93f, 0.45f), Color.white, 1.0f); Pop(best, Color.white); OnCaught?.Invoke("spirit", 2, 30); return Loc.T("✨ 정령을 잡았다! 별조각 +2 · 30G", "✨ Caught a spirit! Shards +2 · 30G"); }
            // 171차: 야생 닭·토끼 → 농장으로(가방 아님). 토끼는 빨라서 70% 만 잡힌다
            if (best.kind == 5 && Random.value > 0.7f) { best.anchor = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized; best.stun = 1.5f; return Loc.T("휙— 토끼가 깡충 도망갔다! 다시 노려 보자.", "Swish — the rabbit hopped away!"); }
            if (best.kind == 4 || best.kind == 5) { _crit.Remove(best); VillagePang.Burst(best.t.position + Vector3.up * 0.3f, new Color(1f, 0.9f, 0.5f), Color.white, 0.9f); Pop(best, Color.white); OnCaught?.Invoke(best.kind == 4 ? "chicken" : "rabbit", 1, 0); return null; }
            // 150차: 잠자리는 빨라서 60% 만 잡힌다(놓치면 멀리 달아남)
            if (best.kind == 1 && Random.value > 0.6f + 0.12f * NetTier) { /* 198차: 잠자리채 등급마다 덜 도망 */ best.anchor += new Vector3(Random.Range(-4f, 4f), 0f, Random.Range(-3f, 3f)); return Loc.T("휙— 잠자리가 도망갔다! 다시 노려 보자.", "Swish — the dragonfly got away!"); }
            _crit.Remove(best); Pop(best, Color.white);
            if (best.rare) { string rk = best.kind == 1 ? "rare_dragonfly" : best.kind == 2 ? "rare_ladybug" : "rare_butterfly"; OnCaught?.Invoke(rk, 5, 0); return best.kind == 1 ? Loc.T("★ 황금잠자리를 잡았다! 아주 귀하다 — 가방에 넣었다", "★ Golden dragonfly!") : best.kind == 2 ? Loc.T("★ 칠보무당벌레를 잡았다! 아주 귀하다 — 가방에 넣었다", "★ Jewel ladybug!") : Loc.T("★ 무지개나비를 잡았다! 아주 귀하다 — 가방에 넣었다", "★ Rainbow butterfly!"); }
            if (best.kind == 1) { OnCaught?.Invoke("dragonfly", 2, 0); return Loc.T("🪰 잠자리를 잡았다! 별조각 +2 · 가방에 넣었다", "🪰 Caught a dragonfly! Shards +2"); }
            if (best.kind == 2) { OnCaught?.Invoke("ladybug", 1, 0); return Loc.T("🐞 무당벌레를 잡았다! 별조각 +1 · 가방에 넣었다", "🐞 Caught a ladybug! Shard +1"); }
            OnCaught?.Invoke("butterfly", 1, 0); return Loc.T("🦋 나비를 잡았다! 별조각 +1 · 가방에 넣었다", "🦋 Caught a butterfly! Shard +1");
        }

        public int GhostCount { get { int n = 0; foreach (var c in _crit) if (c.ghost && c.t != null) n++; return n; } }
        public void ClearGhosts() { for (int i = _crit.Count - 1; i >= 0; i--) if (_crit[i].ghost) { if (_crit[i].t != null) Destroy(_crit[i].t.gameObject); _crit.RemoveAt(i); } }
        /// 155차: 밤 귀신 — 주인공 10~14 m 밖에서 나타나 쫓아온다
        public void SpawnGhost()
        {
            if (Player == null) return;
            var pp = Player.position; float ang = Random.Range(0f, Mathf.PI * 2f), r = Random.Range(10f, 14f);
            var pos = new Vector3(Mathf.Clamp(pp.x + Mathf.Cos(ang) * r, -44f, 44f), 0f, Mathf.Clamp(pp.z + Mathf.Sin(ang) * r, -30f, 44f)); pos.y = VillageWorld.Height(pos.x, pos.z) + 1f;
            var g = new GameObject("Ghost"); g.transform.SetParent(transform, false); g.transform.position = pos;
            var body = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(body.GetComponent<Collider>()); body.transform.SetParent(g.transform, false); body.transform.localScale = new Vector3(1f, 1.25f, 1f);
            body.GetComponent<MeshRenderer>().sharedMaterial = CoastMaterials.CreateTransparent(new Color(0.16f, 0.12f, 0.30f, 0.82f));
            var tail = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(tail.GetComponent<Collider>()); tail.transform.SetParent(g.transform, false); tail.transform.localPosition = new Vector3(0f, -0.75f, -0.25f); tail.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
            tail.GetComponent<MeshRenderer>().sharedMaterial = CoastMaterials.CreateTransparent(new Color(0.16f, 0.12f, 0.30f, 0.55f));
            for (int k = 0; k < 2; k++)
            {
                var e = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(e.GetComponent<Collider>()); e.transform.SetParent(g.transform, false);
                e.transform.localPosition = new Vector3(k == 0 ? -0.2f : 0.2f, 0.15f, 0.42f); e.transform.localScale = new Vector3(0.22f, 0.30f, 0.12f);
                e.GetComponent<MeshRenderer>().sharedMaterial = CoastMaterials.CreateUnlit(new Color(1f, 0.95f, 0.75f));
            }
            var mouth = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(mouth.GetComponent<Collider>()); mouth.transform.SetParent(g.transform, false); mouth.transform.localPosition = new Vector3(0f, -0.18f, 0.46f); mouth.transform.localScale = new Vector3(0.28f, 0.16f, 0.1f);
            mouth.GetComponent<MeshRenderer>().sharedMaterial = CoastMaterials.CreateUnlit(new Color(0.9f, 0.2f, 0.3f));
            // 209차(사용자: 「몬스터 디테일」): 남색 공 두 개 → 블렌더 이불 귀신(VGhost: 둥근 머리·물결 자락·큰 눈·볼터치·짧은 팔). 없으면 예전 모양
            var gm = JejuKit.Spawn("VGhost", g.transform, new Vector3(0f, -0.15f, 0f), 0f, 1.15f);
            if (gm != null) { for (int ci = 0; ci < g.transform.childCount; ci++) { var ch = g.transform.GetChild(ci); if (ch.gameObject != gm) { var mr = ch.GetComponent<MeshRenderer>(); if (mr != null) mr.enabled = false; } } gm.AddComponent<GhostSway>(); }
            var glow = new GameObject("GhostLight").AddComponent<Light>(); glow.transform.SetParent(g.transform, false); glow.type = LightType.Point; glow.range = 5f; glow.intensity = 1.6f; glow.color = new Color(0.6f, 0.5f, 1f);
            _crit.Add(new Critter { spirit = true, ghost = true, life = 600f, phase = Random.value * 6f, anchor = pos, t = g.transform });
        }
        public bool AnyCritterNear(bool spirit, float r = 2.6f)
        {
            if (Player == null) return false;
            if (spirit && AnyBanditNear(r)) return true;   // 171차: 방망이 = 산적도
            if (spirit) foreach (var h in VillageRanch.Horses) if (h != null && !h.Dead && Vector3.Distance(new Vector3(h.transform.position.x, 0f, h.transform.position.z), new Vector3(Player.position.x, 0f, Player.position.z)) < r + 0.2f) return true;   // 183차: 말
            foreach (var c in _crit) if (c.t != null && (BatTarget(c) == spirit || (spirit && c.spirit && !c.ghost && c.kind != 6)) && Vector3.Distance(new Vector3(c.t.position.x, 0f, c.t.position.z), new Vector3(Player.position.x, 0f, Player.position.z)) < r) return true;
            return false;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            TickNpcs(dt); TickKid(dt);
            if ((Locked == null || !Locked()) && (Player == null || VillageZones.At(Player.position) == VillageZones.Zone.None)) { TickCritters(dt); TickBandits(dt); }   // 195차: 시내·관광지·광산엔 벌레·산적 없음
        }

        // ── 171차(사용자: 「산에 산적이 나타나게, 방망이로 이기고, 여러 명이 다닌다」) ─────────────────────────
        // 언덕(z>14)·낮에만. 25~55 s 마다 2~3명이 12 m 밖에서 나타나 주인공을 쫓는다(1.7 m/s). 닿으면 HP −8 + 돈 −3%(뺏김).
        // 방망이 두 방이면 쓰러진다(20G 떨어뜨림). 무리를 다 쓰러뜨리면 보너스 40G. 밤이 되거나 언덕을 내려가면 흩어진다.
        void TickBandits(float dt)
        {
            if (_clearPolice) DoClearPolice();
            bool hill = Player != null && Player.position.z > 14f && VillageDayNight.Night < 0.5f;
            if (BanditsAlive == 0 && _bandits.Count == 0)
            {
                _banditT -= dt;
                if (_banditT <= 0f && hill) { _banditT = Random.Range(120f, 200f); if (Random.value < 0.6f) SpawnBanditGroup(); }   // 195차: 2~3분마다, 그중 60 %만
                return;
            }
            bool allDown = true;
            for (int i = _bandits.Count - 1; i >= 0; i--)
            {
                var b = _bandits[i]; if (b.root == null) { _bandits.RemoveAt(i); continue; }
                b.ph += dt; b.stun -= dt;
                if (b.down)
                {
                    // 쓰러짐: 납작해졌다 2.5 s 뒤 사라짐
                    b.pivot.localRotation = Quaternion.Slerp(b.pivot.localRotation, Quaternion.Euler(-85f, 0f, 0f), dt * 6f);
                    if (b.ph > 2.5f) { Destroy(b.root.gameObject); _bandits.RemoveAt(i); }
                    continue;
                }
                allDown = false;
                if (b.police) { TickPolice(b, i, dt); continue; }   // 183차
                if (!hill || VillageDayNight.Night >= 0.5f)
                {
                    // 언덕 밖·밤: 뒤돌아 도망치다 사라짐
                    var away = b.root.position - Player.position; away.y = 0f; var np = b.root.position + away.normalized * 2.8f * dt;
                    b.root.position = VillageWorld.Ground(np.x, np.z); b.root.rotation = Quaternion.LookRotation(away.normalized, Vector3.up);
                    if (away.magnitude > 26f) { Destroy(b.root.gameObject); _bandits.RemoveAt(i); }
                    continue;
                }
                var d = Player.position - b.root.position; d.y = 0f; float dist = d.magnitude;
                if (b.stun > 0f) { continue; }
                // 서로 겹치지 않게 살짝 밀기
                var sep = Vector3.zero; foreach (var o in _bandits) if (o != b && o.root != null && !o.down) { var v = b.root.position - o.root.position; v.y = 0f; if (v.magnitude < 1.2f) sep += v.normalized * (1.2f - v.magnitude); }
                var step = (dist > 1.0f ? d.normalized * 1.7f : Vector3.zero) + sep * 2f;
                var pos = b.root.position + step * dt; b.root.position = VillageWorld.Ground(pos.x, pos.z);
                if (d.sqrMagnitude > 0.01f) b.root.rotation = Quaternion.Slerp(b.root.rotation, Quaternion.LookRotation(d.normalized, Vector3.up), dt * 8f);
                if (dist < 0.95f)
                {
                    OnBanditHit?.Invoke(8); b.stun = 1.4f;
                    var back = b.root.position - d.normalized * 1.6f; b.root.position = VillageWorld.Ground(back.x, back.z);
                    if (Time.time - b.said > 2f) { b.said = Time.time; b.bubble.Show(Loc.T("돈 내놔!", "Hand it over!")); }
                }
                else if (Time.time - b.said > 6f && Random.value < dt * 0.3f) { b.said = Time.time; b.bubble.Show(Loc.T(Random.value < 0.5f ? "거기 서!" : "잡아라!", Random.value < 0.5f ? "Stop right there!" : "Get her!")); }
            }
            if (allDown && _bandits.Count == 0) _banditT = Random.Range(150f, 240f);   // 195차
        }
        // ── 183차(사용자: 「말을 계속 잡으면 엄청 강한 경찰이 나타나서 공격」) ─────────────────────────
        // 어디서든(언덕·밤 가리지 않고) 쫓아온다. 걷기(2.5 m/s)보다 빠르고 달리기(5.5 m/s)보다 느린 3.6 m/s — 달리면 떼어 놓을 수 있다.
        // 닿으면 HP −22 + 벌금(VillageHub). 방망이 12~20 방(수배 단계마다 +4). 2분이 지나거나 40 m 넘게 떨어지면 포기하고 돌아간다.
        public System.Action<int> OnPoliceHit;
        public System.Action<bool> OnPoliceDown;   // 인자: 모두 쓰러졌는가
        void TickPolice(Bandit b, int i, float dt)
        {
            var d = Player.position - b.root.position; d.y = 0f; float dist = d.magnitude;
            b.life -= dt;
            if (b.life <= 0f || dist > 40f)
            {
                if (b.life > -0.01f && b.life <= 0f || dist > 40f) { if (Time.time - b.said > 3f) { b.said = Time.time; b.bubble.Show(Loc.T("경찰: 오늘은 봐준다…", "Police: I'll let it go… today.")); } }
                var np0 = b.root.position - d.normalized * 3.2f * dt; b.root.position = VillageWorld.Ground(np0.x, np0.z);
                if (d.sqrMagnitude > 0.01f) Face(b.root, -d.normalized);
                if (dist > 30f || b.life < -6f) { Destroy(b.root.gameObject); _bandits.RemoveAt(i); }
                return;
            }
            if (b.stun > 0f) return;
            var sep = Vector3.zero; foreach (var o in _bandits) if (o != b && o.root != null && !o.down) { var v = b.root.position - o.root.position; v.y = 0f; if (v.magnitude < 1.3f) sep += v.normalized * (1.3f - v.magnitude); }
            var step = (dist > 1.1f ? d.normalized * 3.6f : Vector3.zero) + sep * 2f;
            var pos = b.root.position + step * dt; b.root.position = VillageWorld.Ground(pos.x, pos.z);
            if (d.sqrMagnitude > 0.01f) b.root.rotation = Quaternion.Slerp(b.root.rotation, Quaternion.LookRotation(d.normalized, Vector3.up), dt * 10f);
            if (dist < 1.1f)
            {
                OnPoliceHit?.Invoke(22); b.stun = 1.2f; if (_clearPolice) return;
                var back = b.root.position - d.normalized * 1.4f; b.root.position = VillageWorld.Ground(back.x, back.z);
                if (Time.time - b.said > 2f) { b.said = Time.time; b.bubble.Show(Loc.T("경찰: 말 도둑 체포다!", "Police: You're under arrest, horse thief!")); }
            }
            else if (Time.time - b.said > 5f && Random.value < dt * 0.4f) { b.said = Time.time; b.bubble.Show(Loc.T(Random.value < 0.5f ? "경찰: 거기 서!" : "경찰: 도망쳐도 소용없다!", Random.value < 0.5f ? "Police: Freeze!" : "Police: Running won't help!")); }
        }
        public int PoliceCount => PoliceAlive;
        bool _clearPolice;
        /// 체포 뒤 경찰 철수 — 경찰 틱(목록 순회) 안에서 불릴 수 있어 다음 틱 맨 앞에서 지운다
        public void ClearPolice() { _clearPolice = true; }
        void DoClearPolice() { _clearPolice = false; for (int i = _bandits.Count - 1; i >= 0; i--) { var b = _bandits[i]; if (!b.police) continue; if (b.root != null) Destroy(b.root.gameObject); _bandits.RemoveAt(i); } }
        /// 수배 단계(1~3)만큼 경찰이 주인공 뒤 14 m 에서 나타난다
        public void SpawnPolice(int level)
        {
            if (Player == null) return;
            level = Mathf.Clamp(level, 1, 3);
            int n = level; float ang = Random.Range(0f, Mathf.PI * 2f);
            for (int i = 0; i < n; i++)
            {
                float a = ang + (i - (n - 1) * 0.5f) * 0.4f, r = 14f;
                var pos = new Vector3(Mathf.Clamp(Player.position.x + Mathf.Cos(a) * r, -42f, 42f), 0f, Mathf.Clamp(Player.position.z + Mathf.Sin(a) * r, -26f, 44f));
                var b = new Bandit { hp = 12 + 4 * (level - 1), ph = 0f, life = 120f, police = true };
                b.root = new GameObject("Police").transform; b.root.SetParent(transform, false); b.root.position = VillageWorld.Ground(pos.x, pos.z);
                b.pivot = new GameObject("Pivot").transform; b.pivot.SetParent(b.root, false); b.pivot.localScale = Vector3.one * 1.22f;   // 덩치가 크다
                var rig = SkaterRig.SpawnModel(ArtAssets.ResourceRoot + "Rig/Npc_Keeper", b.pivot, 1.05f, true);
                if (rig != null)
                {
                    var anim = rig.GetComponent<Animator>(); if (anim != null) { anim.SetBool("Grounded", true); anim.Play("Run", 0, Random.value); anim.speed = 0f; }
                    b.motion = b.pivot.gameObject.AddComponent<CharacterMotion>(); b.motion.Anim = anim; b.motion.WalkSpeed = 1.6f; b.motion.RunSpeed = 3.6f; b.motion.BobScale = 0.8f; b.motion.LookTarget = Player;
                    // 경찰 제복: 남색 + 짙은 남색, 모자와 금색 배지
                    var navy = CoastMaterials.CreateToon(new Color(0.16f, 0.24f, 0.50f), null, null, 0.05f); var dnavy = CoastMaterials.CreateToon(new Color(0.10f, 0.14f, 0.30f), null, null, 0.05f);
                    foreach (var smr in rig.GetComponentsInChildren<SkinnedMeshRenderer>())
                    {
                        var arr = smr.sharedMaterials; for (int k = 0; k < arr.Length; k++) { string nm = arr[k] != null ? arr[k].name : ""; if (nm.Contains("Skin") || nm.Contains("Eye") || nm.Contains("White") || nm.Contains("Mouth") || nm.Contains("Blush") || nm.Contains("Hair")) continue; arr[k] = (k % 2 == 0) ? navy : dnavy; }
                        smr.sharedMaterials = arr;
                        if (smr.GetComponent<CelOutlineHint>() == null) smr.gameObject.AddComponent<CelOutlineHint>();
                    }
                    if (anim != null && anim.isHuman)
                    {
                        var head = anim.GetBoneTransform(HumanBodyBones.Head);
                        if (head != null)
                        {
                            var cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(cap.GetComponent<Collider>()); cap.transform.SetParent(head, false); cap.transform.localPosition = new Vector3(0f, 0.15f, 0f); cap.transform.localScale = new Vector3(0.30f, 0.06f, 0.30f);
                            cap.GetComponent<MeshRenderer>().sharedMaterial = dnavy;
                            var brim = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(brim.GetComponent<Collider>()); brim.transform.SetParent(head, false); brim.transform.localPosition = new Vector3(0f, 0.11f, 0.12f); brim.transform.localScale = new Vector3(0.22f, 0.02f, 0.10f);
                            brim.GetComponent<MeshRenderer>().sharedMaterial = CoastMaterials.CreateLit(new Color(0.08f, 0.08f, 0.10f), 0.5f);
                            var badge = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(badge.GetComponent<Collider>()); badge.transform.SetParent(head, false); badge.transform.localPosition = new Vector3(0f, 0.16f, 0.15f); badge.transform.localScale = new Vector3(0.07f, 0.07f, 0.03f);
                            badge.GetComponent<MeshRenderer>().sharedMaterial = CoastMaterials.CreateLit(new Color(1f, 0.82f, 0.25f), 0.8f);
                        }
                    }
                }
                else { P(b.pivot, PrimitiveType.Capsule, new Vector3(0f, 0.6f, 0f), new Vector3(0.5f, 0.6f, 0.5f), CoastMaterials.CreateLit(new Color(0.16f, 0.24f, 0.50f))); }
                var col = b.root.gameObject.AddComponent<CapsuleCollider>(); col.center = new Vector3(0f, 0.7f, 0f); col.radius = 0.36f; col.height = 1.4f;
                b.bubble = SpeechBubble.Create(b.root, 1.85f); GroundBlob.Attach(b.root, 0.5f, 0.4f, b.pivot);
                b.said = Time.time; b.bubble.Show(i == 0 ? Loc.T("경찰: 말 도둑이다! 꼼짝 마!", "Police: Horse thief! Freeze!") : "🚨");
                _bandits.Add(b);
            }
        }

        // ── 183차: 말 때리기 ─────────────────────────────────────────
        public System.Action OnHorseKilled;
        string SwingHorse()
        {
            RanchHorse best = null; float bd = 2.8f;
            foreach (var h in VillageRanch.Horses) { if (h == null || h.Dead) continue; float d = Vector3.Distance(new Vector3(h.transform.position.x, 0f, h.transform.position.z), new Vector3(Player.position.x, 0f, Player.position.z)); if (d < bd) { bd = d; best = h; } }
            if (best == null) return null;
            bool killed = best.Hit(Player.position);
            VillagePang.Burst(best.transform.position + Vector3.up * 1.0f, new Color(1f, 0.85f, 0.35f), Color.white, 1.2f);
            if (killed) { OnHorseKilled?.Invoke(); return ""; }   // 메시지는 VillageHub 가(고기·수배) — 빈 문자열 = 처리됨(토스트 없음)
            return Loc.T($"🐴 말이 히힝 달아난다! (남은 힘 {best.Hp})", $"🐴 The horse bolts! ({best.Hp} left)");
        }

        // ── 183차: 자동사냥 대상 — 가장 가까운 잡을 거리(귀신·말 제외). bat = 방망이로 쳐야 하는 것 ─────
        public bool FindHuntTarget(Vector3 from, float r, out Vector3 pos, out bool bat)
        {
            pos = Vector3.zero; bat = false; float bd = r; bool found = false;
            foreach (var b in _bandits)
            {
                if (b.root == null || b.down) continue;
                float d = Vector3.Distance(new Vector3(b.root.position.x, 0f, b.root.position.z), new Vector3(from.x, 0f, from.z));
                if (d < bd) { bd = d; pos = b.root.position; bat = true; found = true; }
            }
            foreach (var c in _crit)
            {
                if (c.t == null || c.ghost) continue;
                float d = Vector3.Distance(new Vector3(c.t.position.x, 0f, c.t.position.z), new Vector3(from.x, 0f, from.z));
                if (d < bd) { bd = d; pos = c.t.position; bat = BatTarget(c); found = true; }
            }
            return found;
        }

        public System.Action<int> OnBanditHit;            // HP 깎기 + 돈 뺏김은 VillageHub 가
        public System.Action OnBanditGroupCleared;

        void SpawnBanditGroup()
        {
            int n = Random.value < 0.5f ? 2 : 3; float ang = Random.Range(0f, Mathf.PI * 2f);
            string[] models = { "Npc_Surfer", "Npc_FisherBoy", "Npc_Cafe", "Npc_Keeper" };
            for (int i = 0; i < n; i++)
            {
                float a = ang + (i - (n - 1) * 0.5f) * 0.35f, r = 12f + Random.Range(0f, 2f);
                var pos = new Vector3(Mathf.Clamp(Player.position.x + Mathf.Cos(a) * r, -42f, 42f), 0f, Mathf.Clamp(Player.position.z + Mathf.Sin(a) * r, 15f, 44f));
                var b = new Bandit { hp = 2, ph = 0f };
                b.root = new GameObject("Bandit").transform; b.root.SetParent(transform, false); b.root.position = VillageWorld.Ground(pos.x, pos.z);
                b.pivot = new GameObject("Pivot").transform; b.pivot.SetParent(b.root, false);
                var rig = SkaterRig.SpawnModel(ArtAssets.ResourceRoot + "Rig/" + models[Random.Range(0, models.Length)], b.pivot, 1.05f, true);
                if (rig != null)
                {
                    var anim = rig.GetComponent<Animator>(); if (anim != null) { anim.SetBool("Grounded", true); anim.Play("Run", 0, Random.value); anim.speed = 0f; }
                    b.motion = b.pivot.gameObject.AddComponent<CharacterMotion>(); b.motion.Anim = anim; b.motion.WalkSpeed = 1.2f; b.motion.RunSpeed = 2.4f; b.motion.BobScale = 0.9f; b.motion.LookTarget = Player;
                    // 산적 옷: 진한 갈색·검정으로 물들이고 눈만 남긴 복면 + 빨간 두건
                    var dark = CoastMaterials.CreateToon(new Color(0.30f, 0.22f, 0.18f), null, null, 0.05f); var black = CoastMaterials.CreateToon(new Color(0.14f, 0.12f, 0.12f), null, null, 0.05f);
                    foreach (var smr in rig.GetComponentsInChildren<SkinnedMeshRenderer>())
                    {
                        var arr = smr.sharedMaterials; for (int k = 0; k < arr.Length; k++) { string nm = arr[k] != null ? arr[k].name : ""; if (nm.Contains("Skin") || nm.Contains("Eye") || nm.Contains("White") || nm.Contains("Mouth") || nm.Contains("Blush")) continue; arr[k] = (k % 2 == 0) ? dark : black; }
                        smr.sharedMaterials = arr;
                        if (smr.GetComponent<CelOutlineHint>() == null) smr.gameObject.AddComponent<CelOutlineHint>();
                    }
                    FaceDecal.HideBlush(rig.gameObject);
                    if (anim != null && anim.isHuman)
                    {
                        var head = anim.GetBoneTransform(HumanBodyBones.Head);
                        if (head != null)
                        {
                            var band = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(band.GetComponent<Collider>()); band.transform.SetParent(head, false); band.transform.localPosition = new Vector3(0f, 0.11f, 0f); band.transform.localScale = new Vector3(0.30f, 0.035f, 0.30f);
                            band.GetComponent<MeshRenderer>().sharedMaterial = CoastMaterials.CreateLit(new Color(0.85f, 0.18f, 0.20f), 0.2f);
                            var mask = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(mask.GetComponent<Collider>()); mask.transform.SetParent(head, false); mask.transform.localPosition = new Vector3(0f, 0.02f, 0.11f); mask.transform.localScale = new Vector3(0.24f, 0.10f, 0.06f);
                            mask.GetComponent<MeshRenderer>().sharedMaterial = black;
                        }
                    }
                }
                else { var g = P(b.pivot, PrimitiveType.Capsule, new Vector3(0f, 0.6f, 0f), new Vector3(0.5f, 0.6f, 0.5f), CoastMaterials.CreateLit(new Color(0.3f, 0.22f, 0.18f))); }
                var col = b.root.gameObject.AddComponent<CapsuleCollider>(); col.center = new Vector3(0f, 0.6f, 0f); col.radius = 0.3f; col.height = 1.2f;
                b.bubble = SpeechBubble.Create(b.root, 1.55f); GroundBlob.Attach(b.root, 0.42f, 0.34f, b.pivot);
                b.said = Time.time; b.bubble.Show(i == 0 ? Loc.T("산적: 거기 서라! 가진 거 다 내놔!", "Bandit: Stop! Hand over your stuff!") : "…!");
                _bandits.Add(b);
            }
        }

        /// 방망이로 2.4 m 안 산적을 때린다. 맞으면 true.
        bool SwingBandit(out bool police)
        {
            police = false;
            Bandit best = null; float bd = 2.6f;
            foreach (var b in _bandits) { if (b.root == null || b.down) continue; float d = Vector3.Distance(new Vector3(b.root.position.x, 0f, b.root.position.z), new Vector3(Player.position.x, 0f, Player.position.z)); if (d < bd) { bd = d; best = b; } }
            if (best == null) return false;
            police = best.police;
            best.hp--; best.stun = best.police ? 0.35f : 0.9f;   // 183차: 경찰은 잘 안 밀린다
            var away = best.root.position - Player.position; away.y = 0f; var np = best.root.position + away.normalized * 1.5f; best.root.position = VillageWorld.Ground(np.x, np.z);
            VillagePang.Burst(best.root.position + Vector3.up * 0.9f, new Color(1f, 0.85f, 0.35f), Color.white, 1.1f);
            if (best.motion != null) best.motion.Hop();
            if (best.hp <= 0)
            {
                best.down = true; best.ph = 0f; best.bubble.Show(Loc.T("으악!", "Argh!"));
                if (best.police) { OnPoliceDown?.Invoke(PoliceAlive == 0); return true; }
                OnCaught?.Invoke("bandit", 1, 40);   // 186차 20→40
                if (BanditsAlive == 0) OnBanditGroupCleared?.Invoke();
            }
            else best.bubble.Show(best.police ? Loc.T($"경찰: 소용없다! (남은 힘 {best.hp})", $"Police: Useless! ({best.hp} left)") : Loc.T("윽!", "Ugh!"));
            return true;
        }
        /// 171차: 카메라 코앞(r 안)에 있는 산적·생물은 렌더러를 잠깐 끈다(카메라가 이들을 통과해 보기 때문).
        public void CullNearCamera(Vector3 cam, float r)
        {
            float r2 = r * r;
            void Apply(Transform t) { if (t == null) return; bool near = (t.position + Vector3.up * 0.8f - cam).sqrMagnitude < r2; foreach (var rd in t.GetComponentsInChildren<Renderer>(true)) { if (rd == null) continue; if (rd.enabled == near) rd.enabled = !near; } }
            foreach (var b in _bandits) Apply(b.root);
            foreach (var c2 in _crit) Apply(c2.t);
            if (_kid != null) Apply(_kid);
        }

        public bool AnyBanditNear(float r = 2.6f) { if (Player == null) return false; foreach (var b in _bandits) if (b.root != null && !b.down && Vector3.Distance(new Vector3(b.root.position.x, 0f, b.root.position.z), new Vector3(Player.position.x, 0f, Player.position.z)) < r) return true; return false; }
    }

    /// 198차(사용자: 「벌레나 에셋들 다리 움직임 만들어줘」): 자식 중 이름이 "Leg" 인 조각을 번갈아 앞뒤로 흔든다 — 움직일 때 빠르게, 멈추면 천천히.
    /// 209차: 귀신이 둥실 떠 있을 때 좌우로 살랑·앞뒤로 까딱(모델 자식만 — 이동·회전은 부모가)
    public class GhostSway : MonoBehaviour { float _p; Vector3 _lp; void Start() { _p = Random.value * 6f; _lp = transform.localPosition; } void Update() { _p += Time.deltaTime; transform.localRotation = Quaternion.Euler(Mathf.Sin(_p * 1.7f) * 6f, 0f, Mathf.Sin(_p * 2.3f) * 9f); transform.localPosition = _lp + new Vector3(Mathf.Sin(_p * 1.1f) * 0.06f, 0f, 0f); } }
    public class LegWiggle : MonoBehaviour
    {
        readonly System.Collections.Generic.List<(Transform t, Quaternion q, int i)> _legs = new System.Collections.Generic.List<(Transform, Quaternion, int)>();
        Vector3 _last; float _ph, _spd;
        void Start() { int i = 0; foreach (Transform c in transform) if (c.name == "Leg") _legs.Add((c, c.localRotation, i++)); _last = transform.position; _ph = Random.value * 6f; }
        void Update()
        {
            float dt = Mathf.Max(1e-4f, Time.deltaTime); float v = (transform.position - _last).magnitude / dt; _last = transform.position;
            _spd = Mathf.Lerp(_spd, v, dt * 8f); _ph += dt * (2f + Mathf.Min(_spd, 3f) * 7f);
            float amp = Mathf.Lerp(6f, 28f, Mathf.Clamp01(_spd / 1.2f));
            foreach (var l in _legs) { if (l.t == null) continue; float s = Mathf.Sin(_ph + (l.i % 2 == 0 ? 0f : Mathf.PI) + (l.i / 2) * 0.9f); l.t.localRotation = l.q * Quaternion.Euler(s * amp * 0.35f, s * amp, 0f); }
        }
    }
    /// 198차: 희귀 벌레 반짝임(숨쉬듯 커졌다 작아짐)
    public class Twinkle198 : MonoBehaviour { Vector3 _s; float _p; void Start() { _s = transform.localScale; _p = Random.value * 6f; } void Update() { _p += Time.deltaTime * 4f; transform.localScale = _s * (0.85f + 0.25f * Mathf.Abs(Mathf.Sin(_p))); } }
}
