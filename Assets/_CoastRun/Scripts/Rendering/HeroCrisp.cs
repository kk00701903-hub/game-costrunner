using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CoastRun
{
    /// 229차(사용자: 「마을에서 주인공이 게임 캐릭터 같지 않게 수채화풍이고 또렷하지 않음」):
    ///   마을은 전체가 부드러운 룩(_CoastSoft: 넓은 명암 경계·밝은 그늘·뿌연 앰비언트)이라 주인공도 배경처럼 물들어 보였다.
    ///   주인공 몸 재질에만 ① 소프트 룩을 덜 받게(_CharCrisp — 명암 경계가 또렷한 셀 셰이딩) ② 몸 테두리빛(_CharRim)
    ///   ③ 잉크 윤곽선을 조금 굵게 — 배경은 그대로 두고 주인공만 또렷하게. 크기·색·모델은 그대로.
    ///   옷을 갈아입어 몸이 다시 만들어져도 1초마다 확인해 다시 적용한다.
    public class HeroCrisp : MonoBehaviour
    {
        public float Crisp = 1.0f;           // 0 = 마을 소프트 룩 그대로, 1 = 러닝처럼 또렷한 셀 셰이딩
        public float Rim = 0.42f;            // 몸 테두리빛
        public float ShadowThreshold = 0.5f, ShadowSoftness = 0.025f;
        public float OutlineWidth = 0.024f;  // 잉크 윤곽 두께(기본 0.010)

        // 230차(사용자 허락: 목장이야기 비교 제안 2·3): 뒷모습 표지(하트 머리핀) · 배경과 색 대비(머리 조금 밝고 따뜻하게, 노란 옷을 돌길과 다른 주황빛 노랑으로)
        public bool HeartPin = true;   // (231차부터 하트 대신 노란 머리핀 두 개)
        public Color HeartColor = new Color(0.98f, 0.30f, 0.45f);
        static readonly Color JacketWarm = new Color(1.0f, 0.62f, 0.20f);   // 돌길(연노랑)과 갈리는 주황빛 노랑
        const float JacketBlend = 0.38f, HairLift = 1.18f;
        readonly HashSet<Material> _tinted = new HashSet<Material>();
        Transform _pin;

        static readonly int IdCrisp = Shader.PropertyToID("_CharCrisp"), IdRim = Shader.PropertyToID("_CharRim"),
            IdThr = Shader.PropertyToID("_ShadowThreshold"), IdSoft = Shader.PropertyToID("_ShadowSoftness"), IdWidth = Shader.PropertyToID("_Width");

        IEnumerator Start()
        {
            yield return null; yield return null;   // CelOutlineHint 가 윤곽 셸을 만든 뒤
            var wait = new WaitForSeconds(1f);
            while (true) { Apply(); yield return wait; }
        }

        /// 재질 이름(HN_Jacket·HN_Hair·HN_HairDark)으로 한 번만 색을 바꾼다(다시 적용돼도 누적되지 않게)
        void Tint(Material m)
        {
            if (_tinted.Contains(m) || !m.HasProperty("_BaseColor")) return;
            _tinted.Add(m);
            // 231차(사용자 결정: 「진짜 하늘」 = 검은 단발 + 노란 머리핀 · 노란 티 · 청반바지 · 파란 가방 · 흰 운동화): 3D 모델 색을 그림과 같게
            string n = m.name; Color? want = null; bool plain = false;
            if (n.StartsWith("HN_HairDark")) want = new Color(0.09f, 0.09f, 0.12f);
            else if (n.StartsWith("HN_Hair")) want = new Color(0.20f, 0.19f, 0.24f);
            else if (n.StartsWith("HN_Jacket")) { want = new Color(1.0f, 0.80f, 0.16f); plain = true; }   // 줄무늬 재킷 → 노란 티처럼(무늬 없이)
            else if (n.StartsWith("HN_Tee")) want = new Color(1.0f, 0.84f, 0.22f);
            else if (n.StartsWith("HN_JeansDark")) want = new Color(0.33f, 0.50f, 0.76f);
            else if (n.StartsWith("HN_Jeans")) want = new Color(0.47f, 0.66f, 0.90f);
            else if (n.StartsWith("HN_PinkDark")) want = new Color(0.18f, 0.33f, 0.68f);   // 분홍 가방 → 파란 가방
            else if (n.StartsWith("HN_Pink")) want = new Color(0.27f, 0.47f, 0.86f);
            else if (n.StartsWith("HN_Shoe")) want = new Color(0.97f, 0.97f, 0.96f);
            if (want.HasValue) { var c = m.GetColor("_BaseColor"); var w = want.Value; w.a = c.a; m.SetColor("_BaseColor", w); }
            if (plain && m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", Texture2D.whiteTexture);
        }

        /// 뒤에서도 「하늘」로 읽히게 — 머리 뒤쪽 위에 하트 머리핀(일러스트의 하트 핀)
        void EnsurePin()
        {
            if (!HeartPin || (_pin != null && _pin.gameObject.activeInHierarchy)) return;
            var anim = GetComponentInChildren<Animator>(); if (anim == null || anim.avatar == null || !anim.avatar.isHuman) return;
            var head = anim.GetBoneTransform(HumanBodyBones.Head); if (head == null) return;
            // 231차: 그림의 노란 머리핀 두 개(이마 위 한쪽) — 앞·옆·뒤 어디서도 보이게 머리카락 겉에 비스듬히
            var mat = CoastMaterials.CreateToon(new Color(1.0f, 0.84f, 0.12f));
            if (mat != null) { mat.SetFloat(IdCrisp, 1f); mat.SetFloat(IdRim, 0.25f); }
            float topY = head.position.y + 0.25f;
            foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>(true)) if (smr.name != "Outline") topY = Mathf.Max(topY, smr.bounds.max.y);
            var body = transform;
            var holder = new GameObject("HairClipHolder").transform; holder.SetParent(head, false);
            var hp = head.position; hp.y = topY - 0.12f;
            // 주인공 왼쪽(정면에서 보면 오른쪽) 이마 위 — 머리카락 겉면(반지름 ≈0.3 m)
            holder.position = hp - body.right * 0.22f + body.forward * 0.12f;
            holder.rotation = Quaternion.LookRotation(body.forward - body.right * 0.9f, body.up) * Quaternion.Euler(0f, 0f, 28f);
            float ls = Mathf.Max(0.0001f, holder.lossyScale.x);
            for (int i = 0; i < 2; i++)
            {
                var bar = GameObject.CreatePrimitive(PrimitiveType.Cube); bar.name = "HairClip"; Destroy(bar.GetComponent<Collider>());
                bar.transform.SetParent(holder, false);
                bar.transform.localScale = new Vector3(0.13f, 0.026f, 0.02f) / ls;
                bar.transform.localPosition = new Vector3(0f, (i == 0 ? 0.022f : -0.022f), 0f) / ls;
                var r = bar.GetComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                bar.AddComponent<CelOutlineHint>();
            }
            var go = holder.gameObject; var prefab = (GameObject)null; var sz = Vector3.one * 0.13f; float k = 1f;
            _pin = holder;
            Debug.Log($"[HeroCrisp] hair clip on {head.name}");
        }

        bool _shortsDone, _bangsDone, _logged; float _t0 = -1f;
        public bool Shorts = true, Bangs = true;
        static readonly Color HairBlack = new Color(0.20f, 0.19f, 0.24f);

        /// 232차: 3D 하늘을 그림(검은 단발·앞머리·청반바지)과 같게 — ① 긴 청바지의 무릎 아래를 맨다리(피부)로 ② 이마 앞머리 덮개
        SkinnedMeshRenderer MainBody()
        {
            SkinnedMeshRenderer best = null; int bestN = -1;
            foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>(true)) { if (smr.name == "Outline" || smr.sharedMesh == null) continue; int n = smr.sharedMesh.vertexCount; if (n > bestN) { bestN = n; best = smr; } }
            return best;
        }

        void LogParts(SkinnedMeshRenderer smr)
        {
            if (_logged || smr == null) return; _logged = true;
            var m = smr.sharedMesh; var vs = m.vertices; var mats = smr.sharedMaterials; var sb = new System.Text.StringBuilder("[HeroCrisp] parts ");
            sb.Append(m.name).Append(" bounds ").Append(m.bounds.ToString()).Append('\n');
            for (int i = 0; i < m.subMeshCount; i++)
            {
                var tr = m.GetTriangles(i); if (tr.Length == 0) { sb.Append($"  {i} {(i < mats.Length && mats[i] != null ? mats[i].name : "-")} (empty)\n"); continue; }
                var mn = vs[tr[0]]; var mx = mn; foreach (var t in tr) { mn = Vector3.Min(mn, vs[t]); mx = Vector3.Max(mx, vs[t]); }
                sb.Append($"  {i} {(i < mats.Length && mats[i] != null ? mats[i].name : "-")} tris {tr.Length / 3} min {mn} max {mx}\n");
            }
            Debug.Log(sb.ToString());
        }

        void MakeShorts(SkinnedMeshRenderer smr)
        {
            var m = smr.sharedMesh; var mats = smr.sharedMaterials;
            if (!m.name.Contains("_NoFace") && Time.time - _t0 < 3f) return;   // FaceDecal 이 메시를 복제한 뒤에
            _shortsDone = true;
            if (!m.name.Contains("_NoFace") && !m.name.Contains("_Shorts")) { m = Instantiate(m); m.name += "_Shorts"; smr.sharedMesh = m; }
            var vs = m.vertices; var jeans = new List<int>(); int skinIdx = -1;
            for (int i = 0; i < mats.Length && i < m.subMeshCount; i++) { var n = mats[i] != null ? mats[i].name : ""; if (n.StartsWith("HN_Jeans")) jeans.Add(i); if (n.StartsWith("HN_Skin")) skinIdx = i; }
            if (jeans.Count == 0 || skinIdx < 0) return;
            // 청바지 전체 높이(메시 공간, 위축 = 바운드가 가장 긴 축)
            var b = m.bounds; int up = b.size.y >= b.size.z ? 1 : 2;
            float lo = float.MaxValue, hi = float.MinValue;
            foreach (var ji in jeans) foreach (var t in m.GetTriangles(ji)) { float y = vs[t][up]; lo = Mathf.Min(lo, y); hi = Mathf.Max(hi, y); }
            float cut = lo + (hi - lo) * 0.60f;   // 위 40% 만 반바지(허벅지 중간), 아래는 맨다리
            var legs = new List<int>();
            foreach (var ji in jeans)
            {
                var tr = m.GetTriangles(ji); var keep = new List<int>(tr.Length);
                for (int t = 0; t < tr.Length; t += 3)
                {
                    float cy = (vs[tr[t]][up] + vs[tr[t + 1]][up] + vs[tr[t + 2]][up]) / 3f;
                    if (cy < cut) { legs.Add(tr[t]); legs.Add(tr[t + 1]); legs.Add(tr[t + 2]); } else { keep.Add(tr[t]); keep.Add(tr[t + 1]); keep.Add(tr[t + 2]); }
                }
                m.SetTriangles(keep, ji, false);
            }
            if (legs.Count == 0) return;
            // 머리 위 안경·띠(HN_Silver·HN_HairDark — 그림에 없음)는 비운다
            for (int i = 0; i < mats.Length && i < m.subMeshCount; i++) { var n2 = mats[i] != null ? mats[i].name : ""; if (n2.StartsWith("HN_Silver") || n2.StartsWith("HN_HairDark")) m.SetTriangles(new int[0], i, false); }
            int newIdx = m.subMeshCount; m.subMeshCount = newIdx + 1; m.SetTriangles(legs, newIdx, false);
            var nm = new Material[newIdx + 1]; for (int i = 0; i < newIdx && i < mats.Length; i++) nm[i] = mats[i]; for (int i = mats.Length; i < newIdx; i++) nm[i] = mats[mats.Length - 1];
            nm[newIdx] = mats[skinIdx]; smr.sharedMaterials = nm;
            foreach (var o in GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (o.name == "Outline" && (o.sharedMesh == m || o.transform.parent == smr.transform))
                {
                    o.sharedMesh = m; var om = o.sharedMaterials; var on = new Material[newIdx + 1]; for (int i = 0; i <= newIdx; i++) on[i] = om.Length > 0 ? om[Mathf.Min(i, om.Length - 1)] : null; o.sharedMaterials = on;
                }
            Debug.Log($"[HeroCrisp] shorts: jeans {jeans.Count} submesh, cut {cut:0.###} (range {lo:0.###}~{hi:0.###}), legs tris {legs.Count / 3}");
        }

        /// 이마를 덮는 앞머리(FaceDecal 과 같은 머리 구 — 머리 뼈 가중치 경계) — 눈 바로 위에서 끝나는 톱니 끝
        void MakeBangs()
        {
            var anim = GetComponentInChildren<Animator>(); if (anim == null || anim.avatar == null || !anim.avatar.isHuman) return;
            var head = anim.GetBoneTransform(HumanBodyBones.Head); if (head == null) return;
            if (transform.parent != null && transform.parent.Find("FaceDecal") == null && Time.time - _t0 < 3f) return;   // 얼굴 데칼이 먼저
            _bangsDone = true;
            if (!HeadSphere(head, out float rad, out Vector3 cLocal)) return;
            var center = head.TransformPoint(cLocal); float R = rad * 1.02f;
            var body = transform; var fwd = body.forward; fwd.y = 0f; fwd.Normalize(); var up = Vector3.up; var right = Vector3.Cross(up, fwd).normalized;
            const int NX = 40, NY = 10; float aMax = 78f;
            var verts = new List<Vector3>(); var norms = new List<Vector3>(); var tris = new List<int>();
            for (int i = 0; i <= NX; i++)
            {
                float a = Mathf.Lerp(-aMax, aMax, i / (float)NX); float aa = Mathf.Abs(a);
                // 아래 끝: 가운데는 눈 위(-4°), 톱니(7갈래)로 조금씩 더 내려오고, 옆(|a|>48°)은 볼 옆까지(-34°)
                float tooth = 0.5f + 0.5f * Mathf.Cos(a * 7f * Mathf.Deg2Rad * 3.2f);
                float bot = 9f - 4f * tooth;   // 눈 윗선(≈ -7.7°)보다 넉넉히 위 — 카메라가 위에서 보면 앞머리가 얼굴 앞에 떠서 더 아래로 보인다
                if (aa > 46f) bot = Mathf.Lerp(bot, -36f, Mathf.InverseLerp(46f, aMax, aa));
                float top = 46f;
                for (int j = 0; j <= NY; j++)
                {
                    float bdeg = Mathf.Lerp(bot, top, j / (float)NY) ;
                    float ar = a * Mathf.Deg2Rad, br = bdeg * Mathf.Deg2Rad;
                    var n = (right * (Mathf.Sin(ar) * Mathf.Cos(br)) + up * Mathf.Sin(br) + fwd * (Mathf.Cos(ar) * Mathf.Cos(br))).normalized;
                    float puff = 1f + 0.02f * Mathf.Sin(j / (float)NY * Mathf.PI);   // 가운데가 살짝 부푼 머리숱
                    verts.Add(head.InverseTransformPoint(center + n * R * puff)); norms.Add(head.InverseTransformDirection(n));
                }
            }
            int W = NY + 1;
            for (int i = 0; i < NX; i++) for (int j = 0; j < NY; j++)
            {
                int v00 = i * W + j, v01 = v00 + 1, v10 = v00 + W, v11 = v10 + 1;
                tris.Add(v00); tris.Add(v01); tris.Add(v11); tris.Add(v00); tris.Add(v11); tris.Add(v10);
                tris.Add(v00); tris.Add(v11); tris.Add(v01); tris.Add(v00); tris.Add(v10); tris.Add(v11);   // 양면
            }
            var mesh = new Mesh { name = "HeroBangs" }; mesh.SetVertices(verts); mesh.SetNormals(norms); mesh.SetTriangles(tris, 0); mesh.RecalculateBounds();
            var go = new GameObject("HeroBangs", typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(head, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var mat = CoastMaterials.CreateToon(HairBlack); if (mat != null) { mat.SetFloat(IdCrisp, 1f); mat.SetFloat(IdRim, 0.35f); mat.SetFloat(IdThr, 0.5f); mat.SetFloat(IdSoft, 0.03f); }
            var mr = go.GetComponent<MeshRenderer>(); mr.sharedMaterial = mat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Debug.Log($"[HeroCrisp] bangs R {R:0.###} center {center}");
        }

        /// FaceDecal.HeadFromWeights 와 같은 방식(머리 뼈 가중치 ≥0.5 정점의 바인드 포즈 경계)
        bool HeadSphere(Transform head, out float rad, out Vector3 centerLocal)
        {
            rad = 0f; centerLocal = Vector3.zero;
            foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (smr.name == "Outline" || smr.sharedMesh == null) continue;
                int hi = System.Array.IndexOf(smr.bones, head); if (hi < 0) continue;
                var mesh = smr.sharedMesh; var bw = mesh.boneWeights; var vs = mesh.vertices; if (bw == null || bw.Length != vs.Length || mesh.bindposes == null || hi >= mesh.bindposes.Length) continue;
                var mn = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue); var mx = -mn; int n = 0;
                for (int i = 0; i < vs.Length; i++)
                {
                    var w = bw[i]; float hw = (w.boneIndex0 == hi ? w.weight0 : 0f) + (w.boneIndex1 == hi ? w.weight1 : 0f) + (w.boneIndex2 == hi ? w.weight2 : 0f) + (w.boneIndex3 == hi ? w.weight3 : 0f);
                    if (hw < 0.5f) continue; mn = Vector3.Min(mn, vs[i]); mx = Vector3.Max(mx, vs[i]); n++;
                }
                if (n < 20) continue;
                var ext = mx - mn; var cMesh = (mn + mx) * 0.5f; float rMesh = (ext.x + ext.y + ext.z) / 6f * 0.92f;
                float sc = smr.transform.lossyScale.y; centerLocal = mesh.bindposes[hi].MultiplyPoint3x4(cMesh);
                rad = Mathf.Clamp(rMesh * sc, 0.08f, 0.40f); return true;
            }
            return false;
        }

        public void Apply()
        {
            if (_t0 < 0f) _t0 = Time.time;
            var body = MainBody(); LogParts(body);
            if (Shorts && !_shortsDone && body != null) MakeShorts(body);
            if (Bangs && !_bangsDone) MakeBangs();
            EnsurePin();
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                if (r == null) continue;
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i]; if (m == null || m.shader == null) continue;
                    string sn = m.shader.name;
                    if (sn == "CoastRun/ToonLit")
                    {
                        if (m.HasProperty(IdCrisp) && Mathf.Approximately(m.GetFloat(IdCrisp), Crisp)) continue;
                        if (!m.name.EndsWith("(Instance)")) { m = r.materials[i]; }   // 공용 재질이면 이 몸만의 복제본으로
                        m.SetFloat(IdCrisp, Crisp); m.SetFloat(IdRim, Rim);
                        Tint(m);
                        m.SetFloat(IdThr, ShadowThreshold); m.SetFloat(IdSoft, ShadowSoftness);
                    }
                    else if (sn == "CoastRun/InkOutline" && m.HasProperty(IdWidth) && m.GetFloat(IdWidth) < OutlineWidth - 0.0005f)
                    {
                        // 윤곽 재질은 CelOutlineHint 가 이 몸 전용으로 만든 것
                        m.SetFloat(IdWidth, OutlineWidth);
                    }
                }
            }
        }
    }
}
