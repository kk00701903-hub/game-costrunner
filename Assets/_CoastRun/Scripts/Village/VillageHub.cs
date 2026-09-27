using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun.Village
{
    /// 136차(사용자): 스토리 모드 허브 = 포켓캠프풍 바닷가 마을. 조이스틱 이동·충돌·나무 흔들기·낚시·상점/가방·텃밭·가구(마당) 배치.
    /// 송전탑 언덕 → 기존 주차 스케줄(TamaRaisingUI), 엄마 집 → 펫 돌보기·잠자기, 구멍가게 → ShopUI, 텃밭 → HomeUI 텃밭 탭, 해변 → FishingMini.
    public partial class VillageHub : MonoBehaviour
    {
        public static bool Enabled = true;
        public static VillageHub I { get; private set; }

        class Spot { public string id, title; public Vector3 pos; public float radius; public Action on; }

        GameManager _gm; RaisingSceneDriver _driver;
        Transform _world, _player, _rigT, _yardHost; CharacterController _cc; SkaterRig _rig; Animator _anim; Camera _cam; CharacterMotion _motion; Vector3 _vel, _acc;
        VillageHud _hud; readonly List<Spot> _spots = new List<Spot>(); Spot _near; Transform _nearTree; int _nearTreeIdx = -1;
        bool _moving; float _idleT; Vector3 _camVel; bool _busy; bool _yard; string _yardPick; float _yardRot; Transform _ghost; Canvas _yardCanvas; RectTransform _yardTray; Text _yardHint;
        VillageCreatures _creatures; VillageMap _map; bool _bat, _axe, _pick, _rod, _road; /* 150차: 도끼(장작) · 154차: 곡괭이(돌) · 168차: 낚싯대 */ Transform _nearRock; int _nearRockIdx = -1; float _doorCooldown; Transform _toolVis;

        SaveData Save => _gm != null ? _gm.Save : null;
        public SaveData SaveRef => Save;   // 171차: VillageRequest 용
        public void PersistRefresh() { if (_gm != null) _gm.Persist(); RefreshStatus(); }
        public void ShowChoice(string title, string sub, (string, Color, Action)[] items) { _hud.Choice(title, sub, items); }
        public string NpcName(int i) => _creatures != null ? _creatures.NpcName(i) : "";
        public Vector3 NpcPos(int i) => _creatures != null ? _creatures.NpcPos(i) : _player.position;   // 172차
        public void NpcSay(int i, string line) { if (_creatures != null) _creatures.NpcSay(i, line); }

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
            // 162차: 오른쪽 반 화면 — 드래그로 카메라 회전(0.32°/px), 톡이면 행동(휘두르기·잡기·들어가기…)
            if (_hud.CamPad != null)
            {
                _hud.CamPad.OnOrbit = dx => { if (_busy || _hud.Locked) return; _camYawTarget += dx * 0.32f; _camYaw += dx * 0.32f; _backT = 0f; _camYawVel = 0f; };
                _hud.CamPad.OnTap = OnPadTap; if (_hud.JoyStick != null) _hud.JoyStick.OnTapAt = p => TryTalkAt(p);   // 187차: 마을 사람을 탭하면 대화, 아니면 행동
            }
            _drops = VillageDrops.Create(transform, _player, () => _busy || _hud.Locked || _interior != null, OnAbsorb);   // 159차
            _creatures = VillageCreatures.Create(transform, _player);
            _creatures.Locked = () => _busy || _hud.Locked || StoryCalm;   // 196차: 튜토리얼 동안 벌레·산적 멈춤
            _creatures.OnSpiritHit = SpiritHit;
            _creatures.OnGhostHit = GhostHit;   // 155차
            _creatures.OnBanditHit = BanditHit;   // 171차
            _creatures.RequestMark = i => VillageRequest.MarkFor(Save, i);
            // 175차(사용자: 「포켓몬스터처럼 펫을 몬스터로 잡는 것도 — 밤에만 5%」): 잡으면 펫 상점과 같은 취급으로 소유에 넣는다
            _creatures.OnMonsterCaught = pet =>
            {
                if (Save == null) return;
                var k = (PetKind)Mathf.Clamp(pet, 1, 4);
                string nm = PetCompanion.Names != null && (int)k < PetCompanion.Names.Length ? PetCompanion.Names[(int)k] : k.ToString();
                if (PetShop.Owns(Save, k))
                {
                    Save.starShards += 3; Save.stats.money += 150; _gm.Persist(); RefreshStatus();
                    CoastToast.Show(Loc.T($"✨ 몬스터를 잡았다! 이미 있는 {nm} — 별조각 +3 · 150G", $"✨ Caught a monster! Already own {k} — shards +3 · 150G"));
                    return;
                }
                Save.ownedPetMask |= 1 << (int)k;
                if (Save.equippedPet == PetKind.None) Save.equippedPet = k;
                _gm.Persist(); RefreshStatus();
                CoastToast.Show(Loc.T($"🎉 몬스터를 잡았다! 펫 「{nm}」 을 얻었다 — 펫 상점에서 장착", $"🎉 Caught a monster! Got the pet 「{k}」 — equip it at the pet shop"));
            };
            _creatures.OnBanditGroupCleared = () => { if (Save == null) return; Save.stats.money += 120; _gm.Persist(); RefreshStatus(); CoastToast.Show(Loc.T("🎉 산적 무리를 다 물리쳤다! 보너스 +120G", "🎉 Bandit gang cleared! Bonus +120G")); };
            _dayNight = VillageDayNight.Create(_world, Save); _dayNight.OnMinute = () => RefreshStatus();
            _creatures.OnCaught = (kind, shards, coins) =>
            {
                if (Save == null) return; Save.starShards += shards; Save.starShardsTotal += shards; Save.stats.money += coins;
                if (kind == "chicken" || kind == "rabbit")
                {
                    // 171차: 야생 닭·토끼 → 농장 울타리 안으로(가득이면 놓아준다)
                    bool ok = kind == "chicken" ? VillageLivestock.AddChicken(Save) : VillageLivestock.AddRabbit(Save);
                    if (ok) { VillageLivestock.Build(_world, Save); MissionTick(kind == "rabbit" ? VillageMission.Kind.Rabbit : VillageMission.Kind.Bug); CoastToast.Show(kind == "chicken" ? Loc.T($"🐔 닭을 잡았다! 농장으로 보냈다 (닭 {Save.farmChickens}/{VillageLivestock.Cap}) — 아침마다 달걀", $"🐔 Caught a chicken! Sent to the farm ({Save.farmChickens}/{VillageLivestock.Cap})") : Loc.T($"🐰 토끼를 잡았다! 농장으로 보냈다 (토끼 {VillageLivestock.Rabbits(Save)}/{VillageLivestock.Cap}) — 3밤 자면 다 큰다", $"🐰 Caught a rabbit! Sent to the farm ({VillageLivestock.Rabbits(Save)}/{VillageLivestock.Cap})")); }
                    else CoastToast.Show(Loc.T("농장 울타리가 가득이다. 놓아줬다.", "The farm is full. Let it go."));
                }
                else if (kind == "bandit") MissionTick(VillageMission.Kind.Bandit);
                else if (kind == "crab" || kind == "hermit") { LifeItems.Add(Save, "bug_" + kind, 1); Save.bugsCaught++; MissionTick(VillageMission.Kind.Crab); }
                else if (kind == "kill") { }   // 198차: 방망이로 처치(가방 X)
                else if (kind != "spirit") { LifeItems.Add(Save, "bug_" + kind, 1); Save.bugsCaught++; MissionTick(VillageMission.Kind.Bug); }
                _gm.Persist(); RefreshStatus(); CoastAudioManager.PlayAnywhere(CoastSfx.Coin);
            };   // 150차: 벌레는 가방(채집)에도
            _map = VillageMap.Create(_hud.Root, _player);
            ApplyTool();
            BuildSpots();
            InitAutoFeatures();   // 183차: 자동이동·자동사냥·말/경찰·땅/월세·팔기
            EnsureMission();   // 160차
            _doorCooldown = Time.time + 2f;   // 156차: 문 앞에서 시작해도 바로 들어가지 않게
            RefreshStatus();
            TitleAudio.PlayRaising();
            if (_gm != null && _gm.PendingVillageEvent != null) StartCoroutine(MorningEvent());   // 156차
            InitStory();   // 196차: 송전탑 위 기상 도입 · 이야기 장소
            InitFun205();   // 205차: 하루 정산 기준점 · 생일 알림
            CoastToast.Show(Loc.T("조이스틱으로 걸어다니고, 가까이 가면 뜨는 분홍 버튼으로 들어가자!", "Walk with the joystick — tap the pink prompt to enter a place!"));
        }

        bool _quitting;
        void OnDestroy() { _quitting = true; RestoreHidden(); RestoreHouseFades(); if (_drops != null && Save != null) _drops.AbsorbAll(); if (I == this) I = null; RenderSettings.fog = false; VillagePalette.ApplySoftLook(false); Shader.SetGlobalVector("_CoastCurveRadial", Vector4.zero); if (_hud != null) Destroy(_hud.gameObject); if (_yardCanvas != null) Destroy(_yardCanvas.gameObject); }

        // ── 플레이어 ─────────────────────────────────────────────────────
        void BuildPlayer()
        {
            var go = new GameObject("VillagePlayer"); _player = go.transform; _player.SetParent(transform, false);
            var start = Save != null && Save.villageX != 0f ? new Vector3(Save.villageX, 0f, Save.villageZ) : new Vector3(0f, 0f, 22f);
            if (Mathf.Abs(start.x - VillageInterior.OX) < 30f && Mathf.Abs(start.z - VillageInterior.OZ) < 30f) start = new Vector3(0f, 0f, 30f);   // 157차: 옛 세이브가 집 안 좌표면 마당에서
            // 157차: 저장 위치가 문 앞(자동 입장 반경)이면 문에서 1.6 m 물러난 자리에서 시작 — 켜자마자 들어가지 않게
            foreach (var hs in VillageWorld.Houses) { if (hs.house == null) continue; if (Vector3.Distance(new Vector3(start.x, 0f, start.z), new Vector3(hs.door.x, 0f, hs.door.z)) < 1.3f) { var f = hs.house.forward; start = new Vector3(hs.door.x + f.x * 1.6f, 0f, hs.door.z + f.z * 1.6f); break; } }
            if (VillageWorld.Shop != null) { var sd = VillageWorld.Shop.TransformPoint(new Vector3(0f, 0f, 2.6f)); if (Vector3.Distance(new Vector3(start.x, 0f, start.z), new Vector3(sd.x, 0f, sd.z)) < 1.3f) { var f = VillageWorld.Shop.forward; start = new Vector3(sd.x + f.x * 1.6f, 0f, sd.z + f.z * 1.6f); } }
            start.y = VillageWorld.Height(start.x, start.z);
            _player.position = start; _player.rotation = Quaternion.Euler(0f, 180f, 0f);
            _cc = go.AddComponent<CharacterController>(); _cc.height = 1.2f; _cc.radius = 0.36f;   // 159차: 몸이 소품에 덜 파고들게
            // 181차(사용자: 「바닷가·언덕 근처에서 옆으로 밀림 / 바닷가 장애물 통과」): 아래 세 줄이 159차부터 위 줄 주석 안에 딸려 들어가 **한 번도 적용되지 않았다**.
            // 그래서 캡슐 중심이 발(0)에 있어 몸 절반(−0.6~0 m)이 땅속에 묻혀 있었고 — 언덕·바닷가처럼 반쯤 묻힌 둥근 바위 옆에서 땅속 반구가
            // 바위 아랫면을 타고 옆으로 미끄러지고, 땅 위로는 0.6 m 만 남아 낮은 바위는 그냥 넘어가거나 통과했다.
            _cc.center = new Vector3(0f, 0.62f, 0f); _cc.slopeLimit = 50f; _cc.stepOffset = 0.28f;
            go.AddComponent<CcHitLog>();   // 149차 진단: 마지막으로 부딪힌 콜라이더
            var rigHost = new GameObject("Rig").transform; rigHost.SetParent(_player, false); _rigT = rigHost;
            _rig = SkaterRig.Spawn(rigHost, 1.25f, true);
            // 141차: 절차 모션(바운스·기울기·스쿼시·발먼지·고개)
            _motion = rigHost.gameObject.AddComponent<CharacterMotion>(); _motion.WalkSpeed = 2.46f; _motion.RunSpeed = 5.46f;   // 159차: +20%
            if (_rig != null)
            {
                _anim = _rig.GetComponent<Animator>(); if (_anim != null) { _anim.SetBool("Grounded", true); _anim.SetFloat("Speed", 0f); _anim.Play("Run", 0, 0.12f); _anim.speed = 0f; }
                foreach (var smr in _rig.GetComponentsInChildren<SkinnedMeshRenderer>()) if (smr.GetComponent<CelOutlineHint>() == null) smr.gameObject.AddComponent<CelOutlineHint>();   // 138차: 잉크 윤곽
                StartCoroutine(AttachFace());   // 155차: 클링 생성 얼굴 데칼(큰 눈·홍조·미소)
            }
            else CoastFigureMesh.BuildHaneul(rigHost, 1.25f);
            // 발밑 그림자
GroundBlob.Attach(_player, 0.5f, 0.36f, rigHost);   // 146차: 접지 블롭
        }

        void BuildCamera()
        {
            _cam = Camera.main;
            if (_cam == null) { var go = new GameObject("Main Camera"); go.tag = "MainCamera"; _cam = go.AddComponent<Camera>(); go.AddComponent<AudioListener>(); }
            _cam.orthographic = false; _cam.fieldOfView = 50f; _cam.nearClipPlane = 0.2f; _cam.farClipPlane = 420f;   // 161차: FOV 46→50(배경 넓게), 근평면 0.2(벽 안으로 당겨질 때 잘림 최소) · 146차: 피치 ≈20° + 방사형 곡면(CurveK)으로 디오라마
            _cam.clearFlags = CameraClearFlags.SolidColor; _cam.backgroundColor = new Color(0.68f, 0.85f, 0.98f);
            if (_cam.GetComponent<CoastPortraitViewport>() == null) _cam.gameObject.AddComponent<CoastPortraitViewport>();
            var camData = _cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>() ?? _cam.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true; camData.antialiasing = UnityEngine.Rendering.Universal.AntialiasingMode.SubpixelMorphologicalAntiAliasing; camData.antialiasingQuality = UnityEngine.Rendering.Universal.AntialiasingQuality.High;
            SnapCamera();
        }

        // 140차(시안): 주인공 뒤에서 낮게 따라가는 카메라 — 바다 쪽을 보면 수평선·등대·마을이 한눈에
        float _camYaw, _camYawVel;
        float _swingK, _camYawTarget, _backT, _stickYaw; bool _stickHeld;
        float _camDodge, _camDodgeTarget, _camDodgeT, _camLowK, _camLowWant, _camHighK, _camHighWant;   // 171차: 건물에 막히면 빈 쪽으로 비켜 도는 각도 / 어느 쪽도 안 비면 낮게 바짝 붙기(지붕·차양 아래)
        public const float CurveK = 0.0018f;   // 방사형 곡률(m/m²): 40 m 에서 2.9 m, 60 m 에서 6.5 m 아래로 — 피치 20°·FOV 46 에서 수평선이 화면 31% 에 온다   // 141차: 크게 방향을 바꿀 때 카메라가 바깥 큰 원을 돌지 않게 — 가까이·높게 붙었다가 다시 멀어진다
        // 161차(사용자: 「캐릭터가 화면을 너무 차지 — 배경 넓게, 주인공 작게」): 뒤 8.8→12.6 m · 높이 4.5→6.4 m(피치 ≈ 같음) · 시선 1.6→2.4 m 앞.
        // 주인공은 화면 높이의 ≈14%(전엔 ≈20%), 폰 세로 화면에서 앞길이 2.4 배 더 보인다. FOV 는 BuildCamera 에서 46→50.
        public static float CamBack = 12.6f, CamHeight = 6.4f, CamAhead = 2.4f;
        Vector3 CamTarget => CamTargetAt(_camYaw + _camDodge);
        Vector3 CamTargetAt(float yaw)
        {
            float hgt = Mathf.Lerp(Mathf.Lerp(CamHeight, CamHeight + 1.4f, _swingK), 3.4f, _camLowK);
            float back = Mathf.Lerp(Mathf.Lerp(-CamBack, -5.2f, _swingK), -7.2f, _camLowK);
            // 172차: 벽에 바짝 붙어 어느 각도도 못 쓰면(집 문 앞 등) 위에서 내려다본다 — 벽 텍스처가 화면을 덮는 것보다 낫다
            hgt = Mathf.Lerp(hgt, 9.5f, _camHighK); back = Mathf.Lerp(back, -4.5f, _camHighK);
            if (_dun != null && _dun.Active && VillageDungeon.Contains(_player.position)) { hgt = 11f; back = -8.5f; }   // 199차: 갱도에선 비스듬히 내려다본다(벽 너머 하늘·벽에 가리지 않게)
            return _player.position + Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, hgt, back);
        }
        Vector3 CamLook => CamLookAt(_camYaw + _camDodge);
        Vector3 CamLookAt(float yaw) => CamLookAt(yaw, CamAhead);
        Vector3 CamLookAt(float yaw, float ahead) => _player.position + new Vector3(0f, 0.7f, 0f) + Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, 0f, ahead);
        void SnapCamera() { _camYaw = _camYawTarget = _player.eulerAngles.y; _backT = 0f; _camDodge = _camDodgeTarget = 0f; _camLowK = _camLowWant = _camHighK = _camHighWant = 0f; _cam.transform.position = CamTarget; _cam.transform.LookAt(CamLook); _camVel = Vector3.zero; _snapCam = true; LateUpdate(); }
        bool _snapCam;
        bool CamBlocked(Vector3 p)
        {
            foreach (var c in Physics.OverlapSphere(p, 0.3f, ~0, QueryTriggerInteraction.Ignore))
            {
                // 174차: 바위·소품은 카메라를 밀어내지 않는다(가리면 HideThinOccluders 가 그동안만 숨긴다) — 「장애물 만나면 확대」의 원인
                if (c == null || c.transform.IsChildOf(_player) || c.GetComponentInParent<VillageCreatures>() != null || c.name == "Bound" || c.name == "FenceCol" || c.name == "GardenFence" || c.name == "Boulder" || c.name == "Rock" || c.name.StartsWith("Rock_")) continue;
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
            _spots.Add(new Spot { id = "mom", title = Loc.T("엄마 집 · 펫 돌보기 / 잠자기", "Mom's house · Pets / Sleep"), pos = VillageWorld.Ground(1.2f, -18.4f), radius = 4.2f, on = MomHouseMenu });
            _spots.Add(new Spot { id = "shop", title = Loc.T("마을상점 · 들어가기", "Village shop · Enter"), pos = VillageWorld.Ground(-5.5f, -29.5f), radius = 4f, on = EnterShopHouse });   // 171차: 실내로
            _spots.Add(new Spot { id = "garden", title = Loc.T("텃밭 · 밭 칸으로 들어가자", "Garden · Step onto a plot"), pos = VillageWorld.Ground(0f, -6f), radius = 6.6f, on = FarmAct });   // 150차: 스타듀식 9칸
            _spots.Add(new Spot { id = "farm", title = Loc.T("🐔 농장", "🐔 Farm"), pos = VillageLivestock.Gate, radius = 3.6f, on = LivestockAct });   // 171차: 우리집 서쪽 가축 울타리
            _spots.Add(new Spot { id = "beach", title = Loc.T("바닷가 · 낚시하기", "Beach · Go fishing"), pos = VillageWorld.Ground(-1f, -31f), radius = 4.5f, on = OpenFishing });
            _spots.Add(new Spot { id = "play", title = Loc.T("정자 · 놀기 / 연습", "Pavilion · Play / Practice"), pos = VillageWorld.Ground(28f, -6f), radius = 4.2f, on = PlayMenu });   // 156차
            // 147차: 집마다 들어가기(문 앞 스팟) — 우리집/엄마 집은 기존 메뉴에 항목 추가
            foreach (var hs in VillageWorld.Houses)
            {
                if (hs.house == VillageWorld.HeroHouse || hs.house == VillageWorld.MomHouse) continue;
                if (hs.house == VillageWorld.Hospital) { _spots.Add(new Spot { id = "hospital", title = Loc.T("🏥 병원 · 들어가기", "🏥 Hospital · Enter"), pos = hs.door, radius = 2.6f, on = EnterHospital }); continue; }   // 189차
                if (hs.house == VillageEast.Bank) { _spots.Add(new Spot { id = "bank", title = Loc.T("🏦 은행 · 들어가기", "🏦 Bank · Enter"), pos = hs.door, radius = 2.6f, on = EnterBank }); continue; }   // 194차
                if (hs.house == VillageEast.Cafe) { _spots.Add(new Spot { id = "cafe", title = Loc.T("🍊 귤빛 브런치 · 들어가기", "🍊 Tangerine Brunch · Enter"), pos = hs.door, radius = 2.6f, on = EnterCafe }); continue; }   // 194차
                if (hs.house == VillageWorld.JobHouse) { _spots.Add(new Spot { id = "job", title = Loc.T("💼 알바나라 · 들어가기", "💼 Job Center · Enter"), pos = hs.door, radius = 2.6f, on = EnterJobHouse }); continue; }   // 156차 → 171차 실내로
                var cap = hs; 
                _spots.Add(new Spot { id = "house_" + cap.name, title = Loc.T(cap.name + " · 들어가기", cap.name + " · Enter"), pos = cap.door, radius = 2.4f, on = () => EnterHouse(cap.house, cap.name, cap.door) });
            }
            AddEastSpots();   // 194차: 버스 정류장
            AddZoneSpots();   // 195차: 시내·관광지·광산·과수원·벌통·축사·계절 부스
            AddFunSpots();   // 205차: 축제 대회 접수
            _spots.Add(new Spot { id = "light", title = Loc.T("등대 · 바다 구경", "Lighthouse · Sea view"), pos = VillageWorld.Ground(-9.5f, -58.2f), radius = 4.5f, on = () => _hud.Bubble(Loc.T("하늘", "Haneul"), Loc.T("등대 불빛이 89.2처럼 깜빡인다. 바다가 오늘은 조용하다.", "The lighthouse blinks like 89.2. The sea is quiet today.")) });
        }

        // ── 매 프레임 ─────────────────────────────────────────────────────
        void Update()
        {
            if (_player == null || _hud == null) return;
            bool locked = _busy || _hud.Locked;
            if (!locked && _hud.Joy.magnitude > 0.3f) CancelAuto(true);
            if (!locked) TickAutoTalk();
            TickRoadGhost(locked);
            TickZones();   // 195차
            TickDungeon();   // 199차
            TickFun205();   // 205차: 비 오면 텃밭 물 · 말 타기 · 하트 이벤트
            TickStory();   // 196차: 이야기 장소(빛) — 들어가면 컷씬
            TickSea();   // 192차: 바다 10초 → 병원   // 187차: 가까이 가면 자동 대화   // 183차: 직접 조이스틱을 밀면 자동이동·자동사냥을 멈춘다
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
            float wantSpeed = mag < 0.05f ? 0f : Mathf.Lerp(2.46f, 5.46f, Mathf.InverseLerp(0.35f, 0.95f, mag)) * RideMul;   // 205차: 말 타면 1.7배   // 144차: +20% · 159차(사용자): +20% 더
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
            if (_cc.enabled) _cc.Move(move);   // 180차: 잠자기(침대에 눕힘)·순간이동 중엔 컨트롤러가 꺼져 있다 — 「Move called on inactive controller」 에러
            if (!locked) Unstick(dt, mag);   // 159차
            // 159차: 줍기(나뭇가지·조개·버섯)는 1.2 m 안에 오면 자동으로
            if (!locked && _picks != null && Time.frameCount % 5 == 0) { foreach (var pk in _picks) { if (pk.go == null) continue; if (Vector3.Distance(new Vector3(_player.position.x, 0f, _player.position.z), new Vector3(pk.pos.x, 0f, pk.pos.z)) < 1.2f) { TakePick(pk); break; } } }
            bool moving = _vel.magnitude > 0.15f;
            _moving = moving;
            // 155차: 밤낮 — 팝업·이벤트 중엔 시계 멈춤, 밤이면 귀신(시간 지날수록 1→3), 20:00 에 한 번 경고
            if (_dayNight != null)
            {
                _dayNight.Paused = locked || _interior != null;
                if (_creatures != null) _creatures.Night = _dayNight.IsNight && _interior == null;   // 175차: 밤 몬스터 스폰 조건
                if (_dayNight.IsNight && _interior == null && _creatures != null)
                {
                    float h = _dayNight.Hour < 6f ? _dayNight.Hour + 24f : _dayNight.Hour;
                    int want = h < 20.5f ? 0 : h < 22f ? 1 : h < 23.5f ? 2 : 3;
                    if (_creatures.GhostCount < want && Time.time > _ghostSpawnAt) { _creatures.SpawnGhost(); _ghostSpawnAt = Time.time + 6f; }
                    if (!_nightWarned) { _nightWarned = true; _hud.Bubble(Loc.T("하늘", "Haneul"), Loc.T("밤이다… 귀신이 나올 시간이야. 빨리 우리집으로 돌아가자! (상점도 닫혔어)", "It's night… ghosts come out now. Run home! (Shop's closed too)")); }
                }
                else if (!_dayNight.IsNight) _nightWarned = false;
            }
            // 지나치는 풀·덤불 흔들기
            if (moving && Time.frameCount % 6 == 0) WindSway.NudgeNear(_player.position, 1.1f);
            WeatherTick(dt, moving);   // 174차-3: 마을에도 비·눈
            // 가까운 장소·나무
            UpdateNear();
            if (_yard) UpdateYard();
            if (Time.frameCount % 30 == 0 && Save != null) { var sp = _interior != null ? _interior.ReturnPos : _player.position; Save.villageX = sp.x; Save.villageZ = sp.z; }   // 156차: 집 안(300,300)은 저장하지 않는다
        }

        void LateUpdate()
        {
            if (_cam == null || _player == null) return;
            if (_storyCam) { StoryCamTick(); return; }   // 196차: 도입 연출 카메라
            // 154차: 휘두르는 동안 오른팔을 캐릭터 기준 앞/위로 들어 올림(애니메이터 위에 덧씌움)
            if (_swingArm != 0f && _anim != null && _anim.avatar != null && _anim.avatar.isHuman)
            {
                var ua = _anim.GetBoneTransform(HumanBodyBones.RightUpperArm); var la = _anim.GetBoneTransform(HumanBodyBones.RightLowerArm); var sp = _anim.GetBoneTransform(HumanBodyBones.Spine);
                if (_swingStyle == 1)
                {
                    // 171차 잠자리채: 팔을 어깨 높이로 들고(앞으로 70°) 오른쪽 바깥(−75°)에서 왼쪽(+60°)으로 수평으로 쓸어 온다. 몸통도 같이 20° 돈다
                    float sweep = _swingArm;   // −1(오른쪽 뒤) → +1(왼쪽 앞)
                    if (sp != null) sp.rotation = Quaternion.AngleAxis(sweep * 22f, Vector3.up) * sp.rotation;
                    if (ua != null) ua.rotation = Quaternion.AngleAxis(sweep * 68f, Vector3.up) * Quaternion.AngleAxis(-75f, _player.right) * ua.rotation;
                    if (la != null) la.rotation = Quaternion.AngleAxis(-15f + Mathf.Max(0f, sweep) * 20f, _player.right) * la.rotation;
                }
                else if (_swingStyle == 2)
                {
                    // 171차 낚싯대: 뒤로 젖혔다가(어깨 뒤 +40°) 앞으로 던진다(−100°). 몸통은 살짝 앞으로
                    float cast = _swingArm;   // −1 뒤로 젖힘 → +1 던짐
                    if (sp != null) sp.rotation = Quaternion.AngleAxis(Mathf.Max(0f, cast) * 12f, _player.right) * sp.rotation;
                    if (ua != null) ua.rotation = Quaternion.AngleAxis(-40f - cast * 70f, _player.right) * ua.rotation;
                    if (la != null) la.rotation = Quaternion.AngleAxis(-Mathf.Max(0f, -cast) * 45f, _player.right) * la.rotation;
                }
                else
                {
                    float lift = Mathf.Clamp(-_swingArm, -0.6f, 1f);   // 위로 들 때 양수
                    if (sp != null) sp.rotation = Quaternion.AngleAxis(Mathf.Max(0f, _swingArm) * 18f, _player.right) * sp.rotation;   // 171차: 내리찍을 때 상체도 숙인다
                    if (ua != null) ua.rotation = Quaternion.AngleAxis(-lift * 120f - 20f, _player.right) * ua.rotation;
                    if (la != null) la.rotation = Quaternion.AngleAxis(-Mathf.Max(0f, lift) * 30f, _player.right) * la.rotation;
                }
            }
            // 144차(사용자: 「좌우로 움직이면 획획 넘어감」): 카메라 방향은 좌우 이동에 따라 돌지 않는다(동물의 숲처럼 고정).
            // 카메라 쪽(뒤)으로 1.2 초 이상 계속 걸을 때만 목표 방향을 바꿔 천천히 돌아붙고, 그 외엔 진행 방향으로 살짝 기울기만.
            float dtc = Time.deltaTime; float pyaw = _player.eulerAngles.y;
            float headErr = Mathf.DeltaAngle(_camYawTarget, pyaw);
            // 실제로 몸이 움직일 때만(벽에 막혀 제자리면 카메라가 돌지 않게) — CharacterController 실제 속도 기준
            bool reallyMoving = _cc != null && _cc.velocity.magnitude > 0.4f;
            // 149차(사용자: 「주인공이 움직이는 방향으로 카메라 이동」): 0.35 초 이상 실제로 걸으면 카메라 목표 방향이 진행 방향을
            // 초당 110° 로 따라가고, 실제 회전은 0.6 초 감쇠 — 좌우 스틱에 휙 돌지 않고 스르륵 돌아붙는다. 멈추면 그 방향 유지.
            if (_moving && reallyMoving) _backT += dtc; else _backT = 0f;
            // 153차(사용자: 「좌측으로 내려가도 오른쪽으로 흐르는 느낌·카메라가 어지럽게 돎」): 옆으로(45~135°) 걸을 때는 카메라가 돌지 않는다.
            // 카메라가 돌면서 같은 스틱 방향이 화면에서 대각선으로 흐르던 것. 뒤로 돌아설 때(>135°)만 1.0 s 뒤 초당 55°, 앞쪽(<45°)은 미세 보정(25°/s).
            float absErr = Mathf.Abs(headErr);
            if (_backT > 1.0f && absErr > 135f) _camYawTarget = Mathf.MoveTowardsAngle(_camYawTarget, pyaw, dtc * 55f);
            // 198차(사용자: 「움직일 때 카메라가 살짝 도는 느낌」): 앞쪽 미세 보정(25°/s)을 뺐다 — 카메라는 뒤돌아설 때만 돈다
            float lean = 0f;   // 기울기(lean)도 옆걸음에서 화면을 흔들어 뺐다
            float yawErr = Mathf.Abs(Mathf.DeltaAngle(_camYaw, _camYawTarget));
            float kWant = Mathf.Clamp01((yawErr - 40f) / 90f);
            _swingK = Mathf.Lerp(_swingK, kWant, 1f - Mathf.Exp(-dtc * (kWant > _swingK ? 9f : 3f)));
            _camYaw = Mathf.SmoothDampAngle(_camYaw, _camYawTarget + lean, ref _camYawVel, 0.8f);
            // 146차: 방사형 곡면 원점 = 주인공(가까운 곳은 평평, 60 m 에서 2.9 m, 바다 끝 70 m 아래로)
            Shader.SetGlobalVector("_CoastCurveRadial", new Vector4(CurveK, _player.position.x, _player.position.z, 320f));
            // 171차(사용자: 「건물 쪽에서 인물이 안 보이고 건물 이미지만 크게 보이는 구간」): 제자리 각도로는 5 m 도 못 물러나면
            // ±35/70/105/140° 중 가장 가까운 빈 각도로 카메라가 비켜 돈다(초당 90°). 원래 각도가 다시 비면 되돌아온다. 0.4 s 마다 판단.
            // 174차-2(사용자: 「움직이면 카메라가 뱅글뱅글 돈다」): 171차의 비켜 돌기를 끈다.
            // 카메라는 늘 주인공 뒤 같은 각도·거리. 가리는 것은 숨기거나 반투명으로 처리한다.
            _camDodgeT -= Time.deltaTime;
            _camDodgeTarget = 0f; _camLowWant = 0f; _camHighWant = 0f;
            if (false)
            {
                _camDodgeT = 0.4f;
                float base0 = _camYaw; float free0 = CamFree(CamLookAt(base0), CamTargetAt(base0), true, true), need0 = (CamTargetAt(base0) - CamLookAt(base0)).magnitude;
                if (!CamAboveGround(CamLookAt(base0), CamTargetAt(base0))) free0 = 0f;   // 171차: 지형에 파묻히는 각도는 0 취급
                if (free0 >= Mathf.Min(need0, 10f)) _camDodgeTarget = 0f;
                else if (_camDodgeTarget == 0f || !CamAboveGround(CamLookAt(base0 + _camDodgeTarget), CamTargetAt(base0 + _camDodgeTarget)) || CamFree(CamLookAt(base0 + _camDodgeTarget), CamTargetAt(base0 + _camDodgeTarget), true, true) < 5f)
                {
                    float pick = _camDodgeTarget; bool found = false; float bestFree = free0;
                    foreach (float off in new[] { 35f, -35f, 70f, -70f, 105f, -105f, 140f, -140f })
                    {
                        float f = CamAboveGround(CamLookAt(base0 + off), CamTargetAt(base0 + off)) ? CamFree(CamLookAt(base0 + off), CamTargetAt(base0 + off), true, true) : 0f;
                        if (f > bestFree) { bestFree = f; if (!found) pick = off; }
                        if (f >= Mathf.Min(need0, 10f)) { pick = off; found = true; break; }
                    }
                    _camDodgeTarget = pick;
                    // 171차 보정: 어느 각도도 안 비고 **가장 잘 비는 각도조차 4.5 m 미만**일 때만 낮게 붙는다(3.4 m 높이·7.2 m 뒤).
                    // 처음엔 2.1 m·4.2 m 였는데 언덕 나무·울타리가 흔한 농장 앞에서 주인공이 화면을 다 덮었다(캡처 r171_farmcam).
                    // 171차 보정: 어느 각도도 안 비고 가장 잘 비는 각도조차 4.5 m 미만이면 낮게(3.4/7.2),
                    // 172차: 2.5 m 도 못 벌면(벽에 바짝, 문 앞) 위에서 내려다보기(9.5/4.5)로 한 단계 더.
                    // 174차(사용자: 「이동하다 장애물 만나면 카메라가 확대된다」): 172차의 「위에서 내려다보기(9.5/4.5)」를 끈다.
                    // 그 모드가 주인공 정수리만 크게 잡혀 확대처럼 보였다. 막히면 낮게 붙기까지만.
                    _camLowWant = 0f; _camHighWant = 0f;
                }
                // 172차: 돌아오기는 더 엄격하게(히스테리시스) — 12 m 가 비어야 기본 카메라로 복귀, 안 그러면 들락날락한다
                if (_camDodgeTarget == 0f && CamFree(CamLookAt(base0), CamTargetAt(base0), true, true) >= Mathf.Min(need0, 12f)) { _camLowWant = 0f; _camHighWant = 0f; }
            }
            _camLowK = Mathf.MoveTowards(_camLowK, _camLowWant, Time.deltaTime * 2.5f);
            _camHighK = Mathf.MoveTowards(_camHighK, _camHighWant, Time.deltaTime * 2.0f);
            _camDodge = Mathf.MoveTowardsAngle(_camDodge, _camDodgeTarget, Time.deltaTime * 90f);
            var look = CamLook; var ct = CamTarget;
            // 140차: 카메라가 멀어진 만큼 지형·바다·건물 안으로 파고들지 않게 — 땅/바다 높이 클램프 + 구체 캐스트로 당기기
            float gy = VillageWorld.Height(ct.x, ct.z) + 1.6f; if (ct.y < gy) ct.y = gy;
            if (ct.y < VillageWorld.SeaLevel + 1.8f) ct.y = VillageWorld.SeaLevel + 1.8f;
            var dir = ct - look; float dist = dir.magnitude; float best = Mathf.Min(CamFree(look, ct, false, true), CamVisualFree(look, ct));   // 174차: 소품은 당김 대상에서 뺀다
            // 174차(사용자: 「이동하다 장애물 만나면 카메라가 확대된다」): 가림 때문에 카메라를 당기지 않는다.
            // 거리는 그대로 두고, 시선과 카메라 사이에 든 것은 HideThinOccluders 가 그 동안만 숨긴다(동물의 숲식).
            if (false)
            {
                // 161차(사용자: 「건물 벽에 가면 카메라가 위로 올라가 뭉개진 건물 이미지만 보임」): 147차의 「위로 올려 내려다보기」를 없앴다.
                // 막히면 같은 각도로 당겨 붙기만 하고(최소 3.4 m), 그보다 더 가까워야 하면 카메라가 벽 안으로 들어가 뒷면 컬링으로 안이 비친다
                // (지붕 텍스처를 코앞에서 찍는 것보다 낫다). 당김은 부드럽게(SmoothDamp) 되돌아온다.
                float pull = Mathf.Max(3.4f, best - 0.3f);
                // 171차: 당겨 붙을수록 시선점(주인공 앞 2.4 m)도 같이 당긴다 — 안 그러면 3.4 m 로 당겨도 주인공이 카메라 코앞(0.9 m)에 와서
                // 뒤통수가 화면을 덮는다(캡처 r171_farmcam, CamInfo low=1.00). 시선이 가까워지면 주인공이 화면 가운데로 온다.
                float ak = Mathf.Clamp01(pull / dist);
                look = CamLookAt(_camYaw + _camDodge, CamAhead * ak);
                dir = CamTargetAt(_camYaw + _camDodge) - look; dist = dir.magnitude;
                ct = look + dir / Mathf.Max(0.01f, dist) * pull;
            }
            // 캐스트 시작점이 이미 건물 안이면(스폿이 건물 바로 앞) 앞으로 당겨 가며 빈 자리 찾기
            // 174차: 여기서도 당기지 않는다(옛 코드는 최소 3.4 m 까지 끌어왔다) — 벽 안이면 그 벽을 숨긴다
            _cam.transform.position = _snapCam ? ct : Vector3.SmoothDamp(_cam.transform.position, ct, ref _camVel, 0.16f); _snapCam = false;
            _cam.transform.LookAt(look);
            // 171차: 산적·벌레는 카메라를 막지 않게 무시하는데, 그러다 보니 코앞에 선 산적 뒤통수가 화면을 덮었다(캡처 r171_farmcam3) → 1.9 m 안이면 잠깐 감춘다
            if (_creatures != null) _creatures.CullNearCamera(_cam.transform.position, 1.9f);
            HideThinOccluders(look, _cam.transform.position);   // 172차: 야자수 줄기·가로등 같은 가는 기둥은 비켜 돌기로 못 피한다 — 가린 동안만 숨긴다
            UpdateHouseFades(_cam.transform.position);   // 179차: 집은 페이드(가리면 반투명, 카메라가 집 안이면 완전히 사라짐)
        }

        // 171차: 건물 지붕·차양은 콜라이더가 없어 구체 캐스트가 지나간다 → 집·상점의 렌더러 경계 상자로도 가림을 잰다
        readonly List<Bounds> _houseBounds = new List<Bounds>(); float _houseBoundsT;
        void RefreshHouseBounds()
        {
            _houseBounds.Clear();
            // 171차: 굴뚝 연기·문 반짝임·간판 글자는 집 밖으로 흘러 다녀서 경계 상자를 몇십 m 로 부풀린다(그 탓에 농장 앞에서도 카메라가 낮게 붙었다).
            // 고정된 몸체(벽·지붕·문·창)만 센다.
            static bool Solid(Renderer r)
            {
                string n = r.gameObject.name;
                if (n.Contains("Smoke") || n.Contains("Puff") || n.Contains("Glow") || n.Contains("Sparkle") || n.Contains("Rim") || n.Contains("Blob") || n.Contains("Text") || n.Contains("Canvas")) return false;
                return !(r is ParticleSystemRenderer) && !(r is LineRenderer) && !(r is TrailRenderer);
            }
            void Add(Transform t)
            {
                if (t == null) return; bool first = true; Bounds b = default;
                foreach (var r in t.GetComponentsInChildren<Renderer>()) { if (!Solid(r)) continue; if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds); }
                if (first) return;
                var s = b.size; b.size = new Vector3(Mathf.Min(s.x, 14f), Mathf.Min(s.y, 12f), Mathf.Min(s.z, 14f));   // 안전 상한
                b.Expand(-0.3f); _houseBounds.Add(b);
            }
            foreach (var hs in VillageWorld.Houses) Add(hs.house);
            Add(VillageWorld.Shop);
        }
        // 172차(사용자: 「야자수 줄기처럼 가는 기둥이 아직 주인공을 가린다」): 시선~카메라 사이를 0.55 m 구체로 훑어
        // **가느다란 것(콜라이더 가로·세로 2.2 m 미만: 야자수·소나무·가로등·간판 기둥·돌하르방·벤치)** 만 그 프레임 동안 렌더러를 끈다.
        // 큰 것(집·절벽)은 끄면 세상이 사라져 보이므로 그대로 두고 카메라가 비켜 돈다(171차).
        readonly List<Renderer> _camHidden = new List<Renderer>();
        float _camHideT;
        void HideThinOccluders(Vector3 look, Vector3 cam)
        {
            if (Time.time < _camHideT) return;
            _camHideT = Time.time + 0.12f;
            RestoreHidden();
            _occHouses.Clear();   // 179차
            if (_interior != null) return;
            // 180차(사용자: 「집이 아닌 사물 근처만 가도 사라진다」): 예전엔 시선점(주인공 2.4 m 앞)에서 쏴서 주인공 앞·옆 소품이 캐스트 시작 구와
            // 겹치기만 해도(거리 0) 숨겨졌다 → 주인공 머리에서 카메라로 쏘고, 시작부터 겹친 것(바로 옆에 닿은 것)은 가림이 아니므로 뺀다.
            var head = _player.position + new Vector3(0f, 1.0f, 0f);
            var d = cam - head; float len = d.magnitude; if (len < 0.5f) return;
            var hits = Physics.SphereCastAll(head, 0.4f, d / len, len, ~0, QueryTriggerInteraction.Ignore);
            foreach (var hit in hits)
            {
                var c = hit.collider; if (c == null) continue;
                if (hit.distance <= 0f) continue;   // 시작부터 겹침 = 주인공 곁에 닿은 것(문 앞에 선 집 포함) — 가림이 아니다
                // 187차(사용자: 「여전히 작은 장애물 근처에 가면 사라진다」): 주인공 바로 뒤 2.5 m 안(발치 바위·울타리)은 카메라가 24° 내려다봐서
                // 주인공을 거의 안 가린다 → 숨기지 않는다. 더 멀어도 물체 윗면이 그 지점의 시선보다 낮으면(발만 걸침) 그대로 둔다.
                if (hit.distance < 2.5f && !(c.bounds.size.x > 6f || c.bounds.size.z > 6f)) continue;
                { float lineY = head.y + (d.y / len) * hit.distance; if (c.bounds.max.y < lineY - 0.15f) continue; }
                if (c.name == "Bound" || c.name == "FenceCol" || c.name == "GardenFence") continue;
                if (c.transform.IsChildOf(_player) || c.GetComponentInParent<VillageCreatures>() != null) continue;
                if (c.name == "Terrain" || c.name == "Sea") continue;   // 174차: 지형·바다는 숨기면 세상이 사라진다
                var hr = FadeRootOf(c.transform); if (hr != null && c.transform == hr) { _occHouses.Add(hr); continue; }   // 179차: 집·상점 몸체는 UpdateHouseFades 가 부드럽게 처리
                // 180차: 집에 딸린 소품(우체통·울타리 등, 집의 자식)에 맞았다고 집 전체를 숨기던 것 — 몸체 콜라이더(루트)만 집으로 친다
                var bs = c.bounds.size; bool big = bs.x > 6f || bs.z > 6f;   // 174차-2: 집·상점은 사라지지 않고 반투명으로
                foreach (var r in c.GetComponentsInChildren<Renderer>())
                {
                    if (r == null || !r.enabled) continue;
                    if (big)
                    {
                        var src = r.sharedMaterials; if (src == null || src.Length == 0) continue;
                        var gs = new Material[src.Length]; bool any = false;
                        for (int k = 0; k < src.Length; k++) { var g = GhostMat(src[k]); if (g != null) { gs[k] = g; any = true; } else gs[k] = src[k]; }
                        if (!any) continue;
                        _camGhosted.Add(new GhostSwap { r = r, mats = src }); r.sharedMaterials = gs;
                    }
                    else { r.enabled = false; _camHidden.Add(r); }
                }
            }
        }

        struct GhostSwap { public Renderer r; public Material[] mats; }
        readonly List<GhostSwap> _camGhosted = new List<GhostSwap>();
        static readonly Dictionary<Material, Material> _ghostMats = new Dictionary<Material, Material>();
        /// 174차-2: 집이 카메라를 가리면 지우지 않고 유리처럼 비친다(사용자: 「집만 반투명으로」)
        static Material GhostMat(Material src)
        {
            if (src == null) return null;
            if (_ghostMats.TryGetValue(src, out var g) && g != null) return g;
            Texture2D tex = null;
            if (src.HasProperty("_BaseMap")) tex = src.GetTexture("_BaseMap") as Texture2D;
            if (tex == null && src.HasProperty("_MainTex")) tex = src.GetTexture("_MainTex") as Texture2D;
            Color c = src.HasProperty("_BaseColor") ? src.GetColor("_BaseColor") : (src.HasProperty("_Color") ? src.GetColor("_Color") : Color.white);
            c.a = 0.30f;
            g = tex != null ? CoastMaterials.CreateTexturedTransparent(tex, c) : CoastMaterials.CreateTransparent(c);
            _ghostMats[src] = g;
            return g;
        }

        // ── 179차(사용자: 「카메라가 집안으로 들어가면 집을 다른 게임들처럼 자연스럽게 숨겨줘」) ──
        // 집·상점마다 알파를 따로 들고 부드럽게 옮긴다: 카메라가 집 경계(+3 m) 안이거나 집이 시선을 가림 → 0(사라짐), 그 밖 → 1(원래 재질로 복귀).
        // 사라질 땐 약 0.22초, 돌아올 땐 약 0.33초. 알파 0 근처에선 렌더러를 꺼서 투명 정렬 잔상도 없앤다.
        class HouseFade
        {
            public Transform root; public Renderer[] rs; public Renderer[] soft; public bool[] softEn; public CanvasGroup[] groups; public Bounds b; public float a = 1f; public bool swapped;
            public Material[][] orig; public bool[] en;
            public readonly Dictionary<Material, Material> map = new Dictionary<Material, Material>();
            public readonly List<(Material m, Color c)> tints = new List<(Material, Color)>();
        }
        readonly List<HouseFade> _fades = new List<HouseFade>(); float _fadeScanT;
        readonly HashSet<Transform> _occHouses = new HashSet<Transform>();
        const float HouseGhostA = 0f;   // 180차: 가릴 때도 완전히 숨긴다 — 0.12 여도 지붕 기와·벽이 여러 겹 겹쳐 분홍 막처럼 화면을 덮었다(아침에 집 앞에서 깰 때)

        void ScanHouseFades()
        {
            _fadeScanT = Time.time + 5f;
            _fades.RemoveAll(f => f.root == null || !f.swapped);   // 켜 둔(페이드 중인) 것만 남기고 다시 훑는다 — 집이 새로 지어져도 따라간다
            void Add(Transform t)
            {
                if (t == null) return; foreach (var f in _fades) if (f.root == t) return;
                var list = new List<Renderer>(); var soft = new List<Renderer>(); bool first = true; Bounds b = default;
                foreach (var r in t.GetComponentsInChildren<Renderer>(true))
                {
                    if (r is ParticleSystemRenderer || r is LineRenderer || r is TrailRenderer) continue;
                    string n = r.gameObject.name;
                    if (n.Contains("Blob")) continue;   // 바닥 그림자는 그대로
                    if (n.Contains("Text") || n.Contains("Canvas") || r.GetComponent<TextMesh>() != null) { soft.Add(r); continue; }   // 글자·간판 글씨는 재질을 못 바꾸니 반쯤 사라질 때 끈다
                    list.Add(r);
                    if (n.Contains("Smoke") || n.Contains("Puff") || n.Contains("Glow") || n.Contains("Sparkle") || n.Contains("Rim")) continue;   // 경계는 몸체만
                    if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds);
                }
                if (first) return;
                var s = b.size; b.size = new Vector3(Mathf.Min(s.x, 16f), Mathf.Min(s.y, 14f), Mathf.Min(s.z, 16f));
                // 180차: 집 앞 돌길·우체통까지 렌더러 경계에 들어가 「집 안」 판정이 문 앞 몇 m 까지 번졌다 → 몸체 콜라이더 경계(+지붕 높이)를 쓴다
                var body = t.GetComponent<BoxCollider>();
                if (body != null) { var cb = body.bounds; cb.Encapsulate(new Vector3(cb.center.x, Mathf.Min(b.max.y, cb.max.y + 4f), cb.center.z)); b = cb; }
                // 간판 글씨는 월드 캔버스 — CanvasGroup 알파로 같이 흐리게
                var groups = new List<CanvasGroup>();
                foreach (var cv in t.GetComponentsInChildren<Canvas>(true)) { var g = cv.GetComponent<CanvasGroup>(); if (g == null) g = cv.gameObject.AddComponent<CanvasGroup>(); groups.Add(g); }
                _fades.Add(new HouseFade { root = t, rs = list.ToArray(), soft = soft.ToArray(), groups = groups.ToArray(), b = b });
            }
            foreach (var hs in VillageWorld.Houses) Add(hs.house);
            Add(VillageWorld.Shop);
        }

        Transform FadeRootOf(Transform t)
        {
            foreach (var f in _fades) if (f.root != null && (t == f.root || t.IsChildOf(f.root))) return f.root;
            return null;
        }

        void UpdateHouseFades(Vector3 cam)
        {
            if (Time.time > _fadeScanT) ScanHouseFades();
            float dt = Time.deltaTime;
            foreach (var f in _fades)
            {
                if (f.root == null) continue;
                var bb = f.b; bb.Expand(3f);   // 벽·지붕에 3 m 안으로 붙으면 집 안으로 친다
                bool inside = _interior == null && bb.Contains(cam);
                float target = inside ? 0f : (_occHouses.Contains(f.root) ? HouseGhostA : 1f);
                if (Mathf.Approximately(f.a, target) && (target < 1f ? f.swapped : !f.swapped)) continue;
                f.a = Mathf.MoveTowards(f.a, target, dt * (target < f.a ? 4.5f : 3f));
                if (f.a >= 0.999f) { f.a = 1f; UnswapFade(f); continue; }
                if (!f.swapped) SwapFade(f);
                foreach (var (m, c) in f.tints) if (m != null) { var cc = c; cc.a = c.a * f.a; m.SetColor("_BaseColor", cc); }
                bool vis = f.a > 0.02f;
                for (int i = 0; i < f.rs.Length; i++) if (f.rs[i] != null) f.rs[i].enabled = vis && f.en[i];
                for (int i = 0; i < f.soft.Length; i++) if (f.soft[i] != null) f.soft[i].enabled = f.a > 0.55f && f.softEn[i];
                foreach (var g in f.groups) if (g != null) g.alpha = f.a;
            }
            // 180차: 문 반짝이 테두리(DoorGlow)는 페이드 뒤에 붙어서 목록에 없다 — 집이 사라지면 허공에 노란 틀만 떠 있던 것
            if (_doorGlow != null)
            {
                bool show = true;
                foreach (var f in _fades) if (f.root != null && _doorGlow.transform.IsChildOf(f.root)) { show = f.a > 0.5f; break; }
                foreach (var r in _doorGlow.GetComponentsInChildren<Renderer>()) r.enabled = show;
            }
        }

        void SwapFade(HouseFade f)
        {
            f.orig = new Material[f.rs.Length][]; f.en = new bool[f.rs.Length];
            f.softEn = new bool[f.soft.Length]; for (int i = 0; i < f.soft.Length; i++) f.softEn[i] = f.soft[i] != null && f.soft[i].enabled;
            for (int i = 0; i < f.rs.Length; i++)
            {
                var r = f.rs[i]; if (r == null) continue;
                f.en[i] = r.enabled; var src = r.sharedMaterials; f.orig[i] = src;
                var gs = new Material[src.Length];
                for (int k = 0; k < src.Length; k++) gs[k] = FadeMat(f, src[k]);
                r.sharedMaterials = gs;
            }
            f.swapped = true;
        }

        void UnswapFade(HouseFade f)
        {
            if (!f.swapped) return;
            for (int i = 0; i < f.rs.Length; i++)
            {
                var r = f.rs[i]; if (r == null || f.orig[i] == null) continue;
                r.sharedMaterials = f.orig[i]; r.enabled = f.en[i];
            }
            for (int i = 0; i < f.soft.Length; i++) if (f.soft[i] != null) f.soft[i].enabled = f.softEn[i];
            foreach (var g in f.groups) if (g != null) g.alpha = 1f;
            f.swapped = false;
        }

        void RestoreHouseFades() { foreach (var f in _fades) if (f.root != null) UnswapFade(f); }

        /// 집마다 따로 쓰는 투명 재질(같은 지붕 재질을 여러 집이 나눠 써도 알파가 섞이지 않게)
        static Material FadeMat(HouseFade f, Material src)
        {
            if (src == null) return null;
            if (f.map.TryGetValue(src, out var g) && g != null) return g;
            Texture2D tex = null;
            if (src.HasProperty("_BaseMap")) tex = src.GetTexture("_BaseMap") as Texture2D;
            if (tex == null && src.HasProperty("_MainTex")) tex = src.GetTexture("_MainTex") as Texture2D;
            Color c = src.HasProperty("_BaseColor") ? src.GetColor("_BaseColor") : (src.HasProperty("_Color") ? src.GetColor("_Color") : Color.white);
            // 언릿 투명이라 낮 조명만큼 살짝 어둡게 — 바꿔 끼울 때 번쩍 밝아지지 않게
            var lit = new Color(c.r * 0.86f, c.g * 0.86f, c.b * 0.88f, 1f);
            g = tex != null ? CoastMaterials.CreateTexturedTransparent(tex, lit) : CoastMaterials.CreateTransparent(lit);
            if (g != null && src.HasProperty("_BaseMap") && g.HasProperty("_BaseMap")) { g.SetTextureScale("_BaseMap", src.GetTextureScale("_BaseMap")); g.SetTextureOffset("_BaseMap", src.GetTextureOffset("_BaseMap")); }
            f.map[src] = g; if (g != null) f.tints.Add((g, lit));
            return g;
        }

        void RestoreHidden()
        {
            for (int i = 0; i < _camHidden.Count; i++) if (_camHidden[i] != null) _camHidden[i].enabled = true;
            _camHidden.Clear();
            for (int i = 0; i < _camGhosted.Count; i++) { var g = _camGhosted[i]; if (g.r != null && g.mats != null) g.r.sharedMaterials = g.mats; }
            _camGhosted.Clear();
        }

        /// 171차: 지형 메시엔 콜라이더가 없어 캐스트로 못 잡는다 — 시선이 모래 언덕·절벽을 뚫고 가는지 높이로 직접 잰다.
        bool CamAboveGround(Vector3 look, Vector3 to)
        {
            for (float t = 0.12f; t <= 0.96f; t += 0.08f)   // 171차: 듬성듬성(0.3 간격) 재면 모래 언덕 봉우리를 그냥 지나쳤다 — 촘촘히
            {
                var p = Vector3.Lerp(look, to, t);
                if (p.y < VillageWorld.Height(p.x, p.z) + 1.0f) return false;
            }
            return true;
        }

        float CamVisualFree(Vector3 look, Vector3 to)
        {
            if (Time.time > _houseBoundsT) { _houseBoundsT = Time.time + 5f; RefreshHouseBounds(); }
            var d = to - look; float len = d.magnitude; if (len < 0.001f) return len; var ray = new Ray(look, d / len); float best = len;
            foreach (var b in _houseBounds) { if (b.Contains(look)) return 0f; /* 차양·지붕 아래 = 어느 각도든 가림 → 낮게 붙기 */ if (b.Contains(to)) return 0f; /* 카메라 자리가 다른 집 지붕 속 */ if (b.IntersectRay(ray, out float t) && t > 0f && t < best) best = t; }
            return best;
        }

        /// look→to 구체 캐스트로 막힘 없는 거리(경계 콜라이더·주인공·NPC 무시). 막힘 없으면 전체 거리.
        float CamFree(Vector3 look, Vector3 to, bool ignoreTerrain = false, bool solidOnly = false)
        {
            var d = to - look; float len = d.magnitude; float best = len; if (len < 0.001f) return len;
            if (ignoreTerrain) best = Mathf.Min(best, CamVisualFree(look, to));   // 171차: 비켜 돌기 판단엔 지붕·차양(렌더러 경계)도
            var hits = Physics.SphereCastAll(look, 0.35f, d / len, len, ~0, QueryTriggerInteraction.Ignore);
            foreach (var hit in hits)
            {
                // 171차: 허리 높이 울타리(FenceCol·GardenFence)는 카메라를 가리지 못한다 — 농장·텃밭 앞에서 공연히 당겨졌다
                if (hit.collider == null || hit.collider.name == "Bound" || hit.collider.name == "FenceCol" || hit.collider.name == "GardenFence" || hit.collider.transform.IsChildOf(_player) || hit.collider.GetComponentInParent<VillageCreatures>() != null) continue;
                if (ignoreTerrain && (hit.collider.name == "Terrain" || hit.collider.name == "Sea")) continue;   // 171차: 비켜 돌기 판단은 건물·바위만
                // 174차(사용자: 「이동하다 장애물 만나면 카메라가 확대된다」): 바위·나무·소품 때문에 카메라를 당기지 않는다.
                // 작은 것은 HideThinOccluders 가 가린 동안만 숨기고, 당김은 집·절벽·지형처럼 큰 것에만 쓴다.
                if (solidOnly) { var bs = hit.collider.bounds.size; if (bs.x <= 6.0f && bs.z <= 6.0f) continue; }   // 174차: 돌담·울타리·나무까지 소품으로
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
            var spT = Resources.Load<Sprite>("CoastRun/Textures/Village/UI_Tool_Pick"); float rd = -1f; foreach (var rk in VillageWorld.Rocks) if (rk.t != null) { float dd = Vector3.Distance(rk.t.position, _player.position); if (rd < 0 || dd < rd) rd = dd; }
            Debug.LogWarning($"[Diag] rocks={VillageWorld.Rocks.Count} nearRock={(_nearRock != null ? _nearRock.name : "-")} rockDist={rd:F1} pick={_pick} sprite={(spT != null)} stone={LifeItems.Count(Save, "mat_stone")} hit={hit} busy={_busy} locked={_hud?.Locked} popup={_hud?.PopupOpen} joy={_hud?.Joy} cc={(_cc != null && _cc.enabled)} ts={Time.timeScale} es={(es != null)} topAtJoy={top} vel={_vel.magnitude:F2} pos={_player.position}");
        }
        Vector3? _walkTo; float _ghostLift;
        /// 개발용: 자리로 걸어가기.
        public void WalkTo(Vector3 p) { _walkTo = p; }
        // ── 147차: 집 안 들어가기/나오기 ────────────────────────────────
        VillageInterior _interior; VillageDrops _drops;
        // 171차(사용자: 「상점이나 알바나라 안에 공간에 NPC 넣고 NPC 불러서 사거나 알바」): 문 → 실내 → 카운터 점원에게 말 걸기
        void EnterShopHouse()
        {
            if (_busy || _interior != null || VillageWorld.Shop == null) return;
            if (_dayNight != null && !_dayNight.ShopOpen) { _hud.Bubble(Loc.T("상점", "Shop"), Loc.T("문이 닫혔다. 영업은 오전 8시 ~ 오후 7시.", "Closed. Open 8 AM – 7 PM.")); return; }
            _interiorKind = "shop"; EnterHouse(VillageWorld.Shop, Loc.T("마을상점", "Village shop"), VillageWorld.Shop.TransformPoint(new Vector3(0f, 0f, 2.6f)));
        }
        void EnterJobHouse()
        {
            if (_busy || _interior != null || VillageWorld.JobHouse == null) return;
            if (_dayNight != null && !_dayNight.ShopOpen) { _hud.Bubble(Loc.T("알바나라", "Job Center"), Loc.T("문이 닫혔다. 접수는 오전 8시 ~ 오후 7시.", "Closed. Open 8 AM – 7 PM.")); return; }
            Vector3 door = Vector3.zero; foreach (var hs in VillageWorld.Houses) if (hs.house == VillageWorld.JobHouse) door = hs.door;
            _interiorKind = "job"; EnterHouse(VillageWorld.JobHouse, Loc.T("알바나라", "Job Center"), door);
        }
        public void EnterHouse(Transform house, string name, Vector3 door)
        {
            if (_interior != null) return;
            Color wall = new Color(0.98f, 0.94f, 0.88f), accent = VillagePalette.UiPink;
            var wr = house != null ? house.Find("Wall") : null;
            if (wr != null) { var m = wr.GetComponent<Renderer>().sharedMaterial; if (m != null && m.HasProperty("_BaseColor")) wall = Color.Lerp(m.GetColor("_BaseColor"), Color.white, 0.35f); }
            string[] acc = { "#FEC4DD", "#A9DCC8", "#BFD8F5", "#F7D5A6", "#CDBDDA" }; accent = VillagePalette.Hex(acc[Mathf.Abs(name.GetHashCode()) % acc.Length]);
            var back = door + (house != null ? house.forward * 1.7f : Vector3.zero);   // 154차: 나오면 문에서 1.7 m 앞(자동 입장 문 밖)
            _heroInside = house != null && house == VillageWorld.HeroHouse;
            _interiorKind = InteriorKindOf(house);   // 194차: 문으로 걸어 들어가도 병원·은행·카페 방이 꾸며지게(예전엔 스팟으로 들어갈 때만)
            StartCoroutine(EnterHouseRoutine(name, wall, accent, back, house != null ? house.eulerAngles.y : 180f));
        }
        bool _heroInside; string _interiorKind;   // 171차: "shop" / "job" 이면 실내에 점원 카운터
        IEnumerator EnterHouseRoutine(string name, Color wall, Color accent, Vector3 back, float houseYaw)
        {
            _busy = true; yield return FadeScreen(true, 0.25f);
            _interior = VillageInterior.Create(transform, name, wall, accent, back, houseYaw);
            Teleport(_interior.Entry, 0f);
            _spots.Add(new Spot { id = "exit", title = Loc.T("문 · 밖으로 나가기", "Door · Go outside"), pos = _interior.ExitSpot, radius = 1.6f, on = ExitHouse });
            if (_heroInside)
            {
                // 156차: 우리집 안 — 식탁(밥) · 침대(잠 → 한 주) · 책상(옛 홈 화면: 방 꾸미기·조리)
                float ox = VillageInterior.OX, oz = VillageInterior.OZ, fy = _interior.FloorY, kx = VillageInterior.KX, kz = VillageInterior.KZ;   // 161차: 방이 2배(10×7.8) — 가구 좌표 비율
                _spots.Add(new Spot { id = "home_table", title = Loc.T("🍚 식탁 · 밥 먹기", "🍚 Table · Eat"), pos = new Vector3(ox + 1.6f * kx, fy, oz - 0.9f * kz), radius = 1.4f, on = EatHome });
                // 161차(사용자: 「침대에서는 잠을 잘 수 있게」): 침대 옆(동쪽)에 서면 뜨는 스팟 — 반경을 침대 길이만큼 넓혀 어느 쪽에서 다가가도 잡힌다
                _spots.Add(new Spot { id = "home_bed", title = Loc.T("🛏 침대 · 자기 (한 주가 지난다)", "🛏 Bed · Sleep (week passes)"), pos = new Vector3(ox - 1.2f * kx, fy, oz + 1.3f * kz), radius = 1.9f, on = SleepBed });
                _spots.Add(new Spot { id = "home_desk", title = Loc.T("📻 책상 · 내 방 꾸미기 / 조리", "📻 Desk · My room / Cook"), pos = new Vector3(ox + 2.1f * kx, fy, oz + 1.6f * kz), radius = 1.3f, on = () => OpenHome(0) });
            }
            if (_interiorKind == "shop" || _interiorKind == "job") BuildCounter(_interiorKind == "shop");
            if (_interiorKind == "hospital") BuildHospitalRoom();   // 189차
            if (_interiorKind == "bank") BuildBankRoom();   // 194차
            if (_interiorKind == "cafe") BuildCafeRoom();   // 194차
            if (_interiorKind != null && _interiorKind != "shop" && _interiorKind != "job" && _interiorKind != "hospital" && _interiorKind != "bank" && _interiorKind != "cafe") BuildZoneRoom(_interiorKind);   // 195차: 시내·관광지 가게
            yield return null; yield return FadeScreen(false, 0.3f);
            _busy = false; _hud.Bubble(Loc.T("하늘", "Haneul"), Loc.T(name + " 안이다. 아늑하네.", "Inside " + name + ". Cozy."));
        }
        /// 171차: 실내 카운터 + 점원(치비 리그) + 진열대/게시판. 카운터 앞 스팟에서 행동 = 점원에게 말 걸기 → 장보기/알바 고르기
        void BuildCounter(bool shop)
        {
            float ox = VillageInterior.OX, oz = VillageInterior.OZ, fy = _interior.FloorY, rd = VillageInterior.RD, rw = VillageInterior.RW;
            var host = new GameObject(shop ? "ShopCounter" : "JobCounter").transform; host.SetParent(_interior.transform, false);
            // 집 가구(침대·책상·의자·러그·선반)는 가게엔 어울리지 않으니 끈다
            foreach (Transform ch in _interior.transform) { string n = ch.name; if (n == "Bed" || n == "Mattress" || n == "Pillow" || n == "Blanket" || n == "Headboard" || n == "Skirt" || n == "Table" || n == "Chair" || n == "ChairBack" || n == "Leg" || n == "Shelf" || n == "Book" || n == "Pot" || n == "RugIn" || n == "LampBase") ch.gameObject.SetActive(false); }
            GameObject B(string n, Vector3 pos, Vector3 size, Color col) { var g = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(g.GetComponent<Collider>()); g.name = n; g.transform.SetParent(host, false); g.transform.position = pos; g.transform.localScale = size; g.GetComponent<MeshRenderer>().sharedMaterial = CoastMaterials.CreateLit(col, 0.08f); return g; }
            var wood = new Color(0.74f, 0.54f, 0.34f); var woodD = new Color(0.52f, 0.36f, 0.22f);
            B("Counter", new Vector3(ox, fy + 0.4f, oz + 1.5f), new Vector3(3.4f, 0.8f, 0.8f), wood); B("CounterTop", new Vector3(ox, fy + 0.82f, oz + 1.5f), new Vector3(3.6f, 0.06f, 0.95f), woodD);
            var cc = new GameObject("CounterCol", typeof(BoxCollider)); cc.transform.SetParent(host, false); cc.transform.position = new Vector3(ox, fy + 0.5f, oz + 1.5f); cc.GetComponent<BoxCollider>().size = new Vector3(3.6f, 1.2f, 0.9f);
            if (shop)
            {
                // 진열대 2단 × 양옆 벽 + 알록달록 상품 상자
                var rng = new System.Random(7);
                for (int side = -1; side <= 1; side += 2)
                    for (int lv = 0; lv < 2; lv++)
                    {
                        float sx = ox + side * (rw * 0.5f - 0.45f);
                        B("Shelf", new Vector3(sx, fy + 0.7f + lv * 0.8f, oz + 0.2f), new Vector3(0.5f, 0.06f, 3.6f), woodD);
                        for (int k = 0; k < 5; k++) B("Goods", new Vector3(sx, fy + 0.7f + lv * 0.8f + 0.2f, oz - 1.4f + k * 0.8f), new Vector3(0.36f, 0.34f, 0.5f), Color.HSVToRGB((float)rng.NextDouble(), 0.55f, 0.95f));
                    }
                B("Register", new Vector3(ox + 1.1f, fy + 1.05f, oz + 1.5f), new Vector3(0.5f, 0.4f, 0.4f), new Color(0.35f, 0.38f, 0.45f));
                B("Basket", new Vector3(ox - 1.2f, fy + 1.0f, oz + 1.5f), new Vector3(0.5f, 0.3f, 0.4f), new Color(0.95f, 0.75f, 0.35f));
            }
            else
            {
                // 구인 게시판(뒷벽) + 종이 쪽지
                B("Board", new Vector3(ox, fy + 2.0f, oz + rd * 0.5f - 0.12f), new Vector3(3.2f, 1.6f, 0.08f), new Color(0.45f, 0.36f, 0.30f));
                var rng = new System.Random(3);
                for (int k = 0; k < 6; k++) B("Note", new Vector3(ox - 1.2f + (k % 3) * 1.2f, fy + 2.3f - (k / 3) * 0.7f, oz + rd * 0.5f - 0.18f), new Vector3(0.7f, 0.5f, 0.02f), Color.HSVToRGB((float)rng.NextDouble(), 0.25f, 1f));
                B("Clock", new Vector3(ox + 3.2f, fy + 2.3f, oz + rd * 0.5f - 0.12f), new Vector3(0.6f, 0.6f, 0.06f), new Color(0.95f, 0.95f, 0.92f));
            }
            // 점원: 상점은 카페 알바 모델, 알바나라는 등대지기 모델 — 카운터 뒤에서 문 쪽을 본다
            var npcRoot = new GameObject("Clerk").transform; npcRoot.SetParent(host, false); npcRoot.position = new Vector3(ox, fy, oz + 2.5f); npcRoot.rotation = Quaternion.Euler(0f, 180f, 0f);
            var pivot = new GameObject("Pivot").transform; pivot.SetParent(npcRoot, false);
            string model = shop ? "Npc_Cafe" : "Npc_Keeper";
            var rig = SkaterRig.SpawnModel(ArtAssets.ResourceRoot + "Rig/" + model, pivot, 1.0f, true);
            if (rig != null)
            {
                var anim = rig.GetComponent<Animator>(); if (anim != null) { anim.SetBool("Grounded", true); anim.Play("Run", 0, 0.12f); anim.speed = 0f; }
                var mo = pivot.gameObject.AddComponent<CharacterMotion>(); mo.Anim = anim; mo.LookTarget = _player; mo.FootDust = false;
                foreach (var smr in rig.GetComponentsInChildren<SkinnedMeshRenderer>()) if (smr.GetComponent<CelOutlineHint>() == null) smr.gameObject.AddComponent<CelOutlineHint>();
                StartCoroutine(ClerkFace(anim, rig.gameObject, model, npcRoot));
            }
            string who = shop ? Loc.T("상점 아줌마", "Shopkeeper") : Loc.T("알바나라 아저씨", "Job clerk");
            _spots.Add(new Spot { id = "counter", title = shop ? Loc.T("🛍 " + who + " · 장보기", "🛍 " + who + " · Buy") : Loc.T("💼 " + who + " · 알바 고르기", "💼 " + who + " · Pick a job"), pos = new Vector3(ox, fy, oz + 0.6f), radius = 2.0f, on = shop ? (Action)ShopCounterMenu : JobMenu });
            var bubble = SpeechBubble.Create(npcRoot, 1.55f); bubble.Show(who + ": " + (shop ? Loc.T("어서 와! 뭐 찾는 거 있어?", "Welcome! Looking for something?") : Loc.T("어서 와. 오늘 할 만한 알바가 있지.", "Come in. There's work today.")));
        }
        IEnumerator ClerkFace(Animator anim, GameObject rig, string model, Transform root)
        {
            yield return null;
            var tex = Resources.Load<Texture2D>("CoastRun/Textures/Village/UI_Face_" + model) ?? Resources.Load<Texture2D>("CoastRun/Textures/Village/UI_Face_Haneul");
            if (tex != null && anim != null && rig != null) FaceDecal.Attach(anim, rig, tex, root.forward);
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
            _spots.RemoveAll(s => s.id == "exit" || s.id.StartsWith("home_") || s.id == "counter" || s.id.StartsWith("hosp_") || s.id.StartsWith("in_")); _interiorKind = null;   // 171차: 카운터 스팟도
            Teleport(it.ReturnPos, it.ReturnYaw); Destroy(it.gameObject);
            // 191차(사용자: 「병원에서 나오면 건물만 보이고 인물이 안 보인다」): 문을 등지고 서면 카메라가 뒤 = 집 지붕 위에 놓여 지붕이 주인공을 덮었다
            // → 나올 때는 카메라를 앞(길 쪽)으로 돌려 주인공과 뒤의 집이 함께 보이게. 걷기 시작하면 평소처럼 뒤로 따라 돈다
            _camYaw = _camYawTarget = it.ReturnYaw + 180f; _cam.transform.position = CamTarget; _cam.transform.LookAt(CamLook); _camVel = Vector3.zero; _snapCam = true;
            _doorCooldown = Time.time + 3f;   // 154차: 문 앞에 나와서 바로 다시 들어가지 않게
            yield return null; yield return FadeScreen(false, 0.3f); _busy = false;
        }

        public void Teleport(Vector3 p, float faceYaw = float.NaN) { _walkTo = null; _vel = Vector3.zero; _cc.enabled = false; _player.position = new Vector3(p.x, VillageWorld.Height(p.x, p.z), p.z); if (!float.IsNaN(faceYaw)) _player.rotation = Quaternion.Euler(0f, faceYaw, 0f); _cc.enabled = true; SnapCamera(); if (_creatures != null) _creatures.SnapKid(); }
        /// 181차 진단: 지금 보는 방향으로 3초 곧장 걷게 하고, 0.25초마다 옆으로 벗어난 거리·마지막으로 부딪힌 콜라이더를 로그로 남긴다
        public void DevLegacyCc(bool legacy) { _cc.enabled = false; _cc.center = legacy ? Vector3.zero : new Vector3(0f, 0.62f, 0f); _cc.slopeLimit = legacy ? 45f : 50f; _cc.stepOffset = legacy ? 0.3f : 0.28f; _cc.enabled = true; Debug.LogWarning("[Cc] legacy=" + legacy); }   // 181차 비교용
        public void DevWalkTest(float yaw) { StartCoroutine(WalkTestCo(yaw)); }
        IEnumerator WalkTestCo(float yaw)
        {
            var fwd = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward; var start = _player.position;
            var hl = _player.GetComponent<CcHitLog>(); var sb = new System.Text.StringBuilder($"[WalkTest] yaw={yaw:F0} from={start}");
            for (float t = 0f; t < 3f; t += 0.25f)
            {
                WalkTo(_player.position + fwd * 6f);
                yield return new WaitForSeconds(0.25f);
                var d = _player.position - start; float along = Vector3.Dot(d, fwd); float side = Vector3.Dot(d, Vector3.Cross(Vector3.up, fwd));
                sb.Append($" | {along:F1}/{side:+0.00;-0.00} {(hl != null && Time.time - hl.At < 0.3f ? hl.Last : "")}");
            }
            _walkTo = null; Debug.LogWarning(sb.ToString());
        }
        public string CamDiag
        {
            get
            {
                float yaw = _camYaw + _camDodge; var lk = CamLookAt(yaw); var tg = CamTargetAt(yaw);
                return $"dodge={_camDodge:F0}/{_camDodgeTarget:F0} low={_camLowK:F2} high={_camHighK:F2} vis={CamVisualFree(lk, tg):F1} free={CamFree(lk, tg, true):F1} need={(tg - lk).magnitude:F1} hid={_camHidden.Count}";
            }
        }   // 171차
        public Vector3 PlayerPos => _player != null ? _player.position : Vector3.zero;
        public float PlayerYaw => _player != null ? _player.eulerAngles.y : 0f;
        public Vector3 SpotPos(string id) { foreach (var s in _spots) if (s.id == id) return s.pos; return _player.position; }

        void UpdateNear()
        {
            UpdateDoorGlow();   // 157차
            _near = null; float best = 999f; var p = _player.position;
            foreach (var s in _spots) { float d = Vector3.Distance(new Vector3(p.x, 0f, p.z), new Vector3(s.pos.x, 0f, s.pos.z)); if (d < s.radius && d < best) { best = d; _near = s; } }
            _nearTree = null; _nearTreeIdx = -1; best = 2.8f;
            for (int i = 0; i < VillageWorld.Trees.Count; i++)
            {
                var t = VillageWorld.Trees[i]; if (t == null) continue;
                float d = Vector3.Distance(new Vector3(p.x, 0f, p.z), new Vector3(t.position.x, 0f, t.position.z));
                if (d < best) { best = d; _nearTree = t; _nearTreeIdx = i; }
            }
            // 154차: 가까운 바위(곡괭이)
            _nearRock = null; _nearRockIdx = -1; float bestR = 2.6f;
            foreach (var rk in VillageWorld.Rocks)
            {
                if (rk.t == null) continue;
                float d = Vector3.Distance(new Vector3(p.x, 0f, p.z), new Vector3(rk.t.position.x, 0f, rk.t.position.z));
                if (d < bestR) { bestR = d; _nearRock = rk.t; _nearRockIdx = rk.idx; }
            }
            // 154차(사용자: 「각 건물 문에서 안으로」): 문 앞 0.9 m 에 닿으면 자동으로 들어간다(나온 뒤 2.5 s 는 쉼)
            if (!_busy && !_hud.Locked && _interior == null && Time.time > _doorCooldown)
            {
                foreach (var hs in VillageWorld.Houses)
                {
                    if (hs.house == null) continue;
                    float dd = Vector3.Distance(new Vector3(p.x, 0f, p.z), new Vector3(hs.door.x, 0f, hs.door.z));
                    if (dd > 0.9f) continue;
                    _doorCooldown = Time.time + 3f;
                    // 156차: 우리집은 늘 집 안(식탁·침대·책상)으로, 알바나라는 알바 고르기
                    if (hs.house == VillageWorld.JobHouse) EnterJobHouse();   // 171차: 실내 카운터
                    else if (hs.house == VillageWorld.HeroHouse) { var h0 = hs; AskEnterHome(() => EnterHouse(h0.house, h0.name, h0.door)); }   // 185차: 우리집은 물어본다
                    else EnterHouse(hs.house, hs.name, hs.door);
                    break;
                }
                if (VillageWorld.Shop != null && Time.time > _doorCooldown)
                {
                    var sd = VillageWorld.Shop.TransformPoint(new Vector3(0f, 0f, 2.6f));
                    if (Vector3.Distance(new Vector3(p.x, 0f, p.z), new Vector3(sd.x, 0f, sd.z)) < 0.9f) { _doorCooldown = Time.time + 3f; EnterShopHouse(); }   // 171차: 실내 카운터
                }
            }
            if (_yard) { _hud.SetPrompt(null, null); _hud.SetAction(Loc.T("놓기", "Place")); return; }
            // 150차: 텃밭 안에서는 서 있는 칸의 상태로 안내가 바뀐다(씨/물/수확/잡초) + 노란 링
            int tile = _near != null && _near.id == "garden" ? VillageFarm.TileAt(p) : -1;
            if (_near != null && _near.id == "garden") _near.title = FarmTitle(tile);
            if (_near != null && _near.id == "farm" && Save != null) _near.title = Save.farmEggs > 0 ? Loc.T($"🥚 달걀 {Save.farmEggs}개 줍기", $"🥚 Collect {Save.farmEggs} eggs") : Loc.T($"🐔 농장 · 닭 {Save.farmChickens} · 토끼 {VillageLivestock.Rabbits(Save)}(다 큼 {VillageLivestock.GrownRabbits(Save)})", $"🐔 Farm · {Save.farmChickens} hens · {VillageLivestock.Rabbits(Save)} rabbits ({VillageLivestock.GrownRabbits(Save)} grown)");
            if (VillageFarm.Marker != null)
            {
                bool show = tile >= 0; if (VillageFarm.Marker.gameObject.activeSelf != show) VillageFarm.Marker.gameObject.SetActive(show);
                if (show) { var c = VillageFarm.TileCenter(tile); VillageFarm.Marker.position = new Vector3(c.x, VillageWorld.Height(c.x, c.z) + VillageFarm.Top + 0.012f, c.z); }
            }
            _hud.SetPrompt(_near != null ? "▶ " + _near.title : null, _near != null ? _near.on : null);
            bool critter = _creatures != null && !_axe && !_rod && !_pick && _creatures.AnyCritterNear(_bat);
            string toolAct = _rod ? Loc.T("낚시", "Fish") : _pick ? Loc.T("돌 캐기", "Mine") : _axe ? Loc.T("나무 패기", "Chop") : _bat ? Loc.T("휘두르기", "Swing") : Loc.T("잡기", "Catch");
            _hud.SetAction(_nearDoorOn != null && !critter ? Loc.T("들어가기", "Enter") : critter ? toolAct : _pick && _nearRock != null ? Loc.T("돌 캐기", "Mine") : _nearTree != null ? (_axe ? Loc.T("나무 패기", "Chop") : Loc.T("흔들기", "Shake")) : _near != null && _near.id == "beach" ? Loc.T("낚시", "Fish") : toolAct);
        }

        // ── 버튼 ─────────────────────────────────────────────────────────
        void OnAct()
        {
            if (_busy || _hud.Locked) return;
            if (_yard) { PlaceGhost(); return; }
            if (DungeonAct()) return;   // 199차: 깊은 갱도 — 싸움·광맥
            // 157차: 반짝이는 문 앞에서 행동 버튼 = 들어가기(벌레가 코앞이면 잡기가 먼저)
            if (_nearDoorOn != null && !(_creatures != null && _creatures.AnyCritterNear(_bat))) { _doorCooldown = Time.time + 3f; var on = _nearDoorOn; on(); return; }
            if (_road) { PlaceRoad(); return; }   // 187차: 도로 깔기
            // 168차: 낚싯대 — 바닷가 스팟이면 낚시, 아니면 헛던지기
            if (_bat && Save != null && VillageLivestock.Inside(_player.position) && VillageLivestock.GrownRabbits(Save) > 0)
            {
                // 171차: 농장 안에서 방망이 = 다 큰 토끼 → 고기 2
                Swing(); VillageLivestock.Butcher(Save); VillageLivestock.Build(_world, Save); _gm.Persist(); RefreshStatus();
                VillagePang.Burst(_player.position + _player.forward * 0.8f + Vector3.up * 0.4f, new Color(1f, 0.6f, 0.5f), Color.white, 1.0f);
                CoastToast.Show(Loc.T($"🍖 토끼를 잡아 고기 2를 얻었다 (남은 토끼 {VillageLivestock.Rabbits(Save)})", $"🍖 Butchered a rabbit — meat ×2 ({VillageLivestock.Rabbits(Save)} left)")); return;
            }
            if (_rod)
            {
                // 171차(사용자: 「낚싯대로 물고기 잡을 수 있게」): 바닷가 스팟(반경 4.5 m)뿐 아니라 물가 어디서든 — 앞 3 m 가 물이거나 발밑이 물가 모래면 던진다
                var pf = _player.position + _player.forward * 3f;
                bool water = VillageWorld.Height(pf.x, pf.z) < VillageWorld.SeaLevel + 0.25f || (VillageWorld.Height(_player.position.x, _player.position.z) < VillageWorld.SeaLevel + 1.0f && _player.position.z < -14f);
                if ((_near != null && _near.id == "beach") || water) { OpenFishing(); return; }
                Swing(); CoastToast.Show(Loc.T("휙— 물가에서 물을 보고 던져야 물고기가 문다.", "Whoosh — face the water at the shore.")); return;
            }
            // 154차: 곡괭이 — 바위 곁이면 캔다(돌), 아니면 헛스윙
            if (_pick && TryMineOre()) return;   // 195차: 광산 광맥
            if (_pick) { if (_nearRock != null) { StartCoroutine(MineRock(_nearRock, _nearRockIdx)); return; } Swing(); CoastToast.Show(Loc.T("탕— 언덕의 바위 곁에서 휘둘러야 돌이 나온다.", "Clang — swing next to a hill rock.")); return; }
            // 150차: 도끼 — 나무 곁이면 팬다(장작), 아니면 헛스윙
            if (_axe) { if (_nearTree != null) { StartCoroutine(ChopTree(_nearTree, _nearTreeIdx)); return; } Swing(); CoastToast.Show(Loc.T("붕— 나무 곁에서 휘둘러야 장작이 나온다.", "Whoosh — swing next to a tree.")); return; }
            // 도구 먼저: 큰 벌레(방망이)·정령/나비(잠자리채)가 가까우면 휘두른다 (170차)
            if (_creatures != null && _creatures.AnyCritterNear(_bat))
            {
                Swing(); var msg = _creatures.Swing(_bat); if (!string.IsNullOrEmpty(msg)) CoastToast.Show(msg); return;
            }
            if (_nearTree != null) { StartCoroutine(ShakeTree(_nearTree, _nearTreeIdx)); return; }
            if (_near != null && _near.id == "beach") { OpenFishing(); return; }
            Swing();
            var m2 = _creatures != null ? _creatures.Swing(_bat) : null;
            if (m2 == "") return;   // 183차: 말이 쓰러짐 — 메시지는 HorseKilled 가
            CoastToast.Show(m2 ?? (_bat ? Loc.T("붕— 큰 벌레가 없다. 왕사슴벌레·왕말벌은 가끔 나타나 다가온다.", "Whoosh — no big bug. They show up now and then.") : Loc.T("휙— 잡을 게 없다. 나비나 정령 곁에서 휘둘러 보자.", "Swish — nothing to catch nearby.")));
        }

        void OnTalk()
        {
            if (_busy || _hud.Locked) return;
            // 마을 사람이 가까우면 그쪽(대답은 「누구세요?」「음…」뿐), 아니면 옆에 붙어 다니는 꼬마
            // 171차: 부탁이 있는 마을 사람이면 부탁부터(받기·진행·보상)
            int ni = _creatures != null ? _creatures.NearestNpcIndex(3.2f) : -1;
            if (ni >= 0 && VillageRequest.OnTalk(this, ni)) { MissionTick(VillageMission.Kind.Talk); return; }
            if (_creatures != null && _creatures.TalkNearestNpc()) { MissionTick(VillageMission.Kind.Talk); FriendTalkVillager(ni); return; }   // 195차: 호감도   // 160차: 대화 미션
            if (_creatures != null && !_creatures.KidHidden && StoryKidTalk()) return;   // 196차: 갈 곳이 있으면 꼬마가 데려다준다
            if (_creatures != null) { _hud.Bubble(Loc.T("꼬마", "Kid"), _creatures.KidLine()); return; }
            if (_near != null) { _hud.Bubble(Loc.T("하늘", "Haneul"), _near.title); return; }
        }

        void OnTool()
        {
            if (_busy || _hud.Locked) return;
            _hud.Choice(Loc.T("도구 고르기", "Choose a tool"), Loc.T("잠자리채: 나비·정령 잡기(별조각·코인) · 방망이: 큰 벌레·산적·말(고기)을 친다, 정령은 흩어질 뿐. 큰 벌레와 부딪히면 HP가 깎여!", "Net: catch butterflies and spirits (shards, coins) · Bat: bop big bugs (shards). Bumping a big bug costs HP!"),
                new (string, Color, Action)[] {
                    (Loc.T("🦋 잠자리채", "🦋 Net") + (!_bat && !_axe && !_pick && !_rod && !_road ? Loc.T(" (지금)", " (now)") : ""), new Color(0.45f, 0.78f, 0.55f), () => { _rod = false; _bat = false; _axe = false; _pick = false; _road = false; ApplyTool(); }),
                    (Loc.T("🏏 방망이", "🏏 Bat") + (_bat && !_axe ? Loc.T(" (지금)", " (now)") : ""), new Color(0.98f, 0.62f, 0.45f), () => { _rod = false; _bat = true; _axe = false; _pick = false; _road = false; ApplyTool(); }),
                    (Loc.T("🪓 도끼 — 나무 패서 장작(3번이면 쓰러짐, 8주 뒤 다시)", "🪓 Axe — chop trees (falls after 3, regrows in 8w)") + (_axe && !_pick ? Loc.T(" (지금)", " (now)") : ""), new Color(0.62f, 0.52f, 0.40f), () => { _rod = false; _axe = true; _pick = false; _road = false; ApplyTool(); }),
                    (Loc.T("⛏ 곡괭이 — 언덕 바위 캐서 돌(3번이면 사라짐, 8주 뒤 다시)", "⛏ Pickaxe — mine hill rocks (gone after 3, regrows in 8w)") + (_pick ? Loc.T(" (지금)", " (now)") : ""), new Color(0.55f, 0.58f, 0.66f), () => { _rod = false; _pick = true; _axe = false; _bat = false; _road = false; ApplyTool(); }),
                    (Loc.T("🎣 낚싯대 — 바닷가에서 낚시", "🎣 Rod — fish at the beach") + (_rod ? Loc.T(" (지금)", " (now)") : ""), new Color(0.40f, 0.62f, 0.85f), () => { _rod = true; _pick = false; _axe = false; _bat = false; _road = false; ApplyTool(); }),
                    (Loc.T($"🛣 도로 깔기 — 한 칸 {VillageRoad.Price}G, 이어진 도로로만 자동이동", $"🛣 Lay road — {VillageRoad.Price}G a tile, auto-move follows roads") + (_road ? Loc.T(" (지금)", " (now)") : ""), new Color(0.80f, 0.66f, 0.45f), () => { _rod = false; _pick = false; _axe = false; _bat = false; _road = true; ApplyTool(); CoastToast.Show(Loc.T($"🛣 도로 깔기: 앞 칸(초록 네모)에 행동 버튼 → {VillageRoad.Price}G", $"🛣 Road: press action on the green tile → {VillageRoad.Price}G")); }),
                });
        }

        /// 도구 표시: 오른손 뼈에 잠자리채(막대+고리) 또는 방망이(막대) 붙이기 + 버튼 라벨
        /// 개발용: 도구 바로 바꾸기(0 잠자리채 1 방망이 2 도끼)
        public void DevHour(float h) { if (_dayNight != null) _dayNight.Hour = h; RefreshStatus(); }
        public void DevTool(int t) { _road = t == 5; _bat = t == 1; _axe = t == 2; _pick = t == 3; _rod = t == 4; ApplyTool(); }
        void ApplyTool()
        {
            PushToolTiers();   // 198차
            if (_hud != null) _hud.SetTool(_road ? Loc.T("도로", "Road") : ToolLabel(_rod ? 4 : _pick ? 3 : _axe ? 2 : _bat ? 1 : 0), _road ? 5 : _rod ? 4 : _pick ? 3 : _axe ? 2 : _bat ? 1 : 0);
            StartCoroutine(OutlineToolLater());
            if (_toolVis != null) Destroy(_toolVis.gameObject);
            if (_road) { _toolVis = null; return; }   // 187차: 도로 깔기 — 손에 든 것 없음(앞 칸 미리보기로 대신)
            Transform hand = _anim != null && _anim.avatar != null && _anim.avatar.isHuman ? _anim.GetBoneTransform(HumanBodyBones.RightHand) : null;
            var root = new GameObject("ToolVis").transform; _toolVis = root;
            root.SetParent(hand != null ? hand : _rigT, false);
            root.localPosition = hand != null ? new Vector3(0f, -0.02f, 0.02f) : new Vector3(0.28f, 0.7f, 0.1f);
            root.localRotation = Quaternion.Euler(-70f, 0f, 0f);
            float k = hand != null ? 1f / Mathf.Max(0.01f, hand.lossyScale.x) : 1f;
            // 168차(사용자: 「블렌더로 도구 다시」): Models/VTool_{Net,Bat,Axe,Pick,Rod}.fbx(village_tool_kit.py, 원점 = 손잡이 아래, +Y 위, 높이 1 m) 가 있으면 그것을 손에
            var kitTool = JejuKit.Spawn("VTool_" + (_rod ? "Rod" : _pick ? "Pick" : _axe ? "Axe" : _bat ? "Bat" : "Net"), root, Vector3.zero, 0f, k);
            if (kitTool != null) return;
            if (_pick)
            {
                // 154차: 곡괭이 — 자루 + 가로 쇠머리(양끝 뾰족)
                var h = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(h.GetComponent<Collider>()); h.transform.SetParent(root, false);
                h.transform.localPosition = new Vector3(0f, 0.30f * k, 0f); h.transform.localScale = new Vector3(0.05f, 0.34f, 0.05f) * k;
                h.GetComponent<MeshRenderer>().sharedMaterial = CoastMaterials.CreateLit(new Color(0.62f, 0.44f, 0.28f));
                var hd = GameObject.CreatePrimitive(PrimitiveType.Capsule); Destroy(hd.GetComponent<Collider>()); hd.transform.SetParent(root, false);
                hd.transform.localPosition = new Vector3(0f, 0.62f * k, 0f); hd.transform.localRotation = Quaternion.Euler(0f, 0f, 90f); hd.transform.localScale = new Vector3(0.07f, 0.20f, 0.07f) * k;
                hd.GetComponent<MeshRenderer>().sharedMaterial = CoastMaterials.CreateLit(new Color(0.55f, 0.58f, 0.64f));
            }
            else if (_axe)
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

        bool _swinging; float _swingArm; int _swingStyle;   // 171차: 0 내리찍기(방망이·도끼·곡괭이) 1 옆으로 쓸기(잠자리채) 2 던지기(낚싯대)
        VillageDayNight _dayNight; bool _nightWarned; float _ghostSpawnAt;   // 155차   // 154차: 팔을 머리 위로 드는 정도(-1 위 … 0.55 내리침)
        /// 153차(사용자: 「잠자리 잡는 모션 부드럽게」): 도구를 살짝 뒤로 당겼다가(0.12 s) 앞으로 호를 그리며 휘두르고(0.2 s) 천천히 제자리(0.3 s),
        /// 몸은 CharacterMotion 으로 앞으로 기울었다 돌아옴. 애니는 Collect 트리거 뒤 Play 로 뚝 끊지 않고 CrossFade 로 되돌린다.
        void Swing()
        {
            if (_swinging) return;
            StartCoroutine(SwingCo());
            CoastPrefs.Vibrate();
        }
        static float EaseInOut(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }
        IEnumerator SwingCo()
        {
            _swinging = true;
            // 171차(사용자: 「잠자리채·방망이 등 모션이 이상하다」): 도구마다 다른 동작 — 방망이·도끼·곡괭이 = 내리찍기(Collect 클립 + 상체 숙임),
            // 잠자리채 = 옆으로 쓸기(클립 없이 팔·몸통만, 0.5 s), 낚싯대 = 뒤로 젖혔다 던지기(0.7 s). 도구 회전도 스타일에 맞춘다.
            _swingStyle = _rod ? 2 : (!_bat && !_axe && !_pick) ? 1 : 0;
            if (_anim != null && _swingStyle == 0) { _anim.speed = 1f; _anim.SetTrigger("Collect"); }
            var tv = _toolVis; var rest = tv != null ? tv.localRotation : Quaternion.identity;
            float t = 0f; float dur = _swingStyle == 1 ? 0.50f : _swingStyle == 2 ? 0.72f : 0.62f;
            while (t < dur)
            {
                t += Time.deltaTime; float k;
                if (_swingStyle == 1)
                {
                    // 쓸기: 오른쪽 바깥으로 빠르게 젖혔다(0.10 s) 왼쪽 앞으로 크게 쓸고(0.22 s) 되돌아온다
                    if (t < 0.10f) k = -1f * EaseInOut(t / 0.10f);
                    else if (t < 0.32f) k = Mathf.Lerp(-1f, 1f, EaseInOut((t - 0.10f) / 0.22f));
                    else k = Mathf.Lerp(1f, 0f, EaseInOut((t - 0.32f) / 0.18f));
                    _swingArm = k;
                    if (tv != null) tv.localRotation = rest * Quaternion.Euler(-35f * Mathf.Abs(k), 0f, k * 40f);
                }
                else if (_swingStyle == 2)
                {
                    // 던지기: 뒤로 젖힘(0.22 s) → 앞으로 휙(0.14 s) → 멈춤(0.16) → 복귀
                    if (t < 0.22f) k = -1f * EaseInOut(t / 0.22f);
                    else if (t < 0.36f) k = Mathf.Lerp(-1f, 1f, EaseInOut((t - 0.22f) / 0.14f));
                    else if (t < 0.52f) k = 1f;
                    else k = Mathf.Lerp(1f, 0f, EaseInOut((t - 0.52f) / 0.20f));
                    _swingArm = k;
                    if (tv != null) tv.localRotation = rest * Quaternion.Euler(k * -75f, 0f, 0f);
                }
                else
                {
                    // 154차(사용자: 「위에서 아래로 잡는 모션」): 머리 위로 크게 들었다가(0.18 s) 앞으로 내리찍고(0.12 s) 잠깐 멈춘 뒤 복귀
                    if (t < 0.18f) k = -1f * EaseInOut(t / 0.18f);
                    else if (t < 0.30f) k = Mathf.Lerp(-1f, 0.55f, EaseInOut((t - 0.18f) / 0.12f));
                    else if (t < 0.40f) k = 0.55f;
                    else k = Mathf.Lerp(0.55f, 0f, EaseInOut((t - 0.40f) / 0.22f));
                    _swingArm = k;
                    if (tv != null) tv.localRotation = rest * Quaternion.Euler(k * -110f, k * 10f, 0f);
                }
                yield return null;
            }
            if (tv != null) tv.localRotation = rest; _swingArm = 0f;
            if (_anim != null && !_moving && _swingStyle == 0) { _anim.CrossFade("Run", 0.3f, 0, 0.12f); yield return new WaitForSeconds(0.3f); if (!_moving && _anim != null) _anim.speed = 0f; }
            _swinging = false;
        }

        /// 155차: 귀신에게 맞음 — HP 0 이면 병원(돈 20%, 다음 날 아침 집 앞)
        /// 171차: 산적에게 잡힘 — HP −8, 돈 2%(최대 100G) 뺏김
        void BanditHit(int dmg)
        {
            if (Save == null) return;
            Save.stats.stamina = Mathf.Max(1, Save.stats.stamina - dmg);
            int steal = Mathf.Min(400, Mathf.RoundToInt(Save.stats.money * 0.02f));   // 186차: 상한 100→400 Save.stats.money = Mathf.Max(0, Save.stats.money - steal);
            _gm.Persist(); RefreshStatus(); CoastPrefs.VibrateEvent();
            CoastToast.Show(Loc.T($"💢 산적에게 잡혔다! HP −{dmg} · {steal}G 뺏김 — 방망이로 두 번!", $"💢 Grabbed by a bandit! HP −{dmg} · lost {steal}G — bat them twice!"));
            VillagePang.Burst(_player.position + Vector3.up * 0.9f, new Color(0.95f, 0.5f, 0.3f), new Color(1f, 0.85f, 0.4f), 1.2f);
            StartCoroutine(HitFlash());
        }
        void GhostHit(int dmg)
        {
            if (Save == null || _busy) return;
            Save.stats.stamina = Mathf.Max(0, Save.stats.stamina - dmg);
            _gm.Persist(); RefreshStatus(); CoastPrefs.VibrateEvent(); StartCoroutine(HitFlash());
            if (_motion != null) _motion.Hop();
            if (Save.stats.stamina <= 0) { StartCoroutine(Hospital()); return; }
            CoastToast.Show(Loc.T($"👻 귀신에게 잡혔다! HP −{dmg} — 집으로 도망쳐!", $"👻 The ghost got you! HP −{dmg} — run home!"));
        }
        IEnumerator Hospital()
        {
            _busy = true;
            yield return FadeScreen(true, 0.6f);
            int lost = Mathf.RoundToInt(Save.stats.money * 0.2f); Save.stats.money -= lost;
            Save.stats.stamina = Mathf.Max(40, PlayerStats.StatMax / 3); Save.condition = Mathf.Max(Save.condition, 40);
            if (_creatures != null) _creatures.ClearGhosts();
            if (_dayNight != null) _dayNight.SetMorning();
            // 189차(사용자: 「병원에서 눈을 뜰 때는 병원 침대 옆에서 보이게」): 병원 안 첫 침대 옆에서 깨어난다(나가면 병원 문 앞)
            if (!WakeInHospital())
            {
                var home = VillageWorld.HeroHouse != null ? VillageWorld.HeroHouse.TransformPoint(new Vector3(0f, 0f, 4.5f)) : new Vector3(0f, 0f, 30f);
                Teleport(new Vector3(home.x, 0f, home.z), 180f);
            }
            _gm.Persist(); RefreshStatus();
            yield return new WaitForSeconds(0.6f);
            yield return FadeScreen(false, 0.6f);
            _busy = false;
            string tail = _hospSea ? Loc.T("바다엔 10초 넘게 있으면 안 돼!", "Don't stay in the sea over 10s!") : _hospDun ? Loc.T("갱도에선 HP 를 보며 싸우자.", "Watch your HP in the shaft.") : Loc.T("밤엔 꼭 집으로!", "Go home at night!"); _hospSea = false; _hospDun = false;   // 192차
            _hud.Bubble(Loc.T("하늘", "Haneul"), Loc.T($"…병원에서 눈을 떴다. 치료비로 {lost:N0}G 를 냈다(20%). ", $"…Woke up in the hospital. Paid {lost:N0}G (20%). ") + tail);
        }
        /// 155차: 밤에 집에 들어가면 잔다 → 다음 날 08:00, HP +40
        IEnumerator SleepHome()
        {
            _busy = true; _doorCooldown = Time.time + 4f;
            yield return FadeScreen(true, 0.5f);
            Save.stats.stamina = Mathf.Min(PlayerStats.StatMax, Save.stats.stamina + 40); Save.condition = Mathf.Min(100, Save.condition + 5);
            if (_creatures != null) _creatures.ClearGhosts();
            if (_dayNight != null) _dayNight.SetMorning();
            var home = VillageWorld.HeroHouse != null ? VillageWorld.HeroHouse.TransformPoint(new Vector3(0f, 0f, 4.5f)) : _player.position;
            Teleport(new Vector3(home.x, 0f, home.z), 180f);
            _gm.Persist(); RefreshStatus();
            yield return new WaitForSeconds(0.5f);
            yield return FadeScreen(false, 0.5f);
            _busy = false;
            CoastToast.Show(Loc.T("푹 잤다. 다음 날 아침 8시 — HP +40 · 컨디션 +5", "Slept well. 8 AM next day — HP +40 · condition +5"));
        }
        void SpiritHit(int dmg)
        {
            if (Save == null) return;
            if (StoryCalm) return;   // 196차
            Save.stats.stamina = Mathf.Max(1, Save.stats.stamina - dmg);
            _gm.Persist(); RefreshStatus(); CoastPrefs.VibrateEvent();
            CoastToast.Show(Loc.T($"💥 큰 벌레에게 물렸다! HP −{dmg} — 방망이로 먼저 톡!", $"💥 Bitten by a big bug! HP −{dmg} — bop it with the bat first!"));
            VillagePang.Burst(_player.position + Vector3.up * 0.9f + _player.forward * 0.3f, new Color(0.95f, 0.45f, 0.65f), new Color(0.75f, 0.6f, 1f), 1.2f);   // 162차: 부딪히면 팡
            StartCoroutine(HitFlash());
        }
        IEnumerator HitFlash()
        {
            var rs = _rigT != null ? _rigT.GetComponentsInChildren<Renderer>() : new Renderer[0];
            // 183차: 깜빡이는 동안 도구가 바뀌면(자동사냥) 옛 도구 렌더러가 파괴돼 있다 — null 건너뛰기
            for (int i = 0; i < 3; i++) { foreach (var r in rs) if (r != null) r.enabled = false; yield return new WaitForSeconds(0.08f); foreach (var r in rs) if (r != null) r.enabled = true; yield return new WaitForSeconds(0.08f); }
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
                    (Loc.T($"🎯 오늘 미션 — {VillageMission.Title(MissionKind)}", $"🎯 Today's mission — {VillageMission.Title(MissionKind)}"), new Color(0.98f, 0.55f, 0.35f), MissionPopup),
                    (Loc.T($"🎮 놀기 · 연습 (오늘 활동 {ActCount}/{ActsPerDay})", $"🎮 Play · Practice ({ActCount}/{ActsPerDay})"), new Color(0.45f, 0.78f, 0.55f), PlayMenu),
                    (Loc.T("🏠 우리집으로 순간이동", "🏠 Warp home"), new Color(0.35f, 0.62f, 0.95f), () => StartCoroutine(WarpHome())),
                    (Loc.T("🛍 마을상점", "🛍 Village shop"), new Color(0.98f, 0.62f, 0.72f), OpenShop),
                    (Loc.T("🧭 자동 이동 — 장소를 고르면 알아서 걸어간다", "🧭 Auto-move — pick a place"), new Color(0.40f, 0.70f, 0.95f), AutoMoveMenu),
                    (_autoHunt ? Loc.T("⚔ 자동사냥 끄기", "⚔ Auto-hunt off") : Loc.T("⚔ 자동사냥 켜기 — 가까운 벌레·정령·산적을 알아서", "⚔ Auto-hunt on"), new Color(0.92f, 0.45f, 0.45f), ToggleAutoHunt),
                    (Loc.T("💰 물건 팔기 (가방 → 코인)", "💰 Sell items"), new Color(0.98f, 0.80f, 0.30f), SellMenu),
                    (Loc.T($"🏡 땅 · 월세 (보유 {VillageLand.OwnedCount(Save)}곳 · 한 달 {VillageLand.MonthlyRent(Save)}G)", $"🏡 Land · rent ({VillageLand.OwnedCount(Save)} lots · {VillageLand.MonthlyRent(Save)}G/mo)"), new Color(0.45f, 0.75f, 0.50f), LandMenu),
                    (Loc.T("🎒 가방", "🎒 Bag"), new Color(0.98f, 0.78f, 0.35f), OnBag),
                    (Loc.T($"📖 도감 — 물고기·벌레·광물·화석·주민 호감도", "📖 Encyclopedia · friendships"), new Color(0.62f, 0.55f, 0.92f), () => OpenDex(0)),   // 195차
                    (Loc.T("🏠 타이틀로", "🏠 To title"), new Color(0.55f, 0.55f, 0.62f), () => { _gm.Persist(); _gm.ToTitle(); }),
                });
        }

        // ── 장소 ─────────────────────────────────────────────────────────
        void EnterSchedule()
        {
            if (_busy) return; _busy = true;
            if (Save != null) { var p = _interior != null ? _interior.ReturnPos : _player.position; Save.villageX = p.x; Save.villageZ = p.z; }
            _gm.Persist();
            _driver.OpenScheduleFromVillage();
        }

        void HeroHouseMenu()
        {
            // 137차(사용자): 우리집에 들어가면 = 기존 스토리 홈 화면(주차 스케줄). 마당 꾸미기는 두 번째.
            _hud.Choice(Loc.T("우리집", "Our home"), Loc.T("언덕 꼭대기 우리집. 들어가서 이번 주 할 일을 정하자.", "Our house on the hilltop. Go in and plan the week."),
                new (string, Color, Action)[] {
                    (Loc.T($"🏠 집에 들어가기 (밥 · 잠 · 책상) — 활동 {ActCount}/{ActsPerDay}", $"🏠 Go inside (eat · sleep · desk) — {ActCount}/{ActsPerDay}"), new Color(0.35f, 0.62f, 0.95f), () => EnterHouse(VillageWorld.HeroHouse, Loc.T("우리집", "Our home"), VillageWorld.HeroHouse.TransformPoint(new Vector3(0f, 0f, 3.3f)))),
                    (Loc.T($"🔥 난로에 장작 넣기 (장작 {LifeItems.Count(Save, "mat_wood")} · 연료 {Save.villageFuel}주)", $"🔥 Stove firewood (wood {LifeItems.Count(Save, "mat_wood")} · fuel {Save.villageFuel}w)"), new Color(0.95f, 0.55f, 0.35f), AddFuel),
                    (Loc.T("🪑 마당 가구 배치", "🪑 Yard furniture"), new Color(0.45f, 0.78f, 0.55f), () => SetYard(true)),
                    (Loc.T("🛋 내 방 꾸미기", "🛋 My room"), new Color(0.98f, 0.62f, 0.72f), () => OpenHome(0)),
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

        void OpenShop()
        {
            if (_busy) return;
            if (_dayNight != null && !_dayNight.ShopOpen) { _hud.Bubble(Loc.T("상점", "Shop"), Loc.T("문이 닫혔다. 영업은 오전 8시 ~ 오후 7시.", "Closed. Open 8 AM – 7 PM.")); return; }
            _busy = true; ShopUI.Open(_gm, 0, () => { _busy = false; RefreshStatus(); });
        }
        void OpenGarden() { OpenHome(1); }
        void OpenFishing() { if (_busy) return; _busy = true; FishingMini.Open(_gm, () => { _busy = false; MissionTick(VillageMission.Kind.Fish); RefreshStatus(); }); }

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
            if (tile < 0) { GardenMenu(); return; }   // 205차: 밭 칸 밖 — 밭 넓히기·제철 안내
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
                else CoastToast.Show(Loc.T($"{seed.Emoji} {seed.Name}이(가) 시들어 버렸다… 물을 두 주 넘게 못 받았다.", $"{seed.Emoji} The {seed.Name} withered… no water for two weeks."));   // 205차
                AfterFarm(); return;
            }
            if (VillageFarm.CanWater(Save, tile))
            {
                VillageFarm.Water(Save, tile); StartCoroutine(WaterPour(tile));
                int g = Save.farm[tile].growth;
                CoastToast.Show(g >= sd.weeks ? Loc.T($"💧 물을 줬다 — {sd.Name} 다 자랐다! 수확하자", $"💧 Watered — {sd.Name} is ready!") : Loc.T($"💧 물을 줬다 — {sd.Name} {g}/{sd.weeks}. 페이즈마다 한 번, 주가 바뀌면 더 자란다", $"💧 Watered — {sd.Name} {g}/{sd.weeks}"));
                AfterFarm(); return;
            }
            FertilizeAsk(tile);   // 205차: 이미 물 줬으면 비료(★+1)를 묻는다
        }

        void SeedMenu(int tile)
        {
            var opts = new List<(string, Color, Action)>();
            // 205차(사용자: 「계절 씨앗」): 제철 씨앗을 앞에 — 수확은 물만 잘 주면 100 %
            var season = (int)Timeline.SeasonOf(Save.week); var seasonal = new List<SeedDef>();
            foreach (var sd in HomeData.SeasonSeeds) if (sd.season == season) seasonal.Add(sd);
            foreach (var sd in seasonal)
            {
                var seed = sd; bool can = Save.stats.money >= seed.price;
                opts.Add((Loc.T($"{seed.Emoji} 제철 {seed.ko} {seed.price}G · {seed.weeks}주 · 1개 {seed.sell}G", $"{seed.Emoji} {seed.en} {seed.price}G · {seed.weeks}w"), can ? new Color(0.98f, 0.62f, 0.40f) : new Color(0.6f, 0.6f, 0.65f),
                    () => { if (VillageFarm.Plant(Save, tile, seed)) { CoastToast.Show(Loc.T($"{seed.Emoji} {seed.ko} 씨를 뿌렸다. 물을 주자!", $"{seed.Emoji} Planted {seed.en}. Water it!")); AfterFarm(); } else CoastToast.Show(Loc.T("돈이 모자라.", "Not enough money.")); }));
            }
            foreach (var sd in HomeData.Seeds)
            {
                var seed = sd; bool can = Save.stats.money >= seed.price;
                opts.Add((Loc.T($"{seed.Emoji} {seed.ko} {seed.price}G · {seed.weeks}주 · {seed.RewardText}", $"{seed.Emoji} {seed.en} {seed.price}G · {seed.weeks}w · {Mathf.RoundToInt(seed.chance * 100)}% · {seed.RewardText}"),
                    can ? new Color(0.45f, 0.78f, 0.55f) : new Color(0.6f, 0.6f, 0.65f),
                    () => { if (VillageFarm.Plant(Save, tile, seed)) { CoastToast.Show(Loc.T($"{seed.Emoji} {seed.ko} 씨를 뿌렸다. 물을 주자!", $"{seed.Emoji} Planted {seed.en}. Water it!")); AfterFarm(); } else CoastToast.Show(Loc.T("돈이 모자라.", "Not enough money.")); }));
            }
            // 169차: 작물 그림(클링) — 없으면 그림 없이 예전처럼
            var icons = new Texture2D[seasonal.Count + HomeData.Seeds.Length];
            for (int k = 0; k < HomeData.Seeds.Length; k++)
            {
                string ck = VillageFarm.CropKey(HomeData.Seeds[k].id);
                if (ck != null) icons[seasonal.Count + k] = Resources.Load<Texture2D>("CoastRun/Textures/Village/UI_Seed_" + ck);
            }
            _hud.Choice(Loc.T("씨 뿌리기", "Plant seeds"), Loc.T($"보유 {Save.stats.money:N0}G · 페이즈마다 물 한 번, 주가 바뀌면 한 단계 더 자란다.", $"{Save.stats.money:N0}G · water once per phase; grows more each week."), opts.ToArray(), icons);
        }

        /// 171차: 농장 문 앞 행동 — 달걀이 있으면 줍고, 없으면 농장 안내 팝업(클링 그림)
        void LivestockAct()
        {
            if (Save == null) return;
            int eggs = VillageLivestock.CollectEggs(Save);
            if (eggs > 0) { MissionTick(VillageMission.Kind.Egg, eggs); _gm.Persist(); RefreshStatus(); CoastAudioManager.PlayAnywhere(CoastSfx.Coin); CoastToast.Show(Loc.T($"🥚 달걀 {eggs}개를 주웠다! (가방 달걀 {LifeItems.Count(Save, "ing_egg")})", $"🥚 Collected {eggs} eggs! ({LifeItems.Count(Save, "ing_egg")} in bag)")); return; }
            Texture2D T(string n) => Resources.Load<Texture2D>("CoastRun/Textures/Village/UI_Farm_" + n);
            int grown = VillageLivestock.GrownRabbits(Save);
            var care205 = CareRows();   // 205차: 돌보기(닭·토끼)·황금 달걀을 맨 위에
            _hud.Choice(Loc.T("🐔 우리 농장", "🐔 Our farm"), Loc.T("산에서 닭·토끼를 잠자리채로 잡아 오면 여기서 키운다. 잠 한 번 = 하루.", "Catch hens and rabbits on the hill with the net. One sleep = one day."), Join205(care205, new[]
            {
                (Loc.T($"닭 {Save.farmChickens}/{VillageLivestock.Cap}마리 — 아침마다 달걀 1개씩", $"Hens {Save.farmChickens}/{VillageLivestock.Cap} — 1 egg each every morning"), new Color(0.98f, 0.80f, 0.45f), (Action)null),
                (Loc.T($"토끼 {VillageLivestock.Rabbits(Save)}/{VillageLivestock.Cap}마리 — 3밤이면 다 큼 (다 큼 {grown})", $"Rabbits {VillageLivestock.Rabbits(Save)}/{VillageLivestock.Cap} — grown after 3 sleeps ({grown} grown)"), new Color(0.75f, 0.72f, 0.80f), (Action)null),
                (Loc.T("달걀 — 줍기: 농장 문 앞에서 행동 버튼", "Eggs — collect at the gate"), new Color(0.95f, 0.90f, 0.70f), (Action)null),
                (Loc.T("고기 — 울타리 안에서 다 큰 토끼를 방망이로", "Meat — bat a grown rabbit inside the pen"), new Color(0.92f, 0.55f, 0.50f), (Action)null),
            }), Pad205(care205.Length, new[] { T("Chicken"), T("Rabbit"), T("Egg"), T("Meat") }));
        }

        void AfterFarm() { MissionTick(VillageMission.Kind.Farm); _gm.Persist(); RefreshStatus(); VillageWorld.BuildCrops(_world, Save); }

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
            _busy = true; MissionTick(VillageMission.Kind.Chop);
            VillageWorld.EnsureGather(Save);
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
                int n = UnityEngine.Random.Range(2, 4) + Tier(2);   // 198차: 도끼 등급
                _drops.Spawn(tree.position + Vector3.up * 1.5f, "mat_wood", n, new Color(0.60f, 0.42f, 0.26f));   // 159차: 조각이 떨어지고 가까이 가면 흡수
                Save.stats.stamina = Mathf.Min(PlayerStats.StatMax, Save.stats.stamina + 1);
                // 154차: 3번 패면 쓰러진다(장작 +4 더) → 그루터기, 8주 뒤 다시 자람
                int hits = idx >= 0 && idx < Save.villageTreeHits.Length ? ++Save.villageTreeHits[idx] : 0;
                if (idx >= 0 && idx < Save.villageTreeGone.Length && hits >= 3)
                {
                    Save.villageTreeGone[idx] = Save.week; Save.villageTreeHits[idx] = 0; _drops.Spawn(tree.position + Vector3.up * 1.2f, "mat_wood", 4, new Color(0.60f, 0.42f, 0.26f));
                    CoastToast.Show(Loc.T($"🌳 나무가 쓰러졌다! 장작 조각 {n + 4}개 — 가까이 가면 주워진다 · {VillageWorld.RegrowWeeks}주 뒤 다시 자란다", $"🌳 Timber! {n + 4} logs dropped — walk over to collect · regrows in {VillageWorld.RegrowWeeks}w"));
                    yield return StartCoroutine(FallTree(tree));
                    VillageWorld.ApplyFelledTrees(_world, Save); _nearTree = null; _nearTreeIdx = -1;
                }
                else CoastToast.Show(Loc.T($"🪓 장작 조각 {n}개 떨어짐 · 체력 +1 · {hits}/3 — 세 번 패면 쓰러진다", $"🪓 {n} logs dropped · stamina +1 · {hits}/3"));
                CoastAudioManager.PlayAnywhere(CoastSfx.Coin); _gm.Persist(); RefreshStatus();
            }
            _busy = false;
        }
        IEnumerator FallTree(Transform tree)
        {
            var rest = tree.rotation; var axis = Vector3.Cross(Vector3.up, (tree.position - _player.position).normalized);
            float t = 0f;
            while (t < 0.9f) { t += Time.deltaTime; float k = EaseInOut(t / 0.9f); tree.rotation = Quaternion.AngleAxis(k * 85f, axis) * rest; yield return null; }
            for (int i = 0; i < 6; i++) StartCoroutine(DropOrange(tree.position + new Vector3(UnityEngine.Random.Range(-1.5f, 1.5f), 1.0f, UnityEngine.Random.Range(-1.5f, 1.5f)), new Color(0.45f, 0.70f, 0.35f), 0.18f));
            yield return new WaitForSeconds(0.4f);
        }
        /// 154차: 곡괭이질 — 3타마다 돌 +1~2, 세 번(9타) 캐면 바위가 사라지고 8주 뒤 다시 생긴다
        IEnumerator MineRock(Transform rock, int idx)
        {
            _busy = true; VillageWorld.EnsureGather(Save); MissionTick(VillageMission.Kind.Mine);
            var rest = rock.localScale;
            for (int hit = 0; hit < 3; hit++)
            {
                Swing(); CoastAudioManager.PlayAnywhere(CoastSfx.SoftHit);
                float t = 0f; while (t < 0.3f) { t += Time.deltaTime; float k = Mathf.Sin(t / 0.3f * Mathf.PI); rock.localScale = rest * (1f - k * 0.06f); yield return null; }
                rock.localScale = rest;
                for (int k2 = 0; k2 < 4; k2++) StartCoroutine(DropOrange(rock.position + new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), 0.9f, UnityEngine.Random.Range(-0.5f, 0.5f)), new Color(0.62f, 0.62f, 0.60f), 0.11f));
                yield return new WaitForSeconds(0.15f);
            }
            if (Save != null)
            {
                int n = UnityEngine.Random.Range(1, 3) + Tier(3);   // 198차: 곡괭이 등급
                bool gem = UnityEngine.Random.value < 0.2f; if (gem) { Save.starShards += 1; Save.starShardsTotal += 1; }
                _drops.Spawn(rock.position + Vector3.up * 1.0f, "mat_stone", n, new Color(0.72f, 0.72f, 0.70f));   // 159차
                int hits = idx >= 0 && idx < Save.villageRockHits.Length ? ++Save.villageRockHits[idx] : 0;
                if (idx >= 0 && idx < Save.villageRockGone.Length && hits >= 3)
                {
                    Save.villageRockGone[idx] = Save.week; Save.villageRockHits[idx] = 0; _drops.Spawn(rock.position + Vector3.up * 0.8f, "mat_stone", 2, new Color(0.72f, 0.72f, 0.70f));
                    CoastToast.Show(Loc.T($"⛏ 바위를 다 캤다! 돌 조각 {n + 2}개{(gem ? " · 별조각 +1" : "")} — 가까이 가면 주워진다 · {VillageWorld.RegrowWeeks}주 뒤 다시 생긴다", $"⛏ Rock cleared! {n + 2} stones dropped — regrows in {VillageWorld.RegrowWeeks}w"));
                    float t = 0f; while (t < 0.35f) { t += Time.deltaTime; rock.localScale = rest * (1f - t / 0.35f); yield return null; }
                    VillageWorld.BuildRocks(_world, Save); _nearRock = null; _nearRockIdx = -1;
                }
                else
                {
                    CoastToast.Show(Loc.T($"⛏ 돌 조각 {n}개 떨어짐{(gem ? " · 별조각 +1" : "")} · {hits}/3", $"⛏ {n} stones dropped · {hits}/3"));
                    VillageWorld.BuildRocks(_world, Save);   // 크기 줄어든 바위로 다시
                }
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
            MissionTick(VillageMission.Kind.Pick);
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

        // ── 155차: 얼굴 데칼 → 161차: FaceDecal.cs 로 공용화(눈 서브메시 제거 + LateUpdate 핀) ─────────────
        FaceDecal _faceDecal;
        IEnumerator AttachFace()
        {
            yield return null;   // 애니메이터가 첫 포즈를 잡은 뒤
            var tex = Resources.Load<Texture2D>("CoastRun/Textures/Village/UI_Face_Haneul"); if (tex == null || _rig == null) yield break;
            _faceDecal = FaceDecal.Attach(_anim, _rig.gameObject, tex, _player.forward);
        }
        /// 데칼 크기·위치 미세 조정(개발용): dx 앞뒤, dy 위아래, s 배율
        public void DevFace(float dx, float dy, float s) { if (_faceDecal != null) _faceDecal.Nudge(_player.forward, dx, dy, s); }

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
            new YardDef { id = "bench", ko = "벤치", en = "Bench", model = "Prop_Bench", price = 1500 },
            new YardDef { id = "planter", ko = "나무 화분", en = "Planter", model = "Kerb_PlanterWood", price = 750 },
            new YardDef { id = "basalt", ko = "현무암 화단", en = "Basalt bed", model = "Kerb_PlanterBasalt", price = 1100 },
            new YardDef { id = "onggi", ko = "항아리", en = "Onggi jar", model = "Kerb_Onggi", price = 1000 },
            new YardDef { id = "buoy", ko = "부표 더미", en = "Buoys", model = "Kerb_Buoys", price = 600 },
            new YardDef { id = "crate", ko = "상자 더미", en = "Crates", model = "Kerb_CrateStack", price = 500 },
            new YardDef { id = "hareubang", ko = "돌하르방", en = "Hareubang", model = "Prop_Hareubang", price = 2500 },
            new YardDef { id = "cafe", ko = "카페 세트", en = "Cafe set", model = "Prop_CafeSet", price = 4000 },
            new YardDef { id = "stall", ko = "귤 매대", en = "Orange stall", model = "Prop_OrangeStall", price = 3000 },
            new YardDef { id = "palm", ko = "야자수", en = "Palm", model = "Prop_Palm", price = 2000 },
            new YardDef { id = "orange", ko = "귤나무", en = "Orange tree", model = "Prop_OrangeTree", price = 2250 },
            new YardDef { id = "pavilion", ko = "정자", en = "Pavilion", model = "Prop_Pavilion", price = 7500 },
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




        // ── 159차(사용자: 「장애물 안으로 몸이 들어간다 / 갇혀서 못 움직인다」) ────────────────
        static readonly Collider[] _ovl = new Collider[12]; float _stuckT; Vector3 _stuckFrom;
        /// 캡슐이 콜라이더와 겹치면 침투 벡터만큼 밀어내고, 스틱을 미는데도 1 s 넘게 못 빠져나오면 가까운 빈 자리로 옮긴다.
        void Unstick(float dt, float mag)
        {
            if (_cc == null || !_cc.enabled) return;
            var p = _player.position; var c = _cc;
            float half = Mathf.Max(0f, c.height * 0.5f - c.radius);
            var p0 = p + c.center - Vector3.up * half; var p1 = p + c.center + Vector3.up * half;
            int n = Physics.OverlapCapsuleNonAlloc(p0, p1, c.radius * 0.92f, _ovl, ~0, QueryTriggerInteraction.Ignore);
            Vector3 push = Vector3.zero; bool any = false;
            for (int i = 0; i < n; i++)
            {
                var col = _ovl[i]; if (col == null || col == c || col.transform.IsChildOf(_player)) continue;
                if (Physics.ComputePenetration(c, p, _player.rotation, col, col.transform.position, col.transform.rotation, out var dir, out var dist))
                {
                    // 181차: 예전엔 수평 성분만 남겨 **정규화**해서, 거의 수직으로 겹친(발밑 바위) 경우에도 옆으로 최대 0.45 m 씩 밀었다 → 옆으로 밀림.
                    // 수평 성분 크기 그대로만 민다(수직 겹침은 밀지 않는다).
                    var h = new Vector3(dir.x, 0f, dir.z) * (dist + 0.02f);
                    if (h.sqrMagnitude > 1e-4f) { push += Vector3.ClampMagnitude(h, 0.25f); any = true; }
                }
            }
            if (any) { c.enabled = false; _player.position = p + push; c.enabled = true; }
            if (mag > 0.3f && any)
            {
                if (Vector3.Distance(p, _stuckFrom) < 0.06f) { _stuckT += dt; if (_stuckT > 1.0f) { _stuckT = 0f; EscapeToFree(); } }
                else { _stuckT = 0f; _stuckFrom = p; }
            }
            else { _stuckT = 0f; _stuckFrom = p; }
        }
        bool FreeAt(Vector3 q)
        {
            var c = _cc; float half = Mathf.Max(0f, c.height * 0.5f - c.radius);
            var p0 = q + c.center - Vector3.up * half; var p1 = q + c.center + Vector3.up * half;
            int n = Physics.OverlapCapsuleNonAlloc(p0, p1, c.radius + 0.06f, _ovl, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++) if (_ovl[i] != null && _ovl[i] != c && !_ovl[i].transform.IsChildOf(_player)) return false;
            return true;
        }
        void EscapeToFree()
        {
            var p = _player.position;
            for (float r = 0.9f; r <= 3.6f; r += 0.9f)
                for (int k = 0; k < 8; k++)
                {
                    float a = k * Mathf.PI * 0.25f + r; var q = new Vector3(p.x + Mathf.Cos(a) * r, 0f, p.z + Mathf.Sin(a) * r);
                    if (_interior != null && !_interior.Contains(q.x, q.z)) continue;
                    q.y = _interior != null ? _interior.FloorY : VillageWorld.Height(q.x, q.z);
                    if (FreeAt(q)) { Teleport(q); CoastToast.Show(Loc.T("끼였다! 옆으로 빠져나왔다.", "Stuck! Wiggled free.")); return; }
                }
        }
        /// 159차(사용자: 「어디서든 우리집으로 순간이동」)
        IEnumerator WarpHome()
        {
            if (_busy) yield break; _busy = true;
            yield return FadeScreen(true, 0.4f);
            if (_interior != null) { var it = _interior; _interior = null; _spots.RemoveAll(s => s.id == "exit" || s.id.StartsWith("home_")); Destroy(it.gameObject); }
            if (_yard) SetYard(false);
            var hh = VillageWorld.HeroHouse; var home = hh != null ? hh.TransformPoint(new Vector3(0f, 0f, 5.6f)) : new Vector3(0f, 0f, 30f);
            Teleport(new Vector3(home.x, 0f, home.z), hh != null ? hh.eulerAngles.y + 180f : 0f); _doorCooldown = Time.time + 2f;
            _gm.Persist(); RefreshStatus();
            yield return new WaitForSeconds(0.2f); yield return FadeScreen(false, 0.4f);
            _busy = false;
            CoastToast.Show(Loc.T("🏠 우리집 앞으로 순간이동했다.", "🏠 Warped home."));
        }
        public void DevWarpHome() { StartCoroutine(WarpHome()); }
        public void DevJoy(float nx, float ny, float sec) { if (_hud != null) _hud.DevJoy(nx, ny, sec); }
        public void DevJoyDiag() { Debug.LogWarning("[JoyDiag] " + (_hud != null ? _hud.JoyDiag() : "-") + $" | mission={MissionLine()} acts={ActCount}/{ActsPerDay} phase={Save.phaseIndex}/{Timeline.PhasesPerWeek}"); }
        /// 개발용: 우리집 콜라이더 한가운데로 — 끼임 탈출 확인
        public void DevStuck() { var hh = VillageWorld.HeroHouse; if (hh != null) Teleport(hh.position + hh.forward * 0.5f, 0f); }
        /// 159차: 드롭 흡수 → 가방 + 토스트
        void OnAbsorb(string item, int n)
        {
            if (Save == null) return;
            LifeItems.Add(Save, item, n);
            if (_quitting) return;   // 씬을 떠날 때 남은 조각은 조용히 가방으로
            if (_motion != null) _motion.Hop();
            CoastAudioManager.PlayAnywhere(CoastSfx.Coin, 0.5f);
            string nm = item == "mat_wood" ? Loc.T("🪵 장작", "🪵 Firewood") : item == "mat_stone" ? Loc.T("🪨 돌", "🪨 Stone") : item;
            CoastToast.Show(Loc.T($"{nm} +{n} (보유 {LifeItems.Count(Save, item)})", $"{nm} +{n} ({LifeItems.Count(Save, item)})"));
            _gm.Persist(); RefreshStatus();
        }

        // ── 157차(사용자: 「들어갈 수 있는 문은 가까이 가면 하이라이트·반짝임, 들어갈 수 있게」) ─────────────
        Transform _nearDoorHouse; Action _nearDoorOn; string _nearDoorLabel; GameObject _doorGlow; static float _glowPulse;
        static Color GlowColor => new Color(1f, 0.92f, 0.45f, 0.18f + 0.30f * (0.5f + 0.5f * Mathf.Sin(Time.time * 5f)));
        static Color RimColor => Color.Lerp(new Color(1f, 0.85f, 0.25f), Color.white, 0.5f + 0.5f * Mathf.Sin(Time.time * 6f));
        /// 2.8 m 안의 들어갈 수 있는 문(집·알바나라·상점)을 찾아 하이라이트를 붙이거나 뗀다.
        void UpdateDoorGlow()
        {
            Transform best = null; Action on = null; string label = null; float bestD = 2.8f; var p = _player.position;
            if (!_busy && _interior == null)
            {
                foreach (var hs in VillageWorld.Houses)
                {
                    if (hs.house == null) continue;
                    float d = Vector3.Distance(new Vector3(p.x, 0f, p.z), new Vector3(hs.door.x, 0f, hs.door.z));
                    if (d >= bestD) continue;
                    var cap = hs; bestD = d; best = hs.house;
                    if (hs.house == VillageWorld.JobHouse) { on = EnterJobHouse; label = Loc.T("알바나라 들어가기", "Enter Job Center"); }
                    else if (hs.house == VillageWorld.HeroHouse) { on = () => AskEnterHome(() => EnterHouse(cap.house, cap.name, cap.door)); label = Loc.T(cap.name + " 들어가기", "Enter " + cap.name); }   // 185차
                    else { on = () => EnterHouse(cap.house, cap.name, cap.door); label = Loc.T(cap.name + " 들어가기", "Enter " + cap.name); }
                }
                if (VillageWorld.Shop != null)
                {
                    var sd = VillageWorld.Shop.TransformPoint(new Vector3(0f, 0f, 2.6f));
                    float d = Vector3.Distance(new Vector3(p.x, 0f, p.z), new Vector3(sd.x, 0f, sd.z));
                    if (d < bestD) { bestD = d; best = VillageWorld.Shop; on = EnterShopHouse; label = Loc.T("마을상점 들어가기", "Enter shop"); }
                }
            }
            if (best != _nearDoorHouse)
            {
                if (_doorGlow != null) { Destroy(_doorGlow); _doorGlow = null; }
                _nearDoorHouse = best;
                if (best != null)
                {
                    var door = best.Find("Door");
                    _doorGlow = new GameObject("DoorGlow"); _doorGlow.transform.SetParent(best, false);
                    var lp = door != null ? door.localPosition : new Vector3(0f, 1f, 2.2f); var ls = door != null ? door.localScale : new Vector3(1f, 1.9f, 0.08f);
                    _doorGlow.transform.localPosition = lp;
                    // 문 앞 얇은 발광판(노란 펄스) + 테두리 띠
                    var g = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(g.GetComponent<Collider>()); g.name = "Glow"; g.transform.SetParent(_doorGlow.transform, false);
                    g.transform.localPosition = new Vector3(0f, 0f, ls.z * 0.5f + 0.06f); g.transform.localScale = new Vector3(ls.x + 0.34f, ls.y + 0.34f, 0.02f);
                    g.GetComponent<Renderer>().sharedMaterial = CoastMaterials.CreateTransparent(GlowColor, () => GlowColor);
                    // 테두리 띠 4개(불투명 노랑↔흰 펄스) — 멀리서도 「여기 들어갈 수 있다」가 보이게
                    var rimM = CoastMaterials.CreateUnlit(RimColor, () => RimColor); float rw = ls.x + 0.30f, rh = ls.y + 0.30f, rz = ls.z * 0.5f + 0.09f;
                    foreach (var (pos, sz) in new[] { (new Vector3(-rw * 0.5f, 0f, rz), new Vector3(0.08f, rh + 0.08f, 0.04f)), (new Vector3(rw * 0.5f, 0f, rz), new Vector3(0.08f, rh + 0.08f, 0.04f)), (new Vector3(0f, rh * 0.5f, rz), new Vector3(rw, 0.08f, 0.04f)), (new Vector3(0f, -rh * 0.5f, rz), new Vector3(rw, 0.08f, 0.04f)) })
                    { var r = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(r.GetComponent<Collider>()); r.name = "Rim"; r.transform.SetParent(_doorGlow.transform, false); r.transform.localPosition = pos; r.transform.localScale = sz; r.GetComponent<Renderer>().sharedMaterial = rimM; }
                    var sp = _doorGlow.AddComponent<DoorSparkle>(); sp.Init(ls.x + 0.4f, ls.y + 0.4f, ls.z * 0.5f + 0.16f);
                    CoastAudioManager.PlayAnywhere(CoastSfx.Coin, 0.15f);
                }
            }
            _nearDoorOn = on; _nearDoorLabel = label;
        }
        /// 문 테두리를 도는 반짝이 별(작은 쿼드 8개, 빌보드·깜빡임).
        public class DoorSparkle : MonoBehaviour
        {
            readonly List<Transform> _stars = new List<Transform>(); readonly List<float> _ph = new List<float>(); float _w, _h, _z;
            public void Init(float w, float h, float z)
            {
                _w = w; _h = h; _z = z;
                var m = CoastMaterials.CreateUnlit(new Color(1f, 0.97f, 0.62f));
                for (int i = 0; i < 8; i++)
                {
                    var q = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(q.GetComponent<Collider>()); q.name = "Star"; q.transform.SetParent(transform, false);
                    q.GetComponent<Renderer>().sharedMaterial = m; q.transform.localScale = Vector3.one * 0.16f;
                    _stars.Add(q.transform); _ph.Add(i / 8f);
                }
            }
            void Update()
            {
                var cam = Camera.main;
                for (int i = 0; i < _stars.Count; i++)
                {
                    float t = Mathf.Repeat(_ph[i] + Time.time * 0.22f, 1f);   // 테두리 한 바퀴(아래 왼쪽 → 위 → 아래 오른쪽)
                    float per = 2f * _w + 2f * _h, s = t * per; Vector3 lp;
                    if (s < _h) lp = new Vector3(-_w * 0.5f, -_h * 0.5f + s, _z);
                    else if (s < _h + _w) lp = new Vector3(-_w * 0.5f + (s - _h), _h * 0.5f, _z);
                    else if (s < 2f * _h + _w) lp = new Vector3(_w * 0.5f, _h * 0.5f - (s - _h - _w), _z);
                    else lp = new Vector3(_w * 0.5f - (s - 2f * _h - _w), -_h * 0.5f, _z);
                    var st = _stars[i]; st.localPosition = lp;
                    float tw = 0.5f + 0.5f * Mathf.Sin(Time.time * 9f + i * 1.7f);
                    st.localScale = new Vector3(0.14f + 0.22f * tw, 0.14f + 0.22f * tw, 1f);
                    if (cam != null) st.rotation = Quaternion.LookRotation(st.position - cam.transform.position) * Quaternion.Euler(0f, 0f, Time.time * 90f + i * 45f);
                }
            }
        }

        // ── 156차(사용자: 「동물의 숲 느낌 — 집에서 밥·잠, 알바는 옆 건물 알바나라, 4개 활동 다 하면 저녁 → 잠, 자면 한 주가 흐른다」) ──
        public const int ActEat = 1, ActPlay = 2, ActJob = 4, ActMission = 8, ActsPerDay = 4;
        int _jobPage, _playPage;
        int ActCount { get { int m = Save != null ? Save.villageActMask : 0, c = 0; for (int b = 1; b <= 8; b <<= 1) if ((m & b) != 0) c++; return c; } }
        bool ActDone(int bit) => Save != null && (Save.villageActMask & bit) != 0;
        string Hero => Loc.T("하늘", "Haneul");
        /// 활동 하나 끝 → 시각이 3시간씩 간다(08 → 11 → 14 → 17 → 20). 4개 다 하면 밤 — 자야 한다.
        void MarkAct(int bit)
        {
            if (Save == null) return;
            if ((Save.villageActMask & bit) != 0) return;
            Save.villageActMask |= bit;
            float h = 8f + ActCount * 3f;
            if (_dayNight != null && _dayNight.Hour < h && _dayNight.Hour >= 5.5f) { _dayNight.Hour = h; Save.villageHour = h; }
            _gm.Persist(); RefreshStatus();
            if (ActCount >= ActsPerDay) CoastToast.Show(Loc.T("오늘 할 일은 다 했다 — 해가 진다. 집 침대에서 자자.", "Done for today — the sun is setting. Go to bed at home."));
        }

        // ── 160차(사용자: 「하루에 미션 포함 4개까지, 그게 1주」): 네 번째 활동 = 일일 미션 ──
        VillageMission.Kind MissionKind => Save == null ? VillageMission.Kind.Bug : (VillageMission.Kind)Mathf.Clamp(Save.villageMissionKind, 0, VillageMission.Count - 1);
        int MissionGoal => VillageMission.Goal(MissionKind);
        /// 아침마다(세이브에 없으면) 오늘 미션을 정한다.
        void EnsureMission()
        {
            if (Save == null) return;
            if (Save.villageMissionKind < 0 || Save.villageMissionKind >= VillageMission.Count)
            { Save.villageMissionKind = (int)VillageMission.Pick(Save); Save.villageMissionProg = 0; _gm.Persist(); }
        }
        public string MissionLine()
        {
            if (Save == null) return null;
            EnsureMission();
            bool done = ActDone(ActMission);
            return done ? Loc.T("🎯 오늘 미션 완료!", "🎯 Mission done!")
                        : Loc.T($"🎯 {VillageMission.Title(MissionKind)}  ({Save.villageMissionProg}/{MissionGoal})",
                                $"🎯 {VillageMission.Title(MissionKind)}  ({Save.villageMissionProg}/{MissionGoal})");
        }
        /// 마을 활동 한 번 → 오늘 미션과 같은 종류면 진행. 목표를 채우면 네 번째 활동으로 센다(보상: 돈·별조각·경험치).
        public void MissionTick(VillageMission.Kind k, int n = 1)
        {
            if (Save == null) return;
            VillageRequest.Tick(this, k, n);   // 171차: 주민 부탁도 같은 종류로 진행
            EnsureMission();
            if (ActDone(ActMission) || MissionKind != k) return;
            Save.villageMissionProg = Mathf.Min(MissionGoal, Save.villageMissionProg + n);
            if (Save.villageMissionProg < MissionGoal)
            { _gm.Persist(); RefreshStatus(); CoastToast.Show(Loc.T($"🎯 {VillageMission.Title(k)} ({Save.villageMissionProg}/{MissionGoal})", $"🎯 {VillageMission.Title(k)} ({Save.villageMissionProg}/{MissionGoal})")); return; }
            int money = VillageMission.Money(k);
            Save.stats.money += money; Save.starShards += VillageMission.Shards; Save.starShardsTotal += VillageMission.Shards;
            LevelSystem.Add(LevelSystem.ExpAction);
            MarkAct(ActMission);
            CoastAudioManager.PlayAnywhere(CoastSfx.Coin);
            CoastToast.Show(Loc.T($"🎯 오늘 미션 완료! +{money}G · 별조각 +{VillageMission.Shards}", $"🎯 Mission complete! +{money}G · shards +{VillageMission.Shards}"));
        }

        /// 스케줄(밥·놀기·알바)을 하나 더 할 수 있나.
        bool CanSchedule(out string why)
        {
            why = null; if (Save == null) { why = "…"; return false; }
            if (Save.boundaryPending) { why = Loc.T("이야기가 기다리고 있어 — 집 침대에서 자고 나면 시작돼.", "A story is waiting — sleep in your bed first."); return false; }
            if (Save.phaseIndex >= Timeline.PhasesPerWeek) { why = Loc.T("이번 주 행동은 다 했어 — 집 침대에서 자자.", "All actions done this week — go to bed."); return false; }
            return true;
        }

        /// 160차: 오늘 미션 안내 팝업(무엇을·어디서·보상)
        void MissionPopup()
        {
            if (Save == null) return; EnsureMission();
            var k = MissionKind; bool done = ActDone(ActMission);
            string body = done
                ? Loc.T($"오늘 미션은 끝났어. 활동 {ActCount}/{ActsPerDay} — 다 하면 집 침대에서 자자(한 주가 지나간다).", $"Done. {ActCount}/{ActsPerDay} today.")
                : Loc.T($"{VillageMission.Title(k)}  ({Save.villageMissionProg}/{MissionGoal})\n{VillageMission.Hint(k)}\n보상: {VillageMission.Money(k)}G · 별조각 {VillageMission.Shards} · 오늘 활동 1칸",
                        $"{VillageMission.Title(k)}  ({Save.villageMissionProg}/{MissionGoal})\n{VillageMission.Hint(k)}\nReward: {VillageMission.Money(k)}G · {VillageMission.Shards} shards · 1 activity");
            _hud.Bubble(Loc.T("🎯 오늘의 미션", "🎯 Today's mission"), body);
        }
        /// 집 식탁: 요리를 골라 먹고(「집밥 먹고 쉬기」 페이즈) 기운 회복.
        void EatHome()
        {
            if (_busy || Save == null) return;
            if (ActDone(ActEat)) { _hud.Bubble(Hero, Loc.T("오늘 밥은 벌써 먹었어. 배불러~", "Already ate today. So full~")); return; }
            if (!CanSchedule(out var why)) { _hud.Bubble(Hero, why); return; }
            LifeItems.Ensure(Save);
            if (!LifeItems.HasEdible(Save))
            {
                int ings = LifeItems.CountCat(Save, LifeItemCat.Ingredient);
                _hud.Bubble(Hero, ings > 0 ? Loc.T("재료만 있어 — 책상(내 방)에서 조리한 뒤 먹자!", "Only ingredients — cook at the desk first!") : Loc.T("먹을 요리가 없어 — 해변 상점에서 재료·요리를 사자!", "No meals — buy some at the beach shop!"));
                return;
            }
            _busy = true;
            MealPickUI.Open(_gm, dishId =>
            {
                _busy = false;
                if (!LifeItems.Eat(Save, dishId)) { CoastToast.Show(Loc.T("먹을 수 없어…", "Can't eat that…")); return; }
                _gm.Persist();
                var def = ScheduleTable.Get("rest_home");
                if (def != null) StartCoroutine(ScheduleActRoutine(ActEat, def));
            }, () => { _busy = false; });
        }
        /// 알바나라(옆 건물): 이번 계절에 할 수 있는 알바를 골라 바로 한다(한 페이지 5개).
        void JobMenu()
        {
            VillageDex.Talk(Save, VillageDex.NpcIndex("job"), 1);   // 195차
            if (_busy || Save == null) return;
            if (_dayNight != null && !_dayNight.ShopOpen) { _hud.Bubble(Loc.T("알바나라", "Job Center"), Loc.T("문이 닫혔다. 접수는 오전 8시 ~ 오후 7시.", "Closed. Open 8 AM – 7 PM.")); return; }
            if (ActDone(ActJob)) { _hud.Bubble(Hero, Loc.T("오늘 알바는 벌써 했어. 내일 또 오자.", "Already worked today. Come back tomorrow.")); return; }
            if (!CanSchedule(out var why)) { _hud.Bubble(Hero, why); return; }
            var season = Timeline.SeasonOf(Save.week);
            var jobs = new List<ScheduleDef>();
            foreach (var d in ScheduleTable.ByCategory(ScheduleCategory.Job, season)) if (d.LockReason(Save.stats) == null) jobs.Add(d);
            if (jobs.Count == 0) { _hud.Bubble(Loc.T("알바나라", "Job Center"), Loc.T("지금 할 수 있는 알바가 없네…", "No jobs available right now…")); return; }
            PickSchedule(Loc.T("💼 알바나라", "💼 Job Center"), Loc.T($"{Save.week}주차 · 돈 {Save.stats.money:N0}G · 스트레스 {Save.stats.stress} — 알바를 고르면 바로 다녀온다", $"Week {Save.week} · {Save.stats.money:N0}G · stress {Save.stats.stress}"), jobs, ActJob, ref _jobPage, JobMenu);
        }
        /// 놀기(정자·해변): 자기계발 + 돈이 있으면 교육.
        void PlayMenu()
        {
            if (_busy || Save == null) return;
            if (ActDone(ActPlay)) { _hud.Bubble(Hero, Loc.T("오늘은 벌써 놀았어. 이제 알바나 집으로!", "Already played today. Work or home!")); return; }
            if (!CanSchedule(out var why)) { _hud.Bubble(Hero, why); return; }
            var season = Timeline.SeasonOf(Save.week);
            var list = new List<ScheduleDef>();
            foreach (var d in ScheduleTable.ByCategory(ScheduleCategory.SelfDev, season)) if (d.LockReason(Save.stats) == null) list.Add(d);
            foreach (var d in ScheduleTable.ByCategory(ScheduleCategory.Lesson, season)) if (d.LockReason(Save.stats) == null) list.Add(d);
            if (list.Count == 0) { _hud.Bubble(Hero, Loc.T("지금은 할 게 없네…", "Nothing to do right now…")); return; }
            PickSchedule(Loc.T("🎮 놀기 · 연습", "🎮 Play · Practice"), Loc.T($"돈 {Save.stats.money:N0}G · 스트레스 {Save.stats.stress} — 산책·수영은 스트레스↓, 연습·교실은 성장↑", $"{Save.stats.money:N0}G · stress {Save.stats.stress}"), list, ActPlay, ref _playPage, PlayMenu);
        }
        void PickSchedule(string title, string sub, List<ScheduleDef> list, int bit, ref int page, Action reopen)
        {
            const int per = 5;
            int pages = (list.Count + per - 1) / per; page = ((page % pages) + pages) % pages;
            var items = new List<(string, Color, Action)>();
            for (int i = page * per; i < Mathf.Min(list.Count, page * per + per); i++)
            {
                var d = list[i];
                string money = d.dMoney != 0 ? $" {d.dMoney:+#;-#}G" : "";
                string st = d.dStress != 0 ? Loc.T($" 스트레스{d.dStress:+#;-#}", $" stress{d.dStress:+#;-#}") : "";
                string lab = $"{d.glyph} {d.Name}{money}{st}" + (d.dTrouble > 0 ? " ⚠" : "");
                Color col = d.category == ScheduleCategory.Job ? (d.dTrouble > 0 ? new Color(0.55f, 0.45f, 0.75f) : new Color(0.95f, 0.65f, 0.30f))
                          : d.category == ScheduleCategory.Lesson ? new Color(0.35f, 0.62f, 0.95f) : new Color(0.45f, 0.78f, 0.55f);
                items.Add((lab, col, () => StartCoroutine(ScheduleActRoutine(bit, d))));
            }
            if (pages > 1) { int p = page; items.Add((Loc.T($"▶ 다른 것 보기 ({p + 1}/{pages})", $"▶ More ({p + 1}/{pages})"), new Color(0.6f, 0.6f, 0.66f), () => { if (bit == ActJob) _jobPage = p + 1; else _playPage = p + 1; reopen(); })); }
            _hud.Choice(title, sub, items.ToArray());
        }
        /// 스케줄 한 칸 실행(TamaRaisingUI.ActionRoutine 의 마을판): 큐 → ResolvePhase → 결과 말풍선 · 경험치 · 일상 장면 · 단서.
        IEnumerator ScheduleActRoutine(int bit, ScheduleDef def)
        {
            if (_busy || Save == null || def == null) yield break;
            _busy = true;
            int slot = Mathf.Clamp(Save.phaseIndex, 0, Timeline.PhasesPerWeek - 1);
            _gm.SetQueued(slot, def.id);
            if (Save.lastCardIds == null || Save.lastCardIds.Length < 3) Save.lastCardIds = new string[3];
            Save.lastCardIds[bit == ActEat ? 0 : bit == ActPlay ? 1 : 2] = def.id;
            CoastAudioManager.PlayAnywhere(CoastSfx.Coin, 0.4f);
            yield return FadeScreen(true, 0.35f);
            _gm.AutoActing = false;
            var result = _gm.ResolvePhase(slot);
            if (bit == ActEat && result.HasValue)
            {
                Save.stats.stamina = Mathf.Min(PlayerStats.StatMax, Save.stats.stamina + 1);
                Save.stats.stress = Mathf.Max(0, Save.stats.stress - 2);
                Survival.OnRestAction(Save);
            }
            MarkAct(bit);
            _gm.Persist(); RefreshStatus();
            yield return new WaitForSeconds(0.45f);
            yield return FadeScreen(false, 0.4f);
            if (result.HasValue)
            {
                var r = result.Value;
                LevelSystem.Add(r.outcome == Outcome.GreatSuccess ? LevelSystem.ExpActionGreat : r.outcome == Outcome.Fail ? LevelSystem.ExpActionFail : LevelSystem.ExpAction);
                if (_gm.LastMasteryUp > 0) CoastToast.Show(Loc.T($"숙련 {RaisingFun.Stars(_gm.LastMasteryUp)} — {def.Name} +{Mathf.RoundToInt((RaisingFun.MasteryMul(_gm.LastMasteryUp) - 1f) * 100f)}%", $"Mastery {RaisingFun.Stars(_gm.LastMasteryUp)} — {def.Name}"));
                if (_gm.LastDailyScene != null && _gm.LastDailyScene[0] >= 0)
                {
                    var ds = _gm.LastDailyScene; _gm.LastDailyScene = null; bool dsDone = false;
                    DailySceneUI.Show(ds[0], ds[1], () => dsDone = true);
                    while (!dsDone) yield return null;
                }
                if (Save.cluePendingMask != 0) { bool clueDone = false; ClueSystem.CheckPending(Save, () => clueDone = true); while (!clueDone) yield return null; }
                string line = r.logLines != null && r.logLines.Length > 0 ? r.logLines[r.logLines.Length - 1] : "";
                string head = r.outcome == Outcome.GreatSuccess ? Loc.T("대성공! ", "Great! ") : r.outcome == Outcome.Fail ? Loc.T("으으… ", "Ugh… ") : "";
                int dM = r.after.money - r.before.money, dS = r.after.stamina - r.before.stamina, dSt = r.after.stress - r.before.stress;
                string delta = (dM != 0 ? $" {dM:+#;-#}G" : "") + (dS != 0 ? Loc.T($" 체력{dS:+#;-#}", $" STA{dS:+#;-#}") : "") + (dSt != 0 ? Loc.T($" 스트레스{dSt:+#;-#}", $" stress{dSt:+#;-#}") : "") + (r.heartsGained > 0 ? $" ♥+{r.heartsGained}" : "");
                string ms = RaisingFun.Milestone(r.before, r.after);
                string clock = _dayNight != null ? " · " + _dayNight.ClockText() : "";
                _busy = false;
                _hud.Bubble(Hero, head + (string.IsNullOrEmpty(line) ? def.Name : line) + delta + clock + (ms != null ? "\n" + ms : ""));
            }
            else _busy = false;
            RefreshStatus();
        }

        /// 156차: 잠에서 깬 아침의 돌발 이벤트(옛 화면의 A/B 카드를 마을 팝업으로).
        IEnumerator MorningEvent()
        {
            yield return new WaitForSeconds(1.2f);
            while (_busy || _hud.Locked) yield return null;
            var ev = _gm.PendingVillageEvent; _gm.PendingVillageEvent = null;
            if (ev == null || Save == null) yield break;
            ShowEventChoice(ev);
        }
        void ShowEventChoice(RandomEventDef ev)
        {
            bool passA = ev.CheckPasses(Save.stats);
            string labelA = ev.ChoiceALabel + (ev.HasStatCheck ? (passA ? $" ✓ {RaisingFun.StatName(ev.condStat)} {ev.condMin}" : Loc.T($" ({RaisingFun.StatName(ev.condStat)} {ev.CheckShort(Save.stats)} 더)", $" (need {RaisingFun.StatName(ev.condStat)} +{ev.CheckShort(Save.stats)})")) : "");
            string body = string.IsNullOrEmpty(ev.body) ? "—" : ev.body;
            _hud.Choice(Loc.T("❗ 돌발 — ", "❗ Event — ") + Loc.Data("ev." + ev.id, ev.title), body,
                new (string, Color, Action)[] {
                    (labelA, passA ? new Color(0.95f, 0.55f, 0.45f) : new Color(0.6f, 0.6f, 0.66f), () => { if (!passA) { CoastToast.Show(Loc.T($"{RaisingFun.StatName(ev.condStat)}이(가) 모자라", $"Not enough {RaisingFun.StatName(ev.condStat)}")); ShowEventChoice(ev); return; } CommitEvent(ev, 0); }),
                    (ev.ChoiceBLabel, new Color(0.35f, 0.62f, 0.95f), () => CommitEvent(ev, 1)),
                });
        }
        void CommitEvent(RandomEventDef ev, int choice)
        {
            var res = _gm.CommitRandomEvent(ev, choice);
            string d = "";
            if (res.dMoney != 0) d += Loc.T($" 돈 {res.dMoney:+#;-#}G", $" {res.dMoney:+#;-#}G");
            if (res.dStamina != 0) d += Loc.T($" 체력 {res.dStamina:+#;-#}", $" STA {res.dStamina:+#;-#}");
            if (res.dStress != 0) d += Loc.T($" 스트레스 {res.dStress:+#;-#}", $" stress {res.dStress:+#;-#}");
            if (res.dHearts != 0) d += Loc.T($" 하트 {res.dHearts:+#;-#}", $" ♥ {res.dHearts:+#;-#}");
            RefreshStatus();
            _hud.Bubble(Hero, res.Body + (d.Length > 0 ? "\n" + Loc.T("돌발 ·", "Event ·") + d : ""));
        }
        /// 침대: 잔다 → 한 주가 흐른다(낮이고 활동이 남았으면 한 번 묻는다).
        void SleepBed()
        {
            if (_busy || Save == null) return;
            if (StorySleepGate()) return;   // 196차: 안 본 이야기(장소)가 있으면 꼬마가 붙잡는다
            bool allDone = Save.phaseIndex >= Timeline.PhasesPerWeek || Save.boundaryPending || ActCount >= ActsPerDay;
            bool late = _dayNight != null && (_dayNight.IsNight || _dayNight.IsDusk);
            if (!allDone && !late)
            {
                int left = Timeline.PhasesPerWeek - Save.phaseIndex;
                _hud.Choice(Loc.T("🛏 침대", "🛏 Bed"), Loc.T($"아직 낮이야. 오늘 활동 {ActCount}/{ActsPerDay} · 남은 행동 {left}번 — 그냥 자면 이번 주가 끝난다.", $"Still daytime. {ActCount}/{ActsPerDay} done · {left} action(s) left — sleeping ends the week."),
                    new (string, Color, Action)[] {
                        (Loc.T("💤 그냥 잔다 — 한 주가 흘러간다", "💤 Sleep anyway — the week passes"), new Color(0.60f, 0.52f, 0.92f), () => StartCoroutine(SleepWeek())),
                        (Loc.T("아직 안 잘래", "Not yet"), new Color(0.6f, 0.6f, 0.66f), null),
                    });
                return;
            }
            StartCoroutine(SleepWeek());
        }
        /// 잠 몽타주(밤 → 새벽 → 아침으로 하늘이 돌고 「n주차 → n+1주차」) → TamaRaisingUI 가 화면 없이 주말 결산·이야기를 돌리고 마을 아침으로.
        IEnumerator SleepWeek()
        {
            _busy = true;
            EndRide();   // 205차
            yield return DaySummaryCo();   // 205차(사용자: 「하루 정산 화면」): 잠들기 전 오늘 한 일 한 장
            // 161차: 집 안이면 침대 매트리스 위에 눕는다(머리는 베개 쪽, 몸은 등을 대고) — 몽타주 뒤 마을 아침으로 가므로 되돌릴 필요 없음
            if (_interior != null && _rigT != null)
            {
                var bed = new Vector3(VillageInterior.OX - 2.2f * VillageInterior.KX, _interior.FloorY, VillageInterior.OZ + 1.15f * VillageInterior.KZ);
                _cc.enabled = false; _player.position = bed; _player.rotation = Quaternion.Euler(0f, 180f, 0f);
                if (_motion != null) _motion.enabled = false;   // 절차 모션(바운스·기울기)이 눕힌 자세를 덮어쓰지 않게
                _rigT.localPosition = new Vector3(0f, 0.72f, 0f); _rigT.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                if (_anim != null) _anim.speed = 0f;
            }
            var cv = CoastUiCanvas.Create("SleepFade", 150);
            var img = CoastHudLayout.MakeImage(CoastUiCanvas.Root(cv), "F", Vector2.zero, Vector2.one, new Vector2(-400f, -400f), new Vector2(400f, 400f), new Color(0.03f, 0.03f, 0.08f, 0f));
            img.raycastTarget = true;
            var zz = CoastHudLayout.MakeText(img.rectTransform, "Z", "z z Z …", 46, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-300f, 20f), new Vector2(300f, 120f)); zz.color = new Color(1f, 1f, 1f, 0f);
            var cap = CoastHudLayout.MakeText(img.rectTransform, "C", "", 24, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-320f, -80f), new Vector2(320f, 10f)); cap.color = new Color(1f, 0.95f, 0.8f, 0f);
            int wk = Save.week;
            cap.text = Loc.T($"{wk}주차의 밤 — 잠이 든다…", $"Week {wk}, night — falling asleep…");
            float t = 0f;
            while (t < 1f) { t += Time.deltaTime; img.color = new Color(0.03f, 0.03f, 0.08f, t); zz.color = new Color(1f, 1f, 1f, t); cap.color = new Color(1f, 0.95f, 0.8f, t); yield return null; }
            // 하늘이 밤 → 새벽 → 아침으로 한 바퀴(창밖 톤이 바뀐다) — 어두운 막 뒤로 살짝 비친다
            if (_dayNight != null)
            {
                _dayNight.Paused = true; float h0 = _dayNight.Hour < 5.5f ? _dayNight.Hour + 24f : Mathf.Max(_dayNight.Hour, 20f);
                for (float u = 0f; u < 1f; u += Time.deltaTime / 2.2f)
                {
                    float h = Mathf.Lerp(h0, 32f, u); _dayNight.Hour = h >= 24f ? h - 24f : h;
                    img.color = new Color(0.03f, 0.03f, 0.08f, Mathf.Lerp(1f, 0.965f, u));
                    cap.text = u < 0.5f ? Loc.T($"{wk}주차의 밤 — 잠이 든다…", $"Week {wk}, night — falling asleep…") : Loc.T($"일주일이 흘러간다 — {wk}주차 → {wk + 1}주차", $"A week goes by — week {wk} → {wk + 1}");
                    yield return null;
                }
            }
            else yield return new WaitForSeconds(1.2f);
            // 다음 날 아침 08:00 · 활동 초기화 · 집 앞에서 시작
            Save.villageActMask = 0; Save.villageHour = 8f;
            VillageLivestock.SleepTick(Save);   // 171차: 닭은 달걀, 토끼는 한 살
            Save.villageMissionKind = -1; Save.villageMissionProg = 0;   // 160차: 내일 아침 새 미션
            var home = VillageWorld.HeroHouse != null ? VillageWorld.HeroHouse.TransformPoint(new Vector3(0f, 0f, 5.6f)) : new Vector3(0f, 0f, 30f);
            Save.villageX = home.x; Save.villageZ = home.z;
            if (_creatures != null) _creatures.ClearGhosts();
            Save.stats.stamina = Mathf.Min(PlayerStats.StatMax, Save.stats.stamina + 20); Save.condition = Mathf.Min(100, Save.condition + 5);   // 잠 보너스(주간 결산의 잠 보너스와 별개, 작게)
            _gm.SleepFromVillage = true; _gm.Persist();
            yield return new WaitForSeconds(0.3f);
            _driver.OpenScheduleFromVillage();   // → TamaRaisingUI.Start: SleepFromVillage → EndWeek(축제·미니게임·결산 카드) → 이야기 → ToVillage(아침)
        }

        // ── 174차-3(사용자: 「kpop 런처럼 비도 오고 눈도 오고, 오면 체력이 빨리 빠지게」) ──
        WeatherFx _wfx; WeatherKind _weather = WeatherKind.Clear; float _weatherT, _wetDrain; bool _wfxOn;
        public WeatherKind Weather => _weather;
        /// 개발용: 밤 몬스터 강제 소환
        public void DevMonster() { if (_creatures != null) _creatures.DevSpawnMonster(); }
        /// 개발용: 날씨 강제
        public void DevWeather(WeatherKind w)
        {
            _weather = w; _weatherT = 180f;
            if (_wfx != null && _interior == null) { _wfx.SetState(w, Save != null ? Timeline.SeasonOf(Save.week) : SeasonKind.Summer); _wfxOn = true; }
            RefreshStatus();
        }
        static WeatherKind PickWeather(SeasonKind season)
        {
            float r = UnityEngine.Random.value;
            if (season == SeasonKind.Winter) return r < 0.34f ? WeatherKind.Snow : r < 0.52f ? WeatherKind.Cloudy : WeatherKind.Clear;
            if (season == SeasonKind.Summer) return r < 0.30f ? WeatherKind.Rain : r < 0.46f ? WeatherKind.Cloudy : WeatherKind.Clear;
            return r < 0.22f ? WeatherKind.Rain : r < 0.40f ? WeatherKind.Cloudy : WeatherKind.Clear;
        }
        void WeatherTick(float dt, bool moving)
        {
            if (Save == null || _cam == null) return;
            var season = Timeline.SeasonOf(Save.week);
            if (_wfx == null)
            {
                var go = new GameObject("VillageWeather"); go.transform.SetParent(transform, false);
                _wfx = go.AddComponent<WeatherFx>(); _wfx.Bind(_cam.transform); _wfx.Density = 0.5f;   // 182차: 마을은 벚꽃·비·눈을 절반만
                _weather = PickWeather(season); _weatherT = 80f;
            }
            if (_interior != null || VillageZones.At(_player.position) == VillageZones.Zone.Mine) { if (_wfxOn) { _wfx.Indoor(); _wfxOn = false; } return; }   // 199차: 광산·갱도(땅속)도   // 집 안엔 안 내린다 — 198차: 봄 벚꽃까지 모두 끄고 지운다(예전엔 벚꽃이 계속 날렸다)
            if (!_wfxOn) { _wfx.SetState(_weather, season); _wfxOn = true; RefreshStatus(); }
            _weatherT -= dt;
            if (_weatherT <= 0f)
            {
                _weatherT = 70f + UnityEngine.Random.value * 70f;
                var w = PickWeather(season);
                if (w != _weather)
                {
                    _weather = w; _wfx.SetState(w, season); RefreshStatus();
                    if (w == WeatherKind.Rain) CoastToast.Show(Loc.T("비가 온다 — 돌아다니면 체력이 빨리 준다.", "Rain — walking tires you faster."));
                    else if (w == WeatherKind.Snow) CoastToast.Show(Loc.T("눈이 온다 — 돌아다니면 체력이 빨리 준다.", "Snow — walking tires you faster."));
                }
            }
            // 비·눈에 돌아다니면 5초마다 체력 1
            if (moving && (_weather == WeatherKind.Rain || _weather == WeatherKind.Snow))
            {
                _wetDrain += dt;
                if (_wetDrain >= 5f) { _wetDrain = 0f; Save.stats.stamina = Mathf.Max(1, Save.stats.stamina - 1); RefreshStatus(); }
            }
            else _wetDrain = 0f;
        }

        // ── 상태 알약 ─────────────────────────────────────────────────────
        public static void RefreshStatus() { if (I != null) I.RefreshStatusInner(); }
        void RefreshStatusInner()
        {
            if (_hud == null || Save == null) return;
            var season = Timeline.SeasonOf(Save.week);
            string weather = _weather == WeatherKind.Rain ? Loc.T("비", "Rain")
                : _weather == WeatherKind.Snow ? Loc.T("눈", "Snow")
                : _weather == WeatherKind.Cloudy ? Loc.T("흐림", "Cloudy")
                : Loc.T("맑음", "Sunny");   // 174차-3: 실제 날씨를 표시
            // 155차: 마을 시계(밤낮) — 밤엔 달 표시
            string clock = _dayNight != null ? _dayNight.ClockText() + " " + (_dayNight.IsNight ? "🌙" : weather) : Loc.T("오전 08:00", "AM 08:00") + " " + weather;
            string acts = Loc.T((ActDone(ActEat) ? "밥" : "·") + (ActDone(ActPlay) ? "놀" : "·") + (ActDone(ActJob) ? "알" : "·") + (ActDone(ActMission) ? "미" : "·"), (ActDone(ActEat) ? "E" : "·") + (ActDone(ActPlay) ? "P" : "·") + (ActDone(ActJob) ? "J" : "·") + (ActDone(ActMission) ? "M" : "·"));   // 이모지는 HUD 폰트에 없음
            string vil = Loc.T($"{Save.week}주차 {Timeline.SeasonName(season)}  ·  Lv.{Save.level}  ·  활동 {ActCount}/{ActsPerDay} {acts}", $"Week {Save.week} {Timeline.SeasonName(season)}  ·  Lv.{Save.level}  ·  {ActCount}/{ActsPerDay} {acts}");
            _hud.SetStatus(Save.stats.stamina, PlayerStats.StatMax, Save.stats.money, clock, vil);
            _hud.SetMission(_interior == null ? (StoryLine() ?? MissionLine()) : null);   // 160차 · 196차: 이야기 줄이 먼저
        }
    }

    /// 149차 진단: CharacterController 가 마지막으로 부딪힌 콜라이더 이름.
    public class CcHitLog : MonoBehaviour
    {
        public string Last = "-"; public float At;
        void OnControllerColliderHit(ControllerColliderHit h) { if (h.collider != null && h.normal.y < 0.7f) { Last = h.collider.name + "@" + h.collider.transform.parent?.name; At = Time.time; } }
    }
}
