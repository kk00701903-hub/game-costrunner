using UnityEngine;
using UnityEngine.UI;

namespace CoastRun
{
    /// 66차-1(사용자): 육성 모드의 대회 러닝에는 **꼬마 + 다른 캐릭터 2명**이 같이 달려 경쟁한다.
    ///   · 라이벌은 길 위의 그림 인형(뒷모습 스프라이트 + 빌보드) — 레인을 따라 달리고, 가끔 레인을 바꾸고, 점프한다.
    ///   · 속도는 주인공과 비슷하게(고무줄: 너무 멀어지면 붙고, 너무 앞서면 늦춘다) 앞뒤로 엎치락뒤치락.
    ///   · ContestHud 에 「N위」 — 주인공 포함 4명 중 진행 거리 순위.
    public class ContestRivals : MonoBehaviour
    {
        public static ContestRivals Instance { get; private set; }

        private class Rival
        {
            public string name; public Color col; public Transform root; public Transform body; public float baseH;
            public float dist, lateral, laneTarget, hop, hopVel, speedMul, phase, nextLane, nextHop, mood, moodT;
            public int lane;
        }

        private PlayerController _player;
        private readonly Rival[] _rivals = new Rival[3];
        private System.Random _rng;
        private float _laneOffset = 2.2f;
        private float _hudT;

        public static void Create(PlayerController player, int seed)
        {
            if (player == null) return;
            if (Instance != null) Destroy(Instance.gameObject);
            var go = new GameObject("ContestRivals");
            var r = go.AddComponent<ContestRivals>();
            r._player = player;
            r._rng = new System.Random(seed);
            r._laneOffset = player.Config != null ? player.Config.laneOffset : 2.2f;
            r.Build();
        }

        public static void Clear() { if (Instance != null) Destroy(Instance.gameObject); Instance = null; }

        private void Awake() { Instance = this; }
        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void Build()
        {
            // 꼬마(집사 소년) + 라이벌 둘. 그림이 없으면 그 자리는 비운다(폴백: 캡슐 인형).
            string[] keys = { "Rival_Kid", "Rival_Boy", "Rival_Girl" };
            string[] fallback = { "MG_Char_ButlerBack", null, null };
            string[] ko = { "꼬마", "보미", "태오" };
            string[] en = { "Kid", "Bomi", "Taeo" };
            Color[] cols = { new Color(0.35f, 0.65f, 1f), new Color(1f, 0.55f, 0.25f), new Color(0.65f, 0.45f, 0.95f) };
            float[] heights = { 1.35f, 1.5f, 1.45f };
            int[] lanes = { -1, 1, 0 };
            float[] starts = { 5f, -4f, 9f };
            for (int i = 0; i < 3; i++)
            {
                var tex = ArtAssets.LoadTexture(keys[i]) ?? (fallback[i] != null ? ArtAssets.LoadTexture(fallback[i]) : null);
                var r = new Rival { name = Loc.T(ko[i], en[i]), col = cols[i], baseH = heights[i], lane = lanes[i] };
                r.root = new GameObject("Rival_" + ko[i]).transform;
                r.root.SetParent(transform, false);
                r.body = MakeBody(r.root, tex, heights[i], cols[i]);
                r.dist = _player.PathDistance + starts[i];
                r.lateral = r.laneTarget = lanes[i] * _laneOffset;
                r.speedMul = 1f;
                r.phase = (float)_rng.NextDouble() * 6.28f;
                r.nextLane = 2f + (float)_rng.NextDouble() * 4f;
                r.nextHop = 3f + (float)_rng.NextDouble() * 5f;
                r.mood = 0f; r.moodT = 0f;
                BlobShadow.Attach(r.root, 0.55f);
                MakeTag(r);
                _rivals[i] = r;
            }
        }

