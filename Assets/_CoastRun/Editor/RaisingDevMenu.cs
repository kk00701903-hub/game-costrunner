#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CoastRun.Editor
{
    /// v2 육성 루프 개발 보조: 육성 씬 직행, 세이브 초기화, 해금, 밸런스 시뮬레이션.
    public static class RaisingDevMenu
    {
        private const string RaisingScene = "Assets/_CoastRun/Scenes/05_Raising.unity";

        [MenuItem("Coast Run/▶ PLAY 육성 (05_Raising) _F5")]
        public static void PlayRaising()
        {
            if (!File.Exists(RaisingScene))
            {
                SceneFlowSetupMenu.Setup();
                if (!File.Exists(RaisingScene))
                {
                    Debug.LogWarning("[Coast Run] 05_Raising missing — Setup Scene Flow failed.");
                    return;
                }
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            EditorSceneManager.OpenScene(RaisingScene, OpenSceneMode.Single);
            EditorApplication.delayCall += () => { EditorApplication.isPlaying = true; };
        }

        [MenuItem("Coast Run/Debug/v2 세이브 삭제 (save_0.json) %#&0")]
        public static void DeleteSave()
        {
            string p = Path.Combine(Application.persistentDataPath, SaveManager.SaveFile);
            if (File.Exists(p)) File.Delete(p);
            Debug.Log("[Coast Run] deleted " + p);
        }

        [MenuItem("Coast Run/Debug/v2 프로필 삭제 (해금 초기화) %#&9")]
        public static void DeleteProfile()
        {
            string p = Path.Combine(Application.persistentDataPath, SaveManager.ProfileFile);
            if (File.Exists(p)) File.Delete(p);
            // 레거시 '캠페인 클리어' 플래그도 함께 — 프로필 마이그레이션이 이걸 해금으로 읽는다.
            PlayerPrefs.DeleteKey(ProgressionManager.ClearedKey);
            PlayerPrefs.DeleteKey("CoastRun_CampaignCleared");
            PlayerPrefs.Save();
            Debug.Log("[Coast Run] deleted " + p + " + cleared flags");
        }

        [MenuItem("Coast Run/Debug/v2 스케이트보드 해금 %#&3")]
        public static void UnlockSkateboard()
        {
            var profile = new MetaProfile { endingsSeen = 1, skateboardUnlocked = true };
            File.WriteAllText(Path.Combine(Application.persistentDataPath, SaveManager.ProfileFile), JsonUtility.ToJson(profile, true));
            Debug.Log("[Coast Run] skateboard unlocked (profile.json)");
        }

        /// 세이브를 '19챕터까지 전부 S급, 20챕터 직전' 상태로 만들어 엔딩 분기를 바로 볼 수 있게.
        [MenuItem("Coast Run/Debug/v2 세이브: 19챕터 전부 S급 (엔딩 직전) %#&1")]
        public static void SaveAllSBeforeEnding() => WriteNearEnd(allS: true);

        [MenuItem("Coast Run/Debug/v2 세이브: 19챕터 중 3챕터 B급 (비극 직전) %#&2")]
        public static void SaveOneMissBeforeEnding() => WriteNearEnd(allS: false);

        private static void WriteNearEnd(bool allS)
        {
            var s = new SaveData { seed = 12345, playthrough = 1, prologueSeen = true };
            s.stats = new PlayerStats { stamina = 90, agility = 70, charm = 75, stress = 20, money = 3200, hearts = 700 };
            ChapterGrading.InitRecords(s);
            for (int c = 1; c < Timeline.Chapters; c++)
            {
                var r = s.chapters[c - 1];
                r.cleared = true;
                r.heartsEarned = allS || c != 3 ? r.heartsTarget : Mathf.RoundToInt(r.heartsTarget * 0.55f);
                r.grade = ChapterGrading.GradeOf(r.Ratio);
                r.snapshotAtStart = s.stats.Clone();
            }
            s.chapter = Timeline.Chapters;
            s.week = Timeline.WeekStart(s.chapter);
            s.chapters[s.chapter - 1].snapshotAtStart = s.stats.Clone();
            File.WriteAllText(Path.Combine(Application.persistentDataPath, SaveManager.SaveFile), JsonUtility.ToJson(s, true));
            Debug.Log("[Coast Run] wrote near-end save (allS=" + allS + ")");
        }

        [MenuItem("Coast Run/Debug/v2 엔딩 10배속 (토글) %#&e")]
        public static void ToggleFastEnding()
        {
            EndingController.DebugTimeMul = EndingController.DebugTimeMul > 1f ? 1f : 10f;
            Debug.Log("[Coast Run] ending time mul = " + EndingController.DebugTimeMul);
        }

        [MenuItem("Coast Run/Debug/v2 현재 챕터 하트 +40 (play) %#&h")]
        public static void AddHearts()
        {
            if (!Application.isPlaying || GameManager.I == null || GameManager.I.Save == null) return;
            GameManager.I.Save.chapterHearts += 40;
            GameManager.I.Persist();
            Debug.Log("[Coast Run] chapterHearts = " + GameManager.I.Save.chapterHearts);
        }

        /// 밸런스 확인: 단순 정책(알바·알바·휴식 / 자기계발·자기계발·휴식)으로 52주를 돌려 스탯·스트레스·실패율을 찍는다.
        [MenuItem("Coast Run/Debug/v2 52주 시뮬레이션 (콘솔)")]
        public static void Simulate()
        {
            string[][] policies =
            {
                new[] { "job_orange", "job_orange", "rest_home" },
                new[] { "dev_skate", "dev_dance", "rest_home" },
                new[] { "job_cafe", "dev_radio", "rest_sea" },
                new[] { "job_delivery", "job_delivery", "job_delivery" },   // 휴식 없음 → 번아웃 확인
            };
            var rng = new System.Random(7);
            foreach (var pol in policies)
            {
                var st = new PlayerStats();
                int fails = 0, greats = 0, total = 0, hearts = 0;
                for (int week = 1; week <= Timeline.Weeks; week++)
                {
                    var season = Timeline.SeasonOf(week);
                    foreach (var id in pol)
                    {
                        var d = ScheduleTable.Get(id);
                        if (d == null || !d.AvailableIn(season)) d = ScheduleTable.Get("rest_home");
                        var r = ScheduleJudge.Resolve(d, st, season, rng.NextDouble());
                        st = r.after;
                        total++;
                        if (r.outcome == Outcome.Fail) fails++;
                        if (r.outcome == Outcome.GreatSuccess) greats++;
                        hearts += r.heartsGained;
                    }
                    ScheduleJudge.WeeklyDecay(st);
                }
                Debug.Log($"[Sim] {string.Join("/", pol)} → 체력 {st.stamina} 순발 {st.agility} 매력 {st.charm} 스트레스 {st.stress} 돈 {st.money} | 실패 {fails}/{total} 대성공 {greats} 하트 {hearts}");
            }
        }
    }
}
#endif
