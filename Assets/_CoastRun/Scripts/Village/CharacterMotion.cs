using System.Collections.Generic;
using UnityEngine;

namespace CoastRun.Village
{
    /// 141차: 동물의 숲 느낌의 절차 모션 레이어 — 리그 피벗(루트의 자식)에 붙여 루트 이동량으로부터
    /// 걸음 바운스·좌우 흔들림·앞으로 기울기·회전 기울기·출발/정지 스쿼시·숨쉬기·고개 돌리기·발먼지를 만든다.
    /// 루트(이동 주체)는 위치·회전만 바꾸면 되고, 이 컴포넌트가 애니메이터 속도까지 맡는다.
    public class CharacterMotion : MonoBehaviour
    {
        public Animator Anim;
        public float WalkSpeed = 1.6f, RunSpeed = 3.6f, Stride = 0.80f;
        public bool FootDust = true;
        public Transform LookTarget;              // 고개를 돌려 볼 대상(없으면 가끔 두리번)
        public float LookMaxYaw = 65f, LookMaxPitch = 22f;
        public float BobScale = 1f;

        Transform _root, _head;
        Vector3 _prevPos; float _phase, _speedS, _yawPrev, _yawRate, _squash, _squashV, _idleT, _glanceT, _glanceYaw, _nodT = -1f, _hopT = -1f, _lastStep;
        bool _wasMoving; float _lookW, _lookYaw, _lookPitch; bool _init;

        public bool Moving => _speedS > 0.15f;
        public float Speed => _speedS;
        public void Hop() { if (_hopT < 0f) _hopT = 0f; }
        public void Nod() { _nodT = 0f; }

        void Start()
        {
            _root = transform.parent; _prevPos = _root.position; _yawPrev = _root.eulerAngles.y;
            if (Anim == null) Anim = GetComponentInChildren<Animator>();
            if (Anim != null && Anim.isHuman) _head = Anim.GetBoneTransform(HumanBodyBones.Head);
            _glanceT = Random.Range(2f, 5f); _init = true;
        }