        private static Transform MakeBody(Transform root, Texture2D tex, float height, Color tint)
        {
            if (tex == null)
            {
                var cap = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                cap.name = "Body"; cap.transform.SetParent(root, false);
                CoastEditUtil.DestroyCollider(cap);
                cap.transform.localScale = new Vector3(0.5f, height * 0.5f, 0.5f);
                cap.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);
                cap.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateLit(tint, 0.4f);
                return cap.transform;
            }
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Body"; quad.transform.SetParent(root, false);
            CoastEditUtil.DestroyCollider(quad);
            float w = height * tex.width / (float)tex.height;
            quad.transform.localScale = new Vector3(w, height, 1f);
            quad.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);
            var shader = Shader.Find("CoastRun/ChromaUnlit") ?? CoastMaterials.UnlitShader;
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex); else mat.mainTexture = tex;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            if (mat.HasProperty("_KeyColor")) mat.SetColor("_KeyColor", new Color(1f, 0f, 1f, 1f));
            if (mat.HasProperty("_OutlineOn")) { mat.SetFloat("_OutlineOn", 1f); mat.SetColor("_OutlineColor", new Color(0.06f, 0.05f, 0.10f, 1f)); mat.SetFloat("_OutlineWidth", 6f); }
            var mr = quad.GetComponent<Renderer>();
            mr.sharedMaterial = mat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
            quad.AddComponent<YawBillboard>();
            return quad.transform;
        }

        /// 머리 위 이름표(월드 캔버스 알약).
        private static void MakeTag(Rival r)
        {
            var go = new GameObject("Tag", typeof(RectTransform), typeof(Canvas));
            go.transform.SetParent(r.root, false);
            var cv = go.GetComponent<Canvas>(); cv.renderMode = RenderMode.WorldSpace; cv.sortingOrder = 20;
            var rt = go.GetComponent<RectTransform>(); rt.sizeDelta = new Vector2(160f, 44f); rt.localScale = Vector3.one * 0.006f;
            rt.localPosition = new Vector3(0f, r.baseH + 0.32f, 0f);
            var pill = CoastUiArt.GlossyPill(rt, "Pill", r.col, 22, 6); pill.raycastTarget = false;
            pill.rectTransform.anchorMin = Vector2.zero; pill.rectTransform.anchorMax = Vector2.one; pill.rectTransform.offsetMin = Vector2.zero; pill.rectTransform.offsetMax = Vector2.zero;
            var t = CoastHudLayout.MakeText(pill.rectTransform, "T", r.name, 24, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0f, 3f), Vector2.zero);
            t.color = Color.white; t.fontStyle = FontStyle.Bold; CoastUiArt.OutlineText(t, new Color(0f, 0f, 0f, 0.45f), 1.5f);
            go.AddComponent<YawBillboard>();
        }

        private void Update()
        {
            if (_player == null) { Destroy(gameObject); return; }
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            bool finished = _player.State == SkateState.Finish;
            float pd = _player.PathDistance;
            float ps = Mathf.Max(2f, _player.Speed * _player.SpeedBoost);
            for (int i = 0; i < _rivals.Length; i++)
            {
                var r = _rivals[i]; if (r == null) continue;
                // 기분(앞서기/뒤처지기)을 3~6초마다 바꾼다 → 엎치락뒤치락
                r.moodT -= dt;
                if (r.moodT <= 0f) { r.mood = (float)_rng.NextDouble() * 2f - 1f; r.moodT = 3f + (float)_rng.NextDouble() * 3f; }
                // 고무줄: 주인공 기준 -10 m ~ +15 m 안에서 엎치락뒤치락
                float rel = r.dist - pd;
                float want = 1f + r.mood * 0.10f;
                if (rel > 15f) want = 0.78f; else if (rel > 9f) want = Mathf.Min(want, 0.94f);
                if (rel < -10f) want = 1.28f; else if (rel < -5f) want = Mathf.Max(want, 1.08f);
                if (finished) want = 0.6f;
                r.speedMul = Mathf.MoveTowards(r.speedMul, want, dt * 0.6f);
                r.dist += ps * r.speedMul * dt;
                // 레인: 가끔 바꾼다(주인공과 가까울 땐 주인공 레인은 피한다)
                r.nextLane -= dt;
                if (r.nextLane <= 0f)
                {
                    r.nextLane = 2.5f + (float)_rng.NextDouble() * 4.5f;
                    int nl = Mathf.Clamp(r.lane + (_rng.Next(2) == 0 ? -1 : 1), -1, 1);
                    bool near = Mathf.Abs(rel) < 4f;
                    if (!(near && nl == _player.Lane) && nl != r.lane)
                    {
                        bool taken = false;
                        for (int j = 0; j < _rivals.Length; j++) if (j != i && _rivals[j] != null && _rivals[j].lane == nl && Mathf.Abs(_rivals[j].dist - r.dist) < 3f) taken = true;
                        if (!taken) { r.lane = nl; r.laneTarget = nl * _laneOffset; }
                    }
                }
                r.lateral = Mathf.MoveTowards(r.lateral, r.laneTarget, dt * 7f);
                // 점프: 가끔
                r.nextHop -= dt;
                if (r.nextHop <= 0f && r.hop <= 0.001f) { r.nextHop = 3f + (float)_rng.NextDouble() * 6f; r.hopVel = 5.2f; }
                if (r.hop > 0f || r.hopVel > 0f)
                {
                    r.hopVel -= 20f * dt; r.hop += r.hopVel * dt;
                    if (r.hop <= 0f) { r.hop = 0f; r.hopVel = 0f; }
                }
                // 달리기 흔들림
                r.phase += dt * (8f + 4f * r.speedMul);
                float bob = r.hop > 0f ? 0f : Mathf.Abs(Mathf.Sin(r.phase)) * 0.06f;
                r.root.position = DownhillPath.Point(r.dist, r.lateral, r.hop + bob);
                r.root.rotation = DownhillPath.Rotation;
                if (r.body != null)
                {
                    float roll = Mathf.Sin(r.phase) * 2.5f + Mathf.Clamp((r.laneTarget - r.lateral) * 5f, -10f, 10f);
                    r.body.localRotation = Quaternion.Euler(0f, 0f, roll);
                }
            }
        }

        /// 주인공 순위(1 = 1등). 라이벌 진행 거리보다 앞선 수로 계산.
        public int PlayerRank()
        {
            if (_player == null) return 1;
            int rank = 1; float pd = _player.PathDistance;
            for (int i = 0; i < _rivals.Length; i++) if (_rivals[i] != null && _rivals[i].dist > pd) rank++;
            return rank;
        }

        public static string RankText()
        {
            if (Instance == null) return "";
            int r = Instance.PlayerRank();
            return Loc.T($"{r}위", r == 1 ? "1st" : r == 2 ? "2nd" : r == 3 ? "3rd" : "4th");
        }
    }
}
