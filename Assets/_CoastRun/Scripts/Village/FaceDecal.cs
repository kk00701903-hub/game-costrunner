using UnityEngine;

namespace CoastRun.Village
{
    /// 161차(사용자: 「데칼 얼굴이 옆으로 밀려 잔상처럼 떠다님 · 원본 눈이 비쳐 얼굴이 둘」): 155·158차 VillageHub.AttachFace 를 떼어 공용화.
    ///  ① 원본 모델의 눈·입·볼(EyeWhite/Iris/Black/White/Mouth/Blush 서브메시)을 메시 복제본에서 비운다 — 데칼 밖으로 삐죽 나오던 3D 눈알이 사라진다.
    ///  ② 데칼은 머리 뼈의 자식이 아니라 월드에 두고, 애니메이터·SkaterRig.ApplyJuiceLate·CharacterMotion 이 뼈를 다 쓴 뒤(실행 순서 +500 LateUpdate)
    ///     머리 뼈 회전·위치에서 다시 계산해 붙인다. 부모 스케일(스쿼시/스트레치)로 찌그러지거나 프레임 순서에 밀려 떠 보이지 않는다.
    ///  ③ 구면 조각(±50°×±35°) 메시는 머리 뼈 로컬 좌표 기준으로 한 번만 굽는다(스케일은 스폰 시 rig 스케일에서).
    [DefaultExecutionOrder(500)]
    public class FaceDecal : MonoBehaviour
    {
        Transform _head; Vector3 _localPos; Quaternion _localRot; float _worldScale = 1f;
        public Transform Head => _head;

        public static FaceDecal Attach(Animator anim, GameObject rig, Texture2D tex, Vector3 forward, float radiusMul = 1f)
        {
            if (anim == null || anim.avatar == null || !anim.avatar.isHuman || rig == null || tex == null) return null;
            var head = anim.GetBoneTransform(HumanBodyBones.Head); if (head == null) return null;
            HideBuiltInFace(rig);
            // 머리 크기·중심: 머리 뼈에 스키닝된 정점들의 바인드 포즈 경계(모자·바구니가 있는 NPC 는 렌더러 꼭대기 기준이 틀렸다 — 162차)
            float rad; Vector3 centerLocal = Vector3.zero; bool fromBones = HeadFromWeights(rig, head, out rad, out centerLocal);
            if (!fromBones)
            {
                float top = 0f; foreach (var r in rig.GetComponentsInChildren<Renderer>()) top = Mathf.Max(top, r.bounds.max.y);
                rad = Mathf.Clamp((top - head.position.y) * 0.62f, 0.10f, 0.35f);
            }
            rad *= radiusMul;
            var q = new GameObject("FaceDecal", typeof(MeshFilter), typeof(MeshRenderer));
            var mat = CoastMaterials.CreateTexturedTransparent(tex, Color.white); q.GetComponent<MeshRenderer>().sharedMaterial = mat;
            q.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var fwd = forward; fwd.y = 0f; fwd = fwd.sqrMagnitude < 0.001f ? Vector3.forward : fwd.normalized; var up = Vector3.up; var right = Vector3.Cross(up, fwd).normalized;
            var center = fromBones ? head.TransformPoint(centerLocal) : head.position + up * (rad * 0.66f); float R = rad * 0.99f;   // 눈알을 없앴으니 머리 표면에 바짝(0.97→0.99)
            const int NX = 18, NY = 12; float hA = 50f * Mathf.Deg2Rad, vA = 35f * Mathf.Deg2Rad;
            var verts = new Vector3[(NX + 1) * (NY + 1)]; var uvs = new Vector2[verts.Length]; var norms = new Vector3[verts.Length]; var tris = new int[NX * NY * 6];
            // 메시는 「머리 뼈 로컬」이 아니라 「데칼 오브젝트 로컬」 — 데칼 오브젝트는 머리 뼈와 같은 자리·회전(스케일 1)에 두고 LateUpdate 로 따라간다
            var pose = Matrix4x4.TRS(head.position, head.rotation, Vector3.one).inverse;
            for (int j = 0; j <= NY; j++)
                for (int i = 0; i <= NX; i++)
                {
                    // 164차(레퍼런스 대조: 「앞머리가 눈을 덮는다」): 정점 가중치로 잡은 머리 중심이 머리카락 캡 때문에 위로 쏠려 얼굴이 높이 붙었다 → 조각 전체를 17° 아래로
                    float a = (i / (float)NX - 0.5f) * 2f * hA, b = (j / (float)NY - 0.5f) * 2f * vA - (fromBones ? 17f * Mathf.Deg2Rad : 0f);
                    var n = (right * (Mathf.Sin(a) * Mathf.Cos(b)) + up * Mathf.Sin(b) + fwd * (Mathf.Cos(a) * Mathf.Cos(b))).normalized;
                    int k = j * (NX + 1) + i;
                    verts[k] = pose.MultiplyPoint3x4(center + n * R); norms[k] = pose.MultiplyVector(n);
                    uvs[k] = new Vector2(0.5f - a / (2f * hA), 0.5f + b / (2f * vA));
                }
            int t = 0;
            for (int j = 0; j < NY; j++)
                for (int i = 0; i < NX; i++)
                {
                    int v00 = j * (NX + 1) + i, v10 = v00 + 1, v01 = v00 + NX + 1, v11 = v01 + 1;
                    tris[t++] = v00; tris[t++] = v10; tris[t++] = v11; tris[t++] = v00; tris[t++] = v11; tris[t++] = v01;
                }
            var mesh = new Mesh { name = "FaceCap" }; mesh.vertices = verts; mesh.uv = uvs; mesh.normals = norms; mesh.triangles = tris; mesh.RecalculateBounds();
            q.GetComponent<MeshFilter>().sharedMesh = mesh;
            var fd = q.AddComponent<FaceDecal>(); fd._head = head; fd._localPos = Vector3.zero; fd._localRot = Quaternion.identity;
            q.transform.SetParent(rig.transform.parent != null ? rig.transform.parent : rig.transform, true);   // 캐릭터 루트 밑(따라 파괴)·월드 포즈 유지
            fd.Pin();
            return fd;
        }

