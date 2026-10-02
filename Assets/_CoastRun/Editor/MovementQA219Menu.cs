using System.IO;
using UnityEditor;
using UnityEngine;

namespace CoastRun.EditorTools
{
    /// 219차: 움직임 QA 메뉴 + 세이브 자동 복원(실행기는 Scripts/DevQA/MovementQA219.cs, UNITY_EDITOR 전용이라 빌드에 안 들어감).
    public static class MovementQA219Menu
    {
        [MenuItem("Coast Run/QA/219 - Movement QA (endless)")] static void RunEndless() => CoastRun.DevQA.MovementQA219.Begin(1);
        [MenuItem("Coast Run/QA/219 - Movement QA (endless, 20fps 80s)")] static void RunEndless20() => CoastRun.DevQA.MovementQA219.Begin(2);
        [MenuItem("Coast Run/QA/219 - Movement QA (story current chapter)")] static void RunStory() => CoastRun.DevQA.MovementQA219.Begin(0);
        [MenuItem("Coast Run/QA/219 - Obstacle QA story sweep (15 chapters)")] static void RunSweep() => CoastRun.DevQA.MovementQA219.Begin(3);
        [MenuItem("Coast Run/QA/219 - Story flow QA (places + cutscenes)")] static void RunFlow() => CoastRun.DevQA.StoryFlowQA219.Begin(0);
        [MenuItem("Coast Run/QA/219 - Story flow QA (resume after CS5)")] static void RunFlowR() => CoastRun.DevQA.StoryFlowQA219.Begin(0, 10);
        [MenuItem("Coast Run/QA/220 - Story flow QA (resume at CS8 → ending)")] static void RunFlow8() => CoastRun.DevQA.StoryFlowQA219.Begin(0, 17);
        [MenuItem("Coast Run/QA/219 - Story flow QA (chapter boundary run)")] static void RunFlowB() => CoastRun.DevQA.StoryFlowQA219.Begin(1);
        [MenuItem("Coast Run/QA/220 - Road QA (static)")] static void RoadS() => CoastRun.DevQA.RoadQA220.Begin(0);
        [MenuItem("Coast Run/QA/220 - Road map png")] static void RoadM() => CoastRun.DevQA.RoadQA220.Begin(2);
        [MenuItem("Coast Run/QA/220 - Road shots")] static void RoadSh() => CoastRun.DevQA.RoadQA220.Begin(3);
        [MenuItem("Coast Run/QA/220 - Visibility QA")] static void RoadV() => CoastRun.DevQA.RoadQA220.Begin(4);
        [MenuItem("Coast Run/QA/220 - Fade debug")] static void RoadF() => CoastRun.DevQA.RoadQA220.Begin(5);
        [MenuItem("Coast Run/QA/220 - Road QA (walk)")] static void RoadW() => CoastRun.DevQA.RoadQA220.Begin(1);
        // 236차: 한 달(접속 30번 = 게임 속 30주) — 마을은 PlaytestQA220, 런은 K-POP 봇(사람 흉내)이 조종
        [MenuItem("Coast Run/QA/236 - Month sim (30 days, new game)")] static void Month236() { CoastRun.DevQA.PlaytestQA220.Begin(30); CoastRun.DevQA.KpopPlaytestQA227.StartPilotLoop(); }
        [MenuItem("Coast Run/QA/236 - 10 weeks sim (skilled player, new game)")] static void Sim10() { CoastRun.DevQA.PlaytestQA220.Begin(10); CoastRun.DevQA.KpopPlaytestQA227.StartPilotLoop(true); }
        [MenuItem("Coast Run/QA/220 - Playtest 3 days (new game)")] static void Play3() => CoastRun.DevQA.PlaytestQA220.Begin(3);
        [MenuItem("Coast Run/QA/219 - Village walk collision QA")] static void RunWalk() => CoastRun.DevQA.VillageWalkQA219.Begin();
        [MenuItem("Coast Run/QA/219 - Movement QA story CH1")] static void S1() => CoastRun.DevQA.MovementQA219.Begin(0, 1);
        [MenuItem("Coast Run/QA/219 - Movement QA story CH5")] static void S5() => CoastRun.DevQA.MovementQA219.Begin(0, 5);
        [MenuItem("Coast Run/QA/219 - Movement QA story CH10")] static void S10() => CoastRun.DevQA.MovementQA219.Begin(0, 10);
        [MenuItem("Coast Run/QA/219 - Movement QA story CH15")] static void S15() => CoastRun.DevQA.MovementQA219.Begin(0, 15);
        [MenuItem("Coast Run/QA/219 - Movement QA story CH20")] static void S20() => CoastRun.DevQA.MovementQA219.Begin(0, 20);