        void Update()
        {
            if (!_init || _root == null) return;
            float dt = Time.deltaTime; if (dt <= 0f) return;
            var p = _root.position; var d = p - _prevPos; d.y = 0f; float v = d.magnitude / dt; _prevPos = p;
            if (v > 30f) v = 0f;                                             // 텔레포트
            _speedS = Mathf.Lerp(_speedS, v, 1f - Mathf.Exp(-dt * 12f));
            float yaw = _root.eulerAngles.y; float dy = Mathf.DeltaAngle(_yawPrev, yaw) / dt; _yawPrev = yaw;
            _yawRate = Mathf.Lerp(_yawRate, Mathf.Clamp(dy, -400f, 400f), 1f - Mathf.Exp(-dt * 8f));
            bool moving = _speedS > 0.15f;
            float run01 = Mathf.InverseLerp(WalkSpeed, RunSpeed, _speedS);
            if (moving) _phase += (_speedS / Stride) * dt * Mathf.PI;          // 반 바퀴 = 한 걸음

            // 애니메이터: 달리기 클립을 속도에 맞춰(천천히 걸으면 느긋한 조깅), 멈추면 첫 프레임에 정지
            if (Anim != null)
            {
                if (moving) { Anim.speed = Mathf.Lerp(0.5f, 1.3f, Mathf.InverseLerp(0.3f, RunSpeed, _speedS)); Anim.SetFloat("Speed", Mathf.Clamp01(_speedS / RunSpeed)); }
                else if (_wasMoving) { Anim.Play("Run", 0, 0.12f); Anim.speed = 0f; Anim.SetFloat("Speed", 0f); }
            }
            // 출발: 위로 쭉, 정지: 납작 — 스프링으로 복귀
            if (moving && !_wasMoving) _squash = 0.09f;
            if (!moving && _wasMoving) _squash = -0.10f;
            _squash = Mathf.SmoothDamp(_squash, 0f, ref _squashV, 0.11f, 100f, dt);

            float bob = moving ? Mathf.Abs(Mathf.Sin(_phase)) * Mathf.Lerp(0.022f, 0.060f, run01) * BobScale : 0f;
            float sway = moving ? Mathf.Sin(_phase * 0.5f) * Mathf.Lerp(2.5f, 4.5f, run01) : 0f;
            float lean = Mathf.Lerp(0f, 8f, run01) + Mathf.Clamp01(_speedS / WalkSpeed) * 2f;
            float turnLean = Mathf.Clamp(-_yawRate * 0.035f, -9f, 9f);
            _idleT = moving ? 0f : _idleT + dt;
            float breathe = moving ? 0f : Mathf.Sin(_idleT * 2.2f) * 0.012f;
            float idleSway = moving ? 0f : Mathf.Sin(_idleT * 1.1f) * 0.8f;
            float hopY = 0f;
            if (_hopT >= 0f) { _hopT += dt; float t = _hopT / 0.42f; if (t >= 1f) { _hopT = -1f; _squash = -0.08f; } else hopY = Mathf.Sin(t * Mathf.PI) * 0.30f; }

            transform.localPosition = new Vector3(0f, bob + hopY, 0f);
            transform.localRotation = Quaternion.Euler(lean, 0f, sway + turnLean + idleSway);
            float sy = 1f + _squash + breathe, sxz = 1f - _squash * 0.55f - breathe * 0.4f;
            transform.localScale = new Vector3(sxz, sy, sxz);

            // 발먼지: 걸음(위상 π 마다) 마다 작은 흙먼지
            if (FootDust && moving && _speedS > 0.9f)
            {
                float step = Mathf.Floor(_phase / Mathf.PI);
                if (step > _lastStep) { _lastStep = step; DustPuff.Spawn(p + Vector3.up * 0.05f - _root.forward * 0.2f, Mathf.Lerp(0.26f, 0.42f, run01)); }
            }
            // 두리번(대상 없을 때, 서 있을 때)
            _glanceT -= dt;
            if (_glanceT <= 0f) { _glanceT = Random.Range(2.5f, 6f); _glanceYaw = !moving && Random.value < 0.7f ? Random.Range(-35f, 35f) : 0f; }
            _wasMoving = moving;
        }

        void LateUpdate()
        {
            if (_head == null || _root == null) return;
            float dt = Time.deltaTime;
            float wantYaw = _glanceYaw, wantPitch = 0f, wantW = _glanceYaw != 0f ? 0.8f : 0f;
            if (LookTarget != null)
            {
                var to = LookTarget.position + Vector3.up * 1.0f - _head.position; var flat = to; flat.y = 0f;
                if (flat.sqrMagnitude > 0.01f)
                {
                    float y = Vector3.SignedAngle(_root.forward, flat.normalized, Vector3.up);
                    float pch = -Mathf.Atan2(to.y, flat.magnitude) * Mathf.Rad2Deg;
                    if (Mathf.Abs(y) < 110f) { wantYaw = Mathf.Clamp(y, -LookMaxYaw, LookMaxYaw); wantPitch = Mathf.Clamp(pch, -LookMaxPitch, LookMaxPitch); wantW = 1f; }
                }
            }
            if (_nodT >= 0f) { _nodT += dt; if (_nodT > 1.0f) _nodT = -1f; else wantPitch += Mathf.Sin(_nodT * Mathf.PI * 2f) * 14f; wantW = 1f; }
            float k = 1f - Mathf.Exp(-dt * 6f);
            _lookW = Mathf.Lerp(_lookW, wantW, k); _lookYaw = Mathf.Lerp(_lookYaw, wantYaw, k); _lookPitch = Mathf.Lerp(_lookPitch, wantPitch, k);
            if (_lookW < 0.01f) return;
            var rot = Quaternion.AngleAxis(_lookYaw * _lookW, Vector3.up) * Quaternion.AngleAxis(_lookPitch * _lookW, _root.right);
            _head.rotation = rot * _head.rotation;
        }
    }

