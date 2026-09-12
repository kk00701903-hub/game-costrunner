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
        [MenuItem("Coast Run/Dev/Boss - Rush")] public static void BossRush() { if (Application.isPlaying) ArcadeRun.StartBossRush(GameManager.I); }
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
        [MenuItem("Coast Run/Dev/Collection - Unlock all (F9)")] public static void UnlockAll() { if (Application.isPlaying) Collection.DebugUnlockAll(); }
    }
}
