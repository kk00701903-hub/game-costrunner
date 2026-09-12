using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CoastRun
{
    /// 51차(사용자): 하늘에서 오는 위협 3종 — 공용 스폰 함수.
    ///   DropRock  : 빈 자리(코인·말랑이·장애물 없는 곳)에 그림자가 생기고 큰 현무암 바위(Obs_BoulderBig)가 떨어져 장애물로 남는다.
    ///   Missile   : 앞에서 날아오는 로켓(Obs_Missile) — 레인 하나를 따라 주인공 쪽으로.
    ///   Tornado   : 태풍(Obs_Tornado) — 앞에서 다가오며 레인을 예측 불가하게 옮겨 다닌다.
    /// 전부 ObstacleHazard(닿으면 피해)를 달아 기존 충돌·팡 연출을 그대로 쓴다. 그림이 없으면 단색 도형.
    public static class SkyHazards
    {
        public const float LaneWidth = 2.2f;

        private static Transform _root;
        public static Transform Root
        {
            get
            {
                if (_root == null) { var go = new GameObject("SkyHazards"); _root = go.transform; }
                return _root;
            }
        }

        /// 살아 있는 하늘 위협 수(보스 감독이 밀도 조절에 씀)
        public static int AliveCount { get; private set; }

        private static PlayerController _player;
        internal static PlayerController Player
        {
            get { if (_player == null) _player = Object.FindAnyObjectByType<PlayerController>(); return _player; }
        }

        // ── 바위 ────────────────────────────────────────────────────────
        /// z(경로 거리)·lane(-1/0/1)에 그림자 → 바위 낙하 → 착지 후 장애물. fallSeconds 동안 떨어진다.
        public static GameObject DropRock(float z, int lane, float fallSeconds = 1.15f, float radius = 0.62f)
        {
            RoadOccupancy.OnObstacle(z, lane, 3f);
            var root = new GameObject("Obstacle_SkyRock");
            root.transform.SetParent(Root, false);
            root.transform.position = RoadPlacement.OnRoad(z, lane * LaneWidth);
            root.transform.rotation = DownhillPath.Rotation;
            var fall = root.AddComponent<FallingRock>();
            fall.Init(lane, fallSeconds, radius);
            return root;
        }

        // ── 미사일 ──────────────────────────────────────────────────────
        /// fromZ 에서 출발해 -z 방향(주인공 쪽)으로 speed m/s. 지나가면 스스로 사라진다.
        public static GameObject FireMissile(float fromZ, int lane, float speed, float height = 0.85f)
        {
            var root = new GameObject("Obstacle_Missile");
            root.transform.SetParent(Root, false);
            root.transform.position = RoadPlacement.OnRoad(fromZ, lane * LaneWidth, height);
            root.transform.rotation = DownhillPath.Rotation;
            var m = root.AddComponent<Missile>();
            m.Init(lane, speed, height);
            return root;
        }

        // ── 태풍 ────────────────────────────────────────────────────────
        public static GameObject SpawnTornado(float fromZ, float speed, float sweepAmp, float sweepHz, float seconds)
        {
            var root = new GameObject("Obstacle_Tornado");
            root.transform.SetParent(Root, false);
            root.transform.position = RoadPlacement.OnRoad(fromZ, 0f);
            root.transform.rotation = DownhillPath.Rotation;
            var t = root.AddComponent<Tornado>();
            t.Init(speed, sweepAmp, sweepHz, seconds);
            return root;
        }

        /// 빈 레인 찾기: 코인·말랑이·아이템·장애물이 dz 안에 없는 레인을 우선(없으면 preferred).
        public static int FindEmptyLane(float z, int preferred, System.Random rng, float dz = 4f)
        {
            int[] order = preferred == 0 ? new[] { 0, -1, 1 } : preferred < 0 ? new[] { -1, 0, 1 } : new[] { 1, 0, -1 };
            if (rng != null && rng.Next(3) == 0) { var t = order[1]; order[1] = order[2]; order[2] = t; }
            foreach (var l in order)
                if (!RoadOccupancy.Near(RoadOccupancy.Kind.Obstacle, z, l, dz) && !RoadOccupancy.Near(RoadOccupancy.Kind.Pickup, z, l, dz) && !RoadOccupancy.Near(RoadOccupancy.Kind.Item, z, l, dz))
                    return l;
            return preferred;
        }

        // ── 공용: 그림 빌보드 + 피해 콜라이더 ───────────────────────────
        internal static Transform Visual(Transform root, string key, float height, Color fallback, bool outline = true)
        {
            if (PaintedProp.Available(key))
                return PaintedProp.Attach(root, key, height, replace: false, outline: outline, outlineColor: new Color(0.55f, 0.05f, 0.08f, 1f), outlineMul: 0.8f);
            var s = GameObject.CreatePrimitive(PrimitiveType.Sphere); s.name = "Fallback"; Object.Destroy(s.GetComponent<Collider>());
            s.transform.SetParent(root, false); s.transform.localPosition = new Vector3(0f, height * 0.5f, 0f); s.transform.localScale = Vector3.one * height;
            s.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateLit(fallback);
            return s.transform;
        }

        internal static ObstacleHazard Hazard(Transform root, Vector3 center, float radius, float damageMul, int lane, int nearReward = 12)
        {
            var hard = new GameObject("HardHit"); hard.transform.SetParent(root, false); hard.transform.localPosition = center;
            var col = hard.AddComponent<SphereCollider>(); col.isTrigger = true; col.radius = radius;
            var hz = hard.AddComponent<ObstacleHazard>(); hz.DamageMul = damageMul;
            var near = new GameObject("NearMiss"); near.transform.SetParent(root, false); near.transform.localPosition = center;
            var ncol = near.AddComponent<SphereCollider>(); ncol.isTrigger = true; ncol.radius = radius + 0.6f;
            var zone = near.AddComponent<NearMissZone>(); zone.Configure(nearReward, lane);
            hz.BindNearMiss(zone);
            return hz;
        }

        internal static void Cleanup(Transform root, float behindZ)
        {
            if (root == null) return;
            if (DownhillPath.DistanceAlong(root.position) < behindZ) Object.Destroy(root.gameObject);
        }

        // ══════════════════════════════════════════════════════════════
        public class FallingRock : MonoBehaviour
        {
            private Transform _rock, _shadow; private Renderer _shadowR; private MaterialPropertyBlock _mpb;
            private float _t, _fall, _radius; private int _lane; private bool _landed;
            private static readonly int ColorId = Shader.PropertyToID("_BaseColor");

            public void Init(int lane, float fallSeconds, float radius)
            {
                _lane = lane; _fall = fallSeconds; _radius = radius;
                AliveCount++;
                // 그림자: 바닥의 어두운 소프트 원판 — 떨어질수록 진해지고 커진다
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad); q.name = "DropShadow"; Destroy(q.GetComponent<Collider>());
                q.transform.SetParent(transform, false); q.transform.localPosition = new Vector3(0f, 0.03f, 0f); q.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                _shadowR = q.GetComponent<Renderer>();
                var m = CoastMaterials.CreateTexturedTransparentCurved(BlobShadow.SoftDisc(), new Color(0.08f, 0.05f, 0.05f, 0.35f), additive: false);
                _shadowR.sharedMaterial = m; _shadowR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _shadow = q.transform; _mpb = new MaterialPropertyBlock();
                // 경고 링(빨간)
                HazardRing.Attach(transform, _radius * 1.5f, new Color(1f, 0.25f, 0.2f, 0.9f), 0.02f);
                // 바위 본체: 위 12 m 에서 시작
                var holder = new GameObject("Rock"); holder.transform.SetParent(transform, false);
                _rock = holder.transform;
                Visual(_rock, "BoulderBig", _radius * 2f, new Color(0.18f, 0.18f, 0.2f));
                _rock.localPosition = new Vector3(0f, 8f, 0f);
                // 피해 콜라이더는 바위와 함께 내려온다(밑에 있으면 맞는다)
                Hazard(_rock, new Vector3(0f, _radius, 0f), _radius * 0.9f, 1f, lane, 14);
            }

            private void Update()
            {
                if (GetComponent<ObstaclePopAnim>() != null) return;   // 팡 터지는 중엔 손대지 않는다
                if (_landed)
                {
                    var pc = Player;
                    if (pc != null) Cleanup(transform, pc.PathDistance - 30f);
                    return;
                }
                _t += Time.deltaTime;
                float u = Mathf.Clamp01(_t / _fall);
                float h = 8f * (1f - u * u);   // 가속 낙하(8 m — 화면 위에서 바로 보이게)
                _rock.localPosition = new Vector3(0f, h, 0f);
                _rock.localRotation = Quaternion.Euler(0f, 0f, u * 90f);
                float s = Mathf.Lerp(0.9f, 2.1f, u) * _radius;
                _shadow.localScale = new Vector3(s, s, 1f);
                _mpb.SetColor(ColorId, new Color(0.08f, 0.05f, 0.05f, Mathf.Lerp(0.25f, 0.7f, u)));
                _shadowR.SetPropertyBlock(_mpb);
                if (u >= 1f)
                {
                    _landed = true;
                    _rock.localPosition = Vector3.zero; _rock.localRotation = Quaternion.identity;
                    JuiceDirector.Instance?.PlayRockLand(transform.position);
                    var ring = GetComponent<HazardRing>(); if (ring != null) Destroy(ring);
                    var ringQuad = transform.Find("HazardRing"); if (ringQuad != null) Destroy(ringQuad.gameObject);
                    _shadow.localScale = new Vector3(_radius * 2.2f, _radius * 2.2f, 1f);
                    _mpb.SetColor(ColorId, new Color(0.08f, 0.05f, 0.05f, 0.45f)); _shadowR.SetPropertyBlock(_mpb);
                    ObstacleOutline.Attach(_rock);
                }
            }

            private void OnDestroy() { AliveCount = Mathf.Max(0, AliveCount - 1); }
        }

        // ══════════════════════════════════════════════════════════════
        public class Missile : MonoBehaviour
        {
            private float _speed, _height, _z; private int _lane; private Transform _vis;
            public void Init(int lane, float speed, float height)
            {
                _lane = lane; _speed = speed; _height = height; _z = DownhillPath.DistanceAlong(transform.position);
                AliveCount++;
                var holder = new GameObject("Body"); holder.transform.SetParent(transform, false);
                _vis = Visual(holder.transform, "Missile", 1.1f, new Color(0.9f, 0.2f, 0.15f));
                // 로켓 그림은 세로(코가 위) — 주인공 쪽으로 코가 향하게 앞으로 눕힌다(빌보드는 요만 돌므로 X 기울기 유지)
                holder.transform.localRotation = Quaternion.Euler(62f, 0f, 0f);
                holder.transform.localPosition = new Vector3(0f, -0.2f, 0f);
                Hazard(transform, new Vector3(0f, 0.15f, 0f), 0.42f, 1f, lane, 18);
                BlobShadow.Attach(transform, 0.7f);
                // 꼬리 연기 파티클(간단): 작은 흰 구 두 개가 뒤에서 흔들림
                for (int i = 0; i < 2; i++)
                {
                    var p = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(p.GetComponent<Collider>()); p.name = "Puff" + i;
                    p.transform.SetParent(transform, false); p.transform.localPosition = new Vector3(0f, 0.1f, 0.5f + i * 0.45f); p.transform.localScale = Vector3.one * (0.28f - i * 0.08f);
                    p.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateUnlit(new Color(1f, 0.95f, 0.85f, 0.8f));
                }
            }
            private void Update()
            {
                if (GetComponent<ObstaclePopAnim>() != null) return;
                var pc = Player;
                _z -= _speed * Time.deltaTime;
                transform.position = RoadPlacement.OnRoad(_z, _lane * LaneWidth, _height + Mathf.Sin(Time.time * 18f) * 0.03f);
                if (pc != null && _z < pc.PathDistance - 6f) Destroy(gameObject);
            }
            private void OnDestroy() { AliveCount = Mathf.Max(0, AliveCount - 1); }
        }

        // ══════════════════════════════════════════════════════════════
        public class Tornado : MonoBehaviour
        {
            private float _speed, _amp, _hz, _life, _t, _z, _phase, _phase2; private Transform _vis;
            public void Init(float speed, float sweepAmp, float sweepHz, float seconds)
            {
                _speed = speed; _amp = sweepAmp; _hz = sweepHz; _life = seconds; _z = DownhillPath.DistanceAlong(transform.position);
                _phase = Random.Range(0f, 6.28f); _phase2 = Random.Range(0f, 6.28f);
                AliveCount++;
                var holder = new GameObject("Body"); holder.transform.SetParent(transform, false);
                _vis = Visual(holder.transform, "Tornado", 3.0f, new Color(0.55f, 0.6f, 0.7f), outline: false);
                Hazard(transform, new Vector3(0f, 0.9f, 0f), 0.75f, 1f, RoadOccupancy.AllLanes, 20);
                BlobShadow.Attach(transform, 1.3f);
            }
            private void Update()
            {
                if (GetComponent<ObstaclePopAnim>() != null) return;
                var pc = Player;
                _t += Time.deltaTime;
                _z -= _speed * Time.deltaTime;
                // 예측 불가한 좌우: 두 주파수 합 + 가끔 확 꺾임
                float lat = Mathf.Sin(_t * _hz * 6.28f + _phase) * _amp + Mathf.Sin(_t * _hz * 2.7f * 6.28f + _phase2) * _amp * 0.45f;
                lat = Mathf.Clamp(lat, -LaneWidth, LaneWidth);
                transform.position = RoadPlacement.OnRoad(_z, lat);
                if (_vis != null) _vis.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(_t * 9f) * 6f);
                if (_t > _life || (pc != null && _z < pc.PathDistance - 8f)) Destroy(gameObject);
            }
            private void OnDestroy() { AliveCount = Mathf.Max(0, AliveCount - 1); }
        }

        // ══════════════════════════════════════════════════════════════
        /// 스토리·일반 러닝용 「가끔 떨어지는 바위」 — 챕터 2부터, 빈 자리에만, 간격 22~40 초.
        public class RockRain : MonoBehaviour
        {
            private PlayerController _p; private float _next; private System.Random _rng = new System.Random(77);
            public float minGap = 22f, maxGap = 40f; public int fromChapter = 2;
            public void Bind(PlayerController p) { _p = p; _next = Time.time + 10f; }
            private void Update()
            {
                if (_p == null || !_p.enabled) return;
                if (BossDirector.Active) return;                 // 보스가 있을 땐 보스가 알아서
                int ch = StageManager.Instance != null ? StageManager.Instance.ChapterIndex : 1;
                if (ch < fromChapter) return;
                if (Time.time < _next) return;
                _next = Time.time + minGap + (float)_rng.NextDouble() * (maxGap - minGap);
                float lead = Mathf.Max(1.4f, 1.15f) + 0.35f;
                float z = _p.PathDistance + _p.Speed * lead + 6f;
                if (StageManager.Instance != null && Mathf.Abs(z - StageManager.Instance.FinishPathZ) < 40f) return;
                int lane = FindEmptyLane(z, _rng.Next(3) - 1, _rng);
                DropRock(z, lane, 1.15f);
            }
        }
    }
}