    /// 발 먼지 — 작은 원판이 커지며 사라진다(풀링).
    public class DustPuff : MonoBehaviour
    {
        static readonly List<DustPuff> Pool = new List<DustPuff>();
        static Material _mat;
        float _t, _size; MeshRenderer _mr; Material _m; string _colorProp;
        public static void Spawn(Vector3 pos, float size)
        {
            DustPuff d = null;
            foreach (var q in Pool) if (q != null && !q.gameObject.activeSelf) { d = q; break; }
            if (d == null)
            {
                if (Pool.Count >= 24) return;
                var go = GameObject.CreatePrimitive(PrimitiveType.Quad); Object.Destroy(go.GetComponent<Collider>()); go.name = "DustPuff";
                d = go.AddComponent<DustPuff>(); d._mr = go.GetComponent<MeshRenderer>();
                if (_mat == null) _mat = MiniStage3D.SoftDisc(new Color(0.97f, 0.93f, 0.82f, 0.75f));
                d._mr.sharedMaterial = new Material(_mat); d._mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                d._m = d._mr.sharedMaterial; d._colorProp = d._m.HasProperty("_BaseColor") ? "_BaseColor" : (d._m.HasProperty("_Color") ? "_Color" : null); Pool.Add(d);
            }
            d.transform.position = pos + new Vector3(Random.Range(-0.08f, 0.08f), 0f, Random.Range(-0.08f, 0.08f));
            d.transform.rotation = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);
            d._t = 0f; d._size = size; d.gameObject.SetActive(true);
        }
        void Update()
        {
            _t += Time.deltaTime; float u = _t / 0.38f;
            if (u >= 1f) { gameObject.SetActive(false); return; }
            float s = _size * (0.5f + u * 0.9f); transform.localScale = new Vector3(s, s, 1f);
            transform.position += Vector3.up * Time.deltaTime * 0.25f;
            if (_colorProp != null) _m.SetColor(_colorProp, new Color(0.95f, 0.90f, 0.78f, 0.75f * (1f - u)));
        }
    }

    /// 바람 흔들림 — 야자·갈대·꽃·덤불이 천천히 흔들리고, 주인공이 스치면 파르르 떤다.
    public class WindSway : MonoBehaviour
    {
        public static readonly List<WindSway> All = new List<WindSway>();
        public float Amp = 2f, Speed = 1f; public bool Nudgeable = true;
        Quaternion _base; float _phase, _nudge, _nudgeT;
        void Awake() { _base = transform.localRotation; _phase = Random.Range(0f, 6.28f); All.Add(this); }
        void OnDestroy() { All.Remove(this); }
        public void Nudge(float strength = 1f) { if (_nudgeT <= 0f) { _nudge = strength; _nudgeT = 0.9f; } }
        void Update()
        {
            float t = Time.time * Speed + _phase;
            float x = Mathf.Sin(t) * Amp + Mathf.Sin(t * 2.3f + 0.7f) * Amp * 0.35f;
            float z = Mathf.Sin(t * 0.7f + 1.3f) * Amp * 0.7f;
            if (_nudgeT > 0f) { _nudgeT -= Time.deltaTime; float e = _nudgeT / 0.9f; x += Mathf.Sin((0.9f - _nudgeT) * 28f) * 9f * e * _nudge; }
            transform.localRotation = _base * Quaternion.Euler(x, 0f, z);
        }
        /// 뿌리 근처를 지나가면 흔들기
        public static void NudgeNear(Vector3 p, float radius)
        {
            foreach (var s in All) { if (s == null || !s.Nudgeable) continue; var q = s.transform.position; if ((q.x - p.x) * (q.x - p.x) + (q.z - p.z) * (q.z - p.z) < radius * radius) s.Nudge(); }
        }
    }
}
