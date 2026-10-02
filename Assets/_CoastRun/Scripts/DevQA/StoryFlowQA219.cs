#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using CoastRun.Village;

namespace CoastRun.DevQA
{
    /// 219차(사용자: 「스토리 모드와 컷씬 연결 자연스러운지 확인」): 마을(장소) 스토리 흐름 자동 점검 — 에디터 전용.
    ///  mode 0: 새 게임 도입(탑 위 기상) → CS1 → 꼬마 튜토리얼 → 장면 18개 + 기억 조각 3개를 **꼬마가 데려다주는 길(잠 막기 → 데려다줘)**로 차례로.
    ///          장면마다 화면 밝기를 계속 재서 검은 화면 길이·깜빡임, 컷씬 시작/끝, 뒤따르는 카드(선택·단서), 배경음, 토스트를 기록하고 스크린샷을 남긴다.
    ///  mode 1: 챕터 경계 — 잠 → 주말 결산 → 대회 → 러닝 → 정산 → 마을 아침 → 다음 이야기 장소가 빛나는지.
    /// 세이브·코인은 MovementQA219 과 같은 방식으로 백업, 플레이 종료 후 자동 복원. 결과: Builds/qa/story_flow_qa_219.txt, 스크린샷 Builds/qa/flow/
    public class StoryFlowQA219 : MonoBehaviour
    {
        public static void Begin(int mode, int from = 0)
        {
            if (!Application.isPlaying) { Debug.LogError("[FlowQA] 플레이 중에만"); return; }
            var go = new GameObject("StoryFlowQA219"); DontDestroyOnLoad(go);
            var q = go.AddComponent<StoryFlowQA219>(); q._mode = mode; q._from = from;
        }

        int _mode, _from;
        const BindingFlags BF = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;
        readonly StringBuilder _r = new StringBuilder();
        string _shotDir;
        string _file;
        void Log(string s) { _r.AppendLine(s); Debug.LogWarning("[FlowQA] " + s); if (_file != null) try { File.AppendAllText(_file, s + "\n", new UTF8Encoding(false)); } catch { } }

        VillageHub Hub => VillageHub.I;
        static object F(object o, string n) { if (o == null) return null; var f = o.GetType().GetField(n, BF); return f != null ? f.GetValue(o) : null; }
        static object M(object o, string n, params object[] a) { if (o == null) return null; var m = o.GetType().GetMethod(n, BF); return m != null ? m.Invoke(o, a) : null; }
        string Target(out string id, out Vector3 pos, out string label)
        {
            var args = new object[] { null, null, null };
            var r = M(Hub, "StoryTarget", args);
            var sc = args[0] as VillageStory.Scene; id = sc != null ? sc.id : "-"; pos = args[1] != null ? (Vector3)args[1] : Vector3.zero; label = args[2] as string;
            return r != null ? r.ToString() : "None";
        }
        bool StoryPlaying => Hub != null && (bool)F(Hub, "_storyPlaying");
        bool Busy => Hub != null && (bool)F(Hub, "_busy");
        VillageHud Hud => F(Hub, "_hud") as VillageHud;
        float VillageFade { get { var img = F(Hub, "_fadeImg") as Image; return img != null ? img.color.a : 0f; } }
        Transform Player => F(Hub, "_player") as Transform;

        // ── 화면 밝기 샘플러 ─────────────────────────────────────────
        float _lum = -1f; float _gameT; bool _sampling; Texture2D _last;
        IEnumerator Sampler()
        {
            int n = 0;
            while (true)
            {
                yield return new WaitForEndOfFrame();
                if (!_sampling || (++n % 3) != 0) continue;
                var tex = ScreenCapture.CaptureScreenshotAsTexture();
                if (tex == null) continue;
                float s = 0f; int c = 0;
                for (int y = 0; y < 40; y++) for (int x = 0; x < 24; x++) { var p = tex.GetPixel((int)((x + 0.5f) / 24f * tex.width), (int)((y + 0.5f) / 40f * tex.height)); s += p.r * 0.299f + p.g * 0.587f + p.b * 0.114f; c++; }
                _lum = s / c;
                if (_last != null) Destroy(_last); _last = tex;
            }
        }
        void Shot(string name)
        {
            if (_last == null) return;
            try { File.WriteAllBytes(Path.Combine(_shotDir, name + ".jpg"), _last.EncodeToJPG(55)); } catch { }
        }

