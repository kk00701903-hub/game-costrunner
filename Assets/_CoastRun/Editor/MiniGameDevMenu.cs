using UnityEditor;
using UnityEngine;

namespace CoastRun.EditorTools
{
    /// 48차-13: 플레이 중 미션 미니게임을 바로 띄우는 개발 메뉴(원격: unity_cmd "menu Coast Run/Dev/Mission - Marbles").
    public static class MiniGameDevMenu
    {
        private static void Play(ChapterMission.Kind kind)
        {
            if (!Application.isPlaying) { Debug.LogWarning("[MiniGameDev] 플레이 모드에서만"); return; }
            ChapterMissionUI.Play(kind, true, won => Debug.LogWarning($"[MiniGameDev] {kind} done won={won}"));
        }

        [MenuItem("Coast Run/Dev/Mission - Marbles")] public static void Marbles() => Play(ChapterMission.Kind.Marbles);
        [MenuItem("Coast Run/Dev/Mission - Yut")] public static void Yut() => Play(ChapterMission.Kind.Yut);
        [MenuItem("Coast Run/Dev/Mission - Tuho")] public static void Tuho() => Play(ChapterMission.Kind.Tuho);
        [MenuItem("Coast Run/Dev/Mission - Ddakji")] public static void Ddakji() => Play(ChapterMission.Kind.Ddakji);
        [MenuItem("Coast Run/Dev/Mission - Mugunghwa")] public static void Mugunghwa() => Play(ChapterMission.Kind.Mugunghwa);
        [MenuItem("Coast Run/Dev/Collection - Photocards")] public static void Cards() { if (Application.isPlaying) CollectionUI.Open(null, 1); }
        [MenuItem("Coast Run/Dev/Collection - Records")] public static void Records() { if (Application.isPlaying) CollectionUI.Open(null, 0); }
        [MenuItem("Coast Run/Dev/Policy - Terms")] public static void PolicyTerms() { if (Application.isPlaying) PolicyUI.Open(PolicyUI.Doc.Terms); }
        [MenuItem("Coast Run/Dev/Policy - Youth")] public static void PolicyYouth() { if (Application.isPlaying) PolicyUI.Open(PolicyUI.Doc.Youth); }
        // 51차: 보스전·하늘 위협 확인용
        [MenuItem("Coast Run/Dev/Mission - Slow flight toggle")] public static void SlowFlight() { MissionMiniGames.DebugSlowFlight = !MissionMiniGames.DebugSlowFlight; Debug.LogWarning("[Dev] slow flight " + MissionMiniGames.DebugSlowFlight); }
        [MenuItem("Coast Run/Dev/Fx - Double jump cloud (slow x40)")] public static void DjCloud()
        {
            var p = Object.FindAnyObjectByType<PlayerController>(); var rig = Object.FindAnyObjectByType<SkaterRig>();
            Debug.LogWarning($"[Dev] dj cloud: player={(p != null)} juice={(JuiceDirector.Instance != null)} puff={(ArtAssets.LoadTexture("Fx_Cloud_Puff") != null)} flat={(ArtAssets.LoadTexture("Fx_Cloud_Flat") != null)}");
            if (p == null || JuiceDirector.Instance == null) return;
            JuiceDirector.DebugFxSlow = 40f;
            JuiceDirector.Instance.OnDoubleJump(p.transform.position + Vector3.up * 0.9f, rig != null ? rig.transform : p.transform);
        }
        [MenuItem("Coast Run/Dev/Input - Probe UI raycast")] public static void ProbeUi()
        {
            var es = UnityEngine.EventSystems.EventSystem.current; if (es == null) { Debug.LogWarning("[Probe] no EventSystem"); return; }
            foreach (var f in new[] { new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.3f), new Vector2(0.2f, 0.5f), new Vector2(0.8f, 0.5f) })
            {
                var pd = new UnityEngine.EventSystems.PointerEventData(es) { position = new Vector2(f.x * Screen.width, f.y * Screen.height) };
                var hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
                es.RaycastAll(pd, hits);
                var sb = new System.Text.StringBuilder($"[Probe] {f}: {hits.Count} hits");
                foreach (var h in hits) { var tr = h.gameObject.transform; string path = tr.name; for (int i = 0; i < 4 && tr.parent != null; i++) { tr = tr.parent; path = tr.name + "/" + path; } sb.Append("\n  ").Append(path); }
                Debug.LogWarning(sb.ToString());
            }
        }
        [MenuItem("Coast Run/Dev/Boss - Rush")] public static void BossRush() { if (Application.isPlaying) ArcadeRun.StartBossRush(GameManager.I); }
        // 90차: "피버 안 눌렀는데 돈이 모인다" — 코인을 끌어당기는 값이 지금 무엇인지 한 줄로.
        [MenuItem("Coast Run/Dev/Run - Log magnet/fever")] public static void LogMagnet()
        {
            if (!Application.isPlaying) { Debug.LogWarning("[Magnet] 플레이 모드에서만"); return; }
            var up = Object.FindAnyObjectByType<UpgradeManager>();
            float upR = up != null ? up.GetMagnetRadius() : 0f;
            var pet = PetCompanion.Instance;
            Debug.LogWarning($"[Magnet] fever={FeverMode.Active} feverBonus={FeverMode.MagnetBonus}m " +
                             $"pet={(pet != null ? pet.Kind.ToString() : "none")} petMagnet={PetCompanion.MagnetBonus}m coinMul={PetCompanion.CoinBonus} " +
                             $"upgradeMagnet={upR:0.0}m(Lv{(up != null ? up.GetLevel(UpgradeStat.MagnetRadius) : 0)}) bonusTime={BonusTimeDirector.IsActive} " +
                             $"kpopChorus={ArcadeRun.KpopChorus} runCoinMul={RunTuning.CoinMul}");
        }
        [MenuItem("Coast Run/Dev/Fx - Item guide (6s)")] public static void ItemGuide() { if (Application.isPlaying) { PickupFloat.ChapterStart(8, "테스트", "안내 띠 확인", 6f); PickupFloat.ItemGuide(6f); } }
        [MenuItem("Coast Run/Dev/Fx - Weather probe")] public static void WeatherProbe()
        {
            var fx = Object.FindAnyObjectByType<WeatherFx>();
            if (fx == null) { Debug.LogWarning("[WeatherProbe] no WeatherFx"); return; }
            var sb = new System.Text.StringBuilder($"[WeatherProbe] weather={fx.Current} pos={fx.transform.position}");
            foreach (var ps in fx.GetComponentsInChildren<ParticleSystem>(true))
            {
                var r = ps.GetComponent<ParticleSystemRenderer>();
                sb.Append($"\n  {ps.name} playing={ps.isPlaying} n={ps.particleCount} active={ps.gameObject.activeInHierarchy} mat={(r != null && r.sharedMaterial != null ? r.sharedMaterial.shader.name : "-")} tex={(r != null && r.sharedMaterial != null && r.sharedMaterial.HasProperty("_BaseMap") && r.sharedMaterial.GetTexture("_BaseMap") != null ? r.sharedMaterial.GetTexture("_BaseMap").name : "-")} pos={ps.transform.position}");
            }
            Debug.LogWarning(sb.ToString());
        }
        // 64차: 제주 집 키트(JHouse_*) 확인용 — 플레이어 앞 오른쪽(바다 쪽) 둔덕에 4채를 나란히(왼쪽은 상가 안에 파묻혀 안 보인다).
        //   timescale 을 먼저 낮추고 부를 것(히트 슬로모가 timeScale 을 1로 되돌리면 금방 지나쳐 버린다).
        [MenuItem("Coast Run/Dev/World - Jeju house showcase")] public static void JejuShowcase()
        {
            var p = Object.FindAnyObjectByType<PlayerController>(); if (p == null) return;
            string[] names = { "JHouse_Thatch_A", "JHouse_Thatch_B", "JHouse_Tile_A", "JHouse_Tile_B" };
            var host = new GameObject("JejuShowcase").transform;
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(host.gameObject, p.gameObject.scene);
            for (int i = 0; i < names.Length; i++)
            {
                var pivot = new GameObject("Pivot" + i).transform; pivot.SetParent(host, false);
                pivot.SetPositionAndRotation(RoadPlacement.OnRoad(p.PathDistance + 14f + i * 11f, 8.2f), DownhillPath.Rotation * DownhillPath.UprightLocal);
                var go = JejuKit.Spawn(names[i], pivot, Vector3.zero, 0f, 1f);
                Debug.LogWarning(go == null ? "[JejuShowcase] 없음: " + names[i] : $"[JejuShowcase] {names[i]} at path z={p.PathDistance + 14f + i * 11f:F0}");
            }
        }
        // 63차: 계절 요소 확인용 — 챕터로 계절이 정해진다(1~5 봄, 6~10 여름, 11~15 가을, 16~20 겨울)
        [MenuItem("Coast Run/Dev/Season - Spring run (ch3)")] public static void RunSpring() { if (Application.isPlaying) ArcadeRun.StartKpop(GameManager.I, 3); }
        [MenuItem("Coast Run/Dev/Season - Summer run (ch8)")] public static void RunSummer() { if (Application.isPlaying) ArcadeRun.StartKpop(GameManager.I, 8); }
        [MenuItem("Coast Run/Dev/Season - Autumn run (ch13)")] public static void RunAutumn() { if (Application.isPlaying) ArcadeRun.StartKpop(GameManager.I, 13); }
        [MenuItem("Coast Run/Dev/Season - Winter run (ch18)")] public static void RunWinter() { if (Application.isPlaying) ArcadeRun.StartKpop(GameManager.I, 18); }
        [MenuItem("Coast Run/Dev/Sky - Drop rock")] public static void DropRock() { var p = Object.FindAnyObjectByType<PlayerController>(); if (p != null) SkyHazards.DropRock(p.PathDistance + p.Speed * 1.4f + 6f, p.Lane, 1.15f); }
        [MenuItem("Coast Run/Dev/Sky - Missile")] public static void Missile() { var p = Object.FindAnyObjectByType<PlayerController>(); if (p != null) SkyHazards.FireMissile(p.PathDistance + 40f, p.Lane, p.Speed + 13f); }
        [MenuItem("Coast Run/Dev/Sky - Tornado")] public static void Tornado() { var p = Object.FindAnyObjectByType<PlayerController>(); if (p != null) SkyHazards.SpawnTornado(p.PathDistance + 45f, p.Speed * 0.55f + 6f, 2.0f, 0.4f, 7f); }
        // 52차: 웹소설 리더·기부 팝업·펫 상점 확인용
        [MenuItem("Coast Run/Dev/Story - Reader CH1")] public static void ReaderCh1() { if (Application.isPlaying) StoryReaderUI.OpenChapter(1, () => Debug.LogWarning("[Dev] reader done")); }
        [MenuItem("Coast Run/Dev/Story - Reader CH4 (long)")] public static void ReaderCh4() { if (Application.isPlaying) StoryReaderUI.OpenChapter(4, () => Debug.LogWarning("[Dev] reader done")); }
        [MenuItem("Coast Run/Dev/Story - Reader close")] public static void ReaderClose() { StoryReaderUI.Close(); DonateUI.Close(); }
        // 53차: 레벨·상태창
        [MenuItem("Coast Run/Dev/Level - +200 EXP")] public static void Exp200() { if (Application.isPlaying) LevelSystem.Add(200); }
        [MenuItem("Coast Run/Dev/Level - Status window")] public static void Status() { if (Application.isPlaying) StatusUI.Open(GameManager.I); }
        [MenuItem("Coast Run/Dev/Donate - Popup")] public static void Donate() { if (Application.isPlaying) DonateUI.Open(); }
        [MenuItem("Coast Run/Dev/Donate - Reset seen")] public static void DonateReset() { PlayerPrefs.DeleteKey(Donation.SeenKey); PlayerPrefs.Save(); }
        // 55차: 턴·생존·대회 확인용
        [MenuItem("Coast Run/Dev/Life - Week pass")] public static void WeekPass() { if (!Application.isPlaying || !GameManager.Active) return; var s = GameManager.I.Save; var rep = Survival.WeekTick(s); WeekPassUI.Show(s.week, s.week + 1, Timeline.SeasonOf(s.week + 1), rep, "다음 턴: 챕터 4 이야기 → 대회 「봄 사진 콘테스트」", () => Debug.LogWarning("[Dev] week pass done")); }
        [MenuItem("Coast Run/Dev/Life - Week pass (hold 60s)")] public static void WeekPassHold() { if (!Application.isPlaying || !GameManager.Active) return; float keep = WeekPassUI.AutoCloseSeconds; WeekPassUI.AutoCloseSeconds = 60f; var s = GameManager.I.Save; var rep = Survival.WeekTick(s); WeekPassUI.Show(s.week, s.week + 1, Timeline.SeasonOf(s.week + 1), rep, "…하늘이 일어나지 못한다", () => { WeekPassUI.AutoCloseSeconds = keep; Debug.LogWarning("[Dev] week pass done"); }); }
        [MenuItem("Coast Run/Dev/UI - Home (Room)")] public static void UiHomeRoom() { if (Application.isPlaying && GameManager.Active) HomeUI.Open(GameManager.I, null, null); }
        [MenuItem("Coast Run/Dev/UI - Star Gacha")] public static void UiStarGacha() { if (Application.isPlaying && GameManager.Active) StarGachaUI.Open(GameManager.I, null); }
        [MenuItem("Coast Run/Dev/Gacha - +50 shards, +2 tickets")] public static void GachaShards() { if (Application.isPlaying && GameManager.Active) { GameManager.I.Save.starShards += 50; GameManager.I.Save.capsuleTickets += 2; GameManager.I.Persist(); Debug.LogWarning("[Dev] shards=" + GameManager.I.Save.starShards); } }
        [MenuItem("Coast Run/Dev/UI - Shop")] public static void UiShop() { if (Application.isPlaying && GameManager.Active) ShopUI.Open(GameManager.I, 0); }
        [MenuItem("Coast Run/Dev/Life - Grocery")] public static void Grocery() { if (Application.isPlaying) GroceryUI.Open(GameManager.I); }
        [MenuItem("Coast Run/Dev/Life - Give dishes x3")] public static void GiveDishes()
        {
            if (!Application.isPlaying || !GameManager.Active) return;
            var sv = GameManager.I.Save; LifeItems.Ensure(sv);
            LifeItems.Add(sv, "dish_rice", 2); LifeItems.Add(sv, "dish_egg", 1); LifeItems.Add(sv, "dish_fish", 1);
            GameManager.I.Persist(); Debug.LogWarning("[Dev] dishes given: " + LifeItems.ListEdible(sv).Count);
        }
        [MenuItem("Coast Run/Dev/Life - Reset week actions")] public static void ResetActions()
        {
            if (!Application.isPlaying || !GameManager.Active) return;
            var sv = GameManager.I.Save; sv.phaseIndex = 0; sv.queuedSchedule = new string[Timeline.PhasesPerWeek]; sv.weekMiniDone = sv.week; sv.boundaryPending = false;
            GameManager.I.Persist(); GameManager.I.EnterRaising(); Debug.LogWarning("[Dev] week actions reset");
        }
        [MenuItem("Coast Run/Dev/Life - Meal pick")] public static void MealPick()
        {
            if (!Application.isPlaying) return;
            MealPickUI.OpenDemo(() => Debug.LogWarning("[Dev] meal closed"));
        }
        [MenuItem("Coast Run/Dev/Life - Game over")] public static void GameOver() { if (Application.isPlaying && GameManager.Active) GameOverUI.Show(GameManager.I, CoastUiArt.AsSprite(ArtAssets.LoadTexture("Raise_Girl_Pose_Cry")), () => Debug.LogWarning("[Dev] revived")); }
        [MenuItem("Coast Run/Dev/Life - Starve (rice 0, cond 5)")] public static void Starve() { if (Application.isPlaying && GameManager.Active) { var s = GameManager.I.Save; s.rice = 0; s.sideDish = 0; s.condition = 5; s.hunger = 10; GameManager.I.Persist(); } }
        // 105·108차(재미요소) 팝업 캡쳐용
        [MenuItem("Coast Run/Dev/Fun - Festival intro")] public static void FunFestIntro() { if (Application.isPlaying && GameManager.Active) FestivalUI.ShowIntro(Festival.All[0], GameManager.I.Save, () => Debug.LogWarning("[Dev] fest go")); }
        [MenuItem("Coast Run/Dev/Fun - Festival result 1st")] public static void FunFestResult() { if (Application.isPlaying) FestivalUI.ShowResult(Festival.All[1], 1, () => Debug.LogWarning("[Dev] fest done")); }
        [MenuItem("Coast Run/Dev/Fun - Daily scene (kid 2)")] public static void FunDaily() { if (Application.isPlaying) DailySceneUI.Show(0, 2, () => Debug.LogWarning("[Dev] daily done")); }
        [MenuItem("Coast Run/Dev/Fun - Daily scene (lady 3)")] public static void FunDaily2() { if (Application.isPlaying) DailySceneUI.Show(1, 3, () => Debug.LogWarning("[Dev] daily done")); }
        [MenuItem("Coast Run/Dev/Fun - Epilogue")] public static void FunEpilogue() { if (Application.isPlaying && GameManager.Active) EpilogueUI.Show(GameManager.I.Save, () => Debug.LogWarning("[Dev] epilogue done")); }
        [MenuItem("Coast Run/Dev/Fun - Clue not yet (radio)")] public static void FunClueNotYet() { if (Application.isPlaying && GameManager.Active) { var s = GameManager.I.Save; s.clueMask &= ~(int)ClueSystem.Clue.Radio; ClueSystem.ShowAfterScene(s, "CS3", () => Debug.LogWarning("[Dev] clue done")); } }
        [MenuItem("Coast Run/Dev/Fun - Close all")] public static void FunClose() { FestivalUI.Close(); DailySceneUI.Close(); EpilogueUI.Close(); ClueSystem.Close(); CalendarUI.Close(); EndingCreditsUI.Close(); CinemaSelect.Close(); ContestIntroUI.Close(); WeekPassUI.Close(); MealPickUI.Close(); var ui = Object.FindAnyObjectByType<TamaRaisingUI>(); if (ui != null) ui.DevCloseOverlays(); var home = Object.FindAnyObjectByType<HomeUI>(); if (home != null) Object.Destroy(home.gameObject); }
        // 109차 캡쳐용
        [MenuItem("Coast Run/Dev/109 - Ending credits")] public static void R109Credits() { if (Application.isPlaying && GameManager.Active) { var p = GameManager.I.Profile; if (p != null && p.cardMask == 0) { p.cardMask = 0b1011_0111; } EndingCreditsUI.Show(p, () => Debug.LogWarning("[Dev] credits done")); } }
        [MenuItem("Coast Run/Dev/109 - Cinema (season tabs)")] public static void R109Cinema() { if (Application.isPlaying && GameManager.Active) CinemaSelect.Open(GameManager.I, null, () => Debug.LogWarning("[Dev] cinema closed")); }
        [MenuItem("Coast Run/Dev/109 - My room (pet)")] public static void R109Room() { if (Application.isPlaying && GameManager.Active) { var s = GameManager.I.Save; if (s.equippedPet == PetKind.None) { s.ownedPetMask |= 1 << (int)PetKind.Sparrow; s.equippedPet = PetKind.Sparrow; } HomeUI.Open(GameManager.I, null, () => Debug.LogWarning("[Dev] room closed")); } }
        // 110차: 아케이드 러닝을 보드 모드로 시작하게 하는 스위치(프로필 해금과 짝).
        [MenuItem("Coast Run/Dev/110 - Board mode ON")]
        public static void BoardModeOn() { PlayerPrefs.SetInt("CoastRun_ArcadeBoard", 1); PlayerPrefs.Save(); Debug.LogWarning("[110] arcade board = ON"); }

