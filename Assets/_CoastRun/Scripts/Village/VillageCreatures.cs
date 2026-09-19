using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun.Village
{
    /// 137차(사용자): 마을 NPC(치비 리그 재활용, 길 따라 산책, 주인공을 보면 「누구세요?」「음…」만 하고 말을 안 걸어줌),
    /// 주인공을 따라다니는 꼬마, 정령(방망이로 잡음·부딪히면 HP 깎임)·나비(잠자리채로 잡음).
    public class VillageCreatures : MonoBehaviour
    {
        public Transform Player; public System.Func<bool> Locked;
        public System.Action<int> OnSpiritHit;           // HP 깎기
        public System.Action<string, int, int> OnCaught; // (종류, 별조각, 코인)

        // ── NPC ──
        class Npc { public Transform root; public Animator anim; public CharacterMotion motion; public Vector3 home, target, vel, acc; public float wait; public Text label; public Canvas cv; public float said; public int line; public string name; public bool noticed; public float idleAct; }
        readonly List<Npc> _npcs = new List<Npc>();
        static readonly string[] NpcKo = { "누구세요?", "음…", "…?", "처음 보는 얼굴인데.", "(고개를 갸웃한다)" };
        static readonly string[] NpcEn = { "Who are you?", "Hmm…", "…?", "Never seen you before.", "(tilts head)" };
        static readonly Color[] Shirts = { new Color(0.55f, 0.75f, 0.98f), new Color(0.98f, 0.70f, 0.45f), new Color(0.72f, 0.88f, 0.55f), new Color(0.90f, 0.60f, 0.90f) };

        // ── 꼬마 ──
        Transform _kid; Vector3 _kidVel; int _kidLine;

        // ── 정령·나비 ──
        class Critter { public Transform t; public bool spirit; public float life, phase; public Vector3 anchor; }
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
            new NpcDef { model = "Npc_Keeper", ko = "등대지기", en = "Lighthouse keeper", home = new Vector2(-10f, -58f) },
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
                }
                var sh = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(sh.GetComponent<Collider>()); sh.transform.SetParent(root, false);
                sh.transform.localPosition = new Vector3(0f, 0.03f, 0f); sh.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); sh.transform.localScale = new Vector3(0.8f, 0.6f, 1f);
                sh.GetComponent<MeshRenderer>().sharedMaterial = MiniStage3D.SoftDisc(new Color(0f, 0f, 0f, 0.25f));
                var col = root.gameObject.AddComponent<CapsuleCollider>(); col.center = new Vector3(0f, 0.6f, 0f); col.radius = 0.3f; col.height = 1.2f;
                // 머리 위 말풍선 라벨(이름 + 한마디)
                var cvGo = new GameObject("Label", typeof(RectTransform), typeof(Canvas)); cvGo.transform.SetParent(root, false);
                cvGo.transform.localPosition = new Vector3(0f, 1.55f, 0f);
                n.cv = cvGo.GetComponent<Canvas>(); n.cv.renderMode = RenderMode.WorldSpace; n.cv.sortingOrder = 6;
                var rt = cvGo.GetComponent<RectTransform>(); rt.sizeDelta = new Vector2(300f, 60f); rt.localScale = Vector3.one * 0.01f;
                var bg = CoastUiArt.CutePill(rt, "Bg", new Color(1f, 1f, 1f, 0.92f), 20, 3); bg.raycastTarget = false;
                bg.rectTransform.anchorMin = Vector2.zero; bg.rectTransform.anchorMax = Vector2.one; bg.rectTransform.offsetMin = bg.rectTransform.offsetMax = Vector2.zero;
                n.label = CoastHudLayout.MakeText(rt, "T", "", 20, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(10f, 2f), new Vector2(-10f, 0f));
                n.label.color = new Color(0.30f, 0.25f, 0.40f); n.label.fontStyle = FontStyle.Bold; n.label.raycastTarget = false;
                n.label.resizeTextForBestFit = true; n.label.resizeTextMinSize = 10; n.label.resizeTextMaxSize = CoastHudLayout.Scaled(20);
                cvGo.SetActive(false);
                n.target = n.home; n.wait = Random.Range(1f, 3f); n.idleAct = Random.Range(3f, 8f);
                _npcs.Add(n);
            }
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
                if (dp < 5f && !n.noticed) { n.noticed = true; n.said = Time.time - 2.6f; n.label.text = "!"; n.cv.gameObject.SetActive(true); if (n.motion != null) n.motion.Hop(); }
                else if (dp > 9f) n.noticed = false;
                if (sees)
                {
                    // 주인공을 보면 멈춰서 쳐다보고, 「누구세요?」「음…」만 — 말은 안 걸어준다
                    var f = pp - p; f.y = 0f; if (f.sqrMagnitude > 0.01f) n.root.rotation = Quaternion.Slerp(n.root.rotation, Quaternion.LookRotation(f, Vector3.up), 1f - Mathf.Exp(-dt * 5f));
                    n.vel = Vector3.zero;
                    if (Time.time - n.said > 3.5f) { n.said = Time.time; n.label.text = n.name + ": " + Loc.T(NpcKo[n.line % NpcKo.Length], NpcEn[n.line % NpcEn.Length]); n.line++; if (n.motion != null && n.line % 2 == 0) n.motion.Nod(); }
                    n.cv.gameObject.SetActive(true);
                }
                else
                {
                    if (n.cv.gameObject.activeSelf && Time.time - n.said > 2.5f) n.cv.gameObject.SetActive(false);
                    var d = n.target - p; d.y = 0f;
                    if (d.magnitude < 0.35f)
                    {
                        n.wait -= dt; n.vel = Vector3.zero;
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
                if (n.cv.gameObject.activeSelf && Camera.main != null) { var cf = Camera.main.transform.forward; cf.y = 0f; n.cv.transform.rotation = Quaternion.LookRotation(cf, Vector3.up); }
            }
        }

        /// 가까운 NPC 에게 말 걸기 — 대답은 「누구세요?」「음…」뿐.
        public bool TalkNearestNpc()
        {
            if (Player == null) return false;
            foreach (var n in _npcs)
            {
                if (n.root == null) continue;
                if (Vector3.Distance(n.root.position, Player.position) < 3.2f)
                {
                    n.said = Time.time; n.label.text = n.name + ": " + Loc.T(NpcKo[n.line % NpcKo.Length], NpcEn[n.line % NpcEn.Length]); n.line++; n.cv.gameObject.SetActive(true);
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
                _kidAnim = rig.GetComponent<Animator>();
                if (_kidAnim != null) { _kidAnim.SetBool("Grounded", true); _kidAnim.Play("Run", 0, 0.12f); _kidAnim.speed = 0f; }
                _kidMotion = pivot.gameObject.AddComponent<CharacterMotion>(); _kidMotion.Anim = _kidAnim; _kidMotion.WalkSpeed = 1.5f; _kidMotion.RunSpeed = 3.4f; _kidMotion.Stride = 0.62f; _kidMotion.BobScale = 1.25f; _kidMotion.LookTarget = Player;
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
            var sh = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(sh.GetComponent<Collider>()); sh.name = "KidShadow"; sh.transform.SetParent(root, false);
            sh.transform.localPosition = new Vector3(0f, 0.03f, 0f); sh.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); sh.transform.localScale = new Vector3(0.7f, 0.5f, 1f);
            sh.GetComponent<MeshRenderer>().sharedMaterial = MiniStage3D.SoftDisc(new Color(0f, 0f, 0f, 0.25f));
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

        void TickKid(float dt)
        {
            if (_kid == null || Player == null) return;
            var want = Player.position - Player.forward * 1.3f + Player.right * 0.8f;
            var cur = _kid.position; cur.y = 0f; want.y = 0f;
            float far = Vector3.Distance(cur, want);
            var next = far < 0.25f ? cur : Vector3.SmoothDamp(cur, want, ref _kidVel, 0.30f, 3.2f, dt);
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
            string[] ko = { "누나! 오늘은 바다가 조용해. 낚시하기 좋은 날이야.", "우리집에 들어가면 이번 주 할 일을 정할 수 있어.", "엄마 집에서 낮잠 자면 기운이 나. 펫도 거기 있어!", "상점 아줌마가 오늘 생선을 싸게 판대.", "텃밭에 물 줬어? 주가 바뀌면 쑥쑥 자라.", "귤나무를 흔들면 귤이 떨어져. 일주일에 한 번만!", "마을 사람들은 아직 누나를 몰라서 말을 안 걸어. 곧 친해질 거야.", "정령이 오면 방망이로 톡! 부딪히면 아파." };
            string[] en = { "Sis! The sea is calm — good for fishing.", "Go in our house to set this week's plan.", "A nap at Mom's brings energy back. The pets are there!", "The shop has cheap fish today.", "Watered the garden? It grows when the week turns.", "Shake the orange tree for oranges — once a week!", "The villagers don't know you yet, so they won't talk. Soon!", "When a spirit comes, bop it with the bat! Bumping hurts." };
            int i = _kidLine++ % ko.Length; return Loc.T(ko[i], en[i]);
        }

        // ── 정령·나비 ──────────────────────────────────────────────────
        void TickCritters(float dt)
        {
            _spawnT -= dt;
            if (_spawnT <= 0f && Player != null)
            {
                _spawnT = Random.Range(6f, 10f);
                int spirits = 0, bugs = 0; foreach (var c in _crit) if (c.spirit) spirits++; else bugs++;
                bool spirit = Random.value < 0.6f ? spirits < 2 : bugs >= 3;
                if (spirit && spirits < 2) Spawn(true); else if (bugs < 3) Spawn(false);
            }
            for (int i = _crit.Count - 1; i >= 0; i--)
            {
                var c = _crit[i]; if (c.t == null) { _crit.RemoveAt(i); continue; }
                c.life -= dt; c.phase += dt;
                var p = c.t.position;
                if (c.spirit)
                {
                    // 주인공 쪽으로 천천히 다가오며 위아래로 흔들림
                    var d = Player.position + Vector3.up * 0.9f - p; d.y = 0f;
                    var step = d.sqrMagnitude > 0.01f ? d.normalized * 1.1f * dt : Vector3.zero;
                    float g = VillageWorld.Height(p.x + step.x, p.z + step.z) + 0.9f + Mathf.Sin(c.phase * 3f) * 0.25f;
                    c.t.position = new Vector3(p.x + step.x, g, p.z + step.z);
                    c.t.localScale = Vector3.one * (0.34f + Mathf.Sin(c.phase * 5f) * 0.04f);
                    if (Vector3.Distance(new Vector3(p.x, 0f, p.z), new Vector3(Player.position.x, 0f, Player.position.z)) < 0.75f)
                    {
                        OnSpiritHit?.Invoke(3); Pop(c, new Color(0.9f, 0.4f, 0.6f)); _crit.RemoveAt(i); continue;
                    }
                }
                else
                {
                    // 나비: 닻 주변을 8자로 팔랑
                    var o = new Vector3(Mathf.Sin(c.phase * 1.3f) * 2.2f, 0f, Mathf.Sin(c.phase * 2.6f) * 1.2f);
                    var np = c.anchor + o; np.y = VillageWorld.Height(np.x, np.z) + 0.9f + Mathf.Sin(c.phase * 7f) * 0.15f;
                    c.t.position = np; c.t.rotation = Quaternion.Euler(0f, c.phase * 90f, Mathf.Sin(c.phase * 14f) * 35f);
                }
                if (c.life <= 0f) { Destroy(c.t.gameObject); _crit.RemoveAt(i); }
            }
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
                var g = new GameObject("Butterfly"); g.transform.SetParent(transform, false); g.transform.position = pos;
                Color col = Random.value < 0.5f ? new Color(1f, 0.75f, 0.30f) : new Color(1f, 0.55f, 0.75f);
                for (int k = 0; k < 2; k++)
                {
                    var w = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(w.GetComponent<Collider>()); w.transform.SetParent(g.transform, false);
                    w.transform.localPosition = new Vector3(k == 0 ? -0.16f : 0.16f, 0f, 0f); w.transform.localScale = new Vector3(0.28f, 0.06f, 0.2f);
                    w.GetComponent<MeshRenderer>().sharedMaterial = CoastMaterials.CreateUnlit(col);
                }
                c.t = g.transform;
            }
            _crit.Add(c);
        }

        void Pop(Critter c, Color col)
        {
            if (c.t == null) return;
            StartCoroutine(PopCo(c.t, col));
        }
        System.Collections.IEnumerator PopCo(Transform t, Color col)
        {
            float k = 0f; var s0 = t.localScale;
            while (k < 0.25f) { k += Time.deltaTime; if (t == null) yield break; t.localScale = s0 * (1f + k * 3f); yield return null; }
            if (t != null) Destroy(t.gameObject);
        }

        /// 도구 휘두르기: bat=true 면 정령, false 면 나비를 2.4m 안에서 잡는다. 결과 메시지 반환(null = 아무것도 없음).
        public string Swing(bool bat)
        {
            if (Player == null) return null;
            Critter best = null; float bd = 2.4f; bool wrong = false;
            foreach (var c in _crit)
            {
                if (c.t == null) continue;
                float d = Vector3.Distance(new Vector3(c.t.position.x, 0f, c.t.position.z), new Vector3(Player.position.x, 0f, Player.position.z));
                if (d < bd) { if (c.spirit == bat) { bd = d; best = c; } else wrong = true; }
            }
            if (best == null) return wrong ? (bat ? Loc.T("나비는 잠자리채로!", "Use the net for butterflies!") : Loc.T("정령은 방망이로!", "Use the bat for spirits!")) : null;
            _crit.Remove(best); Pop(best, Color.white);
            if (bat) { OnCaught?.Invoke("spirit", 2, 30); return Loc.T("✨ 정령을 잡았다! 별조각 +2 · 30G", "✨ Caught a spirit! Shards +2 · 30G"); }
            OnCaught?.Invoke("butterfly", 1, 0); return Loc.T("🦋 나비를 잡았다! 별조각 +1", "🦋 Caught a butterfly! Shard +1");
        }

        public bool AnyCritterNear(bool spirit, float r = 2.6f)
        {
            if (Player == null) return false;
            foreach (var c in _crit) if (c.t != null && c.spirit == spirit && Vector3.Distance(new Vector3(c.t.position.x, 0f, c.t.position.z), new Vector3(Player.position.x, 0f, Player.position.z)) < r) return true;
            return false;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            TickNpcs(dt); TickKid(dt);
            if (Locked == null || !Locked()) TickCritters(dt);
        }
    }
}