        /// 머리 뼈 가중치 ≥0.5 인 정점들의 바인드 포즈 경계 → 반지름(월드)·중심(머리 뼈 로컬). 스킨 정보가 없으면 false.
        static bool HeadFromWeights(GameObject rig, Transform head, out float rad, out Vector3 centerLocal)
        {
            rad = 0f; centerLocal = Vector3.zero;
            foreach (var smr in rig.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (smr.name == "Outline" || smr.sharedMesh == null) continue;
                int hi = System.Array.IndexOf(smr.bones, head); if (hi < 0) continue;
                var mesh = smr.sharedMesh; var bw = mesh.boneWeights; var vs = mesh.vertices; if (bw == null || bw.Length != vs.Length || mesh.bindposes == null || hi >= mesh.bindposes.Length) continue;
                var mn = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue); var mx = -mn; int n = 0;
                for (int i = 0; i < vs.Length; i++)
                {
                    var w = bw[i]; float hw = (w.boneIndex0 == hi ? w.weight0 : 0f) + (w.boneIndex1 == hi ? w.weight1 : 0f) + (w.boneIndex2 == hi ? w.weight2 : 0f) + (w.boneIndex3 == hi ? w.weight3 : 0f);
                    if (hw < 0.5f) continue;
                    mn = Vector3.Min(mn, vs[i]); mx = Vector3.Max(mx, vs[i]); n++;
                }
                if (n < 20) continue;
                var ext = mx - mn; var cMesh = (mn + mx) * 0.5f;
                float rMesh = (ext.x + ext.y + ext.z) / 6f * 0.92f;   // 머리카락 두께만큼 살짝 안쪽
                // 메시 공간 → 월드 반지름: 바인드 포즈에서 메시 공간 = 스킨 렌더러 루트 공간 → lossyScale
                float sc = smr.transform.lossyScale.y; if (mesh.bindposes.Length > hi) { var bp = mesh.bindposes[hi]; centerLocal = bp.MultiplyPoint3x4(cMesh); }
                rad = Mathf.Clamp(rMesh * sc, 0.08f, 0.40f);
                return true;
            }
            return false;
        }