        // 110차(사용자 3번): 「점프해도 보드는 장애물에 부딪히고, 점프 중엔 피해가 없다」를 자동으로 확인한다.
        //   러닝 시작부터 프로브가 직접 몰아서, 주인공 앞에 콘을 놓고 점프시킨 뒤
        //   HP 변화와 보드 충돌 횟수를 콘솔에 찍는다.
        [MenuItem("Coast Run/Dev/110 - Board bump probe")]
        public static void BoardBumpProbe()
        {
            if (!Application.isPlaying) { Debug.LogWarning("[110] play 중에만"); return; }
            var host = new GameObject("BoardProbe");
            Object.DontDestroyOnLoad(host);
            host.AddComponent<BoardProbeRunner>();
        }

        private class BoardProbeRunner : MonoBehaviour
        {
            private System.Collections.IEnumerator Start()
            {
                PlayerPrefs.SetInt("CoastRun_ArcadeBoard", 1); PlayerPrefs.Save();
                if (Object.FindAnyObjectByType<PlayerController>() == null && GameManager.Active)
                {
                    if (GameManager.I.Profile != null) GameManager.I.Profile.skateboardUnlocked = true;
                    ArcadeRun.StartKpop(GameManager.I, 3);
                    RunTuning.Mode = RunMode.Skateboard;
                    RunTuning.SpeedMul = 1.3f; RunTuning.CoinMul = 1.3f;
                    Debug.LogWarning($"[110] StartKpop(3) 호출 mode={RunTuning.Mode}");
                }
                PlayerController pc = null;
                float w = 0f;
                while (w < 70f)
                {
                    w += Time.unscaledDeltaTime;
                    pc = Object.FindAnyObjectByType<PlayerController>();
                    if (pc != null && HealthSystem.Instance != null && HealthSystem.Instance.IsActive
                        && HealthSystem.Instance.Current > 0.5f && PickupReach.BoardActive) break;
                    yield return null;
                }
                var vis = Object.FindAnyObjectByType<CoastPlayerVisual>();
                Debug.LogWarning($"[110] ready t={w:F1} player={(pc != null)} visual={(vis != null)} mode={RunTuning.Mode} boardActive={PickupReach.BoardActive} hp={(HealthSystem.Instance != null ? HealthSystem.Instance.Current : -1f):F1}");
                if (pc == null) { Destroy(gameObject); yield break; }
                if (HealthSystem.Instance != null) HealthSystem.Instance.Heal(HealthSystem.Instance.Max);
                yield return null;

                // 실제 맵에 흘러오는 장애물을 기다렸다가, 내 레인으로 4~6 m 앞에 왔을 때 점프한다.
                Vector3 fwd = DownhillPath.Rotation * Vector3.forward;
                Vector3 right = DownhillPath.Rotation * Vector3.right;
                ObstacleHazard target = null;
                float t = 0f;
                while (t < 45f)
                {
                    t += Time.deltaTime;
                    float pz = DownhillPath.DistanceAlong(pc.transform.position);
                    var list = ObstacleHazard.Active;
                    for (int i = 0; i < list.Count; i++)
                    {
                        var hz = list[i]; if (hz == null) continue;
                        float dz = DownhillPath.DistanceAlong(hz.transform.position) - pz;
                        if (dz < 3.6f || dz > 5.6f) continue;
                        if (Mathf.Abs(Vector3.Dot(hz.transform.position - pc.transform.position, right)) > 0.55f) continue;
                        if (hz.transform.position.y - pc.transform.position.y > 1.2f) continue;
                        target = hz; break;
                    }
                    if (target != null) break;
                    yield return null;
                }
                if (target == null) { Debug.LogWarning("[110] 앞 레인 장애물을 못 찾음"); Destroy(gameObject); yield break; }

                int bump0 = CoastPlayerVisual.BoardBumps;
                float hp0 = HealthSystem.Instance.Current;
                Debug.LogWarning($"[110] target 잡음 t={t:F1} hp={hp0:F1} bumps={bump0}");
                var inp = Object.FindAnyObjectByType<MobileSwipeInput>();
                if (inp != null) inp.Inject(0, true, false); else Debug.LogWarning("[110] MobileSwipeInput 없음");

                // 점프해서 지나가는 동안의 최소 클리어런스와 HP·충돌을 지켜본다.
                float watch = 0f, maxClear = 0f;
                while (watch < 1.8f)
                {
                    watch += Time.deltaTime;
                    maxClear = Mathf.Max(maxClear, PickupReach.BoardDrop);
                    yield return null;
                }
                float hp1 = HealthSystem.Instance.Current;
                int bump1 = CoastPlayerVisual.BoardBumps;
                // 드레인(초당 max*1.6%)만큼은 원래 빠지는 값 — 그만큼 빼고 본다.
                float drain = HealthSystem.Instance.Max * HealthSystem.DrainFracPerSec * watch;
                Debug.LogWarning($"[110] RESULT bumps {bump0}->{bump1} (+{bump1 - bump0})  hp {hp0:F1}->{hp1:F1} (총 -{hp0 - hp1:F1}, 드레인 예상 -{drain:F1}, 피격분 -{Mathf.Max(0f, hp0 - hp1 - drain):F1})  점프높이 {maxClear:F2}");
                Destroy(gameObject);
            }
        }