        // ── 뜬 카드(버튼 있는 캔버스) 자동 누르기 ─────────────────────
        static readonly string[] Prefer = { "Take", "Ok", "Next", "Go", "Continue", "Start", "Confirm" };
        string _cardSeen; float _cardT;
        string TopCard(out Button pick)
        {
            pick = null; Canvas best = null;
            foreach (var cv in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (!cv.isRootCanvas || !cv.isActiveAndEnabled || cv.sortingOrder < 300) continue;
                string nm = cv.name; if (nm.Contains("Cinematic") || nm.Contains("Hud") || nm.Contains("HUD") || nm.Contains("Toast")) continue;
                if (cv.GetComponentInChildren<Button>() == null) continue;
                if (best == null || cv.sortingOrder > best.sortingOrder) best = cv;
            }
            if (best == null) return null;
            var btns = best.GetComponentsInChildren<Button>();
            foreach (var p in Prefer) { foreach (var b in btns) if (b.interactable && b.gameObject.activeInHierarchy && b.name == p) { pick = b; break; } if (pick != null) break; }
            if (pick == null) foreach (var b in btns) if (b.interactable && b.gameObject.activeInHierarchy) { pick = b; break; }
            var sb = new StringBuilder();
            foreach (var t in best.GetComponentsInChildren<Text>()) if (t.isActiveAndEnabled && !string.IsNullOrEmpty(t.text)) { if (sb.Length > 0) sb.Append(" | "); sb.Append(t.text.Replace("\n", " ")); }
            string s = best.name + ": " + sb; return s.Length > 260 ? s.Substring(0, 260) + "…" : s;
        }
        List<string> _ev;   // 장면 타임라인
        float _t0;
        void Ev(string s) { _ev?.Add($"    {_gameT - _t0,6:0.0}s  {s}"); }
        void AutoCards()
        {
            if (Hud != null && Hud.PopupOpen)
            {
                if (_hudPopT < 0f) { _hudPopT = _gameT; Ev("마을 팝업: " + (TopPopupText() ?? "?")); }
                else if (_gameT - _hudPopT > 1.8f) { Hud.ClosePopup(); _hudPopT = -1f; }
            }
            else _hudPopT = -1f;
            var card = TopCard(out var pick);
            if (card == null) { _cardSeen = null; return; }
            if (card != _cardSeen) { _cardSeen = card; _cardT = _gameT; Ev("카드: " + card); _cardShotPending = true; }
            if (_cardShotPending && _gameT - _cardT > 0.7f) { _cardShotPending = false; Shot(_sceneTag + "_card" + (++_cardN)); }
            if (_gameT - _cardT > 1.2f && pick != null)
            {
                var lbl = pick.GetComponentInChildren<Text>(); Ev($"  → 누름 [{pick.name}] {(lbl != null ? lbl.text : "")}");
                pick.onClick.Invoke(); _cardSeen = null; _cardT = _gameT;
            }
        }
        bool _autoStopLogged; int _cardN; string _sceneTag; float _hudPopT = -1f; bool _cardShotPending;

