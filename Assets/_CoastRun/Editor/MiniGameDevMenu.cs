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
        [MenuItem("Coast Run/Dev/Life - Grocery")] public static void Grocery() { if (Application.isPlaying) GroceryUI.Open(GameManager.I); }
        [MenuItem("Coast Run/Dev/Life - Game over")] public static void GameOver() { if (Application.isPlaying && GameManager.Active) GameOverUI.Show(GameManager.I, CoastUiArt.AsSprite(ArtAssets.LoadTexture("Raise_Girl_Pose_Cry")), () => Debug.LogWarning("[Dev] revived")); }
        [MenuItem("Coast Run/Dev/Life - Starve (rice 0, cond 5)")] public static void Starve() { if (Application.isPlaying && GameManager.Active) { var s = GameManager.I.Save; s.rice = 0; s.sideDish = 0; s.condition = 5; s.hunger = 10; GameManager.I.Persist(); } }
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
        [MenuItem("Coast Run/Dev/Collection - Unlock all (F9)")] public static void UnlockAll() { if (Application.isPlaying) Collection.DebugUnlockAll(); }
    }
}
