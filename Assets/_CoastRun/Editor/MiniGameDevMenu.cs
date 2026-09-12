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