        [MenuItem("Coast Run/QA/219 - Restore save now (check)")]
        static void RestoreNow()
        {
            string dir = EditorPrefs.GetString(CoastRun.DevQA.MovementQA219.RestoreKey, "");
            if (string.IsNullOrEmpty(dir)) { Directory.CreateDirectory("Builds/qa"); File.AppendAllText("Builds/qa/restore_log.txt", System.DateTime.Now + " check: 복원 대기 없음(이미 복원됨)\n"); Debug.LogWarning("[MoveQA] 복원 대기 없음(이미 복원됨) — 세이브 경로 " + Application.persistentDataPath + ", 백업 " + Path.Combine(Application.persistentDataPath, "qa219_bak") + " 존재=" + Directory.Exists(Path.Combine(Application.persistentDataPath, "qa219_bak"))); return; }
            DoRestore(dir);
        }

        static void DoRestore(string dir)
        {
            foreach (var f in new[] { "save_0.json", "profile.json" })
            {
                string src = Path.Combine(dir, f), dst = Path.Combine(Application.persistentDataPath, f);
                if (File.Exists(src)) File.Copy(src, dst, true); else if (File.Exists(dst) && File.Exists(Path.Combine(dir, f + ".absent"))) File.Delete(dst);
            }
            EditorPrefs.DeleteKey(CoastRun.DevQA.MovementQA219.RestoreKey); { int c = EditorPrefs.GetInt("MoveQA219_Coins", -1); if (c >= 0) { PlayerPrefs.SetInt(CoinWallet.PrefsKey, c); PlayerPrefs.Save(); } EditorPrefs.DeleteKey("MoveQA219_Coins"); }
            RestoreVn(); Debug.LogWarning("[MoveQA] 세이브 복원 완료 ← " + dir);
            Directory.CreateDirectory("Builds/qa"); File.AppendAllText("Builds/qa/restore_log.txt", System.DateTime.Now + " restored ← " + dir + "\n");
        }


        /// 219차: 스토리 흐름 QA 가 건드린 전역 「본 장면」 기록(PlayerPrefs CoastRun_VN_*)을 되돌린다 — StoryFlowQA219 가 시작 때 적어 둔 값
        static void RestoreVn()
        {
            string snap = EditorPrefs.GetString("MoveQA219_VN", "");
            if (string.IsNullOrEmpty(snap)) return;
            foreach (var kv in snap.Split(';'))
            {
                int i = kv.LastIndexOf('='); if (i <= 0) continue;
                string k = kv.Substring(0, i); int v; if (!int.TryParse(kv.Substring(i + 1), out v)) continue;
                if (v < 0) PlayerPrefs.DeleteKey(k); else PlayerPrefs.SetInt(k, v);
            }
            PlayerPrefs.Save(); EditorPrefs.DeleteKey("MoveQA219_VN");
            Debug.LogWarning("[MoveQA] 본 장면 기록(VN) 복원");
        }