        /// 원본 3D 눈·입·볼 서브메시를 비운다(메시 인스턴스 복제 — 러닝 모드의 공유 메시는 그대로).
        public static void HideBuiltInFace(GameObject rig)
        {
            var swapped = new System.Collections.Generic.Dictionary<Mesh, Mesh>();
            foreach (var smr in rig.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var mats = smr.sharedMaterials; var src = smr.sharedMesh; if (src == null || smr.name == "Outline") continue;
                bool any = false;
                for (int i = 0; i < mats.Length && i < src.subMeshCount; i++) if (IsFacePart(mats[i])) { any = true; break; }
                if (!any) continue;
                var m = Object.Instantiate(src); m.name = src.name + "_NoFace";
                for (int i = 0; i < mats.Length && i < m.subMeshCount; i++) if (IsFacePart(mats[i])) m.SetTriangles(new int[0], i, false);
                smr.sharedMesh = m; swapped[src] = m;
            }
            // CelOutlineHint 가 Start 에서 만든 잉크 셸(자식 "Outline")도 같은 메시를 쓰므로 바꿔 준다 — 안 바꾸면 눈알 윤곽만 까맣게 남는다
            foreach (var smr in rig.GetComponentsInChildren<SkinnedMeshRenderer>())
                if (smr.name == "Outline" && smr.sharedMesh != null && swapped.TryGetValue(smr.sharedMesh, out var nm)) smr.sharedMesh = nm;
        }
        /// 167차(사용자: 「볼에 분홍 넣지 말 것」): 데칼 없이 3D 얼굴을 그대로 쓰는 리그(꼬마)의 볼터치 서브메시만 비운다
        public static void HideBlush(GameObject rig)
        {
            if (rig == null) return;
            foreach (var smr in rig.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var mats = smr.sharedMaterials; var src = smr.sharedMesh; if (src == null || smr.name == "Outline") continue;
                bool any = false; for (int i = 0; i < mats.Length && i < src.subMeshCount; i++) if (mats[i] != null && mats[i].name.Contains("Blush")) any = true;
                if (!any) continue;
                var m = Object.Instantiate(src); m.name = src.name + "_NoBlush";
                for (int i = 0; i < mats.Length && i < m.subMeshCount; i++) if (mats[i] != null && mats[i].name.Contains("Blush")) m.SetTriangles(new int[0], i, false);
                smr.sharedMesh = m;
                foreach (var o in rig.GetComponentsInChildren<SkinnedMeshRenderer>()) if (o.name == "Outline" && o.sharedMesh == src) o.sharedMesh = m;
            }
        }

        static bool IsFacePart(Material m)
        {
            if (m == null) return false; string n = m.name;
            var t = m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") : m.HasProperty("_MainTex") ? m.mainTexture : null; if (t != null) n += " " + t.name;   // InkOutline 엔 _MainTex 가 없어 에러가 났다
            return n.Contains("EyeWhite") || n.Contains("Iris") || n.Contains("_Black") || n.Contains("_White") || n.Contains("Mouth") || n.Contains("Blush");
        }

        void Pin()
        {
            if (_head == null) return;
            transform.position = _head.TransformPoint(_localPos); transform.rotation = _head.rotation * _localRot;
            var ps = transform.parent != null ? transform.parent.lossyScale : Vector3.one;   // 부모 스케일과 무관하게 월드 1 배
            transform.localScale = new Vector3(_worldScale / Mathf.Max(0.001f, ps.x), _worldScale / Mathf.Max(0.001f, ps.y), _worldScale / Mathf.Max(0.001f, ps.z));
        }
        void LateUpdate() { if (_head == null) { Destroy(gameObject); return; } Pin(); }

        /// 개발용 미세 조정: dx 앞뒤(머리 forward), dy 위아래(월드), s 배율
        public void Nudge(Vector3 forward, float dx, float dy, float s)
        {
            _localPos += _head.InverseTransformVector(forward * dx + Vector3.up * dy); _worldScale *= s; Pin();
        }
    }
}