        string Music()
        {
            var sb = new StringBuilder();
            foreach (var a in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
                if (a.isPlaying && a.clip != null && a.loop && a.volume > 0.02f) { if (sb.Length > 0) sb.Append(", "); sb.Append(a.clip.name); }
            return sb.Length == 0 ? "(무음)" : sb.ToString();
        }
        string Toasts() { var f = typeof(CoastToast).GetField("_popping", BF); var hs = f != null ? f.GetValue(null) as HashSet<string> : null; return hs == null || hs.Count == 0 ? null : string.Join(" / ", hs); }

        IEnumerator Start()
        {
            string bk = Path.Combine(Application.persistentDataPath, "qa219_bak"); Directory.CreateDirectory(bk);
            foreach (var f in new[] { "save_0.json", "profile.json" })
            {
                string p = Path.Combine(Application.persistentDataPath, f);
                if (File.Exists(p)) File.Copy(p, Path.Combine(bk, f), true); else File.WriteAllText(Path.Combine(bk, f + ".absent"), "");
            }
            EditorPrefs.SetString(MovementQA219.RestoreKey, bk);
            EditorPrefs.SetInt("MoveQA219_Coins", PlayerPrefs.GetInt(CoinWallet.PrefsKey, -1));
            { var sb = new StringBuilder(); var ids = new List<string> { "PRO" }; for (int c = 1; c <= 20; c++) { ids.Add($"CH{c:00}_Open"); ids.Add($"CH{c:00}_Close"); }
              foreach (var id in ids) { string k = "CoastRun_VN_" + id; sb.Append(k).Append('=').Append(PlayerPrefs.HasKey(k) ? PlayerPrefs.GetInt(k) : -1).Append(';'); }
              EditorPrefs.SetString("MoveQA219_VN", sb.ToString()); }
            _shotDir = Path.Combine(Directory.GetCurrentDirectory(), "Builds/qa/flow"); Directory.CreateDirectory(_shotDir);
            _file = Path.Combine(Directory.GetCurrentDirectory(), "Builds/qa", _mode == 1 ? "story_boundary_qa_219.txt" : "story_flow_qa_219.txt"); File.WriteAllText(_file, "", new UTF8Encoding(false));
            Log($"=== Story Flow QA 219 · {System.DateTime.Now:yyyy-MM-dd HH:mm} · mode={_mode} ===");
            StartCoroutine(Sampler());
            float w = 0f; while ((Hub == null || GameManager.I == null || GameManager.I.Save == null) && w < 30f) { w += Time.unscaledDeltaTime; yield return null; }
            if (Hub == null) { Log("FAIL 마을(VillageHub) 없음"); yield return Finish(); yield break; }
            VillageHub.DevAutoBubble = true;
            // 시간 가속 — 컷씬(unscaled)도 같이 빨라지는지 잰다
            Time.captureDeltaTime = 1f / 15f;
            float rt = Time.realtimeSinceStartup, ut = Time.unscaledTime; for (int i = 0; i < 30; i++) yield return null;
            Log($"시간 가속: 30프레임 실제 {Time.realtimeSinceStartup - rt:0.00}s / 게임(unscaled) {Time.unscaledTime - ut:0.00}s");
            _sampling = true;
            if (_mode == 1) yield return Boundary(); else yield return Places();
            yield return Finish();
        }

        IEnumerator Tick(float sec) { float t = 0f; while (t < sec) { t += Time.unscaledDeltaTime; _gameT += Time.unscaledDeltaTime; AutoCards(); var gs = GameManager.I != null ? GameManager.I.Save : null; if (gs != null && gs.stats.stamina < 60) gs.stats.stamina = 120; yield return null; } }   // 220차: 시험 세이브 HP 1 이라 안내 중 쓰러져 병원행 — 체력 유지

        // ── mode 0 ───────────────────────────────────────────────
        IEnumerator Places()
        {
            var gm = GameManager.I; var s = gm.Save;
            s.chapter = 1; s.week = Timeline.WeekStart(1); s.storySeenMask = 0; s.storyFragMask = 0; s.storyMaskInit = true; s.bond214 = 9999; s.clueMask = 0; s.cluePendingMask = 0;
            if (_from > 0)
            {
                s.prologueSeen = true; s.storyTut = 9;
                for (int i = 0; i < _from && i < VillageStory.Order.Length; i++) { VillageStory.MarkSeen(VillageStory.Order[i], s); }
                foreach (var fr in VillageStory.Frags) { int ai = System.Array.FindIndex(VillageStory.Order, o => o.id == fr.after); if (ai >= 0 && ai < _from - 1) VillageStory.MarkSeen(fr, s); }
                s.chapter = VillageStory.Order[_from - 1].chapter; s.week = Timeline.WeekStart(s.chapter); gm.Persist(); M(Hub, "RefreshStoryBeacon", true);
                Log($"(이어서 시작: {VillageStory.Order[_from - 1].id} 까지 본 것으로, CH{s.chapter})");
                goto loop;
            }
            // 도입(탑 위 기상) → CS1 은 첫 장면으로 따로
            _ev = new List<string>(); _t0 = _gameT; _sceneTag = "00_intro"; _cardN = 0;
            Log("\n## 도입: 탑 위에서 깨어남");
            M(Hub, "DevStoryIntro");
            float lastLum = -1f; float blackSince = -1f, maxBlack = 0f;
            while (StoryPlaying || Busy) { yield return Tick(0.1f); Watch(ref lastLum, ref blackSince, ref maxBlack); if (_gameT - _t0 > 60f) { Ev("!! 도입이 60초 안에 안 끝남"); break; } }
            Shot("00_intro_end"); Ev($"도입 끝 · 가장 긴 검은 화면 {maxBlack:0.0}s · 음악 {Music()} · 토스트 {Toasts()}");
            foreach (var l in _ev) Log(l);

            loop:
            var summary = new List<string>();
            int guard = 0; int fails = 0;   // 220차: 반복 한도 40 → 80(챕터 넘김도 한 번으로 세서 CS8 전에 끝났다)
            while (guard++ < 80)
            {
                string k = Target(out var id, out var pos, out var label);
                if (k == "None")
                {
                    VillageStory.Scene next = null; foreach (var o in VillageStory.Order) if (!VillageStory.Seen(o, s)) { next = o; break; }
                    if (next == null) break;
                    s.chapter = next.chapter; s.week = Timeline.WeekStart(next.chapter); gm.Persist(); M(Hub, "RefreshStoryBeacon", true);
                    Log($"\n(챕터 {next.chapter} 로 넘김 — 다음 장면 {next.id})");
                    continue;
                }
                int before = summary.Count; yield return OneTarget(k, id, pos, label, summary);
                if (summary.Count > before && summary[summary.Count - 1].Contains("도착 실패") && ++fails > 6) { Log("!! 도착 실패가 많아 중단"); break; }
            }
            Log("\n=== 요약 ===");
            foreach (var l in summary) Log(l);
        }

        void Watch(ref float lastLum, ref float blackSince, ref float maxBlack)
        {
            if (_lum < 0f) return;
            bool black = _lum < 0.035f;
            if (black && blackSince < 0f) blackSince = _gameT;
            if (!black && blackSince >= 0f) { float d = _gameT - blackSince; if (d > maxBlack) maxBlack = d; blackSince = -1f; }
            lastLum = _lum;
        }

        IEnumerator OneTarget(string kind, string id, Vector3 pos, string label, List<string> summary)
        {
            _ev = new List<string>(); _t0 = _gameT; _cardN = 0;
            _sceneTag = $"{summary.Count + 1:00}_{(kind == "Tut" ? "tut" + GameManager.I.Save.storyTut : id)}";
            var cine = CinematicTable.Get(id);
            Log($"\n## {_sceneTag}  [{kind}] {label}  (CH{GameManager.I.Save.chapter}{(cine != null ? $", 컷씬 「{cine.title}」 {cine.cuts.Length}컷 {cine.Length:0}s" : "")})");
            var p0 = Player.position; float dist0 = Vector3.Distance(new Vector3(p0.x, 0, p0.z), new Vector3(pos.x, 0, pos.z));
            // 잠 막기(장면만) → 꼬마가 데려다준다
            bool gate = (bool)M(Hub, "StorySleepGate");
            if (gate) { yield return Tick(0.3f); Ev("잠 막기: " + (TopPopupText() ?? "(팝업 없음)")); Hud?.ClosePopup(); }
            else Ev("잠 막기 없음(" + kind + ")");
            M(Hub, "StoryGuide"); Ev($"꼬마 안내 시작 — 거리 {dist0:0} m · {Music()}");
            float t = 0f; string lastToast = null; _autoStopLogged = false;
            while (!StoryPlaying && t < 150f)
            {
                yield return Tick(0.25f); t += 0.25f;
                var tt = Toasts(); if (tt != null && tt != lastToast) { Ev("토스트: " + tt); lastToast = tt; }
                if (F(Hub, "_autoCo") == null && !StoryPlaying && !_autoStopLogged) { _autoStopLogged = true; var pp = Player.position; Ev($"자동 이동 끝남 — 위치 ({pp.x:0},{pp.z:0}) 목표까지 {Vector3.Distance(new Vector3(pp.x,0,pp.z), new Vector3(pos.x,0,pos.z)):0.0} m · 집안={(F(Hub, "_interior") != null)} · 구역={VillageZones.At(pp)}"); Shot(_sceneTag + "_autostop"); }
            }
            if (!StoryPlaying) { var pp = Player.position; Ev($"!! 150초 안에 도착 못 함 — 위치 ({pp.x:0},{pp.z:0}) 목표까지 {Vector3.Distance(new Vector3(pp.x,0,pp.z), new Vector3(pos.x,0,pos.z)):0.0} m · 집안={(F(Hub, "_interior") != null)} · 구역={VillageZones.At(pp)} · busy={Busy} · HUD잠김={(Hud != null && Hud.Locked)} · 자동이동={(F(Hub, "_autoCo") != null)}"); Shot(_sceneTag + "_stuck"); summary.Add($"{_sceneTag}: 도착 실패"); foreach (var l in _ev) Log(l); if (F(Hub, "_interior") != null) { M(Hub, "ExitHouse"); yield return Tick(1.5f); } M(Hub, "DevStoryGoTarget"); yield return Tick(1f); yield break; }
            float travel = _gameT - _t0; Shot(_sceneTag + "_a_arrive"); Ev($"도착 → 장면 시작 ({travel:0.0}s 걸림)");
            // 장면 진행 감시
            float blackSince = -1f, maxBlack = 0f, lastLum = -1f; float cineStart = -1f, cineEnd = -1f, firstImg = -1f, backToVillage = -1f; bool cardShot = false;
            string cineMusic = null, afterMusic = null;
            int flick = 0; float brightSince = -1f; bool wasBlack = false;
            while (StoryPlaying || Busy || VillageFade > 0.02f)
            {
                yield return Tick(0.05f);
                bool cp = CinematicPlayer.IsPlaying;
                if (cp && cineStart < 0f) { cineStart = _gameT; Ev($"컷씬 시작 {CinematicPlayer.CurrentId}"); }
                if (cp && firstImg < 0f && cineStart > 0f) { float el = _gameT - cineStart; foreach (var mark in new[] { 2f, 6f, 10f, 14f }) if (el >= mark && el - 0.05f < mark) { var pl0 = FindAnyObjectByType<CinematicPlayer>(); var fd = F(pl0, "_fader") as Image; var va = F(pl0, "_video") as RawImage; var ai = F(pl0, "_a") as Image; Ev($"  (검은 화면 {mark:0}s · 밝기 {_lum:0.00} · fader {(fd != null ? fd.color.a : -1):0.00} · video {(va != null ? va.color.a : -1):0.00} · shotA {(ai != null ? ai.color.a : -1):0.00} spr={(ai != null && ai.sprite != null)} · 실제시간 {Time.realtimeSinceStartup:0.0})"); Shot(_sceneTag + "_black" + mark); } }
                if (cp && firstImg < 0f && _lum > 0.06f && _gameT - cineStart > 0.2f) { firstImg = _gameT; Ev($"첫 그림 보임 (도착 후 {firstImg - _t0 - travel:0.0}s)"); }
                if (cp && firstImg > 0f && cineMusic == null && _gameT - firstImg > 1.5f) { cineMusic = Music(); Shot(_sceneTag + "_b_cine"); Ev("컷씬 음악: " + cineMusic); }
                if (cp && !cardShot) { var pl = FindAnyObjectByType<CinematicPlayer>(); var cg = F(pl, "_titleCg") as CanvasGroup; if (cg != null && cg.alpha > 0.95f) { cardShot = true; Shot(_sceneTag + "_c_card"); Ev("마무리 카드"); } }
                if (!cp && cineStart > 0f && cineEnd < 0f) { cineEnd = _gameT; Ev($"컷씬 끝 (길이 {cineEnd - cineStart:0.0}s) · 밝기 {_lum:0.00}"); }
                if (cineEnd > 0f && backToVillage < 0f && VillageFade < 0.05f && !StoryPlaying) { backToVillage = _gameT; }
                // 깜빡임: 검은 화면 사이에 0.35s 미만 밝은 화면
                bool black = _lum >= 0f && _lum < 0.035f;
                if (!black && wasBlack) brightSince = _gameT;
                if (black && !wasBlack && brightSince > 0f && _gameT - brightSince < 0.35f && _gameT - brightSince > 0.01f) { flick++; Ev($"!! 깜빡임 — 검은 화면 사이 {(_gameT - brightSince):0.00}s 밝아짐"); Shot(_sceneTag + "_flicker" + flick); }
                wasBlack = black;
                Watch(ref lastLum, ref blackSince, ref maxBlack);
                var tt = Toasts(); if (tt != null && tt != lastToast) { Ev("토스트: " + tt); lastToast = tt; }
                if (_gameT - _t0 > 420f) { Ev("!! 장면이 7분 안에 안 끝남"); break; }
            }
            if (id == "CS8")
            {   // 220차: CS8 뒤엔 마을로 안 돌아오고 엔딩으로 — 30초 동안 무엇이 이어지는지 기록
                string lastCine = null, lastSc = null; float w8 = 0f; int sh = 0;
                while (w8 < 30f)
                {
                    yield return Tick(0.25f); w8 += 0.25f;
                    string cid = CinematicPlayer.CurrentId; string sc = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                    if (cid != lastCine) { Ev($"CS8 뒤 컷씬: {cid ?? "(없음)"} · 음악 {Music()} · 밝기 {_lum:0.00}"); lastCine = cid; if (cid != null && sh < 3) { yield return Tick(2f); Shot(_sceneTag + "_ending" + (++sh)); } }
                    if (sc != lastSc) { Ev($"CS8 뒤 씬: {sc}"); lastSc = sc; }
                }
                summary.Add($"{_sceneTag}: 컷씬 {(cineEnd - cineStart):0}s → 엔딩 {lastCine ?? "-"} / 씬 {lastSc}");
                foreach (var l in _ev) Log(l);
                yield break;
            }
            yield return Tick(0.6f);
            afterMusic = Music(); Shot(_sceneTag + "_d_after");
            var tt2 = Toasts(); if (tt2 != null && tt2 != lastToast) Ev("토스트: " + tt2);
            string nk = Target(out var nid, out var _, out var nl);
            Ev($"마을로 돌아옴 · 음악 {afterMusic} · 다음 목표 [{nk}] {nid} {nl} · 가장 긴 검은 화면 {maxBlack:0.0}s");
            summary.Add($"{_sceneTag}: 이동 {travel:0}s · 컷씬 {(cineStart > 0 ? (cineEnd - cineStart).ToString("0") + "s" : "없음")} · 첫 그림까지 {(firstImg > 0 ? (firstImg - _t0 - travel).ToString("0.0") + "s" : "-")} · 최장 검은 화면 {maxBlack:0.0}s · 깜빡임 {flick} · 음악 {cineMusic ?? "-"} → {afterMusic} · 다음 {nk} {nid}");
            foreach (var l in _ev) Log(l);
        }
        string TopPopupText()
        {
            var h = Hud; if (h == null) return null; var pop = F(h, "_popup") as Component; if (pop == null) { var go = F(h, "_popup") as GameObject; if (go == null) return null; return Join(go.GetComponentsInChildren<Text>()); }
            return Join(pop.GetComponentsInChildren<Text>());
        }
        static string Join(Text[] ts) { var sb = new StringBuilder(); foreach (var t in ts) if (t.isActiveAndEnabled && !string.IsNullOrEmpty(t.text)) { if (sb.Length > 0) sb.Append(" | "); sb.Append(t.text.Replace("\n", " ")); } return sb.ToString(); }

        // ── mode 1: 챕터 경계(잠 → 대회 → 러닝 → 마을) ─────────────────────
        IEnumerator Boundary()
        {
            var gm = GameManager.I; var s = gm.Save;
            int ch = 4;
            s.prologueSeen = true; s.storyTut = 9; s.storyMaskInit = true; s.chapter = ch; s.week = Timeline.WeekEnd(ch); s.phaseIndex = Timeline.PhasesPerWeek;
            s.storySeenMask = 0; for (int i = 0; i < VillageStory.Order.Length; i++) if (VillageStory.Order[i].chapter <= ch) s.storySeenMask |= 1 << i;
            s.stats.stamina = PlayerStats.StatMax; gm.Persist(); M(Hub, "RefreshStoryBeacon", true);
            _ev = new List<string>(); _t0 = _gameT; _sceneTag = "B_ch4";
            Log($"\n## 챕터 경계 CH{ch} (주 {s.week}) — 잠 → 대회 → 러닝 → 마을");
            M(Hub, "SleepBed"); Ev("침대에서 잠 · " + Music());
            string lastScene = null; bool warped = false; float runStart = -1f; string lastToast = null;
            float lastLum = -1f, blackSince = -1f, maxBlack = 0f;
            while (_gameT - _t0 < 400f)
            {
                yield return Tick(0.1f);
                string sc = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                if (sc != lastScene) { Ev($"씬: {sc} · {Music()}"); Shot($"{_sceneTag}_scene_{sc}"); lastScene = sc; }
                var tt = Toasts(); if (tt != null && tt != lastToast) { Ev("토스트: " + tt); lastToast = tt; }
                Watch(ref lastLum, ref blackSince, ref maxBlack);
                var sm = StageManager.Instance;
                if (sm != null && FindAnyObjectByType<PlayerController>() != null && runStart < 0f) { runStart = _gameT; Ev("러닝 시작 · " + Music()); StoryContest.PaidPass = true; }
                if (runStart > 0f && !warped && _gameT - runStart > 6f) { warped = true; HealthSystem.Instance?.SetFraction(1f); sm?.DebugWarpToFinish(); Ev("결승 30m 앞으로 이동"); }
                if (runStart > 0f) HealthSystem.Instance?.SetFraction(1f);
                if (runStart > 0f && VillageHub.I != null && sc != "02_Run" && _gameT - runStart > 10f && !Busy && !StoryPlaying)
                {
                    yield return Tick(2f);
                    string k = Target(out var id, out var _, out var lbl);
                    Shot($"{_sceneTag}_back_village");
                    Ev($"마을로 돌아옴 — CH{s.chapter} 주{s.week} · 다음 이야기 목표 [{k}] {id} {lbl} · {Music()} · 최장 검은 화면 {maxBlack:0.0}s");
                    break;
                }
            }
            foreach (var l in _ev) Log(l);
        }

        IEnumerator Finish()
        {
            _sampling = false; Time.captureDeltaTime = 0f; VillageHub.DevAutoBubble = false;
            string dir = Path.Combine(Directory.GetCurrentDirectory(), "Builds/qa"); Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, _mode == 0 ? "story_flow_qa_219.txt" : "story_boundary_qa_219.txt");
            if (_file == null) File.WriteAllText(path, _r.ToString(), new UTF8Encoding(false));
            Debug.LogWarning("[FlowQA] 결과 저장: " + path + " — 플레이 종료 후 세이브·코인 자동 복원");
            yield break;
        }
    }
}
#endif