        [MenuItem("Coast Run/QA/219 - Dump story seen state")]
        static void DumpSeen()
        {
            var sb = new System.Text.StringBuilder();
            string sp = Path.Combine(Application.persistentDataPath, "save_0.json");
            if (File.Exists(sp)) { var js = File.ReadAllText(sp); foreach (var key in new[] { "\"chapter\"", "\"week\"", "\"storySeenMask\"", "\"storyFragMask\"", "\"prologueSeen\"", "\"clueMask\"" }) { int i = js.IndexOf(key); if (i >= 0) { int e = js.IndexOfAny(new[] { ',', '}' }, i); sb.AppendLine(js.Substring(i, e - i)); } } }
            var ids = new System.Collections.Generic.List<string> { "PRO" };
            for (int c = 1; c <= 20; c++) { ids.Add($"CH{c:00}_Open"); ids.Add($"CH{c:00}_Close"); }
            foreach (var id in ids) sb.Append(id).Append('=').Append(PlayerPrefs.HasKey("CoastRun_VN_" + id) ? PlayerPrefs.GetInt("CoastRun_VN_" + id).ToString() : "-").Append("  ");
            Directory.CreateDirectory("Builds/qa"); File.WriteAllText("Builds/qa/seen_dump.txt", sb.ToString());
            Debug.LogWarning("[MoveQA] " + sb);
        }


        [MenuItem("Coast Run/QA/219 - Probe first cutscene load")]
        static void ProbeCutLoad()
        {
            if (!Application.isPlaying) { Debug.LogError("[Probe] 플레이 중에만"); return; }
            var f = typeof(CoastRun.ArtAssets).GetField("_cutByLegacyId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            bool built = f != null && f.GetValue(null) != null;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var tex = CoastRun.ArtAssets.LoadTexture("Cut_T_V7_N1_01");
            long ms1 = sw.ElapsedMilliseconds; sw.Restart();
            var tex2 = CoastRun.ArtAssets.LoadTexture("Cut_T_V7_N2_06");
            long ms2 = sw.ElapsedMilliseconds; sw.Restart();
            var clip = CoastRun.CoastBgmLibrary.Load("BGM_M1");
            long ms3 = sw.ElapsedMilliseconds;
            var dict = f != null ? f.GetValue(null) as System.Collections.IDictionary : null;
            long bytes = 0; int n = 0;
            if (dict != null) foreach (System.Collections.DictionaryEntry e in dict) { var t = e.Value as Texture; if (t != null) { bytes += UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(t); n++; } }
            string msg = $"색인 미리 있음={built} · 첫 컷 그림 {ms1} ms · 두 번째 {ms2} ms · 음악 BGM_M1 {ms3} ms · 색인에 올라간 그림 {n}장 · 메모리 {bytes / (1024f * 1024f):0} MB (tex={(tex != null)}, {(tex != null ? tex.format.ToString() : "-")})";
            Debug.LogWarning("[Probe] " + msg);
            Directory.CreateDirectory("Builds/qa"); File.AppendAllText("Builds/qa/cut_load_probe.txt", System.DateTime.Now + " " + msg + "\n");
        }

        [InitializeOnLoadMethod]
        static void HookRestore()
        {
            EditorApplication.playModeStateChanged += st =>
            {
                if (st != PlayModeStateChange.EnteredEditMode) return;
                string dir = EditorPrefs.GetString(CoastRun.DevQA.MovementQA219.RestoreKey, "");
                if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return;
                foreach (var f in new[] { "save_0.json", "profile.json" })
                {
                    string src = Path.Combine(dir, f), dst = Path.Combine(Application.persistentDataPath, f);
                    if (File.Exists(src)) File.Copy(src, dst, true); else if (File.Exists(dst) && File.Exists(Path.Combine(dir, f + ".absent"))) File.Delete(dst);
                }
                EditorPrefs.DeleteKey(CoastRun.DevQA.MovementQA219.RestoreKey); { int c = EditorPrefs.GetInt("MoveQA219_Coins", -1); if (c >= 0) { PlayerPrefs.SetInt(CoinWallet.PrefsKey, c); PlayerPrefs.Save(); } EditorPrefs.DeleteKey("MoveQA219_Coins"); }
                RestoreVn(); Debug.LogWarning("[MoveQA] 세이브 복원 완료 ← " + dir);
                Directory.CreateDirectory("Builds/qa"); File.AppendAllText("Builds/qa/restore_log.txt", System.DateTime.Now + " auto-restored ← " + dir + "\n");
            };
        }
    }
}