        [MenuItem("Coast Run/Dev/109 - Week pass card")] public static void R109Week() { if (Application.isPlaying && GameManager.Active) { var s = GameManager.I.Save; var rep = new Survival.WeekReport { ateRice = true, ateSide = true, slept = true, clothesLeft = 3, riceLeft = 2 }; WeekPassUI.Show(s.week, s.week + 1, Timeline.SeasonOf(s.week), rep, null, () => Debug.LogWarning("[Dev] week done")); } }
        [MenuItem("Coast Run/Dev/109 - Event choice (new: kite)")] public static void R109Event() { if (Application.isPlaying) { var ui = Object.FindAnyObjectByType<TamaRaisingUI>(); if (ui != null) foreach (var e in RandomEventTable.All) if (e.id == "ev_kite") { ui.ShowEvent(e); break; } } }
        [MenuItem("Coast Run/Dev/109 - Card pick (job)")] public static void R109Pick() { if (Application.isPlaying) { var ui = Object.FindAnyObjectByType<TamaRaisingUI>(); if (ui != null) ui.DevOpenPick(2); } }
        [MenuItem("Coast Run/Dev/109 - Card pick (rest)")] public static void R109PickRest() { if (Application.isPlaying) { var ui = Object.FindAnyObjectByType<TamaRaisingUI>(); if (ui != null) ui.DevOpenPick(0); } }
        [MenuItem("Coast Run/Dev/109 - Card pick (play)")] public static void R109PickPlay() { if (Application.isPlaying) { var ui = Object.FindAnyObjectByType<TamaRaisingUI>(); if (ui != null) ui.DevOpenPick(1); } }
        [MenuItem("Coast Run/Dev/Fun - Calendar")] public static void FunCalendar() { if (Application.isPlaying && GameManager.Active) CalendarUI.Open(GameManager.I.Save); }
        [MenuItem("Coast Run/Dev/Fun - Event choice (radio)")] public static void FunEvent() { if (Application.isPlaying) { var ui = Object.FindAnyObjectByType<TamaRaisingUI>(); if (ui != null) ui.ShowEvent(RandomEventTable.All[1]); } }
        [MenuItem("Coast Run/Dev/Fun - Event choice (runaway)")] public static void FunEvent2() { if (Application.isPlaying) { var ui = Object.FindAnyObjectByType<TamaRaisingUI>(); if (ui != null) ui.ShowEvent(RandomEventTable.All[14]); } }
        [MenuItem("Coast Run/Dev/Contest - Intro CH1")] public static void ContestIntro() { if (Application.isPlaying) ContestIntroUI.Show(StoryContest.Get(1), () => Debug.LogWarning("[Dev] go")); }
        [MenuItem("Coast Run/Dev/Contest - Fail screen")] public static void ContestFail() { if (Application.isPlaying) { StoryContest.Begin(1); ContestResultUI.ShowFail(false); } }
        [MenuItem("Coast Run/Dev/Life - Test turn end (odd week, phase 2)")] public static void TestTurnEnd() { if (!Application.isPlaying || !GameManager.Active) return; var s = GameManager.I.Save; var rec = s.CurrentChapter; if (s.week % 2 == 0) s.week++; if (rec != null && rec.weekEnd <= s.week) rec.weekEnd = s.week + 2; s.phaseIndex = 2; s.boundaryPending = false; GameManager.I.Persist(); }
        [MenuItem("Coast Run/Dev/Life - Test boundary (last week, phase 2)")] public static void TestBoundary() { if (!Application.isPlaying || !GameManager.Active) return; var s = GameManager.I.Save; var rec = s.CurrentChapter; if (s.week % 2 == 0) s.week++; if (rec != null) { rec.weekEnd = s.week; rec.cleared = false; } s.phaseIndex = 2; s.boundaryPending = false; s.stats.stamina = System.Math.Max(s.stats.stamina, 120); GameManager.I.Persist(); }
        // 66차: 대회 러닝(라이벌·느낌표) 확인용 — 육성 화면에서 챕터를 맞춘 뒤 바로 대회 러닝으로
        [MenuItem("Coast Run/Dev/Contest - Run CH4 (photos, rivals)")] public static void ContestRunCh4() { if (!Application.isPlaying || !GameManager.Active) return; var s = GameManager.I.Save; s.chapter = 4; if (s.week < 7) s.week = 7; GameManager.I.Persist(); GameManager.I.StartStoryRun(); }
        [MenuItem("Coast Run/Dev/Contest - Run CH1 (coins, rivals)")] public static void ContestRunCh1() { if (!Application.isPlaying || !GameManager.Active) return; var s = GameManager.I.Save; s.chapter = 1; GameManager.I.Persist(); GameManager.I.StartStoryRun(); }
        // 66차: 펫 확인용 — 장착 후 대회 러닝(CH1)로
        [MenuItem("Coast Run/Dev/Pet - Shop")] public static void PetShop() { if (Application.isPlaying && GameManager.Active) PetShopUI.Open(GameManager.I); }
        [MenuItem("Coast Run/Dev/Pet - Equip Sparrow + run")] public static void PetSparrow() { PetRun(PetKind.Sparrow); }
        [MenuItem("Coast Run/Dev/Pet - Equip BlackPig + run")] public static void PetPig() { PetRun(PetKind.BlackPig); }
        [MenuItem("Coast Run/Dev/Pet - Equip BikerThug + run")] public static void PetThug() { PetRun(PetKind.BikerThug); }
        [MenuItem("Coast Run/Dev/Pet - Equip WildGoose + run")] public static void PetGoose() { PetRun(PetKind.WildGoose); }
        private static void PetRun(PetKind k) { if (!Application.isPlaying || !GameManager.Active) return; var s = GameManager.I.Save; s.equippedPet = k; s.chapter = 1; GameManager.I.Persist(); GameManager.I.StartStoryRun(); }
        // 66차-8: 한 곡 완주 화면 확인용(K-POP 런 중에)
        [MenuItem("Coast Run/Dev/Fx - Song complete screen")] public static void SongComplete() { if (!Application.isPlaying || !ArcadeRun.KpopMode || RunHudChrome.Instance == null) return; ArcadeRun.MarkKpopFinished(); RunHudChrome.Instance.ShowRunOver(() => Debug.LogWarning("[Dev] retry"), () => Debug.LogWarning("[Dev] exit"), "메인으로"); }
        // 68차: 시네마틱 확인용
        // 71차: 챕터 선택 화면 READY/LOCKED 모양 확인 — 마지막 클리어를 5로 꾸며서 연다(타이틀에서). 끄기 = 실제 진행으로.
        [MenuItem("Coast Run/Dev/Chapter select - preview (last clear 5)")] public static void ChapterPreview() { if (!Application.isPlaying) return; KpopChapterSelect.DebugLastClear = 5; KpopChapterSelect.Open(GameManager.I, null, null); }
        [MenuItem("Coast Run/Dev/Chapter select - preview off")] public static void ChapterPreviewOff() { KpopChapterSelect.DebugLastClear = -1; }
        [MenuItem("Coast Run/Dev/God mode - ON")] public static void GodOn() { PlayerController.DebugGod = true; Debug.LogWarning("[Dev] God mode ON — 장애물 피해 무시(HUD 에 GOD 배지)"); }
        [MenuItem("Coast Run/Dev/God mode - OFF")] public static void GodOff() { PlayerController.DebugGod = false; var p = UnityEngine.Object.FindAnyObjectByType<PlayerController>(); if (p != null) p.Invincible = false; Debug.LogWarning("[Dev] God mode OFF"); }
        // 77차: 시네마 선택 화면(엔딩 카드 잠금 확인용)
        [MenuItem("Coast Run/Dev/Cine - Select")] public static void CineSelect() { if (Application.isPlaying) CinemaSelect.Open(GameManager.I, null, () => Debug.LogWarning("[Dev] cinema select closed")); }
        [MenuItem("Coast Run/Dev/Cine - END_A")] public static void CineEndA() { if (Application.isPlaying) CinematicPlayer.Play("END_A", () => Debug.LogWarning("[Dev] cine done")); }
        [MenuItem("Coast Run/Dev/Cine - OPEN")] public static void CineOpen() { if (Application.isPlaying) CinematicPlayer.Play("OPEN", () => Debug.LogWarning("[Dev] cine done")); }
        [MenuItem("Coast Run/Dev/Cine - CS1")] public static void CineCs1() { if (Application.isPlaying) CinematicPlayer.Play("CS1", () => Debug.LogWarning("[Dev] cine done")); }
        [MenuItem("Coast Run/Dev/Cine - CS2")] public static void CineCs2() { if (Application.isPlaying) CinematicPlayer.Play("CS2", () => Debug.LogWarning("[Dev] cine done")); }
        [MenuItem("Coast Run/Dev/Cine - CS3")] public static void CineCs3() { if (Application.isPlaying) CinematicPlayer.Play("CS3", () => Debug.LogWarning("[Dev] cine done")); }
        [MenuItem("Coast Run/Dev/Cine - CS4")] public static void CineCs4() { if (Application.isPlaying) CinematicPlayer.Play("CS4", () => Debug.LogWarning("[Dev] cine done")); }
        [MenuItem("Coast Run/Dev/Cine - CS5")] public static void CineCs5() { if (Application.isPlaying) CinematicPlayer.Play("CS5", () => Debug.LogWarning("[Dev] cine done")); }
        [MenuItem("Coast Run/Dev/Cine - CS6")] public static void CineCs6() { if (Application.isPlaying) CinematicPlayer.Play("CS6", () => Debug.LogWarning("[Dev] cine done")); }
        [MenuItem("Coast Run/Dev/Cine - CS7")] public static void CineCs7() { if (Application.isPlaying) CinematicPlayer.Play("CS7", () => Debug.LogWarning("[Dev] cine done")); }
        [MenuItem("Coast Run/Dev/Cine - CS8")] public static void CineCs8() { if (Application.isPlaying) CinematicPlayer.Play("CS8", () => Debug.LogWarning("[Dev] cine done")); }
        // 85차: 보조 컷씬·엔딩 B/TRUE·단서 카드
        [MenuItem("Coast Run/Dev/Cine - EV1")] public static void CineEv1() { if (Application.isPlaying) CinematicPlayer.Play("EV1", () => Debug.LogWarning("[Dev] cine done")); }
        [MenuItem("Coast Run/Dev/Cine - EV5")] public static void CineEv5() { if (Application.isPlaying) CinematicPlayer.Play("EV5", () => Debug.LogWarning("[Dev] cine done")); }
        [MenuItem("Coast Run/Dev/Cine - EV9")] public static void CineEv9() { if (Application.isPlaying) CinematicPlayer.Play("EV9", () => Debug.LogWarning("[Dev] cine done")); }
        [MenuItem("Coast Run/Dev/Cine - EV10")] public static void CineEv10() { if (Application.isPlaying) CinematicPlayer.Play("EV10", () => Debug.LogWarning("[Dev] cine done")); }
        [MenuItem("Coast Run/Dev/Cine - END_B")] public static void CineEndB() { if (Application.isPlaying) CinematicPlayer.Play("END_B", () => Debug.LogWarning("[Dev] cine done")); }
        [MenuItem("Coast Run/Dev/Cine - END_TRUE")] public static void CineEndTrue() { if (Application.isPlaying) CinematicPlayer.Play("END_TRUE", () => Debug.LogWarning("[Dev] cine done")); }
        [MenuItem("Coast Run/Dev/Clue - Card CS4 (돌)")] public static void ClueCs4() { if (Application.isPlaying && GameManager.I != null && GameManager.I.Save != null) { GameManager.I.Save.clueMask &= ~(int)ClueSystem.Clue.Stones; ClueSystem.ShowAfterScene(GameManager.I.Save, "CS4", () => Debug.LogWarning("[Dev] clue " + ClueSystem.Summary(GameManager.I.Save))); } }
        [MenuItem("Coast Run/Dev/Clue - Card CS7 (이름)")] public static void ClueCs7() { if (Application.isPlaying && GameManager.I != null && GameManager.I.Save != null) { GameManager.I.Save.clueMask &= ~(int)ClueSystem.Clue.Name; ClueSystem.ShowAfterScene(GameManager.I.Save, "CS7", () => Debug.LogWarning("[Dev] clue " + ClueSystem.Summary(GameManager.I.Save))); } }
        [MenuItem("Coast Run/Dev/186 - Econ table")] public static void EconTable()
        {
            var sb = new System.Text.StringBuilder("[EconTable]");
            foreach (var id in new[] { "ing_rice", "dish_rice", "dish_meat", "dish_soup", "med_cold", "care_perfume", "clothes_set" }) { var d = LifeItems.Get(id); sb.Append($" {id}={(d.HasValue ? d.Value.price : -1)}"); }
            foreach (var j in new[] { "job_orange", "job_night_delivery", "job_hall" }) { var d = ScheduleTable.Get(j); sb.Append($" {j}={(d != null ? d.dMoney : -999)}"); }
            sb.Append($" fest1={Festival.Money(1)} bed={HomeData.Furniture[0].price} seed={HomeData.Seeds[0].price} yard0={CoastRun.Village.VillageHub.Yard[0].price} lot0={CoastRun.Village.VillageLand.Lots[0].price}/{CoastRun.Village.VillageLand.Lots[0].rent} mission={CoastRun.Village.VillageMission.Money(CoastRun.Village.VillageMission.Kind.Fish)}");
            Debug.LogWarning(sb.ToString());
        }
        [MenuItem("Coast Run/Dev/186 - Econ reset")] public static void EconReset() { CoinPickup.DevSpawnedValue = 0; Debug.LogWarning("[Econ] reset"); }
        [MenuItem("Coast Run/Dev/186 - Econ log")] public static void EconLog() { var st = StageRunStats.Instance; var sv = GameManager.I != null ? GameManager.I.PeekSave() : null; Debug.LogWarning($"[Econ] spawned={CoinPickup.DevSpawnedValue} coin={(st != null ? st.CoinValue : -1)} near={(st != null ? st.NearMissValue : -1)} coins#={(st != null ? st.Coins : -1)} lastMoney={ArcadeRun.LastMoney} finished={ArcadeRun.KpopFinished} doubled={ArcadeRun.LastDoubled} money={(sv != null ? sv.stats.money : -1)} jelly={JellyWallet.Total} lastJelly={ArcadeRun.LastJelly}"); }
        [MenuItem("Coast Run/Dev/Kpop - Start")] public static void KpopStart() { if (Application.isPlaying) ArcadeRun.StartKpop(GameManager.Ensure()); }
        [MenuItem("Coast Run/Dev/Kpop - Log pet")] public static void KpopPet() { var gm = GameManager.I; var sv = gm != null ? gm.PeekSave() : null; Debug.LogWarning($"[Dev] pet save={(sv != null ? sv.equippedPet.ToString() : "nosave")} owned={(sv != null ? sv.ownedPetMask : 0)} tuning={RunTuning.Pet} inst={(PetCompanion.Instance != null)}"); }
        [MenuItem("Coast Run/Dev/UI - Donate")] public static void UiDonate() { if (Application.isPlaying) DonateUI.Open(); }
        [MenuItem("Coast Run/Dev/UI - Status")] public static void UiStatus() { if (Application.isPlaying) StatusUI.Open(GameManager.I); }
        [MenuItem("Coast Run/Dev/Contest - HUD test (ch1)")] public static void ContestHud() { if (Application.isPlaying) StoryContest.Begin(1); }
        [MenuItem("Coast Run/Dev/Clue - Log")] public static void ClueLog() { if (Application.isPlaying && GameManager.I != null && GameManager.I.Save != null) Debug.LogWarning("[Dev] " + ClueSystem.Summary(GameManager.I.Save) + " → " + ClueSystem.EndingId(GameManager.I.Save.clueMask)); }
        [MenuItem("Coast Run/Dev/Contest - Close all")] public static void ContestClose() { ContestIntroUI.Close(); ContestResultUI.Close(); WeekPassUI.Close(); GroceryUI.Close(); GameOverUI.Close(); Time.timeScale = 1f; }
        // 56차-2(사용자): 글자가 상자를 넘는지 검사 — 화면의 모든 Text 를 훑어 preferred 크기가 rect 보다 크면 경로·글자·크기를 로그로.
        [MenuItem("Coast Run/Dev/UI - Overflow audit")]
        public static void OverflowAudit()
        {
            if (!Application.isPlaying) return;
            var sb = new System.Text.StringBuilder(); int n = 0, total = 0;
            foreach (var t in Object.FindObjectsByType<UnityEngine.UI.Text>(FindObjectsSortMode.None))
            {
                if (t == null || !t.isActiveAndEnabled || string.IsNullOrWhiteSpace(t.text)) continue;
                if (!t.gameObject.activeInHierarchy) continue;
                var r = t.rectTransform.rect; total++;
                if (r.width < 4f || r.height < 4f) continue;
                if (t.resizeTextForBestFit) continue;
                bool wrap = t.horizontalOverflow == HorizontalWrapMode.Wrap;
                float pw = t.preferredWidth, ph = t.preferredHeight;
                bool over = wrap ? (t.verticalOverflow == VerticalWrapMode.Truncate ? ph > r.height + 2f : ph > r.height + 2f) : (pw > r.width + 2f || (t.verticalOverflow == VerticalWrapMode.Truncate && ph > r.height + 2f));
                if (!over) continue;
                // 부모 레이아웃이 높이를 정하는 것(리더 본문 등)은 제외
                if (t.GetComponentInParent<UnityEngine.UI.LayoutGroup>() != null && wrap && ph <= r.height + 40f) continue;
                string path = t.name; var p = t.transform.parent; int d = 0;
                while (p != null && d++ < 5) { path = p.name + "/" + path; p = p.parent; }
                string txt = t.text.Replace("\n", "⏎"); if (txt.Length > 40) txt = txt.Substring(0, 40) + "…";
                sb.Append($"\n  {path}  [{txt}]  need {pw:0}x{ph:0} > box {r.width:0}x{r.height:0} font {t.fontSize}{(wrap ? " wrap" : "")}");
                n++;
            }
            Debug.LogWarning($"[UIAudit] overflow {n}/{total}: " + sb);
        }
        // 95차(사용자: 「화면 위아래로 짤리지 않게」): 지금 게임뷰 크기에서 **실제로 화면 밖으로 나간 UI**를 찾는다.
        //   배경·딤처럼 일부러 넘치는 것(화면을 거의 다 덮는 것)과 비활성은 뺀다. 위/아래로 나간 양이 큰 것부터.
        [MenuItem("Coast Run/Dev/UI - Offscreen audit")]
        public static void OffscreenAudit()
        {
            if (!Application.isPlaying) { Debug.Log("[UIAudit] 플레이 중에만 검사합니다."); return; }
            float sw = Screen.width, sh = Screen.height;
            var sa = Screen.safeArea;
            if (sa.width < 8f || sa.height < 8f) sa = new Rect(0f, 0f, sw, sh);
            CoastUiCanvas.DesignMetrics(sw, sh, sa.width, sa.height, out var inset, out float fit);
            var hits = new System.Collections.Generic.List<(float over, string line)>();
            var corners = new Vector3[4];
            foreach (var g in Object.FindObjectsByType<UnityEngine.UI.Graphic>(FindObjectsSortMode.None))
            {
                if (g == null || !g.isActiveAndEnabled || !g.gameObject.activeInHierarchy) continue;
                var canvas = g.canvas; if (canvas == null) continue;
                var rt = g.rectTransform;
                rt.GetWorldCorners(corners);
                var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
                for (int i = 0; i < 4; i++)
                {
                    var p = RectTransformUtility.WorldToScreenPoint(cam, corners[i]);
                    min = Vector2.Min(min, p); max = Vector2.Max(max, p);
                }
                float w = max.x - min.x, h = max.y - min.y;
                if (w < 12f || h < 12f) continue;
                if (w > sw * 0.95f && h > sh * 0.95f) continue;             // 배경·딤·입력 막
                float outTop = Mathf.Max(0f, max.y - sa.yMax), outBottom = Mathf.Max(0f, sa.yMin - min.y);
                float outLeft = Mathf.Max(0f, sa.xMin - min.x), outRight = Mathf.Max(0f, max.x - sa.xMax);
                float over = Mathf.Max(Mathf.Max(outTop, outBottom), Mathf.Max(outLeft, outRight));
                if (over < 4f) continue;
                string path = g.name; var p2 = g.transform.parent; int d = 0;
                while (p2 != null && d++ < 4) { path = p2.name + "/" + path; p2 = p2.parent; }
                string dir = (outTop > 0f ? $" 위 {outTop:0}" : "") + (outBottom > 0f ? $" 아래 {outBottom:0}" : "")
                           + (outLeft > 0f ? $" 왼 {outLeft:0}" : "") + (outRight > 0f ? $" 오 {outRight:0}" : "");
                hits.Add((over, $"\n  {over,5:0}px{dir,-20} {path}  ({w:0}x{h:0}px)"));
            }
            hits.Sort((a, b) => b.over.CompareTo(a.over));
            var sb = new System.Text.StringBuilder();
            sb.Append($"[UIAudit] 화면 {sw:0}x{sh:0} (비율 {sw / sh:0.000}) · 안전영역 {sa.width:0}x{sa.height:0}"
                      + $" · 인셋 {inset.x:0}x{inset.y:0} 배율 {fit:0.000} · 기준 {CoastUiCanvas.HudDesignWidth:0}x{CoastUiCanvas.HudDesignHeight:0}");
            sb.Append(fit > CoastUiCanvas.MinFitScale + 0.001f ? "  → 좌표계는 기준 크기 확보(잘림 없음)" : "  → 축소 하한에 걸림(잘릴 수 있음)");
            sb.Append($"\n  화면 밖으로 나간 UI {hits.Count}개");
            for (int i = 0; i < hits.Count && i < 40; i++) sb.Append(hits[i].line);
            Debug.LogWarning(sb.ToString());
        }
        // 95차-2(사용자: 「스토리모드 상단의 버튼들이 다 사라졌다」): 상단 줄만 콕 집어 찍는다.
        //   「없음」이면 만들어지지 않은 것(예외·컴파일), 「밖」이면 화면 밖으로 나간 것(비율·게임뷰 배율),
        //   「꺼짐」이면 누가 SetActive(false) 한 것 — 원인이 바로 갈린다.
        [MenuItem("Coast Run/Dev/UI - 육성 상단바 덤프")]
        public static void RaisingTopBarDump()
        {
            if (!Application.isPlaying) { Debug.Log("[TopBar] 플레이 중에만 검사합니다."); return; }
            string[] names = { "Week", "GoalRibbon", "Money", "StatusBtn", "TutorialBtn", "RoomBtn", "ShopBtn", "BagBtn", "Home", "ActRing0", "HpTrack", "StressTrack", "NextTurn" };
            var canvas = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            RectTransform root = null;
            foreach (var c in canvas)
                if (c != null && c.name == "TamaRaisingCanvas") { root = CoastUiCanvas.Root(c); break; }
            if (root == null) { Debug.LogWarning("[TopBar] TamaRaisingCanvas 가 없습니다 — 육성(스토리) 화면에서 실행하세요."); return; }

            float sw = Screen.width, sh = Screen.height;
            var sa = Screen.safeArea; if (sa.width < 8f || sa.height < 8f) sa = new Rect(0f, 0f, sw, sh);
            var all = root.GetComponentsInChildren<RectTransform>(true);
            var sb = new System.Text.StringBuilder();
            sb.Append($"[TopBar] 화면 {sw:0}x{sh:0} · 안전영역 {sa.width:0}x{sa.height:0}");
            var fitBox = root.Find("Fit") as RectTransform;
            if (fitBox != null) sb.Append($" · Fit 상자 {fitBox.rect.width:0}x{fitBox.rect.height:0} 배율 {fitBox.localScale.x:0.000}");
            sb.Append($" · 인셋 {root.rect.width:0}x{root.rect.height:0} 배율 {root.localScale.x:0.000}");
            var corners = new Vector3[4];
            foreach (var n in names)
            {
                RectTransform rt = null;
                foreach (var c in all) if (c != null && c.name == n) { rt = c; break; }
                if (rt == null) { sb.Append($"\n  {n,-12} 없음 (만들어지지 않음)"); continue; }
                if (!rt.gameObject.activeInHierarchy) { sb.Append($"\n  {n,-12} 꺼짐 (SetActive false)"); continue; }
                rt.GetWorldCorners(corners);
                Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
                for (int i = 0; i < 4; i++) { var p = RectTransformUtility.WorldToScreenPoint(null, corners[i]); min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
                float outTop = Mathf.Max(0f, max.y - sa.yMax), outBottom = Mathf.Max(0f, sa.yMin - min.y);
                float outLeft = Mathf.Max(0f, sa.xMin - min.x), outRight = Mathf.Max(0f, max.x - sa.xMax);
                bool outside = outTop + outBottom + outLeft + outRight > 4f;
                var g = rt.GetComponent<UnityEngine.UI.Graphic>();
                string vis = g == null ? "" : g.color.a < 0.02f ? " 투명" : "";
                sb.Append($"\n  {n,-12} {(outside ? "밖 " : "OK ")} 화면 x {min.x:0}~{max.x:0} y {min.y:0}~{max.y:0}{vis}"
                          + (outside ? (outTop > 0f ? $" 위로 {outTop:0}px" : "") + (outBottom > 0f ? $" 아래로 {outBottom:0}px" : "")
                                     + (outLeft > 0f ? $" 왼쪽 {outLeft:0}px" : "") + (outRight > 0f ? $" 오른쪽 {outRight:0}px" : "") : ""));
            }
            Debug.LogWarning(sb.ToString());
        }

        [MenuItem("Coast Run/Dev/Collection - Unlock all (F9)")] public static void UnlockAll() { if (Application.isPlaying) Collection.DebugUnlockAll(); }

        // ── 136차: 바닷가 마을 ──
        [MenuItem("Coast Run/Dev/Village - Open")]
        private static void VillageOpen() { var gm = GameManager.I; if (gm == null || gm.Save == null) return; gm.EnterRaising(); }   // 148차: 05_Raising 은 항상 마을부터
        [MenuItem("Coast Run/Dev/Village - Go tower")] private static void VGoTower() => VGo("tower");
        [MenuItem("Coast Run/Dev/Village - Go hero house")] private static void VGoHero() => VGo("hero");
        [MenuItem("Coast Run/Dev/Village - Go mom house")] private static void VGoMom() => VGo("mom");
        [MenuItem("Coast Run/Dev/Village - Go shop")] private static void VGoShop() => VGo("shop");
        [MenuItem("Coast Run/Dev/Village - Go garden")] private static void VGoGarden() => VGo("garden");
        /// 147차: 파스텔 집들 사이 골목(꽃집 오른쪽·해녀네/등대지기 왼쪽) — 시안(두 집 사이 길) 비교용
        [MenuItem("Coast Run/Dev/Village - Diag")] private static void VDiag() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DiagLog(); }
        [MenuItem("Coast Run/Dev/Village - Go palm")] private static void VGoPalm() { var h = CoastRun.Village.VillageHub.I; if (h == null) return; h.Teleport(new Vector3(13f, 0f, -18.5f), 215f); }
        [MenuItem("Coast Run/Dev/Village - Go farm tile")] private static void VGoFarm() { var h = CoastRun.Village.VillageHub.I; if (h == null) return; var c = CoastRun.Village.VillageFarm.TileCenter(4); h.Teleport(new Vector3(c.x, 0f, c.z), 180f); }
        [MenuItem("Coast Run/Dev/Village - Tool axe")] private static void VToolAxe() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevTool(2); }
        [MenuItem("Coast Run/Dev/Village - Tool net")] private static void VToolNet() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevTool(0); }
        [MenuItem("Coast Run/Dev/Village - Go hill down")] private static void VGoHillD() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.Teleport(new Vector3(0f, 0f, 21f), 180f); }
        [MenuItem("Coast Run/Dev/Village - Go hill up")] private static void VGoHillU() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.Teleport(new Vector3(0f, 0f, 12f), 0f); }
        [MenuItem("Coast Run/Dev/Village - Go hill west")] private static void VGoHillW() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.Teleport(new Vector3(-8f, 0f, 30f), 270f); }
        [MenuItem("Coast Run/Dev/Village - Count pines")] private static void VCountPines() { int n = 0; foreach (var t in GameObject.FindObjectsByType<Transform>(FindObjectsSortMode.None)) if (t.name.StartsWith("Tree_Pine") || t.name == "Windmill") n++; Debug.LogWarning("[Pines] " + n); }
        [MenuItem("Coast Run/Dev/Village - Tool pick")] private static void VToolPick() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevTool(3); }
        // 172차: 이번 주 주민 부탁을 로그로 찍고, 첫 번째 부탁 NPC 앞으로 옮긴다
        [MenuItem("Coast Run/Dev/Village - Go request NPC")]
        private static void VGoReq()
        {
            var h = CoastRun.Village.VillageHub.I; var gm = CoastRun.GameManager.I;
            if (h == null || gm == null || gm.Save == null) return;
            var s = gm.Save; CoastRun.Village.VillageRequest.Ensure(s);
            var sb = new System.Text.StringBuilder($"[Req] week={s.week} n={s.reqKind.Length}");
            for (int i = 0; i < s.reqKind.Length; i++)
                sb.Append($" | npc{s.reqNpc[i]}({h.NpcName(s.reqNpc[i])}) {(CoastRun.Village.VillageMission.Kind)s.reqKind[i]} state={s.reqState[i]} prog={s.reqProg[i]}");
            Debug.LogWarning(sb.ToString());
            if (s.reqNpc.Length == 0) return;
            var p = h.NpcPos(s.reqNpc[0]); h.Teleport(new Vector3(p.x, 0f, p.z - 1.6f), 0f);
        }
        [MenuItem("Coast Run/Dev/Village - Act now (test)")] private static void VActNow() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.SendMessage("OnAct", SendMessageOptions.DontRequireReceiver); }   // 172차: 행동 버튼(오른쪽 반 톡) 대신 바로 실행 — 원격 tap 으로는 CameraPad 를 못 누른다
        [MenuItem("Coast Run/Dev/218 - Swing film (side, 12 frames)")] private static void VSwingFilm() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.StartCoroutine(h.DevSwingFilm()); }   // 218차: 옆에서 본 휘두르기 12장 → Tools/_shots/p218_film_*.png
        [MenuItem("Coast Run/Dev/Village - Swing now (test)")] private static void VSwingNow() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.SendMessage("Swing", SendMessageOptions.DontRequireReceiver); }   // 171차: 도구 모션 확인용
        [MenuItem("Coast Run/Dev/Village - Tool bat")] private static void VToolBat() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevTool(1); }   // 171차: 방망이(빠져 있었음)
        [MenuItem("Coast Run/Dev/Village - Tool rod")] private static void VToolRod() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevTool(4); }
        [MenuItem("Coast Run/Dev/Village - Go rock")] private static void VGoRock() { var h = CoastRun.Village.VillageHub.I; if (h == null) return; foreach (var rk in CoastRun.Village.VillageWorld.Rocks) if (rk.t != null) { var p = rk.t.position + new Vector3(0f, 0f, 1.9f); h.Teleport(new Vector3(p.x, 0f, p.z), 180f); return; } }
        [MenuItem("Coast Run/Dev/Village - Go ranch inside")] private static void VGoRanchIn() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.Teleport(new Vector3(CoastRun.Village.VillageRanch.CX + 2f, 0f, CoastRun.Village.VillageRanch.CZ - 4f), 340f); }
        [MenuItem("Coast Run/Dev/Village - Go ranch")] private static void VGoRanch() { var h = CoastRun.Village.VillageHub.I; if (h == null) return; var g = CoastRun.Village.VillageRanch.Gate; h.Teleport(new Vector3(g.x + 3f, 0f, g.z), 270f); }
        [MenuItem("Coast Run/Dev/Village - Spawn monster")] private static void VMonster() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevMonster(); }
        [MenuItem("Coast Run/Dev/Village - Weather rain")] private static void VWRain() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevWeather(CoastRun.WeatherKind.Rain); }
        [MenuItem("Coast Run/Dev/Village - Weather snow")] private static void VWSnow() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevWeather(CoastRun.WeatherKind.Snow); }
        [MenuItem("Coast Run/Dev/Village - Weather clear")] private static void VWClear() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevWeather(CoastRun.WeatherKind.Clear); }
        // 178차: 밤 몬스터 → 실제 펫 획득 분기 확인용(펫을 잠시 비웠다가 되돌린다)
        static int _petBakMask = -1; static PetKind _petBakEq;
        [MenuItem("Coast Run/Dev/Village - Pet test: clear pets (backup)")] private static void VPetClear()
        {
            var gm = GameManager.I; if (!Application.isPlaying || gm == null || gm.Save == null) return; var s = gm.Save;
            if (_petBakMask < 0) { _petBakMask = s.ownedPetMask; _petBakEq = s.equippedPet; }
            s.ownedPetMask = 0; s.equippedPet = PetKind.None;
            Debug.LogWarning($"[PetTest] cleared (backup mask={_petBakMask} eq={_petBakEq})");
        }
        [MenuItem("Coast Run/Dev/Village - Pet test: catch in front")] private static void VPetCatch()
        {
            var h = CoastRun.Village.VillageHub.I; if (h == null) return;
            h.DevTool(0); h.DevMonster();
            typeof(CoastRun.Village.VillageHub).GetMethod("OnAct", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.Invoke(h, null);
            var s = GameManager.I.Save; Debug.LogWarning($"[PetTest] after catch mask={s.ownedPetMask} eq={s.equippedPet}");
        }
        [MenuItem("Coast Run/Dev/Village - Go behind hero house")] private static void VGoBehindHero() { var h = CoastRun.Village.VillageHub.I; if (h == null) return; h.Teleport(h.SpotPos("hero") + new Vector3(0f, 0f, 9.5f), 0f); }   // 178차: 집이 카메라를 가리는 자리(반투명 확인)
        [MenuItem("Coast Run/Dev/Village - Go wake-up spot")] private static void VGoWake() { var h = CoastRun.Village.VillageHub.I; var hh = CoastRun.Village.VillageWorld.HeroHouse; if (h == null || hh == null) return; h.Teleport(hh.TransformPoint(new Vector3(0f, 0f, 5.6f)), 180f); }   // 180차: 잠 뒤 아침에 서는 자리(집 앞 5.6 m, 남향)
        // 181차 진단: 바닷가·언덕 걷기 시험(북·남·동·서로 3초씩)
        [MenuItem("Coast Run/Dev/Village - Walk test N")] private static void VWalkTestN() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevWalkTest(0f); }
        [MenuItem("Coast Run/Dev/Village - Walk test S")] private static void VWalkTestS() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevWalkTest(180f); }
        [MenuItem("Coast Run/Dev/Village - Walk test E")] private static void VWalkTestE() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevWalkTest(90f); }
        [MenuItem("Coast Run/Dev/Village - Walk test W")] private static void VWalkTestW() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevWalkTest(270f); }
        [MenuItem("Coast Run/Dev/Village - Go beach rocks")] private static void VGoBeachRocks() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.Teleport(new Vector3(-9f, 0f, -58f), 90f); }
        [MenuItem("Coast Run/Dev/Village - Go beach rocks W")] private static void VGoBeachRocksW() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.Teleport(new Vector3(-21f, 0f, -50f), 270f); }
        // 181차 진단: 바닷가(z<-14) 근처에 서 있는데 콜라이더가 없는 소품(발밑 높이 0.5 m 이상 솟은 것) 목록
        [MenuItem("Coast Run/Dev/Village - Diag no-collider props")] private static void VDiagNoCol()
        {
            var w = GameObject.Find("VillageWorld"); if (w == null) { Debug.LogWarning("[NoCol] no world"); return; }
            var sb = new System.Text.StringBuilder("[NoCol]"); int n = 0; var seen = new System.Collections.Generic.HashSet<string>();
            foreach (Transform ch in w.transform)
            {
                var p = ch.position; if (p.z > -14f) continue;
                string nm = ch.name; if (nm == "Terrain" || nm == "Sea" || nm == "Foam" || nm.StartsWith("Vista") || nm == "FarHill" || nm == "Bound" || nm == "Cloud" || nm == "Bird" || nm == "SkyDome" || nm == "Sun" || nm == "SunDisc" || nm == "Fill" || nm == "PathMesh" || nm == "Pampas" || nm == "FlowerClump" || nm == "Sparkle") continue;
                if (ch.GetComponentInChildren<Collider>() != null) continue;
                Bounds b = default; bool f = true; foreach (var r in ch.GetComponentsInChildren<Renderer>()) { if (f) { b = r.bounds; f = false; } else b.Encapsulate(r.bounds); }
                if (f) continue; float gy = CoastRun.Village.VillageWorld.Height(p.x, p.z); if (b.max.y - gy < 0.5f || b.size.x < 0.3f && b.size.z < 0.3f) continue;
                n++; if (seen.Add(nm)) sb.Append($" | {nm} @({p.x:F0},{p.z:F0}) h={b.max.y - gy:F1} w={Mathf.Max(b.size.x, b.size.z):F1}");
            }
            Debug.LogWarning(sb.Append($" | total={n}").ToString());
        }
        [MenuItem("Coast Run/Dev/Village - Cc legacy (old capsule)")] private static void VCcOld() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevLegacyCc(true); }
        [MenuItem("Coast Run/Dev/Village - Cc fixed")] private static void VCcNew() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevLegacyCc(false); }
        [MenuItem("Coast Run/Dev/Fx - Weather probe all")] private static void WeatherProbeAll()
        {
            var sb = new System.Text.StringBuilder("[WeatherAll]");
            foreach (var fx in Object.FindObjectsByType<WeatherFx>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                sb.Append($"\n {fx.gameObject.name} parent={(fx.transform.parent != null ? fx.transform.parent.name : "-")} active={fx.isActiveAndEnabled} weather={fx.Current} density={fx.Density}");
                foreach (var ps in fx.GetComponentsInChildren<ParticleSystem>(true)) if (ps.particleCount > 0) sb.Append($" | {ps.name} n={ps.particleCount} rate={ps.emission.rateOverTime.constant:F0}");
            }
            Debug.LogWarning(sb.ToString());
        }
        // 183차 진단: 임대 땅 후보 — 7×7 m 안에 콜라이더·렌더러가 없고 평평(높이차 <0.9 m)하고 길에서 4 m 이상 떨어진 칸
        [MenuItem("Coast Run/Dev/Village - Diag free lots")] private static void VDiagLots()
        {
            var sb = new System.Text.StringBuilder("[Lots]"); int n = 0;
            for (float z = -22f; z <= 42f; z += 4f) for (float x = -42f; x <= 42f; x += 4f)
            {
                var W = CoastRun.Village.VillageWorld.Height(x, z);
                if (W < -0.3f || z < -18f) continue;   // 바다·모래
                float hmin = 99f, hmax = -99f; for (int i = -1; i <= 1; i++) for (int j = -1; j <= 1; j++) { float h = CoastRun.Village.VillageWorld.Height(x + i * 3.5f, z + j * 3.5f); hmin = Mathf.Min(hmin, h); hmax = Mathf.Max(hmax, h); }
                if (hmax - hmin > 1.3f) continue;
                if (CoastRun.Village.VillageWorld.PathDist(x, z) < 4.5f) continue;
                if (Physics.CheckBox(new Vector3(x, W + 1.5f, z), new Vector3(3.5f, 1.4f, 3.5f), Quaternion.identity, ~0, QueryTriggerInteraction.Collide)) continue;
                bool rend = false; var bb = new Bounds(new Vector3(x, W + 1.5f, z), new Vector3(7f, 3f, 7f));
                foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)) { var nm = r.gameObject.name; if (nm == "Terrain" || nm == "Sea" || nm.Contains("Path") || nm.Contains("Sky") || nm.Contains("Cloud") || nm.Contains("Vista") || nm.Contains("FarHill")) continue; if (r.bounds.size.x > 40f || Mathf.Max(r.bounds.size.x, r.bounds.size.y, r.bounds.size.z) < 1.3f) continue; if (r.bounds.Intersects(bb)) { rend = true; break; } }
                if (rend) continue;
                n++; sb.Append($" ({x:F0},{z:F0} h{W:F1})");
            }
            Debug.LogWarning(sb.Append($" n={n}").ToString());
        }
        [MenuItem("Coast Run/Dev/Village - Diag lot blockers")] private static void VDiagBlk()
        {
            var sb = new System.Text.StringBuilder("[Blk]");
            foreach (var q in new[] { new Vector2(20f, 10f), new Vector2(-15f, 5f), new Vector2(30f, 20f), new Vector2(-30f, 0f), new Vector2(15f, -12f), new Vector2(-20f, -10f) })
            {
                float W = CoastRun.Village.VillageWorld.Height(q.x, q.y); sb.Append($"\n ({q.x},{q.y}) h={W:F1} pd={CoastRun.Village.VillageWorld.PathDist(q.x, q.y):F1}:");
                foreach (var c in Physics.OverlapBox(new Vector3(q.x, W + 1.5f, q.y), new Vector3(3.5f, 1.4f, 3.5f), Quaternion.identity, ~0, QueryTriggerInteraction.Collide)) sb.Append(" " + c.name + "/" + (c.transform.parent != null ? c.transform.parent.name : "-"));
            }
            Debug.LogWarning(sb.ToString());
        }
        // 183차 시험 메뉴
        [MenuItem("Coast Run/Dev/183 - Dump editor log")] private static void D183Log()
        {
            var src = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "Unity/Editor/Editor.log");
            string txt; using (var fs = new System.IO.FileStream(src, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite)) using (var sr = new System.IO.StreamReader(fs)) txt = sr.ReadToEnd();
            var lines = txt.Split('\n'); var ex = new System.Text.StringBuilder(); for (int i = 0; i < lines.Length; i++) if (lines[i].Contains("Exception:")) { for (int k = i; k < Mathf.Min(lines.Length, i + 14); k++) ex.AppendLine(lines[k]); ex.AppendLine("-----"); } System.IO.File.WriteAllText(System.IO.Path.Combine(Application.dataPath, "../Tools/_view/editor_ex.txt"), ex.ToString()); int from = Mathf.Max(0, lines.Length - 400);
            System.IO.File.WriteAllText(System.IO.Path.Combine(Application.dataPath, "../Tools/_view/editorlog.txt"), string.Join("\n", lines, from, lines.Length - from));
        }
        [MenuItem("Coast Run/Dev/183 - Backup save")] private static void D183Bak() { var src = CoastRun.SaveManager.SavePath; var dst = System.IO.Path.Combine(Application.dataPath, "../Tools/_savebak_183.json"); System.IO.File.Copy(src, dst, true); Debug.LogWarning("[SaveBak] " + src + " -> " + dst); }
        [MenuItem("Coast Run/Dev/183 - Restore save (not playing)")] private static void D183Res() { if (Application.isPlaying) { Debug.LogWarning("[SaveBak] stop play first"); return; } var dst = CoastRun.SaveManager.SavePath; var src = System.IO.Path.Combine(Application.dataPath, "../Tools/_savebak_183.json"); System.IO.File.Copy(src, dst, true); Debug.LogWarning("[SaveBak] restored " + dst); }
        [MenuItem("Coast Run/Dev/185 - Walk into hero door")] private static void D185Door() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevWalkIntoHeroDoor(); }
        [MenuItem("Coast Run/Dev/187 - Road report")] private static void D187RoadRep() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevRoadReport(); }
        [MenuItem("Coast Run/Dev/187 - Road demo")] private static void D187RoadDemo() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevRoadDemo(); }
        [MenuItem("Coast Run/Dev/187 - Obstacle map")] private static void D187Obs() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevObstacleMap(); }
        [MenuItem("Coast Run/Dev/187 - NPC tap setup")] private static void D187Npc() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevNpcTapSetup(); }
        [MenuItem("Coast Run/Dev/188 - Auto move play")] private static void D188Goplay() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevAutoMove("play"); }
        [MenuItem("Coast Run/Dev/188 - Auto move job")] private static void D188Gojob() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevAutoMove("job"); }
        [MenuItem("Coast Run/Dev/188 - Auto move tower")] private static void D188Gotower() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevAutoMove("tower"); }
        [MenuItem("Coast Run/Dev/191 - Go east beach N")] private static void D191GoEN() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevGo(38f, -22f, 0f); }
        [MenuItem("Coast Run/Dev/191 - Go east beach E")] private static void D191GoEE() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevGo(38f, -22f, 90f); }
        [MenuItem("Coast Run/Dev/191 - Go west beach N")] private static void D191GoWN() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevGo(-40f, -22f, 0f); }
        [MenuItem("Coast Run/Dev/191 - Go west beach W")] private static void D191GoWW() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevGo(-40f, -22f, -90f); }
        [MenuItem("Coast Run/Dev/192 - Tour shots")] private static void D192Tour() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevTour(); }
        [MenuItem("Coast Run/Dev/192 - Fade log")] private static void D192Fade() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevFadeLog(); }
        [MenuItem("Coast Run/Dev/192 - Go hero front S")] private static void D192Hero() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevGo(0f, 26f, 180f); }
        [MenuItem("Coast Run/Dev/193 - Go shore S")] private static void D193Shore() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevGo(-2.5f, -23f, 180f); }
        [MenuItem("Coast Run/Dev/193 - Face bird")] private static void D193Bird() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevFaceBird(); }
        [MenuItem("Coast Run/Dev/193 - Face flock")] private static void D193Flock() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevFaceFlock(); }
        [MenuItem("Coast Run/Dev/192 - Go sea")] private static void D192Sea() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevGoSea(); }
        [MenuItem("Coast Run/Dev/191 - Obstacle map east")] private static void D191ObsE() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevObstacleMap(-2, 50, -32, 22, "_obsmap_east.txt"); }
        [MenuItem("Coast Run/Dev/191 - Obstacle map west")] private static void D191ObsW() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevObstacleMap(-50, -2, -32, 22, "_obsmap_west.txt"); }
        [MenuItem("Coast Run/Dev/191 - Obstacle map north")] private static void D191ObsN() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevObstacleMap(-50, 50, 20, 50, "_obsmap_north.txt"); }
        [MenuItem("Coast Run/Dev/189 - Obstacle map beach")] private static void D189ObsB() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevObstacleMap(-36, 16, -74, -12, "_obsmap_beach.txt"); }
        [MenuItem("Coast Run/Dev/189 - Collider dump beach")] private static void D189Col() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevColliderDump(); }
        [MenuItem("Coast Run/Dev/189 - Beach reach test")] private static void D189Reach() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevBeachReach(); }
        [MenuItem("Coast Run/Dev/190 - Exit house")] private static void D190Exit() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevExitHouse(); }
        [MenuItem("Coast Run/Dev/194 - KPOP tutorial")] private static void D194Tut() { if (!Application.isPlaying) return; CoastRun.GameDirector.Instance?.Flow?.ForceIdle(); CoastRun.KpopTutorial.Pending = true; CoastRun.ArcadeRun.StartKpop(CoastRun.GameManager.I, 1); }
        [MenuItem("Coast Run/Dev/194 - KPOP chapter 1")] private static void D194Ch1() { if (!Application.isPlaying) return; CoastRun.GameDirector.Instance?.Flow?.ForceIdle(); CoastRun.ArcadeRun.StartKpop(CoastRun.GameManager.I, 1); }
        [MenuItem("Coast Run/Dev/194 - Heart heal test")] private static void D194Heart() { var h = CoastRun.HealthSystem.Instance; if (h == null) return; h.SetFraction(0.5f); float a = h.Normalized; h.HealHeart(); Debug.LogWarning($"[194] heart heal {a:F2} -> {h.Normalized:F2}"); }
        [MenuItem("Coast Run/Dev/194 - Restore prefs (coins 753, jelly 840, tutorial unseen)")] private static void D194Prefs() { PlayerPrefs.SetInt("CoastRun.Coins", 753); PlayerPrefs.SetInt("CoastRun.Jelly", 840); PlayerPrefs.DeleteKey("CoastRun_KpopTutorialDone"); PlayerPrefs.Save(); Debug.LogWarning("[194] prefs restored"); }
        [MenuItem("Coast Run/Dev/195 - Bus to city")] private static void D195City() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevTravel(1); }
        [MenuItem("Coast Run/Dev/195 - Bus to Jungmun")] private static void D195Tour() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevTravel(2); }
        [MenuItem("Coast Run/Dev/195 - Bus to village")] private static void D195Vil() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevTravel(0); }
        [MenuItem("Coast Run/Dev/195 - Next spot")] private static void D195Spot() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevNextSpot(); }
        [MenuItem("Coast Run/Dev/195 - Go tour shore")] private static void D195Shore() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevGo(330f, -21f, 180f); }
        [MenuItem("Coast Run/Dev/195 - Log tour heights")] private static void D195H() { var p = GameObject.Find("Player"); var ts = GameObject.Find("TourSea"); var tg = GameObject.Find("Zones/Tour/Terrain"); Debug.LogWarning($"[195] floor(-21)={CoastRun.Village.VillageZones.TourFloor(330f,-21f)} h={CoastRun.Village.VillageWorld.Height(330f,-21f)} sea={(ts!=null?ts.transform.position.ToString():"none")} seaActive={(ts!=null&&ts.activeInHierarchy)} ground={(tg!=null? tg.GetComponent<MeshFilter>().sharedMesh.bounds.ToString():"none")} cam={Camera.main.transform.position}"); }
        [MenuItem("Coast Run/Dev/195 - Sea probe")] private static void D195SeaP() { var ts = GameObject.Find("TourSea"); if (ts == null) return; var mr = ts.GetComponent<MeshRenderer>(); Debug.LogWarning($"[195] sea mat={mr.sharedMaterial.shader.name} q={mr.sharedMaterial.renderQueue} enabled={mr.enabled} layer={ts.layer} fwd={ts.transform.forward}"); ts.transform.position = new Vector3(330f, 0.6f, -90f); }
        [MenuItem("Coast Run/Dev/195 - Go counter")] private static void D195Ctr() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevGo(300f, 300.4f, 0f); }
        [MenuItem("Coast Run/Dev/195 - Look north (Hallasan)")] private static void D195N() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevGo(-2f, 10f, 0f); }
        [MenuItem("Coast Run/Dev/195 - Enter mine")] private static void D195Mine() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevMine(); }
        [MenuItem("Coast Run/Dev/195 - Mine one ore")] private static void D195Ore() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevMineOre(); }
        [MenuItem("Coast Run/Dev/195 - Open encyclopedia")] private static void D195Dex() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevDex(); }
        [MenuItem("Coast Run/Dev/195 - Give animals+hives+orchard")] private static void D195Ani() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevGiveAnimals(); }
        [MenuItem("Coast Run/Dev/195 - Enter records")] private static void D195Rec() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevEnterZone("records"); }
        [MenuItem("Coast Run/Dev/195 - Enter boutique")] private static void D195Bou() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevEnterZone("boutique"); }
        // 196차: 이야기 장소 · 송전탑 위 기상 도입
        [MenuItem("Coast Run/Dev/196 - Story intro (tower wake)")] private static void D196Intro() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevStoryIntro(); }
        [MenuItem("Coast Run/Dev/196 - Story go target")] private static void D196Go() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevStoryGoTarget(); }
        [MenuItem("Coast Run/Dev/196 - Story force CS2")] private static void D196F2() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevStoryForce("CS2"); }
        [MenuItem("Coast Run/Dev/196 - Story force EV3")] private static void D196F3() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevStoryForce("EV3"); }
        [MenuItem("Coast Run/Dev/196 - Story force CS8")] private static void D196F8() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevStoryForce("CS8"); }
        [MenuItem("Coast Run/Dev/196 - Activity jetski")] private static void D196A0() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevActivity(0); }
        [MenuItem("Coast Run/Dev/196 - Activity yacht")] private static void D196A1() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevActivity(1); }
        [MenuItem("Coast Run/Dev/196 - Activity surf")] private static void D196A2() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevActivity(2); }
        [MenuItem("Coast Run/Dev/196 - Activity kart")] private static void D196A3() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevActivity(3); }
        [MenuItem("Coast Run/Dev/196 - Activity horse")] private static void D196A4() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevActivity(4); }
        [MenuItem("Coast Run/Dev/196 - Go marina")] private static void D196Gmarina() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevGoSpot("marina"); }
        [MenuItem("Coast Run/Dev/196 - Go surf")] private static void D196Gsurf() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevGoSpot("surf"); }
        [MenuItem("Coast Run/Dev/196 - Go track")] private static void D196Gtrack() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevGoSpot("track"); }
        [MenuItem("Coast Run/Dev/196 - Go photo")] private static void D196Gphoto() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevGoSpot("photo"); }
        [MenuItem("Coast Run/Dev/196 - New game (village story)")] private static void D196New() { var gm = CoastRun.GameManager.I; if (gm != null) gm.NewGame(CoastRun.RunMode.Running); }
        [MenuItem("Coast Run/Dev/196 - Story force EV1")] private static void D196F1() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevStoryForce("EV1"); }
        [MenuItem("Coast Run/Dev/196 - Try sleep")] private static void D196Sl() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevTrySleep(); }
        [MenuItem("Coast Run/Dev/196 - Kid talk")] private static void D196Kt() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevKidTalk(); }
        [MenuItem("Coast Run/Dev/196 - Story guide (auto-move)")] private static void D196Gd() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevStoryGuide(); }
        [MenuItem("Coast Run/Dev/196 - Story force OPEN_F1")] private static void D196Fr1() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevStoryForce("OPEN_F1"); }
        [MenuItem("Coast Run/Dev/196 - Enter teddy museum")] private static void D196Td() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevEnterZone("teddy"); }
        [MenuItem("Coast Run/Dev/198 - Gacha 1")] private static void D198_0() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevGacha(1); }
        [MenuItem("Coast Run/Dev/198 - Gacha 10")] private static void D198_1() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevGacha(10); }
        [MenuItem("Coast Run/Dev/198 - Workshop (give mats)")] private static void D198_2() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevWorkshop(); }
        [MenuItem("Coast Run/Dev/198 - Tool log")] private static void D198_3() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevToolLog(); }
        [MenuItem("Coast Run/Dev/198 - Story man show")] private static void D198_4() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevStoryManShow(); }
        [MenuItem("Coast Run/Dev/198 - Enter gacha")] private static void D198_5() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevEnterZone("gacha"); }
        [MenuItem("Coast Run/Dev/198 - Enter workshop")] private static void D198_6() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevEnterZone("workshop"); }
        [MenuItem("Coast Run/Dev/198 - Fishing")] private static void D198_7() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevFish(); }
        [MenuItem("Coast Run/Dev/199 - Dungeon B1")] private static void D199_1() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevDungeon(1); }
        [MenuItem("Coast Run/Dev/199 - Dungeon B3")] private static void D199_2() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevDungeon(3); }
        [MenuItem("Coast Run/Dev/199 - Dungeon boss")] private static void D199_3() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevDungeon(5); }
        [MenuItem("Coast Run/Dev/199 - Dungeon swing")] private static void D199_8() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevDunSwing(); }
        [MenuItem("Coast Run/Dev/199 - Boss rush (3D)")] private static void D199_9() { if (!Application.isPlaying) return; CoastRun.GameDirector.Instance?.Flow?.ForceIdle(); CoastRun.ArcadeRun.StartBossRush(CoastRun.GameManager.I); }
        [MenuItem("Coast Run/Dev/199 - Bug log")] private static void D199_10() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevBugLog(); }
        [MenuItem("Coast Run/Dev/199 - Go meadow")] private static void D199_11() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.Teleport(new UnityEngine.Vector3(-24f, 0f, 18f), 0f); }
        [MenuItem("Coast Run/Dev/199 - Man log")] private static void D199_12() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevManLog(); }
        [MenuItem("Coast Run/Dev/199 - Man approach")] private static void D199_13() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevManApproach(); }
        [MenuItem("Coast Run/Dev/201 - KPOP chapter 10")] private static void D201_1() { if (!Application.isPlaying) return; CoastRun.GameDirector.Instance?.Flow?.ForceIdle(); CoastRun.ArcadeRun.StartKpop(CoastRun.GameManager.I, 10); }
        [MenuItem("Coast Run/Dev/201 - KPOP chapter 18")] private static void D201_2() { if (!Application.isPlaying) return; CoastRun.GameDirector.Instance?.Flow?.ForceIdle(); CoastRun.ArcadeRun.StartKpop(CoastRun.GameManager.I, 18); }
        [MenuItem("Coast Run/Dev/201 - Giant now")] private static void D201_3() { if (Application.isPlaying) CoastRun.GiantMode.Ensure().Activate(); }
        [MenuItem("Coast Run/Dev/199 - Dungeon log")] private static void D199_4() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevDungeonLog(); }
        [MenuItem("Coast Run/Dev/199 - Boss hurt 18")] private static void D199_5() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevBossHurt(); }
        [MenuItem("Coast Run/Dev/199 - Dungeon exit")] private static void D199_6() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevDungeon(0); }
        [MenuItem("Coast Run/Dev/199 - Auto-move menu")] private static void D199_7() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevAutoMoveMenu(); }
        [MenuItem("Coast Run/Dev/198 - Fishing auto on")] private static void D198_8() { CoastRun.Village.FishingMini.DevAuto = true; }
        [MenuItem("Coast Run/Dev/198 - Fishing auto cast")] private static void D198_9() { var f = UnityEngine.Object.FindObjectOfType<CoastRun.Village.FishingMini>(); if (f != null) f.DevCast(); }
        [MenuItem("Coast Run/Dev/196 - Story log")] private static void D196Log() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevStoryLog(); }
        [MenuItem("Coast Run/Dev/196 - Story skip tutorial")] private static void D196Skip() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevStorySkipTut(); }
        [MenuItem("Coast Run/Dev/196 - Story auto bubbles toggle")] private static void D196Auto() { CoastRun.Village.VillageHub.DevAutoBubble = !CoastRun.Village.VillageHub.DevAutoBubble; Debug.LogWarning("[Story] auto bubbles " + CoastRun.Village.VillageHub.DevAutoBubble); }
        [MenuItem("Coast Run/Dev/195 - Enter museum")] private static void D195Mus() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevEnterZone("museum"); }
        [MenuItem("Coast Run/Dev/195 - Enter noodle")] private static void D195Noo() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevEnterZone("noodle"); }
        [MenuItem("Coast Run/Dev/195 - Enter market")] private static void D195Mkt() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevEnterZone("market"); }
        [MenuItem("Coast Run/Dev/195 - Enter souvenir")] private static void D195Sou() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevEnterZone("souv"); }
        [MenuItem("Coast Run/Dev/194 - Go bank")] private static void D194GoBank() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevGoEast(0); }
        [MenuItem("Coast Run/Dev/194 - Go cafe")] private static void D194GoCafe() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevGoEast(1); }
        [MenuItem("Coast Run/Dev/194 - Go bus stop")] private static void D194GoBus() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevGoEast(2); }
        [MenuItem("Coast Run/Dev/194 - Enter bank")] private static void D194InBank() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevEnterBank(); }
        [MenuItem("Coast Run/Dev/194 - Enter cafe")] private static void D194InCafe() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevEnterCafe(); }
        [MenuItem("Coast Run/Dev/194 - Bank menu")] private static void D194BankMenu() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevBankMenu(); }
        [MenuItem("Coast Run/Dev/194 - Cafe menu")] private static void D194CafeMenu() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevCafeMenu(); }
        [MenuItem("Coast Run/Dev/190 - Auto move menu")] private static void D190Menu() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevAutoMenu(); }
        [MenuItem("Coast Run/Dev/190 - Go lighthouse")] private static void D190Light() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevGo(-9.5f, -56f, 180f); }
        [MenuItem("Coast Run/Dev/190 - Go peninsula")] private static void D190Pen() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevGo(-11f, -34f, 180f); }
        [MenuItem("Coast Run/Dev/189 - Hospital (faint)")] private static void D189Hosp() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevHospital(); }
        [MenuItem("Coast Run/Dev/189 - Enter hospital")] private static void D189HospIn() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevEnterHospital(); }
        [MenuItem("Coast Run/Dev/189 - Auto move hospital")] private static void D189GoHosp() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevAutoMove("hospital"); }
        [MenuItem("Coast Run/Dev/187 - Road tool")] private static void D187RoadTool() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevRoadTool(); }
        [MenuItem("Coast Run/Dev/187 - Place road")] private static void D187RoadPlace() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevPlaceRoad(); }
        [MenuItem("Coast Run/Dev/187 - Face sea")] private static void D187Sea() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevFaceSea(); }
        [MenuItem("Coast Run/Dev/183 - Heal")] private static void D183Heal() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevHeal(); }
        [MenuItem("Coast Run/Dev/183 - State")] private static void D183State() { var h = CoastRun.Village.VillageHub.I; if (h == null) return; Debug.LogWarning("[183] " + h.DevStateLine()); }
        [MenuItem("Coast Run/Dev/183 - Auto move shop")] private static void D183Shop() { var h = CoastRun.Village.VillageHub.I; if (h == null) return; h.DevAutoMove("shop"); }
        [MenuItem("Coast Run/Dev/183 - Auto move my room")] private static void D183Room() { var h = CoastRun.Village.VillageHub.I; if (h == null) return; h.DevAutoMove("hero"); }
        [MenuItem("Coast Run/Dev/183 - Auto move ranch")] private static void D183Ranch() { var h = CoastRun.Village.VillageHub.I; if (h == null) return; h.DevAutoMove("ranch"); }
        [MenuItem("Coast Run/Dev/183 - Auto move lot 3")] private static void D183Lot() { var h = CoastRun.Village.VillageHub.I; if (h == null) return; h.DevAutoMove("lot_3"); }
        [MenuItem("Coast Run/Dev/183 - Auto hunt on")] private static void D183HuntOn() { var h = CoastRun.Village.VillageHub.I; if (h == null) return; h.DevAutoHunt(true); }
        [MenuItem("Coast Run/Dev/183 - Auto hunt off")] private static void D183HuntOff() { var h = CoastRun.Village.VillageHub.I; if (h == null) return; h.DevAutoHunt(false); }
        [MenuItem("Coast Run/Dev/183 - Spirit + bat")] private static void D183Spirit() { var h = CoastRun.Village.VillageHub.I; if (h == null) return; h.DevSpawnSpiritBat(); }
        [MenuItem("Coast Run/Dev/183 - Bat nearest horse")] private static void D183Horse() { var h = CoastRun.Village.VillageHub.I; if (h == null) return; h.DevBatNearestHorse(); }
        [MenuItem("Coast Run/Dev/183 - Police Lv2")] private static void D183Police() { var h = CoastRun.Village.VillageHub.I; if (h == null) return; h.DevPolice(2); }
        [MenuItem("Coast Run/Dev/183 - Give sellables")] private static void D183Give() { var h = CoastRun.Village.VillageHub.I; if (h == null) return; h.DevGiveSellables(); }
        [MenuItem("Coast Run/Dev/183 - Sell all")] private static void D183Sell() { var h = CoastRun.Village.VillageHub.I; if (h == null) return; h.DevSellAll(); }
        [MenuItem("Coast Run/Dev/183 - Buy lot 0")] private static void D183Buy() { var h = CoastRun.Village.VillageHub.I; if (h == null) return; h.DevBuyLot(0); }
        [MenuItem("Coast Run/Dev/183 - Rent sim +8w")] private static void D183Rent() { var h = CoastRun.Village.VillageHub.I; if (h == null) return; h.DevRentSim(8); }
        [MenuItem("Coast Run/Dev/183 - Go lot 0")] private static void D183GoLot() { var h = CoastRun.Village.VillageHub.I; if (h == null) return; h.Teleport(CoastRun.Village.VillageLand.Front(0) + new Vector3(0f, 0f, 4f), 180f); }
        [MenuItem("Coast Run/Dev/Village - Pet test: restore")] private static void VPetRestore()
        {
            var gm = GameManager.I; if (gm == null || gm.Save == null || _petBakMask < 0) { Debug.LogWarning("[PetTest] nothing to restore"); return; }
            gm.Save.ownedPetMask = _petBakMask; gm.Save.equippedPet = _petBakEq; gm.Persist();
            Debug.LogWarning($"[PetTest] restored mask={_petBakMask} eq={_petBakEq}"); _petBakMask = -1;
        }
        [MenuItem("Coast Run/Dev/Village - Go boundary")] private static void VGoBound() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.Teleport(new Vector3(0f, 0f, 41.5f), 0f); }   // 173차: 경계 바위 담 확인용
        [MenuItem("Coast Run/Dev/Village - Time 18")] private static void VTime18() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevHour(18.3f); }
        [MenuItem("Coast Run/Dev/Village - Time 22")] private static void VTime22() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevHour(22.2f); }
        [MenuItem("Coast Run/Dev/Village - Time 8")] private static void VTime8() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevHour(8f); }
        [MenuItem("Coast Run/Dev/Village - Face cam")] private static void VFaceCam() { var h = CoastRun.Village.VillageHub.I; if (h == null) return; h.Teleport(new Vector3(0f, 0f, -10.8f), 0f); h.WalkTo(new Vector3(0f, 0f, -14.5f)); }
        // 156차: 우리집 안(식탁·침대) · 알바나라
        [MenuItem("Coast Run/Dev/Village - Enter home")] private static void VEnterHome() { var h = CoastRun.Village.VillageHub.I; var hh = CoastRun.Village.VillageWorld.HeroHouse; if (h == null || hh == null) return; h.EnterHouse(hh, "우리집", hh.TransformPoint(new Vector3(0f, 0f, 3.3f))); }
        [MenuItem("Coast Run/Dev/Village - Go home table")] private static void VGoTable() { var h = CoastRun.Village.VillageHub.I; if (h == null) return; var p = h.SpotPos("home_table"); h.Teleport(new Vector3(p.x, 0f, p.z), 0f); }
        [MenuItem("Coast Run/Dev/Village - Go home bed")] private static void VGoBed() { var h = CoastRun.Village.VillageHub.I; if (h == null) return; var p = h.SpotPos("home_bed"); h.Teleport(new Vector3(p.x, 0f, p.z), 270f); }
        [MenuItem("Coast Run/Dev/Village - Go job house")] private static void VGoJob() { var h = CoastRun.Village.VillageHub.I; if (h == null) return; var p = h.SpotPos("job"); h.Teleport(new Vector3(p.x, 0f, p.z + 3.2f), 0f); }
        [MenuItem("Coast Run/Dev/Village - Exit house")] private static void VExitHouse() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.ExitHouse(); }
        // 169차: 작물별 모델 확인용 — 9칸을 5종으로 채운다(앞 5칸 다 자람, 뒤 4칸 자라는 중) 뒤 텃밭으로.
        [MenuItem("Coast Run/Dev/Village - Farm demo (test)")]
        private static void VFarmDemo()
        {
            var h = CoastRun.Village.VillageHub.I; var gm = CoastRun.GameManager.I;
            if (h == null || gm == null || gm.Save == null) return;
            var s = gm.Save; CoastRun.Village.VillageFarm.Ensure(s); s.farmWeedMask = 0;
            string[] ids = { "tomato", "potato", "rice", "rose", "lavender" };
            for (int i = 0; i < CoastRun.Village.VillageFarm.Tiles; i++)
            {
                var sd = CoastRun.HomeData.Seed(ids[i % ids.Length]); if (sd == null) continue;
                s.farm[i].seed = sd.id;
                s.farm[i].growth = i < 5 ? sd.weeks : Mathf.Max(1, sd.weeks - 1);
                s.farm[i].waterStamp = -1;
            }
            h.SendMessage("AfterFarm", SendMessageOptions.DontRequireReceiver);
            var c = CoastRun.Village.VillageFarm.TileCenter(1); h.Teleport(new Vector3(c.x, 0f, c.z - 3.4f), 0f);
            Debug.LogWarning("[FarmDemo] 9칸 = 토마토·감자·벼·장미·라벤더 (앞 5칸 수확기, 뒤 4칸 성장 중)");
        }

        [MenuItem("Coast Run/Dev/Village - Sky plane now")] private static void VSkyPlane() { var s = CoastRun.Village.VillageSky.I; if (s != null) s.DevPlaneNow(); }   // 170차
        // 171차: 보이지 않는 벽 진단 — Bound 벽마다 양쪽이 모두 걸을 수 있는 땅(해수면+0.3 위)인 구간 비율을 로그
        [MenuItem("Coast Run/Dev/Village - Bound diag")]
        private static void VBoundDiag()
        {
            foreach (var bc in GameObject.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None))
            {
                if (bc.gameObject.name != "Bound" && bc.gameObject.name != "FenceCol" && bc.gameObject.name != "GardenFence") continue;
                var c = bc.transform.position + bc.center; var s = bc.size; bool alongX = s.x > s.z;
                int n = 0, both = 0; var spans = new System.Text.StringBuilder();
                for (float t = -0.5f; t <= 0.5f; t += 0.05f)
                {
                    float x = alongX ? c.x + t * s.x : c.x, z = alongX ? c.z : c.z + t * s.z;
                    float hA = CoastRun.Village.VillageWorld.Height(alongX ? x : x - 1.5f, alongX ? z - 1.5f : z), hB = CoastRun.Village.VillageWorld.Height(alongX ? x : x + 1.5f, alongX ? z + 1.5f : z);
                    n++; if (hA > CoastRun.Village.VillageWorld.SeaLevel + 0.3f && hB > CoastRun.Village.VillageWorld.SeaLevel + 0.3f) { both++; spans.Append($"({x:F0},{z:F0}) "); }
                }
                Debug.LogWarning($"[Bound] {bc.gameObject.name} c=({c.x:F1},{c.z:F1}) size=({s.x:F0},{s.z:F0}) 양쪽땅={both}/{n} {spans}");
            }
        }
        [MenuItem("Coast Run/Dev/Village - Go farm")] private static void VGoLivestock() { var h = CoastRun.Village.VillageHub.I; if (h == null) return; var g = CoastRun.Village.VillageLivestock.Gate; h.Teleport(new Vector3(g.x, 0f, g.z - 2.5f), 0f); }   // 171차
        [MenuItem("Coast Run/Dev/Village - Farm demo animals (test)")] private static void VFarmAnimals() { var h = CoastRun.Village.VillageHub.I; var gm = CoastRun.GameManager.I; if (h == null || gm == null || gm.Save == null) return; var s = gm.Save; s.farmChickens = 3; s.farmRabbitAge = new[] { 0, 2, 4 }; s.farmEggs = 2; gm.Persist(); CoastRun.Village.VillageLivestock.Build(GameObject.Find("VillageWorld").transform, s); CoastRun.Village.VillageHub.RefreshStatus(); }   // 171차
        [MenuItem("Coast Run/Dev/Village - Dev reset week (test)")] private static void VResetWeek() { var gm = CoastRun.GameManager.I; if (gm == null || gm.Save == null) return; gm.Save.boundaryPending = false; gm.Save.phaseIndex = 0; gm.Save.villageActMask = 0; gm.Persist(); CoastRun.Village.VillageHub.RefreshStatus(); }
        [MenuItem("Coast Run/Dev/Village - Go hero door")] private static void VGoHeroDoor() { var h = CoastRun.Village.VillageHub.I; var hh = CoastRun.Village.VillageWorld.HeroHouse; if (h == null || hh == null) return; var d = hh.TransformPoint(new Vector3(0f, 0f, 2.2f)) + hh.forward * 3.0f; h.Teleport(new Vector3(d.x, 0f, d.z), hh.eulerAngles.y + 180f); }
        [MenuItem("Coast Run/Dev/Village - Go shop door")] private static void VGoShopDoor() { var h = CoastRun.Village.VillageHub.I; var sh = CoastRun.Village.VillageWorld.Shop; if (h == null || sh == null) return; var d = sh.TransformPoint(new Vector3(0f, 0f, 2.6f)) + sh.forward * 2.2f; h.Teleport(new Vector3(d.x, 0f, d.z), sh.eulerAngles.y + 180f); }
        [MenuItem("Coast Run/Dev/Village - Warp home")] private static void VWarpHome() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevWarpHome(); }
        [MenuItem("Coast Run/Dev/Village - Stuck test")] private static void VStuck() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevStuck(); }
        [MenuItem("Coast Run/Dev/Village - Joy diag")] private static void VJoyDiag() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevJoyDiag(); }
        [MenuItem("Coast Run/Dev/Village - Joy demo L")] private static void VJoyL() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevJoy(0.28f, 0.30f, 3.5f); }
        [MenuItem("Coast Run/Dev/Village - Joy demo R")] private static void VJoyR() { var h = CoastRun.Village.VillageHub.I; if (h != null) h.DevJoy(0.74f, 0.42f, 3.5f); }
        [MenuItem("Coast Run/Dev/Village - Go job door")] private static void VGoJobDoor() { var h = CoastRun.Village.VillageHub.I; if (h == null) return; var p = h.SpotPos("job"); h.Teleport(new Vector3(p.x, 0f, p.z), 0f); }
        [MenuItem("Coast Run/Dev/Village - Go lane")] private static void VGoLane() { var h = CoastRun.Village.VillageHub.I; if (h == null) return; h.Teleport(new Vector3(-10.2f, 0f, -33.2f), 180f); }
        [MenuItem("Coast Run/Dev/Village - Cam info")] private static void VCamInfo() { var h = CoastRun.Village.VillageHub.I; var c = Camera.main; if (h == null || c == null) return; var p = c.transform.position; Debug.LogWarning($"[CamInfo] cam={p} player={h.PlayerPos} camYaw={c.transform.eulerAngles.y:F0} playerYaw={h.PlayerYaw:F0} {h.CamDiag}"); }
        [MenuItem("Coast Run/Dev/Village - Curve info")] private static void VCurveInfo() { var v = Shader.GetGlobalVector("_CoastCurveRadial"); var sea = GameObject.Find("Sea"); var mr = sea != null ? sea.GetComponent<Renderer>() : null; var m = mr != null ? mr.sharedMaterial : null; Debug.LogWarning($"[CurveInfo] radial={v} sea={(m != null ? m.shader.name : "none")} w={(m != null && m.HasProperty("_CurveWeight") ? m.GetFloat("_CurveWeight") : -1f)} curve={Shader.GetGlobalVector("_CoastCurve")}"); }
        [MenuItem("Coast Run/Dev/Village - Enter house 0")] private static void VEnterHouse() { var h = CoastRun.Village.VillageHub.I; if (h == null || CoastRun.Village.VillageWorld.Houses.Count == 0) return; var hs = CoastRun.Village.VillageWorld.Houses[2]; h.Teleport(hs.door, 0f); h.EnterHouse(hs.house, hs.name, hs.door); }
        [MenuItem("Coast Run/Dev/Village - Go house 0")] private static void VGoHouse() { var h = CoastRun.Village.VillageHub.I; if (h == null || CoastRun.Village.VillageWorld.Houses.Count == 0) return; var hs = CoastRun.Village.VillageWorld.Houses[2]; h.Teleport(hs.door + hs.house.forward * 3f, hs.house.eulerAngles.y + 180f); Debug.LogWarning("[GoHouse] " + hs.name + " door=" + hs.door + " fwd=" + hs.house.forward); }
        [MenuItem("Coast Run/Dev/Village - Go beach")] private static void VGoBeach() => VGo("beach");
        [MenuItem("Coast Run/Dev/Village - Go tree")] private static void VGoTree() { var h = CoastRun.Village.VillageHub.I; if (h == null || CoastRun.Village.VillageWorld.Trees.Count == 0) return; var t = CoastRun.Village.VillageWorld.Trees[0].position; h.Teleport(t + new Vector3(0f, 0f, -2.2f)); }
        [MenuItem("Coast Run/Dev/Village - Go kid")] private static void VGoKid() { var h = CoastRun.Village.VillageHub.I; if (h == null) return; h.Teleport(new Vector3(-4f, 0f, -21f)); }
        [MenuItem("Coast Run/Dev/Village - Walk south 6m")] private static void VWalkS() { var h = CoastRun.Village.VillageHub.I; if (h == null) return; h.WalkTo(h.transform.Find("VillagePlayer").position + new Vector3(0f, 0f, -6f)); }
        [MenuItem("Coast Run/Dev/Village - Walk east 6m")] private static void VWalkE() { var h = CoastRun.Village.VillageHub.I; if (h == null) return; h.WalkTo(h.transform.Find("VillagePlayer").position + new Vector3(6f, 0f, 0f)); }
        [MenuItem("Coast Run/Dev/Village - Walk north 6m")] private static void VWalkN() { var h = CoastRun.Village.VillageHub.I; if (h == null) return; h.WalkTo(h.transform.Find("VillagePlayer").position + new Vector3(0f, 0f, 6f)); }
        private static void VGo(string id) { var h = CoastRun.Village.VillageHub.I; if (h == null) return; var p = h.SpotPos(id); if (id == "garden") h.Teleport(new Vector3(0f, 0f, -10.8f), 180f); else if (id == "beach" || id == "shop" || id == "mom" || id == "light") h.Teleport(p + new Vector3(0f, 0f, 2.4f), 180f); else h.Teleport(p + new Vector3(0f, 0f, -2.6f), 0f); }
    }
}
