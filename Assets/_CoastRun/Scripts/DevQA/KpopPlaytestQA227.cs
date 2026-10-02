#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CoastRun.DevQA
{
    /// 227차(사용자: 「kpop러닝 5번 게임성 테스트」): 보통 사람처럼 손으로 하는 봇으로 K-POP 한 곡 달리기 5판.
    ///   봇: 앞 장애물을 「부딪히기 0.35~1.0초 전」에 보고 피함(반응 늦음·실수 8%), 빈 레인으로 코인·말랑이·하트를 따라감,
    ///       피버 제안이 뜨면 대개 1초 안에 누름(15%는 놓침). 체력은 건드리지 않음(진짜로 죽을 수 있음).
    ///   기록: 판마다 챕터·곡·버틴 시간·완주·맞은 횟수·체력 흐름·장애물 밀도·빈 구간·속도·피버·보스·결과창 글·점수·코인.
    ///   결과 Builds/qa/kpop_play_227.txt, 스크린샷 Builds/qa/kpop227/. 세이브·프로필·코인은 플레이 종료 후 자동 복원(MovementQA219 경로).
    public class KpopPlaytestQA227 : MonoBehaviour
    {
        const BindingFlags BF = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;
        static readonly int[] Chapters = { 1, 1, 5, 10, 15 };   // 처음 해 보는 사람 2판 → 중반 → 후반

        [MenuItem("Coast Run/QA/227 - K-POP playtest (5 runs + first contest)")]
        public static void Begin() { if (!Application.isPlaying) { Debug.LogError("[Kpop227] 플레이 중에만"); return; } var go = new GameObject("KpopPlaytestQA227"); DontDestroyOnLoad(go); go.AddComponent<KpopPlaytestQA227>(); }

        [MenuItem("Coast Run/QA/227 - K-POP playtest (5 runs, round 2)")]
        public static void Begin2() { if (!Application.isPlaying) return; var go = new GameObject("KpopPlaytestQA227"); DontDestroyOnLoad(go); var q = go.AddComponent<KpopPlaytestQA227>(); q._round2 = true; }
        bool _round2, _skilled;
        [MenuItem("Coast Run/QA/227 - K-POP playtest (5 runs, skilled timing)")]
        public static void Begin3() { if (!Application.isPlaying) return; var go = new GameObject("KpopPlaytestQA227"); DontDestroyOnLoad(go); var q = go.AddComponent<KpopPlaytestQA227>(); q._round2 = true; q._skilled = true; }


        /// 228차 확인: 대회 안내 카드(조건 줄) · 보스 이름 배너를 4개 언어로 띄워 스크린샷 → Builds/qa/fit228/
        [MenuItem("Coast Run/QA/228 - Contest card + boss banner shots (4 langs)")]
        public static void Shots228() { if (!Application.isPlaying) return; var go = new GameObject("Shots228"); DontDestroyOnLoad(go); go.AddComponent<KpopPlaytestQA227>()._shots = true; }
        bool _shots;
        IEnumerator ShotsCo()
        {
            string dir = Path.Combine(Directory.GetCurrentDirectory(), "Builds/qa/fit228"); Directory.CreateDirectory(dir);
            var log = new StringBuilder(); string lang0 = Loc.Lang;
            foreach (var lg in new[] { "ko", "en", "ja", "es" })
            {
                Loc.SetLang(lg); yield return null;
                ContestIntroUI.Show(StoryContest.Get(1), null);
                yield return new WaitForSecondsRealtime(1.2f);
                foreach (var t in FindObjectsByType<Text>(FindObjectsSortMode.None))
                    if (t != null && t.name == "Row" && t.isActiveAndEnabled) { var g = t.cachedTextGenerator; log.AppendLine($"{lg} | {t.text} | size {t.cachedTextGenerator.fontSizeUsedForBestFit}/{t.fontSize} | lines {g.lineCount} | 넘침 {(g.characterCountVisible < t.text.Length ? "예" : "아니오")}"); }
                ScreenCapture.CaptureScreenshot(Path.Combine(dir, $"contest_{lg}.png")); yield return new WaitForSecondsRealtime(0.5f);
                ContestIntroUI.Close(); yield return new WaitForSecondsRealtime(0.3f);
                foreach (var nm in new[] { Loc.T("태풍 도깨비", "Typhoon Dokkaebi"), Loc.T("돌하르방 골렘", "Hareubang Golem"), Loc.T("갈매기 해적", "Seagull Pirate") })
                {
                    PickupFloat.Banner(nm, new Color(1f, 0.35f, 0.35f), 1.6f);
                    yield return new WaitForSecondsRealtime(0.6f);
                    var bt = GameObject.Find("Banner")?.GetComponent<Text>();
                    if (bt != null) { var g = bt.cachedTextGenerator; log.AppendLine($"{lg} 배너 | {bt.text} | size {g.fontSizeUsedForBestFit} | lines {g.lineCount} | 보이는 글자 {g.characterCountVisible}/{bt.text.Length}"); }
                    ScreenCapture.CaptureScreenshot(Path.Combine(dir, $"banner_{lg}_{nm.Length}.png")); yield return new WaitForSecondsRealtime(1.2f);
                }
            }
            Loc.SetLang(lang0);
            File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "Builds/qa/fit228.txt"), log.ToString(), new UTF8Encoding(false));
            Debug.LogWarning("[Shots228] done");
            Destroy(gameObject);
        }

        /// 229차: 마을 주인공 모습 점검 — 화면 스샷 + 주인공 재질·셰이더·포스트(볼륨) 목록 → Builds/qa/hero229/
        [MenuItem("Coast Run/QA/229 - Hero look probe")]
        public static void Hero229() { if (!Application.isPlaying) return; var go = new GameObject("Hero229"); DontDestroyOnLoad(go); go.AddComponent<KpopPlaytestQA227>()._hero = true; }
        bool _hero;
        IEnumerator HeroCo()
        {
            string dir = Path.Combine(Directory.GetCurrentDirectory(), "Builds/qa/hero229_" + DateTime.Now.ToString("HHmmss")); Directory.CreateDirectory(dir);
            var log = new StringBuilder();
            var vp = GameObject.Find("VillagePlayer");
            log.AppendLine($"화면 {Screen.width}x{Screen.height} · 플레이어 {(vp != null ? vp.transform.position.ToString() : "없음")}");
            var cam = Camera.main;
            if (cam != null && vp != null)
            {
                var head = vp.transform.position + Vector3.up * 1.2f; var feet = vp.transform.position;
                var a = cam.WorldToScreenPoint(head); var b = cam.WorldToScreenPoint(feet);
                log.AppendLine($"주인공 화면 높이 {Mathf.Abs(a.y - b.y):0}px ({Mathf.Abs(a.y - b.y) / Screen.height * 100f:0.0}% ) · 화면 위치 x{b.x / Screen.width:0.00} y{b.y / Screen.height:0.00} · 카메라 거리 {Vector3.Distance(cam.transform.position, head):0.0} m · FOV {cam.fieldOfView}");
            }
            if (vp != null)
                foreach (var r in vp.GetComponentsInChildren<Renderer>(true))
                {
                    var sb = new StringBuilder();
                    foreach (var m in r.sharedMaterials) { if (m == null) continue; sb.Append(m.name).Append('[').Append(m.shader != null ? m.shader.name : "-").Append(']');
                        if (m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") != null) { var t = m.GetTexture("_BaseMap"); sb.Append($" tex {t.name} {t.width}x{t.height} filter {t.filterMode} aniso {t.anisoLevel} mip {(t is Texture2D t2 ? t2.mipmapCount : 0)}"); }
                        else if (m.HasProperty("_MainTex") && m.GetTexture("_MainTex") != null) { var t = m.GetTexture("_MainTex"); sb.Append($" tex {t.name} {t.width}x{t.height} filter {t.filterMode}"); }
                        sb.Append(" | "); }
                    log.AppendLine($"  {r.GetType().Name} {r.name} on={r.enabled && r.gameObject.activeInHierarchy} : {sb}");
                }
            foreach (var v in FindObjectsByType<UnityEngine.Rendering.Volume>(FindObjectsSortMode.None))
            {
                if (v.sharedProfile == null) continue; log.AppendLine($"볼륨 {v.name} global={v.isGlobal} w={v.weight} 우선 {v.priority}");
                foreach (var c in v.sharedProfile.components)
                {
                    if (c == null || !c.active) continue; var sb = new StringBuilder();
                    foreach (var p in c.parameters) if (p.overrideState) { var val = p.GetType().GetProperty("value")?.GetValue(p); sb.Append($"{p.GetType().Name.Replace("Parameter", "")}={val} "); }
                    log.AppendLine($"  {c.GetType().Name}: {sb}");
                }
            }
            var urp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
            if (urp != null) log.AppendLine($"URP renderScale {urp.renderScale} msaa {urp.msaaSampleCount} upscale {urp.upscalingFilter} · Quality {QualitySettings.names[QualitySettings.GetQualityLevel()]}");
            if (cam != null) { var cd = cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(); if (cd != null) log.AppendLine($"카메라 AA {cd.antialiasing} post {cd.renderPostProcessing} · 렌더 텍스처 {cam.targetTexture}"); }
            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)) { var n = mb.GetType().Name; if (n.Contains("Paper") || n.Contains("Water") && n.Contains("color") || n.Contains("Grain") || n.Contains("Blur") || n.Contains("Filter") || n.Contains("Look")) log.AppendLine($"  효과? {n} on {mb.name} en={mb.enabled}"); }
            File.WriteAllText(Path.Combine(dir, "hero229.txt"), log.ToString(), new UTF8Encoding(false));
            string stamp = DateTime.Now.ToString("HHmmss");
            var hc = vp != null ? vp.GetComponentInChildren<HeroCrisp>() : null;
            if (hc != null)
            {
                // 같은 자리·같은 자세로 전/후 비교: 끈 상태(마을 소프트 룩 그대로) → 켠 상태
                Time.timeScale = 0f;
                float c0 = hc.Crisp, r0 = hc.Rim, w0 = hc.OutlineWidth, t0 = hc.ShadowThreshold, s0 = hc.ShadowSoftness;
                hc.Crisp = 0f; hc.Rim = 0f; hc.ShadowThreshold = 0.45f; hc.ShadowSoftness = 0.08f; hc.OutlineWidth = 0.010f;
                foreach (var m in vp.GetComponentsInChildren<Renderer>(true)) foreach (var mm in m.sharedMaterials) if (mm != null && mm.HasProperty("_Width")) mm.SetFloat("_Width", 0.010f);
                hc.Apply(); yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(dir, $"before_{stamp}.png")); yield return new WaitForSecondsRealtime(0.6f);
                hc.Crisp = c0; hc.Rim = r0; hc.OutlineWidth = w0; hc.ShadowThreshold = t0; hc.ShadowSoftness = s0; hc.Apply(); yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(dir, $"after_{stamp}.png")); yield return new WaitForSecondsRealtime(0.6f);
                Time.timeScale = 1f;
                log.AppendLine($"전후 비교 저장 before_{stamp}.png / after_{stamp}.png");
            }
            else { log.AppendLine("HeroCrisp 없음"); ScreenCapture.CaptureScreenshot(Path.Combine(dir, $"now_{stamp}.png")); yield return new WaitForSecondsRealtime(1f); }
            File.WriteAllText(Path.Combine(dir, "hero229.txt"), log.ToString(), new UTF8Encoding(false));
            Debug.LogWarning("[Hero229] done");
            Destroy(gameObject);
        }

        /// 229차-2: 주인공 방향별(뒤·옆·3/4 앞·앞) 스크린샷 — 얼굴·실루엣 읽힘 점검
        [MenuItem("Coast Run/QA/229 - Hero turnaround shots")]
        public static void Turn229() { if (!Application.isPlaying) return; var go = new GameObject("Turn229"); DontDestroyOnLoad(go); go.AddComponent<KpopPlaytestQA227>()._turn = true; }
        bool _turn;
        IEnumerator TurnCo()
        {
            string dir = Path.Combine(Directory.GetCurrentDirectory(), "Builds/qa/turn229_" + DateTime.Now.ToString("HHmmss")); Directory.CreateDirectory(dir);
            var vp = GameObject.Find("VillagePlayer"); var cam = Camera.main;
            if (vp == null || cam == null) yield break;
            var rig = vp.transform;
            var q0 = rig.localRotation;
            float camYaw = cam.transform.eulerAngles.y;
            foreach (var a in new[] { 0f, 90f, 135f, 180f })
            {
                rig.rotation = Quaternion.Euler(0f, camYaw + a, 0f);
                yield return null; yield return new WaitForEndOfFrame();
                rig.rotation = Quaternion.Euler(0f, camYaw + a, 0f);
                ScreenCapture.CaptureScreenshot(Path.Combine(dir, $"yaw{a:000}.png"));
                yield return new WaitForSecondsRealtime(0.5f);
            }
            rig.localRotation = q0;
            // 230차: 카메라 반대쪽으로 걸어가는 뒷모습(하트 핀 확인) — 마을 이동 코드의 _walkTo 를 써서 실제로 걷게 한다
            var hub = FindAnyObjectByType<CoastRun.Village.VillageHub>();
            var fw = hub != null ? hub.GetType().GetField("_walkTo", BF) : null;
            if (fw != null)
            {
                var f = cam.transform.forward; f.y = 0f; f.Normalize(); var home = vp.transform.position;
                fw.SetValue(hub, (Vector3?)(vp.transform.position + f * 8f));
                yield return new WaitForSecondsRealtime(1.1f);
                ScreenCapture.CaptureScreenshot(Path.Combine(dir, "walk_away.png"));
                { var pin = GameObject.Find("HeartPin"); var sbp = new StringBuilder();
                  if (pin == null) sbp.Append("HeartPin 없음");
                  else foreach (var r in pin.GetComponentsInChildren<Renderer>(true)) { var sp = cam.WorldToScreenPoint(r.bounds.center); sbp.AppendLine($"{r.name} on={r.enabled && r.gameObject.activeInHierarchy} vis={r.isVisible} center={r.bounds.center} size={r.bounds.size} screen=({sp.x / Screen.width:0.00},{sp.y / Screen.height:0.00}) mat={(r.sharedMaterial != null ? r.sharedMaterial.shader.name : "-")}"); }
                  var head = vp.GetComponentInChildren<Animator>()?.GetBoneTransform(HumanBodyBones.Head); sbp.AppendLine($"head={(head != null ? head.position.ToString() : "-")} player={vp.transform.position} fwd={vp.transform.forward}");
                  File.WriteAllText(Path.Combine(dir, "pin.txt"), sbp.ToString()); }
                yield return new WaitForSecondsRealtime(0.6f);
                fw.SetValue(hub, (Vector3?)home);   // 제자리로 돌아오기(세이브 위치 그대로)
                yield return new WaitForSecondsRealtime(2.2f);
                fw.SetValue(hub, null);
                yield return new WaitForSecondsRealtime(2.5f);
                ScreenCapture.CaptureScreenshot(Path.Combine(dir, "idle_after.png"));
                yield return new WaitForSecondsRealtime(0.6f);
            }
            // 231차: 얼굴 가까이(임시 카메라) — 정면·3/4·뒤 세 장
            {
                var anim = vp.GetComponentInChildren<Animator>(); var head = anim != null ? anim.GetBoneTransform(HumanBodyBones.Head) : null;
                if (head != null)
                {
                    var go = new GameObject("PortraitCam"); var pc = go.AddComponent<Camera>(); pc.fieldOfView = 30f; pc.nearClipPlane = 0.05f; pc.clearFlags = CameraClearFlags.SolidColor; pc.backgroundColor = new Color(0.8f, 0.86f, 0.9f);
                    var rt = new RenderTexture(600, 800, 24); pc.targetTexture = rt; pc.enabled = false;
                    var pf = vp.transform.forward; var c = head.position + Vector3.up * 0.05f;
                    int idx = 0;
                    foreach (var ang in new[] { 0f, 40f, 180f })
                    {
                        var dir2 = Quaternion.Euler(0f, ang, 0f) * pf;
                        pc.transform.position = c + dir2 * 2.4f + Vector3.up * 0.25f; pc.transform.LookAt(c - Vector3.up * 0.35f);
                        pc.Render(); RenderTexture.active = rt; var tx = new Texture2D(600, 800, TextureFormat.RGB24, false); tx.ReadPixels(new Rect(0, 0, 600, 800), 0, 0); tx.Apply(); RenderTexture.active = null;
                        File.WriteAllBytes(Path.Combine(dir, $"portrait_{idx++}.png"), tx.EncodeToPNG()); Destroy(tx);
                    }
                    pc.targetTexture = null; Destroy(rt); Destroy(go);
                }
            }
            Debug.LogWarning("[Turn229] done " + dir);
            Destroy(gameObject);
        }
        readonly StringBuilder _r = new StringBuilder();
        string _file, _shotDir; float _t0;
        static object F(object o, string n) { if (o == null) return null; var f = o.GetType().GetField(n, BF); return f != null ? f.GetValue(o) : null; }
        void Log(string s) { string l = $"[{Time.realtimeSinceStartup - _t0,6:0}s] {s}"; _r.AppendLine(l); Debug.LogWarning("[Kpop227] " + s); try { File.WriteAllText(_file, _r.ToString(), new UTF8Encoding(false)); } catch { } }
        void Shot(string name) { try { ScreenCapture.CaptureScreenshot(Path.Combine(_shotDir, name + ".png")); } catch { } }

        PlayerController _pc; MobileSwipeInput _in;
        float LaneOffset { get { var c = F(_pc, "config"); var f = c != null ? c.GetType().GetField("laneOffset", BF) : null; return f != null ? (float)f.GetValue(c) : 1.6f; } }

        /// 236차: 한 달 플레이 시뮬용 — 런(스토리·대회·K-POP)이 시작될 때마다 같은 사람 흉내 봇으로 조종하고 기록(Builds/qa/month236_runs.txt).
        ///   마을 쪽은 PlaytestQA220 이 맡는다. 세이브 백업·복원도 그쪽에서.
        public static void StartPilotLoop(bool skilled = false) { var go = new GameObject("PilotLoop236"); DontDestroyOnLoad(go); var q = go.AddComponent<KpopPlaytestQA227>(); q._pilotLoop = true; q._round2 = true; q._skilled = skilled; }
        bool _pilotLoop;
        IEnumerator PilotLoopCo()
        {
            _t0 = Time.realtimeSinceStartup;
            Directory.CreateDirectory("Builds/qa");
            _file = Path.Combine(Directory.GetCurrentDirectory(), _skilled ? "Builds/qa/sim236_10w_runs.txt" : "Builds/qa/month236_runs.txt");
            _shotDir = Path.Combine(Directory.GetCurrentDirectory(), "Builds/qa/month236"); Directory.CreateDirectory(_shotDir);
            Log($"=== 한 달 시뮬 — 런 조종 · {DateTime.Now:yyyy-MM-dd HH:mm} ===");
            var summary = new List<string>(); int n = 0;
            while (true)
            {
                while (true)
                {
                    var pc = FindAnyObjectByType<PlayerController>(); var ses = FindAnyObjectByType<GameSession>();
                    if (pc != null && pc.isActiveAndEnabled && pc.Speed > 0.5f && ses != null && ses.IsRunning) break;
                    yield return new WaitForSecondsRealtime(0.5f);
                }
                n++;
                var sv = GameManager.I != null ? GameManager.I.Save : null;
                string kind = StoryContest.Active ? "대회" : ArcadeRun.KpopMode ? "K-POP" : ArcadeRun.Active ? "아케이드" : "스토리";
                string lab = $"{n}번째 {kind} · {(sv != null ? $"{sv.week}주차 CH{sv.chapter} Lv{sv.level} 체력{sv.stats.stamina} 순발{sv.stats.agility}" : "-")}";
                Log($"\n##### {lab}");
                _storyPlain = !ArcadeRun.KpopMode && !StoryContest.Active;
                yield return Pilot(lab, summary, ArcadeRun.KpopMode);
                try { File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), _skilled ? "Builds/qa/sim236_10w_runs_summary.txt" : "Builds/qa/month236_runs_summary.txt"), string.Join("\n", summary), new UTF8Encoding(false)); } catch { }
                float w = 0f; while (w < 60f) { var ses = FindAnyObjectByType<GameSession>(); if (ses == null || !ses.IsRunning) break; yield return new WaitForSecondsRealtime(0.5f); w += 0.5f; }
                yield return new WaitForSecondsRealtime(2f);
            }
        }
        bool _storyPlain;

        IEnumerator Start()
        {
            if (_pilotLoop) { yield return PilotLoopCo(); yield break; }
            if (_shots) { yield return ShotsCo(); yield break; }
            if (_hero) { yield return HeroCo(); yield break; }
            if (_turn) { yield return TurnCo(); yield break; }
            _t0 = Time.realtimeSinceStartup;
            Directory.CreateDirectory("Builds/qa");
            _file = Path.Combine(Directory.GetCurrentDirectory(), _skilled ? "Builds/qa/kpop_play_227_skilled.txt" : _round2 ? "Builds/qa/kpop_play_227_r2.txt" : "Builds/qa/kpop_play_227.txt");
            _shotDir = Path.Combine(Directory.GetCurrentDirectory(), "Builds/qa/kpop227"); Directory.CreateDirectory(_shotDir);
            string bk = Path.Combine(Application.persistentDataPath, "qa219_bak"); Directory.CreateDirectory(bk);
            foreach (var f in new[] { "save_0.json", "profile.json" }) { string p = Path.Combine(Application.persistentDataPath, f); if (File.Exists(p)) File.Copy(p, Path.Combine(bk, f), true); else File.WriteAllText(Path.Combine(bk, f + ".absent"), ""); }
            EditorPrefs.SetString(MovementQA219.RestoreKey, bk); EditorPrefs.SetInt("MoveQA219_Coins", PlayerPrefs.GetInt(CoinWallet.PrefsKey, -1));
            Log($"=== K-POP 게임성 테스트 5판 · {DateTime.Now:yyyy-MM-dd HH:mm} ===");
            var gm = GameManager.I; float w = 0f; while (gm == null && w < 30f) { yield return null; w += Time.unscaledDeltaTime; gm = GameManager.I; }
            if (gm == null) { Log("!! GameManager 없음"); yield break; }
            var sv = gm.PeekSave();
            Log(sv != null ? $"세이브: {sv.week}주차 CH{sv.chapter} · Lv{sv.level} · 체력 {sv.stats.stamina} · 모드 {sv.runMode}" : "세이브 없음(기본 스탯)");
            var summary = new List<string>();
            for (int i = 0; i < Chapters.Length; i++)
            {
                yield return OneRun(gm, i + 1, Chapters[i], summary);
                ArcadeRun.Exit();
                float tw = 0f; yield return new WaitForSecondsRealtime(3f);
                while (tw < 30f && (FindAnyObjectByType<PlayerController>() != null || GameManager.I == null)) { yield return null; tw += Time.unscaledDeltaTime; }
                yield return new WaitForSecondsRealtime(3f);
                gm = GameManager.I;
            }
            if (!_round2) yield return ContestRuns(summary);
            Log("\n=== 요약 ===");
            foreach (var s in summary) Log(s);
            Log("끝 — 플레이 종료 후 세이브·코인 자동 복원");
            yield return new WaitForSecondsRealtime(1f);
            EditorApplication.isPlaying = false;
        }

        class Ob { public Component key; public Collider c; public bool duck, tall, decided, passed; public string name; public int plan; public float trig = -1f; public int dir; }

        IEnumerator OneRun(GameManager gm, int n, int chapter, List<string> summary)
        {
            Log($"\n##### {n}판 — 챕터 {chapter}");
            KpopTutorial.SuppressOnce = true;
            ArcadeRun.StartKpop(gm, chapter);
            yield return Pilot($"{n}판 CH{chapter}", summary, true);
        }

        void SkipCutscenes()
        {
            foreach (var t in new[] { typeof(ChapterVN), typeof(CinematicPlayer) })
                foreach (var o in FindObjectsByType(t, FindObjectsSortMode.None)) { var f = t.GetField("_skip", BF); if (f != null) f.SetValue(o, true); }
        }

        /// 지금 시작되는 런을 사람처럼 플레이하고 기록(K-POP·스토리 대회 공용)
        IEnumerator Pilot(string label, List<string> summary, bool kpop)
        {
            int n = label.GetHashCode() & 0xffff;
            float t0 = Time.realtimeSinceStartup;
            _pc = null; _in = null;
            while (Time.realtimeSinceStartup - t0 < 60f)
            {
                SkipCutscenes();
                _pc = FindAnyObjectByType<PlayerController>(); _in = FindAnyObjectByType<MobileSwipeInput>();
                if (_pc != null && _in != null && _pc.Speed > 0.5f) break;
                yield return null;
            }
            if (_pc == null || _in == null) { Log("!! 런 시작 안 됨"); summary.Add($"{label}: 시작 실패"); yield break; }
            var tr = ArcadeRun.KpopTrack; if (!kpop) tr = new KpopTrackMeta(0, 0f, 0f, 0f, 0f, 0f, 0f);
            string shotKey = label.Replace(" ", "_");
            Log($"시작까지 {Time.realtimeSinceStartup - t0:0.0}s · 곡 길이 {tr.length:0}s · 속도 {_pc.Speed:0.0} m/s · 레인 폭 {LaneOffset:0.00} m");

            var hs = HealthSystem.Instance;
            int hits = 0; float dmg = 0f; float minHp = 1f; var hitNames = new List<string>();
            Action<float> onDmg = a => { hits++; dmg += a; };
            if (hs != null) hs.OnDamaged += onDmg;
            var rng = new System.Random(1000 + n);
            var obs = new Dictionary<Component, Ob>();
            int seen = 0, dodgedSide = 0, jumped = 0, ducked = 0, mistakes = 0, fevers = 0, feverMissed = 0, laneMovesForItems = 0;
            float maxSpeed = 0f, gapNow = 0f, maxGap = 0f, lastObsT = 0f; bool bossSeen = false; float bossAt = -1f;
            var perSeg = new int[40]; var hpLine = new StringBuilder(); var spdLine = new StringBuilder();
            float feverSeenAt = -1f; bool feverDecided = false, feverWillTap = false; float feverDelay = 0f;
            float elapsed = 0f, nextMark = 10f; int shotK = 0;
            float actBusyUntil = 0f;
            float hpPrev = hs != null ? hs.Normalized : 1f; int bigDrops = 0; string lastAct = "-"; float lastActAt = -9f; int heals = 0; float healSum = 0f;
            bool ended = false; int cProg = 0; bool cSucc = false; string cInfo = "";
            while (elapsed < 420f)
            {
                yield return null;
                float dt = Time.deltaTime; elapsed += Time.unscaledDeltaTime;
                if (_pc == null) { ended = true; break; }
                var session = FindAnyObjectByType<GameSession>();
                if (session != null && !session.IsRunning && elapsed > 3f) { ended = true; break; }
                if (hs == null) { hs = HealthSystem.Instance; if (hs != null) hs.OnDamaged += onDmg; }
                if (hs != null)
                {
                    float hpNow = hs.Normalized; float dlt = hpNow - hpPrev;
                    if (dlt < -0.04f)
                    {
                        bigDrops++;
                        string near = "?"; float bestD = 99f;
                        foreach (var o in obs.Values) { if (o.key == null || o.c == null) continue; var rr = _pc.transform.InverseTransformPoint(o.c.bounds.center); float dd = Mathf.Abs(rr.z) + Mathf.Abs(rr.x); if (dd < bestD) { bestD = dd; near = $"{o.name}(z{rr.z:0.0},x{rr.x:0.0},{(o.duck ? "숙임" : o.tall ? "높음" : "낮음")},{(o.decided ? "봄" : "못봄")})"; } }
                        Log($"    ↓ {elapsed:0.0}s 체력 {hpPrev * 100f:0}→{hpNow * 100f:0}% · 가까운 것 {near} · 마지막 행동 {lastAct}({elapsed - lastActAt:0.00}s 전) · 레인 {_pc.Lane} · 상태 {_pc.State} · 속도 {_pc.Speed:0.0}");
                    }
                    else if (dlt > 0.02f) { heals++; healSum += dlt; }
                    hpPrev = hpNow;
                    minHp = Mathf.Min(minHp, hpNow);
                }
                if (!kpop && StoryContest.Active && StoryContest.Current != null) { cProg = StoryContest.Progress(); cSucc = StoryContest.Succeeded; cInfo = $"목표 {StoryContest.Current.goal} {StoryContest.Current.target} · 제한 {StoryContest.Current.seconds:0}s"; }
                maxSpeed = Mathf.Max(maxSpeed, _pc.Speed);
                if (BossDirector.Active && !bossSeen) { bossSeen = true; bossAt = elapsed; Log($"  보스 등장 {elapsed:0}s"); Shot($"{shotKey}_boss"); }
                if (elapsed >= nextMark) { hpLine.Append($"{nextMark:0}s:{(hs != null ? hs.Normalized * 100f : -1):0} "); spdLine.Append($"{_pc.Speed:0.0} "); nextMark += 10f; }
                if (shotK < 3 && elapsed > (shotK == 0 ? 20f : shotK == 1 ? 80f : 140f)) { Shot($"{shotKey}_{shotK}_{elapsed:0}s"); shotK++; }

                // 피버 제안
                var fm = FeverMode.Instance;
                float offerUntil = fm != null ? (float)F(fm, "_offerUntil") : -1f;
                if (offerUntil > 0f && !FeverMode.Active)
                {
                    if (feverSeenAt < 0f) { feverSeenAt = elapsed; feverDecided = true; feverWillTap = rng.NextDouble() > 0.15; feverDelay = 0.5f + (float)rng.NextDouble() * 0.8f; }
                    if (feverWillTap && elapsed - feverSeenAt > feverDelay) { var m = fm.GetType().GetMethod("OnPressed", BF); m?.Invoke(fm, null); if (FeverMode.Active) { fevers++; feverSeenAt = -1f; } }
                }
                else if (feverSeenAt >= 0f && !FeverMode.Active) { if (feverDecided && !feverWillTap) feverMissed++; feverSeenAt = -1f; }

                // 장애물 보기
                var ptr = _pc.transform; float lo = LaneOffset; float speed = Mathf.Max(1f, _pc.Speed);
                foreach (var h in ObstacleHazard.Active) Reg(obs, h, false);
                foreach (var d in FindObjectsByType<DuckHazard>(FindObjectsSortMode.None)) Reg(obs, d, true);
                bool anyNear = false;
                Ob threat = null; float threatT = 99f;
                foreach (var kv in obs)
                {
                    var o = kv.Value; if (o.passed || o.key == null || o.c == null || !o.c.enabled) continue;
                    var rel = ptr.InverseTransformPoint(o.c.bounds.center);
                    if (rel.z < -1f) { o.passed = true; continue; }
                    if (rel.z < 40f) anyNear = true;
                    float ttc = rel.z / speed;
                    var e = o.c.bounds.extents; var rv = ptr.right; float halfW = Mathf.Abs(rv.x) * e.x + Mathf.Abs(rv.y) * e.y + Mathf.Abs(rv.z) * e.z + 0.3f;
                    if (Mathf.Abs(rel.x) < halfW && ttc > 0f && ttc < threatT) { threat = o; threatT = ttc; }
                }
                if (anyNear) { if (gapNow > maxGap) maxGap = gapNow; gapNow = 0f; } else gapNow += Time.unscaledDeltaTime;

                if (_skilled && threat != null && !threat.decided && threatT < 1.0f)
                {
                    // 익숙한 사람: 무엇을 할지 먼저 정하고, 그 동작에 맞는 타이밍에 누름(점프 0.22~0.38s · 숙임 0.08~0.25s · 옆 0.3~0.6s 전)
                    if (threat.plan == 0)
                    {
                        int dir = FreeSide(obs, ptr, lo, speed);
                        if (rng.NextDouble() < 0.05) { threat.plan = 9; threat.trig = 0f; }
                        else if (threat.duck) { threat.plan = 1; threat.trig = 0.08f + (float)rng.NextDouble() * 0.17f; }
                        else if (dir != 0 && (threat.tall || rng.NextDouble() < 0.5)) { threat.plan = 2; threat.dir = dir; threat.trig = 0.3f + (float)rng.NextDouble() * 0.3f; }
                        else { threat.plan = 3; threat.trig = 0.22f + (float)rng.NextDouble() * 0.16f; }
                    }
                    if (threatT <= threat.trig)
                    {
                        threat.decided = true; seen++;
                        int seg = Mathf.Clamp((int)(elapsed / 10f), 0, perSeg.Length - 1); perSeg[seg]++;
                        switch (threat.plan)
                        {
                            case 1: _in.Inject(0, false, true); ducked++; lastAct = "숙임"; break;
                            case 2: _in.Inject(threat.dir, false, false); dodgedSide++; lastAct = threat.dir < 0 ? "왼쪽" : "오른쪽"; break;
                            case 3: _in.Inject(0, true, false); jumped++; lastAct = "점프"; break;
                            default: mistakes++; lastAct = "실수(안 함)"; break;
                        }
                        lastActAt = elapsed; actBusyUntil = elapsed + 0.2f;
                    }
                }
                else if (!_skilled && threat != null && !threat.decided && threatT < 1.0f && threatT > 0.12f && elapsed > actBusyUntil)
                {
                    // 사람 반응: 0.35~1.0초 전 사이 아무 때나 — 한 번 정하면 그대로
                    float trigger = 0.35f + (float)rng.NextDouble() * 0.45f;
                    if (threatT <= trigger || threatT < 0.4f)
                    {
                        threat.decided = true; seen++;
                        int seg = Mathf.Clamp((int)(elapsed / 10f), 0, perSeg.Length - 1); perSeg[seg]++;
                        if (rng.NextDouble() < 0.08) { mistakes++; lastAct = "실수(안 함)"; lastActAt = elapsed; }
                        else if (threat.duck) { _in.Inject(0, false, true); ducked++; lastAct = "숙임"; lastActAt = elapsed; }
                        else
                        {
                            int dir = FreeSide(obs, ptr, lo, speed);
                            if (threat.tall || dir != 0 && rng.NextDouble() < 0.5)
                            {
                                if (dir != 0) { _in.Inject(dir, false, false); dodgedSide++; lastAct = dir < 0 ? "왼쪽" : "오른쪽"; lastActAt = elapsed; }
                                else { _in.Inject(0, true, false); jumped++; lastAct = "점프(막힘)"; lastActAt = elapsed; }
                            }
                            else { _in.Inject(0, true, false); jumped++; lastAct = "점프"; lastActAt = elapsed; }
                        }
                        actBusyUntil = elapsed + 0.3f;
                    }
                }
                else if ((threat == null || threatT > 1.2f) && elapsed > actBusyUntil + 0.4f)
                {
                    // 빈 시간: 앞 코인·말랑이·하트 쪽 레인으로
                    int want = ItemLane(ptr, lo);
                    if (want != _pc.Lane && rng.NextDouble() < (_round2 ? 0.12 : 0.04))
                    {
                        int dir = want > _pc.Lane ? 1 : -1;
                        if (LaneClear(obs, ptr, lo, speed, _pc.Lane + dir)) { _in.Inject(dir, false, false); laneMovesForItems++; actBusyUntil = elapsed + 0.3f; }
                    }
                }
            }
            if (hs != null) hs.OnDamaged -= onDmg;
            if (gapNow > maxGap) maxGap = gapNow;
            bool finished = ArcadeRun.KpopFinished;
            float hpEnd = hs != null ? hs.Normalized : -1f;
            if (!kpop && _storyPlain) { finished = hs != null && hs.Normalized > 0f; Log($"  스토리 런: 끝 체력 {(hs != null ? hs.Normalized * 100f : -1f):0}%"); }
            else if (!kpop) { finished = cSucc && hs != null && hs.Normalized > 0f; Log($"  대회: {cInfo} · 진행 {cProg} · 성공 {cSucc} · 시간초과 {StoryContest.TimedOut}"); }
            Log($"끝: {(finished ? "완주" : ended ? "쓰러짐/끝" : "시간 초과(봇)")} · 버틴 {elapsed:0}s / 곡 {tr.length:0}s · 거리 {ArcadeRun.Distance:0} m · 최고 속도 {maxSpeed:0.0} m/s");
            Log($"  만난 장애물 {seen} (옆으로 {dodgedSide} · 점프 {jumped} · 숙임 {ducked} · 일부러 실수 {mistakes}) · 맞음 {hits}회(피해 {dmg:0}) · 최저 체력 {minHp * 100f:0}% · 끝 체력 {hpEnd * 100f:0}%");
            Log($"  큰 체력 감소 {bigDrops}번 · 회복 {heals}번(합 {healSum * 100f:0}%)");
            Log($"  피버 {fevers}회(놓침 {feverMissed}) · 후렴 피버 {ArcadeRun.FeverInChorus} · 보스 {(bossSeen ? $"{bossAt:0}s 등장 · 깬 보스 {ArcadeRun.BossesCleared}" : "없음")} · 아이템 따라 레인 이동 {laneMovesForItems}");
            var dens = new StringBuilder(); for (int s = 0; s < Mathf.Min(perSeg.Length, Mathf.CeilToInt(elapsed / 10f)); s++) dens.Append(perSeg[s]).Append(' ');
            Log($"  10초마다 장애물 수: {dens}| 장애물 없는 최장 구간 {maxGap:0.0}s");
            Log($"  체력 흐름: {hpLine}");
            Log($"  속도 흐름(10초마다): {spdLine}");
            // 결과창
            yield return new WaitForSecondsRealtime(3.5f);
            Shot($"{shotKey}_result");
            var texts = new List<string>();
            foreach (var t in FindObjectsByType<Text>(FindObjectsSortMode.None))
            {
                if (t == null || !t.isActiveAndEnabled || string.IsNullOrWhiteSpace(t.text)) continue;
                var cv = t.canvas; if (cv == null || cv.rootCanvas.sortingOrder < 100) continue;
                string s = t.text.Replace("\n", " / ").Trim(); if (s.Length > 70) s = s.Substring(0, 70) + "…";
                if (!texts.Contains(s)) texts.Add(s);
            }
            Log($"  결과창: {string.Join(" | ", texts)}");
            Log($"  정산: 점수 {ArcadeRun.LastScore} · 코인 {ArcadeRun.LastMoney} · 말랑이 {ArcadeRun.LastJelly} · 최고기록 {ArcadeRun.LastNewBest} · 도장 {ArcadeRun.LastStamped} (+{ArcadeRun.LastStampCoins}) · 미션 올클 {ArcadeRun.LastAllClear} (+{ArcadeRun.LastAllClearCoins})");
            _lastTexts = texts;
            summary.Add($"{label}: {(finished ? "완주" : "쓰러짐")} {elapsed:0}/{tr.length:0}s · 맞음 {hits} · 최저 체력 {minHp * 100f:0}% · 장애물 {seen} · 최장 빈 구간 {maxGap:0}s · 피버 {fevers} · 보스 {(bossSeen ? "O" : "-")} · 점수 {ArcadeRun.LastScore} · 코인 {ArcadeRun.LastMoney}");
        }

        List<string> _lastTexts = new List<string>();

        /// 스토리 첫 대회(3주 끝, 「첫 해안도로 달리기 대회」) — 새 게임을 3일 플레이 끝 상태(HP 196·Lv2)로 맞추고 같은 봇으로 최대 3번
        IEnumerator ContestRuns(List<string> summary)
        {
            Log("\n##### 스토리 첫 대회 (3일 플레이 끝 상태)");
            { var sb = new StringBuilder(); var ids = new List<string> { "PRO" }; for (int c = 1; c <= 20; c++) { ids.Add($"CH{c:00}_Open"); ids.Add($"CH{c:00}_Close"); } foreach (var id in ids) { string k = "CoastRun_VN_" + id; sb.Append(k).Append('=').Append(PlayerPrefs.HasKey(k) ? PlayerPrefs.GetInt(k) : -1).Append(';'); } EditorPrefs.SetString("MoveQA219_VN", sb.ToString()); }
            var gm = GameManager.I; if (gm == null) { Log("!! GameManager 없음"); yield break; }
            gm.NewGame(RunMode.Running);
            float w = 0f; while (FindAnyObjectByType<CoastRun.Village.VillageHub>() == null && w < 90f) { yield return null; w += Time.unscaledDeltaTime; }
            yield return new WaitForSecondsRealtime(4f);
            gm = GameManager.I; var s = gm.Save;
            s.stats.stamina = 196; s.level = 2; s.week = 4; s.prologueSeen = true;
            Log($"상태: {s.week}주차 CH{s.chapter} · HP {s.stats.stamina} · Lv{s.level}");
            for (int a = 1; a <= 3; a++)
            {
                if (a == 1) gm.StartStoryRun();
                else
                {
                    Button retry = null;
                    foreach (var b in FindObjectsByType<Button>(FindObjectsSortMode.None)) { var t = b.GetComponentInChildren<Text>(); if (b.isActiveAndEnabled && t != null && t.text.Contains("다시")) { retry = b; break; } }
                    if (retry == null) { Log("  (다시 도전 버튼 없음 — 끝)"); break; }
                    retry.onClick.Invoke();
                }
                yield return Pilot($"대회 {a}번째", summary, false);
                bool cleared = false; foreach (var t in _lastTexts) if (t.Contains("우승") || t.Contains("통과") || t.Contains("성공") || t.Contains("클리어")) cleared = true;
                if (cleared) { Log("  대회 통과 — 그만"); break; }
            }
        }

        void Reg(Dictionary<Component, Ob> obs, Component key, bool duck)
        {
            if (key == null || obs.ContainsKey(key)) return;
            var c = key.GetComponent<Collider>(); if (c == null) return;
            bool tall = !duck && key is ObstacleHazard oh && (oh.GetComponentInParent<OncomingCar>() != null || oh.ClassifyHit(_pc) == HitKind.Bounce);
            obs[key] = new Ob { key = key, c = c, duck = duck, tall = tall, name = key.transform.root.name };
        }

        bool LaneClear(Dictionary<Component, Ob> obs, Transform ptr, float lo, float speed, int lane)
        {
            if (lane < -1 || lane > 1) return false;
            float x = (lane - _pc.Lane) * lo;
            foreach (var o in obs.Values)
            {
                if (o.passed || o.key == null || o.c == null) continue;
                var rel = ptr.InverseTransformPoint(o.c.bounds.center);
                if (rel.z < -1f || rel.z / speed > 1.6f) continue;
                if (Mathf.Abs(rel.x - x) < lo * 0.6f) return false;
            }
            return true;
        }

        int FreeSide(Dictionary<Component, Ob> obs, Transform ptr, float lo, float speed)
        {
            bool l = LaneClear(obs, ptr, lo, speed, _pc.Lane - 1), r = LaneClear(obs, ptr, lo, speed, _pc.Lane + 1);
            if (l && r) return UnityEngine.Random.value < 0.5f ? -1 : 1;
            return l ? -1 : r ? 1 : 0;
        }

        int ItemLane(Transform ptr, float lo)
        {
            float best = 30f; int lane = _pc.Lane;
            foreach (var c in FindObjectsByType<CoinPickup>(FindObjectsSortMode.None)) Consider(c.transform.position, ptr, lo, ref best, ref lane);
            foreach (var j in FindObjectsByType<JellyPickup>(FindObjectsSortMode.None)) Consider(j.transform.position, ptr, lo, ref best, ref lane);
            return lane;
        }
        void Consider(Vector3 p, Transform ptr, float lo, ref float best, ref int lane)
        {
            var rel = ptr.InverseTransformPoint(p); if (rel.z < 3f || rel.z > best) return;
            int l = Mathf.Clamp(Mathf.RoundToInt((rel.x + _pc.LateralOffset) / Mathf.Max(0.5f, lo)), -1, 1);
            best = rel.z; lane = l;
        }
    }
}
#endif
