using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun.Village
{
    /// 136차(사용자): 스토리 모드 허브 = 포켓캠프풍 바닷가 마을. 조이스틱 이동·충돌·나무 흔들기·낚시·상점/가방·텃밭·가구(마당) 배치.
    /// 송전탑 언덕 → 기존 주차 스케줄(TamaRaisingUI), 엄마 집 → 펫 돌보기·잠자기, 구멍가게 → ShopUI, 텃밭 → HomeUI 텃밭 탭, 해변 → FishingMini.
    public class VillageHub : MonoBehaviour
    {
        public static bool Enabled = true;
        public static VillageHub I { get; private set; }

        class Spot { public string id, title; public Vector3 pos; public float radius; public Action on; }

        GameManager _gm; RaisingSceneDriver _driver;
        Transform _world, _player, _rigT, _yardHost; CharacterController _cc; SkaterRig _rig; Animator _anim; Camera _cam; CharacterMotion _motion; Vector3 _vel, _acc;
        VillageHud _hud; readonly List<Spot> _spots = new List<Spot>(); Spot _near; Transform _nearTree; int _nearTreeIdx = -1;
        bool _moving; float _idleT; Vector3 _camVel; bool _busy; bool _yard; string _yardPick; float _yardRot; Transform _ghost; Canvas _yardCanvas; RectTransform _yardTray; Text _yardHint;
        VillageCreatures _creatures; VillageMap _map; bool _bat, _axe; /* 150차: 도끼(장작) */ Transform _toolVis;

        SaveData Save => _gm != null ? _gm.Save : null;

        public static VillageHub Create(GameManager gm, RaisingSceneDriver driver)
        {
            var go = new GameObject("VillageHub"); var h = go.AddComponent<VillageHub>();
            h._gm = gm; h._driver = driver; I = h;
            return h;
        }

        void Start()
        {
            _world = VillageWorld.Build(transform, Save);
            // 141차: 바람 흔들림(야자·갈대·꽃·덤불)
            foreach (Transform t in _world)
            {
                if (t.name.StartsWith("Tree_Palm")) { var s = t.gameObject.AddComponent<WindSway>(); s.Amp = 1.5f; s.Speed = 0.7f; s.Nudgeable = false; }
                else if (t.name == "Pampas") { var s = t.gameObject.AddComponent<WindSway>(); s.Amp = 5f; s.Speed = 1.5f; }
                else if (t.name == "FlowerClump" || t.name == "FlowerBed") { var s = t.gameObject.AddComponent<WindSway>(); s.Amp = 3.5f; s.Speed = 1.3f; }
                else if (t.name == "Bush") { var s = t.gameObject.AddComponent<WindSway>(); s.Amp = 1.8f; s.Speed = 1.1f; }
            }
            VillageWorld.BuildCrops(_world, Save);
            BuildPickups();   // 150차: 나뭇가지·조개·버섯
            BuildYardItems();
            BuildPlayer();
            BuildCamera();
            _hud = VillageHud.Create(OnAct, OnTalk, OnBag, OnMenu, OnTool);
            _creatures = VillageCreatures.Create(transform, _player);
            _creatures.Locked = () => _busy || _hud.Locked;
            _creatures.OnSpiritHit = SpiritHit;
            _creatures.OnCaught = (kind, shards, coins) => { if (Save == null) return; Save.starShards += shards; Save.starShardsTotal += shards; Save.stats.money += coins; if (kind != "spirit") { LifeItems.Add(Save, "bug_" + kind, 1); Save.bugsCaught++; } _gm.Persist(); RefreshStatus(); CoastAudioManager.PlayAnywhere(CoastSfx.Coin); };   // 150차: 벌레는 가방(채집)에도
            _map = VillageMap.Create(_hud.Root, _player);
            ApplyTool();
            BuildSpots();
            RefreshStatus();
            TitleAudio.PlayRaising();
            CoastToast.Show(Loc.T("조이스틱으로 걸어다니고, 가까이 가면 뜨는 분홍 버튼으로 들어가자!", "Walk with the joystick — tap the pink prompt to enter a place!"));
        }

        void OnDestroy() { if (I == this) I = null; RenderSettings.fog = false; VillagePalette.ApplySoftLook(false); Shader.SetGlobalVector("_CoastCurveRadial", Vector4.zero); if (_hud != null) Destroy(_hud.gameObject); if (_yardCanvas != null) Destroy(_yardCanvas.gameObject); }

        // ── 플레이어 ─────────────────────────────────────────────────────
        void BuildPlayer()
        {
            var go = new GameObject("VillagePlayer"); _player = go.transform; _player.SetParent(transform, false);
            var start = Save != null && Save.villageX != 0f ? new Vector3(Save.villageX, 0f, Save.villageZ) : new Vector3(0f, 0f, 22f);
            start.y = VillageWorld.Height(start.x, start.z);
            _player.position = start; _player.rotation = Quaternion.Euler(0f, 180f, 0f);
            _cc = go.AddComponent<CharacterController>(); _cc.height = 1.2f; _cc.radius = 0.32f; _cc.center = new Vector3(0f, 0.62f, 0f); _cc.slopeLimit = 60f; _cc.stepOffset = 0.35f;
            go.AddComponent<CcHitLog>();   // 149차 진단: 마지막으로 부딪힌 콜라이더
            var rigHost = new GameObject("Rig").transform; rigHost.SetParent(_player, false); _rigT = rigHost;
            _rig = SkaterRig.Spawn(rigHost, 1.25f, true);
            // 141차: 절차 모션(바운스·기울기·스쿼시·발먼지·고개)
            _motion = rigHost.gameObject.AddComponent<CharacterMotion>(); _motion.WalkSpeed = 2.05f; _motion.RunSpeed = 4.55f;
            if (_rig != null)
            {
                _anim = _rig.GetComponent<Animator>(); if (_anim != null) { _anim.SetBool("Grounded", true); _anim.SetFloat("Speed", 0f); _anim.Play("Run", 0, 0.12f); _anim.speed = 0f; }
                foreach (var smr in _rig.GetComponentsInChildren<SkinnedMeshRenderer>()) if (smr.GetComponent<CelOutlineHint>() == null) smr.gameObject.AddComponent<CelOutlineHint>();   // 138차: 잉크 윤곽
            }
            else CoastFigureMesh.BuildHaneul(rigHost, 1.25f);
            // 발밑 그림자
GroundBlob.Attach(_player, 0.5f, 0.36f, rigHost);   // 146차: 접지 블롭
        }

        void BuildCamera()
        {
            _cam = Camera.main;
            if (_cam == null) { var go = new GameObject("Main Camera"); go.tag = "MainCamera"; _cam = go.AddComponent<Camera>(); go.AddComponent<AudioListener>(); }
            _cam.orthographic = false; _cam.fieldOfView = 46f;   // 146차: 피치 ≈20° + 방사형 곡면(CurveK)으로 디오라마 — 수평선은 화면 31% _cam.nearClipPlane = 0.3f; _cam.farClipPlane = 420f;
            _cam.clearFlags = CameraClearFlags.SolidColor; _cam.backgroundColor = new Color(0.68f, 0.85f, 0.98f);
            if (_cam.GetComponent<CoastPortraitViewport>() == null) _cam.gameObject.AddComponent<CoastPortraitViewport>();
            var camData = _cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>() ?? _cam.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true; camData.antialiasing = UnityEngine.Rendering.Universal.AntialiasingMode.SubpixelMorphologicalAntiAliasing; camData.antialiasingQuality = UnityEngine.Rendering.Universal.AntialiasingQuality.High;
            SnapCamera();
        }

        // 140차(시안): 주인공 뒤에서 낮게 따라가는 카메라 — 바다 쪽을 보면 수평선·등대·마을이 한눈에
        float _camYaw, _camYawVel;
        float _swingK, _camYawTarget, _backT, _stickYaw; bool _stickHeld;
        public const float CurveK = 0.0018f;   // 방사형 곡률(m/m²): 40 m 에서 2.9 m, 60 m 에서 6.5 m 아래로 — 피치 20°·FOV 46 에서 수평선이 화면 31% 에 온다   // 141차: 크게 방향을 바꿀 때 카메라가 바깥 큰 원을 돌지 않게 — 가까이·높게 붙었다가 다시 멀어진다
        Vector3 CamTarget => _player.position + Quaternion.Euler(0f, _camYaw, 0f) * new Vector3(0f, Mathf.Lerp(4.5f, 5.6f, _swingK), Mathf.Lerp(-8.8f, -4.0f, _swingK));
        Vector3 CamLook => _player.position + new Vector3(0f, 0.7f, 0f) + Quaternion.Euler(0f, _camYaw, 0f) * new Vector3(0f, 0f, 1.6f);
        void SnapCamera() { _camYaw = _camYawTarget = _player.eulerAngles.y; _backT = 0f; _cam.transform.position = CamTarget; _cam.transform.LookAt(CamLook); _camVel = Vector3.zero; _snapCam = true; LateUpdate(); }
        bool _snapCam;
        bool CamBlocked(Vector3 p)
        {
            foreach (var c in Physics.OverlapSphere(p, 0.3f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (c == null || c.transform.IsChildOf(_player) || c.GetComponentInParent<VillageCreatures>() != null || c.name == "Bound") continue;
                return true;
            }
            return false;
        }

        void BuildSpots()
        {
            _spots.Clear();
            // 148차(사용자): 스케줄(홈) 화면은 우리집에 들어갈 때만 — 송전탑 언덕 스팟은 경치만(스케줄 진입 찌꺼기 제거)
            _spots.Add(new Spot { id = "tower", title = Loc.T("송전탑 언덕 · 마을이 다 보인다", "Tower hill · View of the village"), pos = VillageWorld.Ground(11f, 38f), radius = 5.5f, on = () => _hud.Bubble(Loc.T("하늘", "Haneul"), Loc.T("여기서 보면 마을이 다 보여. 이번 주 할 일은 집에 가서 정하자.", "You can see the whole village from here. Let's plan the week at home.")) });
            _spots.Add(new Spot { id = "hero", title = Loc.T("우리집 · 들어가기", "Our home · Enter"), pos = VillageWorld.Ground(0f, 32f), radius = 4.6f, on = HeroHouseMenu });
            _spots.Add(new Spot { id = "mom", title = Loc.T("엄마 집 · 펫 돌보기 / 잠자기", "Mom's house · Pets / Sleep"), pos = VillageWorld.Ground(1.2f, -20f), radius = 4.2f, on = MomHouseMenu });
            _spots.Add(new Spot { id = "shop", title = Loc.T("해변 상점 · 장보기", "Beach shop · Buy"), pos = VillageWorld.Ground(-5.5f, -29.5f), radius = 4f, on = OpenShop });
            _spots.Add(new Spot { id = "garden", title = Loc.T("텃밭 · 밭 칸으로 들어가자", "Garden · Step onto a plot"), pos = VillageWorld.Ground(0f, -6f), radius = 6.6f, on = FarmAct });   // 150차: 스타듀식 9칸
            _spots.Add(new Spot { id = "beach", title = Loc.T("바닷가 · 낚시하기", "Beach · Go fishing"), pos = VillageWorld.Ground(-1f, -31f), radius = 4.5f, on = OpenFishing });
            // 147차: 집마다 들어가기(문 앞 스팟) — 우리집/엄마 집은 기존 메뉴에 항목 추가
            foreach (var hs in VillageWorld.Houses)
            {
                if (hs.house == VillageWorld.HeroHouse || hs.house == VillageWorld.MomHouse) continue;
                var cap = hs; 
                _spots.Add(new Spot { id = "house_" + cap.name, title = Loc.T(cap.name + " · 들어가기", cap.name + " · Enter"), pos = cap.door, radius = 2.4f, on = () => EnterHouse(cap.house, cap.name, cap.door) });
            }
            _spots.Add(new Spot { id = "light", title = Loc.T("등대 · 바다 구경", "Lighthouse · Sea view"), pos = VillageWorld.Ground(-9f, -61f), radius = 4.5f, on = () => _hud.Bubble(Loc.T("하늘", "Haneul"), Loc.T("등대 불빛이 89.2처럼 깜빡인다. 바다가 오늘은 조용하다.", "The lighthouse blinks like 89.2. The sea is quiet today.")) });
        }

        // ── 매 프레임 ─────────────────────────────────────────────────────
        void Update()
        {
            if (_player == null || _hud == null) return;
            bool locked = _busy || _hud.Locked;
            Vector2 j = locked ? Vector2.zero : _hud.Joy;
            // 에디터/원격 테스트: 방향키·WASD
            if (!locked)
            {
                float kx = (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A) ? 1f : 0f);
                float ky = (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S) ? 1f : 0f);
                if (Mathf.Abs(kx) + Mathf.Abs(ky) > 0f) j = Vector2.ClampMagnitude(new Vector2(kx, ky), 1f);
                if (_walkTo.HasValue) { var d = _walkTo.Value - _player.position; d.y = 0f; if (d.magnitude < 0.4f) _walkTo = null; else { var ld = Quaternion.Euler(0f, -(_stickHeld ? _stickYaw : _camYaw), 0f) * d.normalized; j = new Vector2(ld.x, ld.z); } }
            }
            float dt = Time.deltaTime; float mag = j.magnitude;
            // 140차: 조이스틱은 카메라 기준(위 = 카메라가 보는 방향)
            // 145차(사용자: 「뒤로가기가 잠깐 되다 안 됨」): 스틱을 누르는 동안 카메라가 뒤로 돌아붙으면 같은 스틱 방향이
            // 반대 월드 방향으로 뒤집혀 주인공이 1.2초마다 돌아섰다 — 스틱을 처음 민 순간의 카메라 방향을 놓을 때까지 고정
            if (mag > 0.05f && !_stickHeld) { _stickHeld = true; _stickYaw = _camYaw; }
            else if (mag <= 0.05f) _stickHeld = false;
            var camRot = Quaternion.Euler(0f, _stickHeld ? _stickYaw : _camYaw, 0f);
            var dir = camRot * new Vector3(j.x, 0f, j.y);
            // 141차(동물의 숲 느낌): 스틱을 조금 밀면 걷고 끝까지 밀면 달림, 가속·감속은 부드럽게, 몸은 진행 방향으로 스르륵
            float wantSpeed = mag < 0.05f ? 0f : Mathf.Lerp(2.05f, 4.55f, Mathf.InverseLerp(0.35f, 0.95f, mag));   // 144차: +20%
            var wantVel = mag < 0.05f ? Vector3.zero : dir.normalized * wantSpeed;
            _vel = Vector3.SmoothDamp(_vel, wantVel, ref _acc, mag < 0.05f ? 0.08f : 0.13f, 100f, dt);
            var move = _vel * dt;
            if (mag > 0.05f)
            {
                var face = Quaternion.LookRotation(dir, Vector3.up);
                _player.rotation = Quaternion.Slerp(_player.rotation, face, 1f - Mathf.Exp(-dt * 10f));
            }
            float gy = VillageWorld.Height(_player.position.x, _player.position.z);
            if (VillageFarm.TileAt(_player.position) >= 0) gy += VillageFarm.Top;   // 150차: 밭 흙 상자 위에 선다
            move.y = gy - _player.position.y;
            _cc.Move(move);
            bool moving = _vel.magnitude > 0.15f;
            _moving = moving;
            // 지나치는 풀·덤불 흔들기
            if (moving && Time.frameCount % 6 == 0) WindSway.NudgeNear(_player.position, 1.1f);
            // 가까운 장소·나무
            UpdateNear();
            if (_yard) UpdateYard();
            if (Time.frameCount % 30 == 0 && Save != null) { Save.villageX = _player.position.x; Save.villageZ = _player.position.z; }
        }

        void LateUpdate()
        {
            if (_cam == null || _player == null) return;
            // 144차(사용자: 「좌우로 움직이면 획획 넘어감」): 카메라 방향은 좌우 이동에 따라 돌지 않는다(동물의 숲처럼 고정).
            // 카메라 쪽(뒤)으로 1.2 초 이상 계속 걸을 때만 목표 방향을 바꿔 천천히 돌아붙고, 그 외엔 진행 방향으로 살짝 기울기만.
            float dtc = Time.deltaTime; float pyaw = _player.eulerAngles.y;
            float headErr = Mathf.DeltaAngle(_camYawTarget, pyaw);
            // 실제로 몸이 움직일 때만(벽에 막혀 제자리면 카메라가 돌지 않게) — CharacterController 실제 속도 기준
            bool reallyMoving = _cc != null && _cc.velocity.magnitude > 0.4f;
            // 149차(사용자: 「주인공이 움직이는 방향으로 카메라 이동」): 0.35 초 이상 실제로 걸으면 카메라 목표 방향이 진행 방향을
            // 초당 110° 로 따라가고, 실제 회전은 0.6 초 감쇠 — 좌우 스틱에 휙 돌지 않고 스르륵 돌아붙는다. 멈추면 그 방향 유지.
            if (_moving && reallyMoving) _backT += dtc; else _backT = 0f;
            if (_backT > 0.35f) _camYawTarget = Mathf.MoveTowardsAngle(_camYawTarget, pyaw, dtc * 110f);
            float lean = Mathf.Clamp(headErr, -90f, 90f) * (_moving ? 0.06f : 0f);
            float yawErr = Mathf.Abs(Mathf.DeltaAngle(_camYaw, _camYawTarget));
            float kWant = Mathf.Clamp01((yawErr - 40f) / 90f);
            _swingK = Mathf.Lerp(_swingK, kWant, 1f - Mathf.Exp(-dtc * (kWant > _swingK ? 9f : 3f)));
            _camYaw = Mathf.SmoothDampAngle(_camYaw, _camYawTarget + lean, ref _camYawVel, 0.6f);
            // 146차: 방사형 곡면 원점 = 주인공(가까운 곳은 평평, 60 m 에서 2.9 m, 바다 끝 70 m 아래로)
            Shader.SetGlobalVector("_CoastCurveRadial", new Vector4(CurveK, _player.position.x, _player.position.z, 320f));
            var look = CamLook; var ct = CamTarget;
            // 140차: 카메라가 멀어진 만큼 지형·바다·건물 안으로 파고들지 않게 — 땅/바다 높이 클램프 + 구체 캐스트로 당기기
            float gy = VillageWorld.Height(ct.x, ct.z) + 1.6f; if (ct.y < gy) ct.y = gy;
            if (ct.y < VillageWorld.SeaLevel + 1.8f) ct.y = VillageWorld.SeaLevel + 1.8f;
            var dir = ct - look; float dist = dir.magnitude; float best = CamFree(look, ct);
            if (best < dist)
            {
                float pull = Mathf.Max(2.6f, best - 0.3f);
                var ctPull = look + dir / dist * pull;
                // 147차: 건물 사이에 끼어 카메라가 머리 뒤까지 당겨지면(5.5 m 미만) 위로 올려 내려다본다(머리통만 찍히던 문제).
                // 올린 자리도 지붕·차양에 막히면 그쪽 자유 거리만큼만.
                float kUp = Mathf.Clamp01((5.5f - pull) / 3f);
                var flat = new Vector3(dir.x, 0f, dir.z); if (flat.sqrMagnitude < 0.01f) flat = -_cam.transform.forward; flat.y = 0f;
                var ctHigh = look + flat.normalized * 2.4f + Vector3.up * 7.2f;
                var dh = ctHigh - look; float lh = dh.magnitude; float fh = CamFree(look, ctHigh);
                if (fh < lh) ctHigh = look + dh / lh * Mathf.Max(2.6f, fh - 0.4f);
                ct = Vector3.Lerp(ctPull, ctHigh, kUp);
            }
            // 캐스트 시작점이 이미 건물 안이면(스폿이 건물 바로 앞) 앞으로 당겨 가며 빈 자리 찾기
            for (int guard = 0; guard < 24 && CamBlocked(ct); guard++) { var dd = ct - look; float d2 = dd.magnitude - 0.5f; if (d2 < 2.6f) break; ct = look + dd.normalized * d2; }
            _cam.transform.position = _snapCam ? ct : Vector3.SmoothDamp(_cam.transform.position, ct, ref _camVel, 0.16f); _snapCam = false;
            _cam.transform.LookAt(look);
        }

        /// look→to 구체 캐스트로 막힘 없는 거리(경계 콜라이더·주인공·NPC 무시). 막힘 없으면 전체 거리.
        float CamFree(Vector3 look, Vector3 to)
        {
            var d = to - look; float len = d.magnitude; float best = len; if (len < 0.001f) return len;
            var hits = Physics.SphereCastAll(look, 0.35f, d / len, len, ~0, QueryTriggerInteraction.Ignore);
            foreach (var hit in hits)
            {
                if (hit.collider == null || hit.collider.name == "Bound" || hit.collider.transform.IsChildOf(_player) || hit.collider.GetComponentInParent<VillageCreatures>() != null) continue;
                if (hit.distance > 0f && hit.distance < best) best = hit.distance;
            }
            return best;
        }

        /// 149차 진단: 이동이 막히는 원인(잠금·팝업·조이스틱 위 UI·타임스케일) 한 줄 로그.
        public void DiagLog()
        {
            var es = UnityEngine.EventSystems.EventSystem.current;
            string top = "none";
            if (es != null && _hud != null && _hud.JoyRect != null)
            {
                var sp = RectTransformUtility.WorldToScreenPoint(null, _hud.JoyRect.position);
                var ped = new UnityEngine.EventSystems.PointerEventData(es) { position = sp };
                var res = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>(); es.RaycastAll(ped, res);
                top = res.Count > 0 ? res[0].gameObject.name + "(" + res.Count + ")" : "nothing";
                for (int i = 0; i < res.Count && i < 4; i++) top += " / " + res[i].gameObject.name;
            }
            var hl = _player.GetComponent<CcHitLog>(); string hit = hl != null ? hl.Last : "-";
            Debug.LogWarning($"[Diag] hit={hit} busy={_busy} locked={_hud?.Locked} popup={_hud?.PopupOpen} joy={_hud?.Joy} cc={(_cc != null && _cc.enabled)} ts={Time.timeScale} es={(es != null)} topAtJoy={top} vel={_vel.magnitude:F2} pos={_player.position}");
        }
        Vector3? _walkTo; float _ghostLift;
        /// 개발용: 자리로 걸어가기.
        public void WalkTo(Vector3 p) { _walkTo = p; }
        // ── 147차: 집 안 들어가기/나오기 ────────────────────────────────
        VillageInterior _interior;
        public void EnterHouse(Transform house, string name, Vector3 door)
        {
            if (_interior != null) return;
            Color wall = new Color(0.98f, 0.94f, 0.88f), accent = VillagePalette.UiPink;
            var wr = house != null ? house.Find("Wall") : null;
            if (wr != null) { var m = wr.GetComponent<Renderer>().sharedMaterial; if (m != null && m.HasProperty("_BaseColor")) wall = Color.Lerp(m.GetColor("_BaseColor"), Color.white, 0.35f); }
            string[] acc = { "#FEC4DD", "#A9DCC8", "#BFD8F5", "#F7D5A6", "#CDBDDA" }; accent = VillagePalette.Hex(acc[Mathf.Abs(name.GetHashCode()) % acc.Length]);
            var back = door + (house != null ? house.forward * 0.6f : Vector3.zero);
            StartCoroutine(EnterHouseRoutine(name, wall, accent, back, house != null ? house.eulerAngles.y : 180f));
        }
        IEnumerator EnterHouseRoutine(string name, Color wall, Color accent, Vector3 back, float houseYaw)
        {
            _busy = true; yield return FadeScreen(true, 0.25f);
            _interior = VillageInterior.Create(transform, name, wall, accent, back, houseYaw);
            Teleport(_interior.Entry, 0f);
            _spots.Add(new Spot { id = "exit", title = Loc.T("문 · 밖으로 나가기", "Door · Go outside"), pos = _interior.ExitSpot, radius = 1.6f, on = ExitHouse });
            yield return null; yield return FadeScreen(false, 0.3f);
            _busy = false; _hud.Bubble(Loc.T("하늘", "Haneul"), Loc.T(name + " 안이다. 아늑하네.", "Inside " + name + ". Cozy."));
        }
        Image _fadeImg;
        IEnumerator FadeScreen(bool toBlack, float dur)
        {
            if (_fadeImg == null && _hud != null)
            {
                var go = new GameObject("Fade", typeof(RectTransform), typeof(Image)); go.transform.SetParent(_hud.Root, false); go.transform.SetAsLastSibling();
                _fadeImg = go.GetComponent<Image>(); _fadeImg.color = new Color(0f, 0f, 0f, 0f); _fadeImg.raycastTarget = false;
                var rt = go.GetComponent<RectTransform>(); rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            }
            float a0 = _fadeImg != null ? _fadeImg.color.a : 0f, a1 = toBlack ? 1f : 0f;
            for (float t = 0f; t < dur; t += Time.deltaTime) { if (_fadeImg != null) _fadeImg.color = new Color(0f, 0f, 0f, Mathf.Lerp(a0, a1, t / dur)); yield return null; }
            if (_fadeImg != null) _fadeImg.color = new Color(0f, 0f, 0f, a1);
        }
        public void ExitHouse()
        {
            if (_interior == null) return;
            StartCoroutine(ExitHouseRoutine());
        }
        IEnumerator ExitHouseRoutine()
        {
            _busy = true; yield return FadeScreen(true, 0.25f);
            var it = _interior; _interior = null;
            _spots.RemoveAll(s => s.id == "exit");
            Teleport(it.ReturnPos, it.ReturnYaw); Destroy(it.gameObject);
            yield return null; yield return FadeScreen(false, 0.3f); _busy = false;
        }

        public void Teleport(Vector3 p, float faceYaw = float.NaN) { _walkTo = null; _vel = Vector3.zero; _cc.enabled = false; _player.position = new Vector3(p.x, VillageWorld.Height(p.x, p.z), p.z); if (!float.IsNaN(faceYaw)) _player.rotation = Quaternion.Euler(0f, faceYaw, 0f); _cc.enabled = true; SnapCamera(); if (_creatures != null) _creatures.SnapKid(); }
        public Vector3 PlayerPos => _player != null ? _player.position : Vector3.zero;
        public float PlayerYaw => _player != null ? _player.eulerAngles.y : 0f;
        public Vector3 SpotPos(string id) { foreach (var s in _spots) if (s.id == id) return s.pos; return _player.position; }

        void UpdateNear()
        {
            _near = null; float best = 999f; var p = _player.position;
            foreach (var s in _spots) { float d = Vector3.Distance(new Vector3(p.x, 0f, p.z), new Vector3(s.pos.x, 0f, s.pos.z)); if (d < s.radius && d < best) { best = d; _near = s; } }
            _nearTree = null; _nearTreeIdx = -1; best = 2.8f;
            for (int i = 0; i < VillageWorld.Trees.Count; i++)
            {
                var t = VillageWorld.Trees[i]; if (t == null) continue;
                float d = Vector3.Distance(new Vector3(p.x, 0f, p.z), new Vector3(t.position.x, 0f, t.position.z));
                if (d < best) { best = d; _nearTree = t; _nearTreeIdx = i; }
            }
            if (_yard) { _hud.SetPrompt(null, null); _hud.SetAction(Loc.T("놓기", "Place")); return; }
            // 150차: 텃밭 안에서는 서 있는 칸의 상태로 안내가 바뀐다(씨/물/수확/잡초) + 노란 링
            int tile = _near != null && _near.id == "garden" ? VillageFarm.TileAt(p) : -1;
            if (_near != null && _near.id == "garden") _near.title = FarmTitle(tile);
            if (VillageFarm.Marker != null)
            {
                bool show = tile >= 0; if (VillageFarm.Marker.gameObject.activeSelf != show) VillageFarm.Marker.gameObject.SetActive(show);
                if (show) { var c = VillageFarm.TileCenter(tile); VillageFarm.Marker.position = new Vector3(c.x, VillageWorld.Height(c.x, c.z) + VillageFarm.Top + 0.012f, c.z); }
            }
            _hud.SetPrompt(_near != null ? "▶ " + _near.title : null, _near != null ? _near.on : null);
            bool critter = _creatures != null && !_axe && _creatures.AnyCritterNear(_bat);
            string toolAct = _axe ? Loc.T("나무 패기", "Chop") : _bat ? Loc.T("휘두르기", "Swing") : Loc.T("잡기", "Catch");
            _hud.SetAction(critter ? toolAct : _nearTree != null ? (_axe ? Loc.T("나무 패기", "Chop") : Loc.T("흔들기", "Shake")) : _near != null && _near.id == "beach" ? Loc.T("낚시", "Fish") : toolAct);
        }

        // ── 버튼 ─────────────────────────────────────────────────────────
        void OnAct()
        {
            if (_busy || _hud.Locked) return;
            if (_yard) { PlaceGhost(); return; }
            // 150차: 도끼 — 나무 곁이면 팬다(장작), 아니면 헛스윙
            if (_axe) { if (_nearTree != null) { StartCoroutine(ChopTree(_nearTree, _nearTreeIdx)); return; } Swing(); CoastToast.Show(Loc.T("붕— 나무 곁에서 휘둘러야 장작이 나온다.", "Whoosh — swing next to a tree.")); return; }
            // 도구 먼저: 정령(방망이)·나비(잠자리채)가 가까우면 휘두른다
            if (_creatures != null && _creatures.AnyCritterNear(_bat))
            {
                Swing(); var msg = _creatures.Swing(_bat); if (msg != null) CoastToast.Show(msg); return;
            }
            if (_nearTree != null) { StartCoroutine(ShakeTree(_nearTree, _nearTreeIdx)); return; }
            if (_near != null && _near.id == "beach") { OpenFishing(); return; }
            Swing();
            var m2 = _creatures != null ? _creatures.Swing(_bat) : null;
            CoastToast.Show(m2 ?? (_bat ? Loc.T("붕— 정령이 없다. 정령은 가끔 나타나 다가온다.", "Whoosh — no spirit. They show up now and then.") : Loc.T("휙— 나비가 없다. 팔랑이는 나비 곁에서 잡아 보자.", "Swish — no butterfly nearby.")));
        }

        void OnTalk()
        {
            if (_busy || _hud.Locked) return;
            // 마을 사람이 가까우면 그쪽(대답은 「누구세요?」「음…」뿐), 아니면 옆에 붙어 다니는 꼬마
            if (_creatures != null && _creatures.TalkNearestNpc()) return;
            if (_creatures != null) { _hud.Bubble(Loc.T("꼬마", "Kid"), _creatures.KidLine()); return; }
            if (_near != null) { _hud.Bubble(Loc.T("하늘", "Haneul"), _near.title); return; }
        }

        void OnTool()
        {
            if (_busy || _hud.Locked) return;
            _hud.Choice(Loc.T("도구 고르기", "Choose a tool"), Loc.T("잠자리채: 나비 잡기(별조각) · 방망이: 다가오는 정령을 톡(별조각·코인). 정령과 부딪히면 HP가 깎여!", "Net: catch butterflies (shards) · Bat: bop spirits (shards, coins). Bumping a spirit costs HP!"),
                new (string, Color, Action)[] {
                    (Loc.T("🦋 잠자리채", "🦋 Net") + (!_bat ? Loc.T(" (지금)", " (now)") : ""), new Color(0.45f, 0.78f, 0.55f), () => { _bat = false; _axe = false; ApplyTool(); }),
                    (Loc.T("🏏 방망이", "🏏 Bat") + (_bat && !_axe ? Loc.T(" (지금)", " (now)") : ""), new Color(0.98f, 0.62f, 0.45f), () => { _bat = true; _axe = false; ApplyTool(); }),
                    (Loc.T("🪓 도끼 — 나무 패서 장작(난로 연료)", "🪓 Axe — chop trees for firewood") + (_axe ? Loc.T(" (지금)", " (now)") : ""), new Color(0.62f, 0.52f, 0.40f), () => { _axe = true; ApplyTool(); }),
                });
        }

        /// 도구 표시: 오른손 뼈에 잠자리채(막대+고리) 또는 방망이(막대) 붙이기 + 버튼 라벨
        /// 개발용: 도구 바로 바꾸기(0 잠자리채 1 방망이 2 도끼)
        public void DevTool(int t) { _bat = t == 1; _axe = t == 2; ApplyTool(); }
        void ApplyTool()
        {
            if (_hud != null) _hud.SetTool(_axe ? Loc.T("도끼", "Axe") : _bat ? Loc.T("방망이", "Bat") : Loc.T("잠자리채", "Net"));
            StartCoroutine(OutlineToolLater());
            if (_toolVis != null) Destroy(_toolVis.gameObject);
            Transform hand = _anim != null && _anim.avatar != null && _anim.avatar.isHuman ? _anim.GetBoneTransform(HumanBodyBones.RightHand) : null;
            var root = new GameObject("ToolVis").transform; _toolVis = root;
            root.SetParent(hand != null ? hand : _rigT, false);
            root.localPosition = hand != null ? new Vector3(0f, -0.02f, 0.02f) : new Vector3(0.28f, 0.7f, 0.1f);
            root.localRotation = Quaternion.Euler(-70f, 0f, 0f);
            float k = hand != null ? 1f / Mathf.Max(0.01f, hand.lossyScale.x) : 1f;
            if (_axe)
            {
                // 150차: 도끼 — 자루 + 날
                var h = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(h.GetComponent<Collider>()); h.transform.SetParent(root, false);
                h.transform.localPosition = new Vector3(0f, 0.30f * k, 0f); h.transform.localScale = new Vector3(0.05f, 0.32f, 0.05f) * k;
                h.GetComponent<MeshRenderer>().sharedMaterial = CoastMaterials.CreateLit(new Color(0.62f, 0.44f, 0.28f));
                var bl = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(bl.GetComponent<Collider>()); bl.transform.SetParent(root, false);
                bl.transform.localPosition = new Vector3(0.07f * k, 0.56f * k, 0f); bl.transform.localScale = new Vector3(0.18f, 0.14f, 0.04f) * k;
                bl.GetComponent<MeshRenderer>().sharedMaterial = CoastMaterials.CreateLit(new Color(0.78f, 0.80f, 0.84f));
            }
            else if (_bat)
            {
                var b = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(b.GetComponent<Collider>()); b.transform.SetParent(root, false);
                b.transform.localPosition = new Vector3(0f, 0.32f * k, 0f); b.transform.localScale = new Vector3(0.06f, 0.34f, 0.06f) * k;
                b.GetComponent<MeshRenderer>().sharedMaterial = CoastMaterials.CreateLit(new Color(0.80f, 0.60f, 0.35f));
                var tip = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(tip.GetComponent<Collider>()); tip.transform.SetParent(root, false);
                tip.transform.localPosition = new Vector3(0f, 0.55f * k, 0f); tip.transform.localScale = new Vector3(0.09f, 0.14f, 0.09f) * k;
                tip.GetComponent<MeshRenderer>().sharedMaterial = CoastMaterials.CreateLit(new Color(0.85f, 0.35f, 0.35f));
            }
            else
            {
                var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(pole.GetComponent<Collider>()); pole.transform.SetParent(root, false);
                pole.transform.localPosition = new Vector3(0f, 0.36f * k, 0f); pole.transform.localScale = new Vector3(0.035f, 0.38f, 0.035f) * k;
                pole.GetComponent<MeshRenderer>().sharedMaterial = CoastMaterials.CreateLit(new Color(0.55f, 0.40f, 0.25f));
                var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(ring.GetComponent<Collider>()); ring.transform.SetParent(root, false);
                ring.transform.localPosition = new Vector3(0f, 0.82f * k, 0f); ring.transform.localScale = new Vector3(0.34f, 0.015f, 0.34f) * k; ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                ring.GetComponent<MeshRenderer>().sharedMaterial = CoastMaterials.CreateTransparent(new Color(0.85f, 0.95f, 1f, 0.30f));   // 138차: 속은 비치고 테두리는 잉크 셸
                var mesh = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(mesh.GetComponent<Collider>()); mesh.transform.SetParent(root, false);
                mesh.transform.localPosition = new Vector3(0f, 0.82f * k, -0.10f * k); mesh.transform.localScale = new Vector3(0.32f, 0.32f, 0.22f) * k;
                mesh.GetComponent<MeshRenderer>().sharedMaterial = CoastMaterials.CreateTransparent(new Color(0.85f, 0.95f, 1f, 0.35f));
            }
        }

        IEnumerator OutlineToolLater()
        {
            yield return null;
            if (_toolVis != null) BuildingOutline.Attach(_toolVis, 0.012f);
            // 138차: 캐릭터 잉크 윤곽을 마을 카메라 거리에 맞게 조금 굵게
            foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>())
                if (smr.name == "Outline") foreach (var m in smr.sharedMaterials) if (m != null && m.HasProperty("_Width")) m.SetFloat("_Width", 0.016f);
        }

        void Swing()
        {
            if (_anim != null) { _anim.speed = 1f; _anim.SetTrigger("Collect"); StartCoroutine(SwingSettle()); }
            CoastPrefs.Vibrate();
        }
        IEnumerator SwingSettle() { yield return new WaitForSeconds(0.9f); if (_anim != null && !_moving) { _anim.Play("Run", 0, 0.12f); _anim.speed = 0f; } }

        void SpiritHit(int dmg)
        {
            if (Save == null) return;
            Save.stats.stamina = Mathf.Max(1, Save.stats.stamina - dmg);
            _gm.Persist(); RefreshStatus(); CoastPrefs.VibrateEvent();
            CoastToast.Show(Loc.T($"💥 정령과 부딪혔다! HP −{dmg} — 방망이로 먼저 톡!", $"💥 Bumped a spirit! HP −{dmg} — bop it with the bat first!"));
            StartCoroutine(HitFlash());
        }
        IEnumerator HitFlash()
        {
            var rs = _rigT != null ? _rigT.GetComponentsInChildren<Renderer>() : new Renderer[0];
            for (int i = 0; i < 3; i++) { foreach (var r in rs) r.enabled = false; yield return new WaitForSeconds(0.08f); foreach (var r in rs) r.enabled = true; yield return new WaitForSeconds(0.08f); }
        }

        void OnBag()
        {
            if (_busy || _hud.Locked) return;
            _busy = true; InventoryUI.Open(_gm, () => { _busy = false; RefreshStatus(); });
        }

        void OnMenu()
        {
            if (_busy) return;
            _hud.Choice(Loc.T("마을 메뉴", "Village menu"), Loc.T($"{Save.week}주차 · {Timeline.SeasonName(Timeline.SeasonOf(Save.week))} · Lv.{Save.level}", $"Week {Save.week} · {Timeline.SeasonName(Timeline.SeasonOf(Save.week))} · Lv.{Save.level}"),
                new (string, Color, Action)[] {
                    (Loc.T("📅 이번 주 스케줄(송전탑)", "📅 Weekly schedule (tower)"), new Color(0.35f, 0.62f, 0.95f), EnterSchedule),
                    (Loc.T("🛍 구멍가게", "🛍 Corner shop"), new Color(0.98f, 0.62f, 0.72f), OpenShop),
                    (Loc.T("🎒 가방", "🎒 Bag"), new Color(0.98f, 0.78f, 0.35f), OnBag),
                    (Loc.T("🏠 타이틀로", "🏠 To title"), new Color(0.55f, 0.55f, 0.62f), () => { _gm.Persist(); _gm.ToTitle(); }),
                });
        }

        // ── 장소 ─────────────────────────────────────────────────────────
        void EnterSchedule()
        {
            if (_busy) return; _busy = true;
            if (Save != null) { Save.villageX = _player.position.x; Save.villageZ = _player.position.z; }
            _gm.Persist();
            _driver.OpenScheduleFromVillage();
        }

        void HeroHouseMenu()
        {
            // 137차(사용자): 우리집에 들어가면 = 기존 스토리 홈 화면(주차 스케줄). 마당 꾸미기는 두 번째.
            _hud.Choice(Loc.T("우리집", "Our home"), Loc.T("언덕 꼭대기 우리집. 들어가서 이번 주 할 일을 정하자.", "Our house on the hilltop. Go in and plan the week."),
                new (string, Color, Action)[] {
                    (Loc.T("🏠 집에 들어가기 (홈 화면)", "🏠 Go inside (home screen)"), new Color(0.35f, 0.62f, 0.95f), EnterSchedule),
                    (Loc.T($"🔥 난로에 장작 넣기 (장작 {LifeItems.Count(Save, "mat_wood")} · 연료 {Save.villageFuel}주)", $"🔥 Stove firewood (wood {LifeItems.Count(Save, "mat_wood")} · fuel {Save.villageFuel}w)"), new Color(0.95f, 0.55f, 0.35f), AddFuel),
                    (Loc.T("🪑 마당 가구 배치", "🪑 Yard furniture"), new Color(0.45f, 0.78f, 0.55f), () => SetYard(true)),
                    (Loc.T("🛋 내 방 꾸미기", "🛋 My room"), new Color(0.98f, 0.62f, 0.72f), () => OpenHome(0)),
                    (Loc.T("🚪 집 안 둘러보기", "🚪 Look inside"), new Color(0.95f, 0.78f, 0.45f), () => EnterHouse(VillageWorld.HeroHouse, Loc.T("우리집", "Our home"), VillageWorld.HeroHouse.TransformPoint(new Vector3(0f, 0f, 3.3f)))),
                });
        }

        void MomHouseMenu()
        {
            bool slept = Save != null && Save.villageSleepStamp == Save.week * 4 + Save.phaseIndex;
            _hud.Choice(Loc.T("엄마 집", "Mom's house"), Loc.T("펫들이 마당에서 논다. 낮잠은 한 턴에 한 번.", "The pets play in the yard. One nap per turn."),
                new (string, Color, Action)[] {
                    (Loc.T("🐾 펫 돌보기", "🐾 Care for pets"), new Color(0.98f, 0.62f, 0.72f), () => OpenHome(0)),
                    (slept ? Loc.T("💤 (이번 턴엔 이미 잤어)", "💤 (Already napped)") : Loc.T("💤 잠자기 — 기운 +12 · 컨디션 +8", "💤 Sleep — Energy +12 · Condition +8"), slept ? new Color(0.6f, 0.6f, 0.65f) : new Color(0.60f, 0.52f, 0.92f), Sleep),
                    (Loc.T("🚪 집 안 둘러보기", "🚪 Look inside"), new Color(0.95f, 0.78f, 0.45f), () => EnterHouse(VillageWorld.MomHouse, Loc.T("엄마 집", "Mom's"), VillageWorld.MomHouse.TransformPoint(new Vector3(0f, 0f, 3.4f)))),
                });
        }

        void Sleep()
        {
            if (Save == null) return;
            int stamp = Save.week * 4 + Save.phaseIndex;
            if (Save.villageSleepStamp == stamp) { CoastToast.Show(Loc.T("이번 턴엔 이미 잤어. 다음 턴에 또 자자.", "Already napped this turn.")); return; }
            StartCoroutine(SleepCo(stamp));
        }

        IEnumerator SleepCo(int stamp)
        {
            _busy = true;
            var cv = CoastUiCanvas.Create("SleepFade", 200);
            var img = CoastHudLayout.MakeImage(CoastUiCanvas.Root(cv), "F", Vector2.zero, Vector2.one, new Vector2(-400f, -400f), new Vector2(400f, 400f), new Color(0f, 0f, 0f, 0f));
            img.raycastTarget = true;
            var zz = CoastHudLayout.MakeText(img.rectTransform, "Z", "z z Z …", 40, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero); zz.color = new Color(1f, 1f, 1f, 0f);
            float t = 0f;
            while (t < 1f) { t += Time.deltaTime; img.color = new Color(0f, 0f, 0f, t); zz.color = new Color(1f, 1f, 1f, t); yield return null; }
            Save.villageSleepStamp = stamp;
            Save.stats.stress = Mathf.Max(0, Save.stats.stress - 12);
            Save.condition = Mathf.Min(100, Save.condition + 8);
            Save.hunger = Mathf.Max(0, Save.hunger - 4);
            _gm.Persist(); RefreshStatus();
            yield return new WaitForSeconds(0.8f);
            t = 1f;
            while (t > 0f) { t -= Time.deltaTime; img.color = new Color(0f, 0f, 0f, t); zz.color = new Color(1f, 1f, 1f, t); yield return null; }
            Destroy(cv.gameObject);
            _busy = false;
            CoastToast.Show(Loc.T("푹 잤다. 기운 +12 · 컨디션 +8", "Slept well. Energy +12 · Condition +8"));
        }

        void OpenHome(int tab)
        {
            if (_busy) return; _busy = true;
            var girl = CoastUiArt.Art("UI_Haneul_Stand");
            var ui = HomeUI.Open(_gm, girl, () => { _busy = false; RefreshStatus(); VillageWorld.BuildCrops(_world, Save); });
            if (ui != null && tab == 1) ui.OpenBalcony();
        }

        void OpenShop() { if (_busy) return; _busy = true; ShopUI.Open(_gm, 0, () => { _busy = false; RefreshStatus(); }); }
        void OpenGarden() { OpenHome(1); }
        void OpenFishing() { if (_busy) return; _busy = true; FishingMini.Open(_gm, () => { _busy = false; RefreshStatus(); }); }

        // ── 150차: 텃밭(스타듀식 9칸) ─────────────────────────────────────
        string FarmTitle(int tile)
        {
            if (Save == null) return Loc.T("텃밭", "Garden");
            if (tile < 0) return Loc.T("텃밭 · 밭 칸으로 들어가자", "Garden · Step onto a plot");
            VillageFarm.Ensure(Save);
            if (VillageFarm.HasWeed(Save, tile)) return Loc.T("🌿 잡초 뽑기", "🌿 Pull weeds");
            var sd = VillageFarm.SeedOf(Save, tile);
            if (sd == null) return Loc.T("🌱 씨 뿌리기", "🌱 Plant seeds");
            if (VillageFarm.Bloomed(Save, tile)) return Loc.T($"{sd.Emoji} {sd.Name} 수확하기", $"{sd.Emoji} Harvest {sd.Name}");
            int g = Save.farm[tile].growth;
            if (VillageFarm.CanWater(Save, tile)) return Loc.T($"💧 물 주기 · {sd.Name} {g}/{sd.weeks}", $"💧 Water · {sd.Name} {g}/{sd.weeks}");
            return Loc.T($"{sd.Emoji} {sd.Name} 자라는 중 {g}/{sd.weeks} (물 줬음)", $"{sd.Emoji} {sd.Name} growing {g}/{sd.weeks} (watered)");
        }

        void FarmAct()
        {
            if (_busy || _hud.Locked || Save == null) return;
            int tile = VillageFarm.TileAt(_player.position);
            if (tile < 0) { CoastToast.Show(Loc.T("밭 칸 위에 서서 눌러 보자.", "Stand on a plot first.")); return; }
            VillageFarm.Ensure(Save);
            if (VillageFarm.HasWeed(Save, tile))
            {
                Swing(); VillageFarm.ClearWeed(Save, tile); Save.stats.stress = Mathf.Max(0, Save.stats.stress - 1);
                CoastToast.Show(Loc.T("🌿 잡초를 뽑았다. 이제 심을 수 있다.", "🌿 Weeds pulled. Ready to plant."));
                AfterFarm(); return;
            }
            var sd = VillageFarm.SeedOf(Save, tile);
            if (sd == null) { SeedMenu(tile); return; }
            if (VillageFarm.Bloomed(Save, tile))
            {
                Swing();
                bool ok = VillageFarm.Harvest(Save, tile, out var seed, out var gk, out var ge);
                if (ok) { CoastToast.Show(Loc.T($"{seed.Emoji} 수확! {gk} → 가방", $"{seed.Emoji} Harvested! {ge} → bag")); CoastAudioManager.PlayAnywhere(CoastSfx.Coin); StartCoroutine(HarvestPuff(tile, seed.petal)); }
                else CoastToast.Show(Loc.T($"{seed.Emoji} {seed.Name}이(가) 시들어 버렸다… (성공 {Mathf.RoundToInt(seed.chance * 100)}%)", $"{seed.Emoji} The {seed.Name} withered… ({Mathf.RoundToInt(seed.chance * 100)}%)"));
                AfterFarm(); return;
            }
            if (VillageFarm.CanWater(Save, tile))
            {
                VillageFarm.Water(Save, tile); StartCoroutine(WaterPour(tile));
                int g = Save.farm[tile].growth;
                CoastToast.Show(g >= sd.weeks ? Loc.T($"💧 물을 줬다 — {sd.Name} 다 자랐다! 수확하자", $"💧 Watered — {sd.Name} is ready!") : Loc.T($"💧 물을 줬다 — {sd.Name} {g}/{sd.weeks}. 페이즈마다 한 번, 주가 바뀌면 더 자란다", $"💧 Watered — {sd.Name} {g}/{sd.weeks}"));
                AfterFarm(); return;
            }
            CoastToast.Show(Loc.T("이번 페이즈엔 이미 물을 줬어. 다음 턴에!", "Already watered this phase."));
        }

        void SeedMenu(int tile)
        {
            var opts = new List<(string, Color, Action)>();
            foreach (var sd in HomeData.Seeds)
            {
                var seed = sd; bool can = Save.stats.money >= seed.price;
                opts.Add((Loc.T($"{seed.Emoji} {seed.ko} {seed.price}G · {seed.weeks}주 · 성공 {Mathf.RoundToInt(seed.chance * 100)}% · {seed.RewardText}", $"{seed.Emoji} {seed.en} {seed.price}G · {seed.weeks}w · {Mathf.RoundToInt(seed.chance * 100)}% · {seed.RewardText}"),
                    can ? new Color(0.45f, 0.78f, 0.55f) : new Color(0.6f, 0.6f, 0.65f),
                    () => { if (VillageFarm.Plant(Save, tile, seed)) { CoastToast.Show(Loc.T($"{seed.Emoji} {seed.ko} 씨를 뿌렸다. 물을 주자!", $"{seed.Emoji} Planted {seed.en}. Water it!")); AfterFarm(); } else CoastToast.Show(Loc.T("돈이 모자라.", "Not enough money.")); }));
            }
            _hud.Choice(Loc.T("씨 뿌리기", "Plant seeds"), Loc.T($"보유 {Save.stats.money:N0}G · 페이즈마다 물 한 번, 주가 바뀌면 한 단계 더 자란다.", $"{Save.stats.money:N0}G · water once per phase; grows more each week."), opts.ToArray());
        }

        void AfterFarm() { _gm.Persist(); RefreshStatus(); VillageWorld.BuildCrops(_world, Save); }

        IEnumerator WaterPour(int tile)
        {
            var c = VillageFarm.TileCenter(tile); float gy = VillageWorld.Height(c.x, c.z);
            var drops = new List<Transform>();
            for (int i = 0; i < 10; i++)
            {
                var d = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(d.GetComponent<Collider>()); d.transform.SetParent(transform, false);
                d.transform.position = _player.position + _player.forward * 0.6f + new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), 1.1f + i * 0.08f, UnityEngine.Random.Range(-0.4f, 0.4f));
                d.transform.localScale = Vector3.one * 0.09f; d.GetComponent<MeshRenderer>().sharedMaterial = CoastMaterials.CreateUnlit(new Color(0.55f, 0.80f, 1f));
                drops.Add(d.transform);
            }
            float t = 0f;
            while (t < 0.7f) { t += Time.deltaTime; foreach (var d in drops) { var p = d.position; p.y -= 2.4f * Time.deltaTime; if (p.y < gy + 0.12f) p.y = gy + 0.12f; d.position = p; } yield return null; }
            foreach (var d in drops) Destroy(d.gameObject);
        }
        IEnumerator HarvestPuff(int tile, Color col)
        {
            var c = VillageFarm.TileCenter(tile); float gy = VillageWorld.Height(c.x, c.z);
            for (int i = 0; i < 3; i++) StartCoroutine(DropOrange(new Vector3(c.x + UnityEngine.Random.Range(-0.6f, 0.6f), gy + 1.2f, c.z + UnityEngine.Random.Range(-0.5f, 0.5f)), col, 0.22f));
            yield return null;
        }

        // ── 150차: 도끼질(장작) · 난로 연료 · 줍기 ───────────────────────
        IEnumerator ChopTree(Transform tree, int idx)
        {
            _busy = true;
            if (Save != null && Save.villageChopWeek != Save.week) { Save.villageChopWeek = Save.week; Save.villageChopMask = 0; }
            int bit = 1 << Mathf.Clamp(idx, 0, 30);
            if (Save != null && (Save.villageChopMask & bit) != 0) { CoastToast.Show(Loc.T("이 나무는 이번 주에 이미 팼어. 다음 주에!", "Already chopped this tree this week.")); _busy = false; yield break; }
            var rest = tree.localRotation;
            for (int hit = 0; hit < 3; hit++)
            {
                Swing(); CoastAudioManager.PlayAnywhere(CoastSfx.SoftHit);
                float t = 0f; while (t < 0.35f) { t += Time.deltaTime; tree.localRotation = rest * Quaternion.Euler(Mathf.Sin(t * 50f) * 3f * (1f - t / 0.35f), 0f, Mathf.Sin(t * 41f) * 2f * (1f - t / 0.35f)); yield return null; }
                tree.localRotation = rest;
                // 나뭇조각
                for (int k = 0; k < 3; k++) StartCoroutine(DropOrange(tree.position + new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), 1.2f, UnityEngine.Random.Range(-0.5f, 0.5f)), new Color(0.72f, 0.55f, 0.35f), 0.12f));
                yield return new WaitForSeconds(0.15f);
            }
            if (Save != null)
            {
                Save.villageChopMask |= bit;
                int n = UnityEngine.Random.Range(2, 4); LifeItems.Add(Save, "mat_wood", n);
                for (int k = 0; k < n; k++) StartCoroutine(DropOrange(tree.position + new Vector3(UnityEngine.Random.Range(-1f, 1f), 1.6f, UnityEngine.Random.Range(-1f, 1f)), new Color(0.60f, 0.42f, 0.26f), 0.3f));
                Save.stats.stamina = Mathf.Min(PlayerStats.StatMax, Save.stats.stamina + 1);
                CoastToast.Show(Loc.T($"🪓 장작 +{n} (보유 {LifeItems.Count(Save, "mat_wood")}) · 체력 +1 — 우리집 난로에 넣자", $"🪓 Firewood +{n} ({LifeItems.Count(Save, "mat_wood")} total) · stamina +1"));
                CoastAudioManager.PlayAnywhere(CoastSfx.Coin); _gm.Persist(); RefreshStatus();
            }
            _busy = false;
        }

        void AddFuel()
        {
            if (Save == null) return;
            int have = LifeItems.Count(Save, "mat_wood");
            if (have <= 0) { _hud.Bubble(Loc.T("하늘", "Haneul"), Loc.T("장작이 없다. 도끼로 나무를 패거나 풀밭의 나뭇가지를 주워 오자.", "No firewood. Chop a tree or pick up branches.")); return; }
            int put = Mathf.Min(have, Mathf.Max(0, 8 - Save.villageFuel));
            if (put <= 0) { _hud.Bubble(Loc.T("하늘", "Haneul"), Loc.T("난로 옆 장작 더미가 꽉 찼다 (8주분).", "The woodpile is full (8 weeks).")); return; }
            LifeItems.Take(Save, "mat_wood", put); Save.villageFuel += put; _gm.Persist(); RefreshStatus();
            _hud.Bubble(Loc.T("하늘", "Haneul"), Loc.T($"장작 {put}개를 난로 옆에 쌓았다. 연료 {Save.villageFuel}주분 — 주마다 하나씩 타고, 집이 따뜻하면 컨디션 +3.", $"Stacked {put} logs. Fuel for {Save.villageFuel} weeks — one burns per week, +3 condition."));
        }

        List<VillagePickups.Pick> _picks;
        void BuildPickups()
        {
            _spots.RemoveAll(sp => sp.id.StartsWith("pick_"));
            _picks = VillagePickups.Build(_world, Save);
            foreach (var pk in _picks)
            {
                var pick = pk;
                _spots.Add(new Spot { id = "pick_" + pick.idx, title = VillagePickups.Title(pick.kind), pos = pick.pos, radius = 1.5f, on = () => TakePick(pick) });
            }
        }
        void TakePick(VillagePickups.Pick pk)
        {
            if (_busy || _hud.Locked || Save == null || pk.go == null) return;
            VillagePickups.Take(Save, pk.idx); LifeItems.Add(Save, VillagePickups.ItemId(pk.kind), 1);
            _spots.RemoveAll(sp => sp.id == "pick_" + pk.idx);
            StartCoroutine(PopPick(pk.go)); pk.go = null;
            if (_motion != null) _motion.Hop();
            CoastToast.Show(VillagePickups.Got(pk.kind)); CoastAudioManager.PlayAnywhere(CoastSfx.Coin);
            _gm.Persist(); RefreshStatus();
        }
        IEnumerator PopPick(GameObject go)
        {
            float t = 0f; var p0 = go.transform.position;
            while (t < 0.35f) { t += Time.deltaTime; go.transform.position = p0 + Vector3.up * t * 2.2f; go.transform.localScale = Vector3.one * (1f - t / 0.35f); yield return null; }
            Destroy(go);
        }

        // ── 나무 흔들기 ──────────────────────────────────────────────────
        IEnumerator ShakeTree(Transform tree, int idx)
        {
            _busy = true;
            var rest = tree.localRotation; float t = 0f;
            while (t < 0.9f) { t += Time.deltaTime; tree.localRotation = rest * Quaternion.Euler(Mathf.Sin(t * 40f) * 4f * (1f - t / 0.9f), 0f, Mathf.Sin(t * 33f) * 3f * (1f - t / 0.9f)); yield return null; }
            tree.localRotation = rest;
            if (Save != null)
            {
                if (Save.villageTreeWeek != Save.week) { Save.villageTreeWeek = Save.week; Save.villageTreeMask = 0; }
                int bit = 1 << Mathf.Clamp(idx, 0, 30);
                if ((Save.villageTreeMask & bit) != 0) CoastToast.Show(Loc.T("이 나무는 이번 주에 이미 털었어. 다음 주에!", "Already shook this tree this week."));
                else
                {
                    Save.villageTreeMask |= bit;
                    if (tree.name.Contains("Palm"))
                    {
                        // 150차: 야자수 → 코코넛(채집 아이템)
                        int cn = UnityEngine.Random.Range(1, 3); LifeItems.Add(Save, "gath_coconut", cn);
                        for (int i = 0; i < cn; i++) StartCoroutine(DropOrange(tree.position + new Vector3(UnityEngine.Random.Range(-0.8f, 0.8f), 3.4f, UnityEngine.Random.Range(-0.8f, 0.8f)), new Color(0.45f, 0.30f, 0.16f), 0.34f));
                        CoastToast.Show(Loc.T($"🥥 코코넛 {cn}개가 떨어졌다 → 가방", $"🥥 {cn} coconut(s) fell → bag"));
                        CoastAudioManager.PlayAnywhere(CoastSfx.Coin); _gm.Persist(); RefreshStatus(); _busy = false; yield break;
                    }
                    int n = UnityEngine.Random.Range(2, 5); int coins = n * UnityEngine.Random.Range(12, 20);
                    for (int i = 0; i < n; i++) StartCoroutine(DropOrange(tree.position + new Vector3(UnityEngine.Random.Range(-1.2f, 1.2f), 2.6f, UnityEngine.Random.Range(-1.2f, 1.2f))));
                    Save.stats.money += coins; Save.stats.stress = Mathf.Max(0, Save.stats.stress - 1);
                    if (UnityEngine.Random.value < 0.35f) { LifeItems.Add(Save, "ing_veg", 1); CoastToast.Show(Loc.T($"🍊 귤 {n}개 → {coins}G · 재료 채소 +1", $"🍊 {n} oranges → {coins}G · Veg +1")); }
                    else CoastToast.Show(Loc.T($"🍊 귤 {n}개가 떨어졌다 → {coins}G", $"🍊 {n} oranges fell → {coins}G"));
                    CoastAudioManager.PlayAnywhere(CoastSfx.Coin); _gm.Persist(); RefreshStatus();
                }
            }
            _busy = false;
        }

        IEnumerator DropOrange(Vector3 from) { return DropOrange(from, new Color(1f, 0.60f, 0.15f), 0.28f); }
        IEnumerator DropOrange(Vector3 from, Color col, float size)
        {
            var o = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(o.GetComponent<Collider>()); o.transform.SetParent(transform, false);
            o.transform.position = from; o.transform.localScale = Vector3.one * size;
            o.GetComponent<MeshRenderer>().sharedMaterial = CoastMaterials.CreateLit(col);
            float gy = VillageWorld.Height(from.x, from.z) + 0.14f; float vy = 0f, t = 0f;
            while (t < 1.6f) { t += Time.deltaTime; vy -= 9.8f * Time.deltaTime; var p = o.transform.position; p.y += vy * Time.deltaTime; if (p.y < gy) { p.y = gy; vy = -vy * 0.35f; } o.transform.position = p; yield return null; }
            t = 0f; while (t < 0.3f) { t += Time.deltaTime; o.transform.localScale = Vector3.one * size * (1f - t / 0.3f); yield return null; }
            Destroy(o);
        }

        // ── 마당 가구 배치 ───────────────────────────────────────────────
        public struct YardDef { public string id, ko, en, model; public int price; }
        public static readonly YardDef[] Yard = {
            new YardDef { id = "bench", ko = "벤치", en = "Bench", model = "Prop_Bench", price = 300 },
            new YardDef { id = "planter", ko = "나무 화분", en = "Planter", model = "Kerb_PlanterWood", price = 150 },
            new YardDef { id = "basalt", ko = "현무암 화단", en = "Basalt bed", model = "Kerb_PlanterBasalt", price = 220 },
            new YardDef { id = "onggi", ko = "항아리", en = "Onggi jar", model = "Kerb_Onggi", price = 200 },
            new YardDef { id = "buoy", ko = "부표 더미", en = "Buoys", model = "Kerb_Buoys", price = 120 },
            new YardDef { id = "crate", ko = "상자 더미", en = "Crates", model = "Kerb_CrateStack", price = 100 },
            new YardDef { id = "hareubang", ko = "돌하르방", en = "Hareubang", model = "Prop_Hareubang", price = 500 },
            new YardDef { id = "cafe", ko = "카페 세트", en = "Cafe set", model = "Prop_CafeSet", price = 800 },
            new YardDef { id = "stall", ko = "귤 매대", en = "Orange stall", model = "Prop_OrangeStall", price = 600 },
            new YardDef { id = "palm", ko = "야자수", en = "Palm", model = "Prop_Palm", price = 400 },
            new YardDef { id = "orange", ko = "귤나무", en = "Orange tree", model = "Prop_OrangeTree", price = 450 },
            new YardDef { id = "pavilion", ko = "정자", en = "Pavilion", model = "Prop_Pavilion", price = 1500 },
        };
        static YardDef? FindYard(string id) { foreach (var y in Yard) if (y.id == id) return y; return null; }
        static bool InYard(Vector3 p) => Mathf.Abs(p.x) < 9.5f && p.z > 25f && p.z < 33.2f;

        void BuildYardItems()
        {
            if (_yardHost != null) Destroy(_yardHost.gameObject);
            _yardHost = new GameObject("Yard").transform; _yardHost.SetParent(_world, false);
            if (Save == null || Save.yardItems == null) return;
            foreach (var s in Save.yardItems)
            {
                var parts = s.Split('|'); if (parts.Length < 4) continue;
                var def = FindYard(parts[0]); if (def == null) continue;
                float x = float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture), z = float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture), rot = float.Parse(parts[3], System.Globalization.CultureInfo.InvariantCulture);
                var go = VillageWorld.SpawnScaled(def.Value.model, _yardHost, x, z, rot);
                if (go != null) { go.name = "Y_" + def.Value.id; VillageWorld.AddCollider(go); BuildingOutline.Attach(go.transform, 0.03f); }
            }
        }

        void SetYard(bool on)
        {
            _yard = on; _yardPick = null; _yardRot = 0f;
            if (_ghost != null) { Destroy(_ghost.gameObject); _ghost = null; }
            if (_yardCanvas != null) { Destroy(_yardCanvas.gameObject); _yardCanvas = null; }
            if (!on) { RefreshStatus(); return; }
            // 마당으로 순간이동(집 앞)
            if (!InYard(_player.position)) Teleport(new Vector3(0f, 0f, 29f));
            _yardCanvas = CoastUiCanvas.Create("YardCanvas", 125);
            var root = CoastUiCanvas.Root(_yardCanvas);
            var bar = CoastUiArt.CutePill(root, "Bar", new Color(0.36f, 0.30f, 0.52f, 0.94f), 22, 4); bar.raycastTarget = true;
            var brt = bar.rectTransform; brt.anchorMin = new Vector2(0f, 1f); brt.anchorMax = new Vector2(1f, 1f); brt.pivot = new Vector2(0.5f, 1f);
            brt.anchoredPosition = new Vector2(0f, -96f); brt.sizeDelta = new Vector2(-16f, 196f);   // 위쪽(알약 아래) — 발밑의 고스트가 가려지지 않게
            _yardHint = CoastHudLayout.MakeText(brt, "H", Loc.T("🪑 마당 꾸미기 — 가구를 고르고 걸어가서 「놓기」. 산 가구는 치우면 반값 환불.", "🪑 Yard — pick an item, walk, then 「Place」. Removing refunds half."), 15, TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(14f, -40f), new Vector2(-14f, -6f));
            _yardHint.color = Color.white; _yardHint.horizontalOverflow = HorizontalWrapMode.Wrap; _yardHint.verticalOverflow = VerticalWrapMode.Truncate;
            _yardHint.resizeTextForBestFit = true; _yardHint.resizeTextMinSize = 10; _yardHint.resizeTextMaxSize = CoastHudLayout.Scaled(15);
            // 가로 스크롤 트레이
            var vp = new GameObject("VP", typeof(RectTransform), typeof(Image), typeof(Mask)).GetComponent<RectTransform>(); vp.SetParent(brt, false);
            vp.anchorMin = new Vector2(0f, 0f); vp.anchorMax = new Vector2(1f, 1f); vp.offsetMin = new Vector2(10f, 60f); vp.offsetMax = new Vector2(-10f, -44f);
            vp.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.06f); vp.GetComponent<Mask>().showMaskGraphic = true;
            var sc = vp.gameObject.AddComponent<ScrollRect>(); sc.horizontal = true; sc.vertical = false; sc.movementType = ScrollRect.MovementType.Clamped;
            _yardTray = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>(); _yardTray.SetParent(vp, false);
            _yardTray.anchorMin = new Vector2(0f, 0f); _yardTray.anchorMax = new Vector2(0f, 1f); _yardTray.pivot = new Vector2(0f, 0.5f); _yardTray.offsetMin = _yardTray.offsetMax = Vector2.zero;
            sc.content = _yardTray; sc.viewport = vp;
            float x = 6f;
            for (int i = 0; i < Yard.Length; i++)
            {
                var d = Yard[i];
                var chip = CoastUiArt.CutePill(_yardTray, "C_" + d.id, new Color(0.96f, 0.93f, 1f), 14, 3); chip.raycastTarget = true;
                var crt = chip.rectTransform; crt.anchorMin = new Vector2(0f, 0f); crt.anchorMax = new Vector2(0f, 1f); crt.pivot = new Vector2(0f, 0.5f);
                crt.anchoredPosition = new Vector2(x, 0f); crt.sizeDelta = new Vector2(112f, -4f);
                var nm = CoastHudLayout.MakeText(crt, "N", Loc.T(d.ko, d.en), 15, TextAnchor.UpperCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(4f, -46f), new Vector2(-4f, -8f));
                nm.color = new Color(0.30f, 0.25f, 0.45f); nm.fontStyle = FontStyle.Bold; nm.resizeTextForBestFit = true; nm.resizeTextMinSize = 10; nm.resizeTextMaxSize = CoastHudLayout.Scaled(15);
                var pr = CoastHudLayout.MakeText(crt, "P", d.price + "G", 15, TextAnchor.LowerCenter, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(4f, 6f), new Vector2(-4f, 34f));
                pr.color = new Color(0.65f, 0.45f, 0.15f); pr.fontStyle = FontStyle.Bold;
                var b = chip.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None; var cap = d.id;
                b.onClick.AddListener(() => { CoastPrefs.Vibrate(); PickYard(cap); });
                x += 118f;
            }
            _yardTray.sizeDelta = new Vector2(x + 6f, 0f);
            // 아래 버튼: 회전 · 치우기 · 끝내기
            Btn(brt, "Rot", Loc.T("↻ 회전", "↻ Rotate"), new Color(0.55f, 0.75f, 0.98f), new Vector2(0.18f, 0f), () => { _yardRot += 45f; });
            Btn(brt, "Del", Loc.T("🗑 치우기", "🗑 Remove"), new Color(0.98f, 0.62f, 0.62f), new Vector2(0.5f, 0f), RemoveNearestYard);
            Btn(brt, "End", Loc.T("✔ 끝내기", "✔ Done"), new Color(0.45f, 0.78f, 0.55f), new Vector2(0.82f, 0f), () => SetYard(false));
            _hud.SetAction(Loc.T("놓기", "Place"));
        }

        static void Btn(RectTransform parent, string name, string label, Color col, Vector2 anchor, Action on)
        {
            var b = CoastUiArt.GlossyPill(parent, name, col, 18, 6); b.raycastTarget = true;
            var rt = b.rectTransform; rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f, 0f); rt.anchoredPosition = new Vector2(0f, 8f); rt.sizeDelta = new Vector2(190f, 46f);
            var t = CoastHudLayout.MakeText(rt, "T", label, 18, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(6f, 2f), new Vector2(-6f, 0f));
            t.color = Color.white; t.fontStyle = FontStyle.Bold; t.resizeTextForBestFit = true; t.resizeTextMinSize = 11; t.resizeTextMaxSize = CoastHudLayout.Scaled(18);
            var btn = b.gameObject.AddComponent<Button>(); btn.transition = Selectable.Transition.None; btn.onClick.AddListener(() => { CoastPrefs.Vibrate(); on?.Invoke(); });
        }

        void PickYard(string id)
        {
            _yardPick = id;
            if (_ghost != null) Destroy(_ghost.gameObject);
            var def = FindYard(id); if (def == null) return;
            var go = VillageWorld.SpawnScaled(def.Value.model, _world, _player.position.x, _player.position.z, 0f);
            if (go == null) { CoastToast.Show(Loc.T("모델이 없어 놓을 수 없어.", "Model missing.")); return; }
            _ghost = go.transform; go.name = "Ghost"; _ghostLift = go.transform.position.y - VillageWorld.Height(_player.position.x, _player.position.z);
            foreach (var r in go.GetComponentsInChildren<Renderer>()) { var m = CoastMaterials.CreateTransparent(new Color(0.5f, 0.9f, 1f, 0.55f)); var arr = r.sharedMaterials; for (int i = 0; i < arr.Length; i++) arr[i] = m; r.sharedMaterials = arr; }
            foreach (var c in _yardTray.GetComponentsInChildren<Image>()) if (c.name.StartsWith("C_")) c.color = c.name == "C_" + id ? new Color(1f, 0.85f, 0.45f) : new Color(0.96f, 0.93f, 1f);
            if (_yardHint != null) _yardHint.text = Loc.T($"{def.Value.ko} {def.Value.price}G — 걸어가서 자리를 잡고 「놓기」", $"{def.Value.en} {def.Value.price}G — walk to a spot, then 「Place」");
        }

        Vector3 GhostPos => _player.position + _player.forward * 1.6f;
        void UpdateYard()
        {
            if (_ghost == null) return;
            var p = GhostPos; p.y = VillageWorld.Height(p.x, p.z) + _ghostLift;
            _ghost.position = p; _ghost.rotation = Quaternion.Euler(0f, _yardRot, 0f);
        }

        void PlaceGhost()
        {
            if (_ghost == null || _yardPick == null) { CoastToast.Show(Loc.T("아래 트레이에서 가구를 먼저 골라.", "Pick an item from the tray first.")); return; }
            var def = FindYard(_yardPick).Value; var p = GhostPos;
            if (!InYard(p)) { CoastToast.Show(Loc.T("마당 안(집 앞 잔디)에만 놓을 수 있어.", "Only inside the yard (lawn in front of the house).")); return; }
            foreach (Transform c in _yardHost) if (Vector3.Distance(new Vector3(c.position.x, 0f, c.position.z), new Vector3(p.x, 0f, p.z)) < 1.3f) { CoastToast.Show(Loc.T("다른 가구와 너무 가까워.", "Too close to another item.")); return; }
            if (Save.stats.money < def.price) { CoastToast.Show(Loc.T($"돈이 모자라. {def.price}G 필요.", $"Not enough coins — need {def.price}G.")); return; }
            Save.stats.money -= def.price;
            var list = new List<string>(Save.yardItems ?? new string[0]);
            list.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}|{1:0.00}|{2:0.00}|{3:0}", def.id, p.x, p.z, _yardRot));
            Save.yardItems = list.ToArray(); _gm.Persist();
            BuildYardItems(); RefreshStatus();
            CoastAudioManager.PlayAnywhere(CoastSfx.Purchase); CoastPrefs.VibrateEvent();
            CoastToast.Show(Loc.T($"{def.ko}를 놓았다! (−{def.price}G)", $"Placed {def.en}! (−{def.price}G)"));
        }

        void RemoveNearestYard()
        {
            if (Save == null || Save.yardItems == null || Save.yardItems.Length == 0) { CoastToast.Show(Loc.T("치울 가구가 없어.", "Nothing to remove.")); return; }
            int best = -1; float bd = 2.6f; var p = _player.position;
            for (int i = 0; i < Save.yardItems.Length; i++)
            {
                var parts = Save.yardItems[i].Split('|'); if (parts.Length < 4) continue;
                float x = float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture), z = float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture);
                float d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(x, z)); if (d < bd) { bd = d; best = i; }
            }
            if (best < 0) { CoastToast.Show(Loc.T("가까이 있는 가구가 없어. 가구 옆으로 가서 눌러.", "No item nearby — stand next to one.")); return; }
            var def = FindYard(Save.yardItems[best].Split('|')[0]);
            var list = new List<string>(Save.yardItems); list.RemoveAt(best); Save.yardItems = list.ToArray();
            int refund = def != null ? def.Value.price / 2 : 0; Save.stats.money += refund; _gm.Persist();
            BuildYardItems(); RefreshStatus();
            CoastToast.Show(Loc.T($"치웠다. +{refund}G 환불", $"Removed. +{refund}G refunded"));
        }

        // ── 상태 알약 ─────────────────────────────────────────────────────
        public static void RefreshStatus() { if (I != null) I.RefreshStatusInner(); }
        void RefreshStatusInner()
        {
            if (_hud == null || Save == null) return;
            string[] clocks = { Loc.T("오전 09:00", "AM 09:00"), Loc.T("오후 02:30", "PM 02:30"), Loc.T("저녁 06:00", "PM 06:00") };
            var season = Timeline.SeasonOf(Save.week);
            string weather = season == SeasonKind.Winter ? Loc.T("흐림", "Cloudy") : season == SeasonKind.Autumn ? Loc.T("바람", "Breezy") : Loc.T("맑음", "Sunny");
            string clock = clocks[Mathf.Clamp(Save.phaseIndex, 0, 2)] + " " + weather;
            string vil = Loc.T($"마을: 하늘 바닷가 마을  Lv.{Save.level}  ·  {Save.week}주차 {Timeline.SeasonName(season)}", $"Village: Haneul Seaside  Lv.{Save.level}  ·  Week {Save.week} {Timeline.SeasonName(season)}");
            _hud.SetStatus(Save.stats.stamina, PlayerStats.StatMax, Save.stats.money, clock, vil);
        }
    }

    /// 149차 진단: CharacterController 가 마지막으로 부딪힌 콜라이더 이름.
    public class CcHitLog : MonoBehaviour
    {
        public string Last = "-"; public float At;
        void OnControllerColliderHit(ControllerColliderHit h) { if (h.collider != null && h.normal.y < 0.7f) { Last = h.collider.name + "@" + h.collider.transform.parent?.name; At = Time.time; } }
    }
}
