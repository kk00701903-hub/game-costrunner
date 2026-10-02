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
    /// 220차(사용자: 「3일 정도 스토리 모드 플레이해 보고 유료 구매할 가치가 있는지」): 처음 사는 사람 조건(새 프로필·새 게임)으로
    ///   마을 스토리 모드를 3일(잠 3번) 플레이하는 봇 — 도입·튜토리얼·이야기 장소·가게·텃밭·낚시·알바·엄마 집·농장·정자·미션을 돌고,
    ///   뜨는 화면 문구와 선택, 돈·레벨·체력 변화를 기록하고 스크린샷을 남긴다. 세이브·프로필·코인·본 장면 기록은 끝나면 복원.
    public class PlaytestQA220 : MonoBehaviour
    {
        public static void Begin(int days) { if (!Application.isPlaying) { Debug.LogError("[Play3] 플레이 중에만"); return; } var go = new GameObject("PlaytestQA220"); DontDestroyOnLoad(go); go.AddComponent<PlaytestQA220>()._days = days; }
        int _days;
        const BindingFlags BF = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;
        static object F(object o, string n) { if (o == null) return null; var f = o.GetType().GetField(n, BF); return f != null ? f.GetValue(o) : null; }
        static object M(object o, string n, params object[] a) { if (o == null) return null; var m = o.GetType().GetMethod(n, BF); return m != null ? m.Invoke(o, a) : null; }
        string _file, _shotDir; float _t0; int _shots; float _nextShot;
        void Log(string s) { string line = $"[{Time.realtimeSinceStartup - _t0,6:0}s] {s}"; Debug.LogWarning("[Play3] " + s); try { File.AppendAllText(_file, line + "\n", new UTF8Encoding(false)); } catch { } }
        VillageHub Hub => VillageHub.I;
        SaveData S => GameManager.I != null ? GameManager.I.Save : null;
        VillageHud Hud => F(Hub, "_hud") as VillageHud;

        void Shot(string tag)
        {
            StartCoroutine(ShotCo(tag));
        }
        IEnumerator ShotCo(string tag)
        {
            yield return new WaitForEndOfFrame();
            var tex = ScreenCapture.CaptureScreenshotAsTexture(); if (tex == null) yield break;
            var small = new Texture2D(tex.width / 2, tex.height / 2, TextureFormat.RGB24, false);
            for (int y = 0; y < small.height; y++) for (int x = 0; x < small.width; x++) small.SetPixel(x, y, tex.GetPixel(x * 2, y * 2));
            try { File.WriteAllBytes(Path.Combine(_shotDir, $"p{++_shots:000}_{tag}.jpg"), small.EncodeToJPG(60)); } catch { }
            Destroy(tex); Destroy(small);
        }

        // ── 화면 읽기·누르기 ─────────────────────────────────────
        static string Texts(Component root, int max = 300)
        {
            var sb = new StringBuilder();
            foreach (var t in root.GetComponentsInChildren<Text>()) if (t.isActiveAndEnabled && !string.IsNullOrEmpty(t.text)) { var s = t.text.Replace("\n", " ").Trim(); if (s.Length == 0 || s == "✦") continue; if (sb.Length > 0) sb.Append(" | "); sb.Append(s); }
            var r = sb.ToString(); return r.Length > max ? r.Substring(0, max) + "…" : r;
        }
        static readonly string[] Good = { "감귤 팬케이크", "장보기", "밥 먹을래", "흰밥", "넘어가기", "출발", "시작", "심기", "심는", "구매", "사기", "산다", "받기", "받는다", "먹", "쓰다듬", "하기", "일한다", "고른다", "확인", "확정", "발사", "던지기", "좋아", "다음", "기억해", "데려다", "Take", "Ok", "Go", "Next", "Continue" };
        static readonly string[] Bad = { "다음에 올게요", "땅 사기", "물건 팔기", "삭제", "초기화", "메인", "타이틀", "처음부터", "그만", "포기", "팔기 전부", "모두 팔기", "환불", "리셋", "종료하기", "다시하기" };
        static readonly string[] CloseW = { "닫기", "나가기", "나중에", "✕", "돌아가기", "아직", "알았어", "그냥" };
        int _clicksSame; string _lastUi; float _uiSince;
        /// 떠 있는 화면(카드·가게·팝업) 하나 처리 — 처리했으면 true
        bool DriveUi()
        {
            // 1) 마을 말풍선·선택 팝업
            var hud = Hud;
            if (hud != null && hud.PopupOpen)
            {
                var pop = F(hud, "_popup") as RectTransform;
                string txt = pop != null ? Texts(pop) : "?";
                if (txt != _lastUi) { _lastUi = txt; _uiSince = Time.realtimeSinceStartup; _clicksSame = 0; Log("팝업: " + txt); Shot("pop"); return true; }
                if (Time.realtimeSinceStartup - _uiSince < 1.4f) return true;
                var btns = pop != null ? pop.GetComponentsInChildren<Button>() : new Button[0];
                Button pick = Pick(btns, _clicksSame >= 2);
                _clicksSame++; _uiSince = Time.realtimeSinceStartup;
                if (pick != null) { Log("  → " + Label(pick)); pick.onClick.Invoke(); }
                else hud.ClosePopup();
                return true;
            }
            // 2) 따로 뜬 화면(가게·카드·알바·펫·미니게임 …)
            Canvas best = null;
            foreach (var cv in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (cv == null || !cv.isRootCanvas || !cv.isActiveAndEnabled || cv.sortingOrder < 140) continue;
                string n = cv.name; if (n.Contains("Hud") || n.Contains("Toast") || n.Contains("Cinematic") || n.Contains("Fade") || n.Contains("Sleep")) continue;
                if (cv.GetComponentInChildren<Button>() == null) continue;
                if (best == null || cv.sortingOrder > best.sortingOrder) best = cv;
            }
            if (best == null) { _lastUi = null; return false; }
            string t2 = best.name + ": " + System.Text.RegularExpressions.Regex.Replace(Texts(best), "[0-9]+", "#");
            if (t2 != _lastUi) { bool sameCanvas = _lastUi != null && _lastUi.StartsWith(best.name + ":"); _lastUi = t2; _uiSince = Time.realtimeSinceStartup; if (!sameCanvas) { _clicksSame = 0; Shot(best.name); } Log("화면 " + t2); return true; }
            if (Time.realtimeSinceStartup - _uiSince < 1.6f) return true;
            var bs = best.GetComponentsInChildren<Button>();
            // 미션 미니게임에 졌을 때: 두 번은 다시 해 보고, 그래도 지면(코인 모자라면) 스킵 비용만큼 넣어 주고 바로 낸다 = 돈 변화 0 (QA 보정, 로그에 남김)
            if (best.name.Contains("Mission"))
            {
                Button retry = null, pay = null; foreach (var b in bs) { if (b == null || !b.gameObject.activeInHierarchy) continue; if (b.name == "Retry") retry = b; if (b.name == "Pay") pay = b; }
                if (retry != null && pay != null)
                {
                    _missionFails++; _uiSince = Time.realtimeSinceStartup; _clicksSame = 0;
                    var m = System.Text.RegularExpressions.Regex.Match(Label(pay), "([0-9]+)G"); int cost = m.Success ? int.Parse(m.Groups[1].Value) : 0;
                    if (S != null && S.stats.money >= cost) { Log($"  (미션 짐 {_missionFails}번째) → " + Label(pay)); pay.onClick.Invoke(); _missionFails = 0; return true; }
                    if (_missionFails <= 2) { Log($"  (미션 짐 {_missionFails}번째 · 돈 {S?.stats.money}G < 스킵 {cost}G) → 다시하기"); retry.onClick.Invoke(); return true; }
                    int add = cost - S.stats.money; S.stats.money += add; Log($"  !! QA 보정: 세 번 져서 스킵 비용 모자란 {add}G 를 넣음 → 한 번 더 하고 지면 그 돈으로 넘어감(돈 0 으로 끝남) → 다시하기"); retry.onClick.Invoke(); return true;
                }
            }
            // 235차: 대회·런에서 진 결과창 — 봇은 조작을 안 하니 「다시 도전」만 끝없이 누르게 됨 → 두 번 다시 해 보고 그다음엔 육성으로 돌아간다
            if (best.name.Contains("RunOver"))
            {
                _runOverRetries++;
                if (_runOverRetries > 2) foreach (var b in bs) if (b != null && b.interactable && b.gameObject.activeInHierarchy && Label(b).Contains("육성")) { Log("  (런 결과 3번째 — 육성으로) → " + Label(b)); _runOverRetries = 0; _uiSince = Time.realtimeSinceStartup; b.onClick.Invoke(); return true; }
            }
            var pk = Pick(bs, _clicksSame >= 4);
            _clicksSame++; _uiSince = Time.realtimeSinceStartup;
            if (_clicksSame > (best.name.Contains("Mission") ? 60 : 9)) { Log("  (같은 화면 10번 — 닫음) " + best.name); Destroy(best.gameObject); var fb = Hub != null ? Hub.GetType().GetField("_busy", BF) : null; fb?.SetValue(Hub, false); _clicksSame = 0; return true; }
            if (pk != null) { Log("  → " + Label(pk)); pk.onClick.Invoke(); }
            return true;
        }
        static string Label(Button b) { var t = b.GetComponentInChildren<Text>(); return (t != null ? t.text.Replace("\n", " ") : "") + " [" + b.name + "]"; }
        static bool Has(string s, string[] ws) { foreach (var w in ws) if (s.Contains(w)) return true; return false; }
        int _missionFails, _runOverRetries;
        /// 236차: 사람 손가락이 닿는 버튼만 — 위에 덮인 설명 카드(윷놀이 「하는 법」 등) 아래 버튼을 누르던 것.
        static readonly List<UnityEngine.EventSystems.RaycastResult> _hits = new List<UnityEngine.EventSystems.RaycastResult>();
        static bool OnTop(Button b)
        {
            var es = UnityEngine.EventSystems.EventSystem.current; if (es == null) return true;
            var rt = (RectTransform)b.transform; var cv = b.GetComponentInParent<Canvas>(); if (cv == null) return true;
            var cam = cv.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : cv.rootCanvas.worldCamera;
            Vector3 wc = rt.TransformPoint(rt.rect.center); Vector2 sp = RectTransformUtility.WorldToScreenPoint(cam, wc);
            var pd = new UnityEngine.EventSystems.PointerEventData(es) { position = sp }; _hits.Clear(); es.RaycastAll(pd, _hits);
            if (_hits.Count == 0) return true;
            var top = _hits[0].gameObject; return top != null && (top.transform == b.transform || top.transform.IsChildOf(b.transform));
        }
        Button Pick(Button[] bs, bool wantClose)
        {
            var ok = new List<Button>(); foreach (var b in bs) if (b != null && b.interactable && b.gameObject.activeInHierarchy && OnTop(b)) ok.Add(b);
            if (ok.Count == 0) foreach (var b in bs) if (b != null && b.interactable && b.gameObject.activeInHierarchy) ok.Add(b);   // 다 가려졌다고 나오면 예전처럼
            if (ok.Count == 0) return null;
            if (wantClose) foreach (var b in ok) if (Has(Label(b), CloseW)) return b;
            foreach (var b in ok) { var l = Label(b); if (Has(l, Good) && !Has(l, Bad)) return b; }
            foreach (var b in ok) { var l = Label(b); if (!Has(l, Bad) && !Has(l, CloseW)) return b; }
            foreach (var b in ok) if (Has(Label(b), CloseW)) return b;
            return null;
        }

        // ── 흐름 ─────────────────────────────────────────────────
        IEnumerator Run(float sec, System.Func<bool> until = null)
        {
            float t = 0f;
            while (t < sec)
            {
                if (until != null && until()) yield break;
                DriveUi();
                if (Time.realtimeSinceStartup > _nextShot) { _nextShot = Time.realtimeSinceStartup + 20f; Shot("tick"); }
                t += Time.unscaledDeltaTime; yield return null;
            }
        }
        string Stat() { var s = S; if (s == null) return "-"; return $"{s.week}주차 CH{s.chapter} · 돈 {s.stats.money}G · HP {s.stats.stamina} · Lv{s.level} · 스트레스 {s.stats.stress}"; }

        IEnumerator Start()
        {
            _t0 = Time.realtimeSinceStartup;
            // 백업(MovementQA219 과 같은 복원 경로)
            string bk = Path.Combine(Application.persistentDataPath, "qa219_bak"); Directory.CreateDirectory(bk);
            foreach (var f in new[] { "save_0.json", "profile.json" }) { string p = Path.Combine(Application.persistentDataPath, f); if (File.Exists(p)) File.Copy(p, Path.Combine(bk, f), true); else File.WriteAllText(Path.Combine(bk, f + ".absent"), ""); }
            EditorPrefs.SetString(MovementQA219.RestoreKey, bk); EditorPrefs.SetInt("MoveQA219_Coins", PlayerPrefs.GetInt(CoinWallet.PrefsKey, -1));
            { var sb = new StringBuilder(); var ids = new List<string> { "PRO" }; for (int c = 1; c <= 20; c++) { ids.Add($"CH{c:00}_Open"); ids.Add($"CH{c:00}_Close"); } foreach (var id in ids) { string k = "CoastRun_VN_" + id; sb.Append(k).Append('=').Append(PlayerPrefs.HasKey(k) ? PlayerPrefs.GetInt(k) : -1).Append(';'); PlayerPrefs.DeleteKey(k); } foreach (var k in new[] { "CoastRun.VillageHint220", "CoastRun.AutoHint221", "CoastRun.StressHint221" }) { sb.Append(k).Append('=').Append(PlayerPrefs.HasKey(k) ? PlayerPrefs.GetInt(k) : -1).Append(';'); PlayerPrefs.DeleteKey(k); } EditorPrefs.SetString("MoveQA219_VN", sb.ToString()); }
            _file = Path.Combine(Directory.GetCurrentDirectory(), _days >= 20 ? "Builds/qa/month236_days.txt" : _days == 10 ? "Builds/qa/sim236_10w_days.txt" : "Builds/qa/playtest_3days_220.txt");   // 236차: 한 달 시뮬은 따로
            File.WriteAllText(_file, "", new UTF8Encoding(false));
            _shotDir = Path.Combine(Directory.GetCurrentDirectory(), "Builds/qa/play3"); Directory.CreateDirectory(_shotDir);
            Log($"=== {_days}일 플레이 · {System.DateTime.Now:yyyy-MM-dd HH:mm} ===");
            var gm = GameManager.I; while (gm == null) { yield return null; gm = GameManager.I; }
            // 처음 사는 사람: 지난 회차 계승·전체 열림·기부 선물 없음(프로필은 끝나고 복원)
            var pf = gm.Profile; pf.hasLastFinal = false; pf.devUnlockAll = false; pf.donateGiftMask = 0; pf.endingsSeen = 0; pf.endingMask = 0;
            PlayerPrefs.SetInt(CoinWallet.PrefsKey, 0);
            Log("새 게임 시작(새 프로필 조건)");
            gm.NewGame(RunMode.Running);
            float w = 0f; while ((Hub == null || S == null) && w < 60f) { w += Time.unscaledDeltaTime; yield return null; }
            if (Hub == null) { Log("!! 마을이 안 열림"); yield break; }
            VillageHub.DevAutoBubble = false;
            yield return Run(3f);
            Log("시작 상태: " + Stat()); Shot("start");
            // 도입(탑 위에서 깨어남) — 버튼이 나오면 누른다
            float t = 0f; while ((bool)F(Hub, "_storyPlaying") && t < 60f) { var down = GameObject.Find("Down"); if (down != null) { var b = down.GetComponent<Button>(); if (b != null) { Log("도입: 「탑에서 내려가기」 누름"); Shot("intro"); b.onClick.Invoke(); } } yield return Run(0.5f); t += 0.5f; }
            for (int day = 1; day <= _days; day++)
            {
                { float hw = 0f; while (Hub == null && hw < 180f) { yield return Run(1f); hw += 1f; } if (Hub == null) { Log("!! 마을로 안 돌아옴 · " + Stat()); break; } }
                Log($"\n##### {day}일째 아침 · {Stat()}");
                Shot($"day{day}_morning");
                // 1) 이야기(빛나는 곳) — 꼬마가 데려다준다
                for (int k = 0; k < 6; k++) { bool any = (bool)M(Hub, "StorySleepGate") || StoryAny(); if (!any) break; yield return Run(0.5f); Hud?.ClosePopup(); yield return FollowStory(); }
                // 2) 하루 활동
                foreach (var id in new[] { "shop", "garden", "beach", "job", "mom", "farm", "play", "cafe" })
                {
                    if (S == null || Hub == null) break;
                    yield return Visit(id);
                    if (StoryAny()) yield return FollowStory();
                }
                // 3) 미션 안내
                M(Hub, "MissionPopup"); yield return Run(3f);
                Log($"잠들기 전 · {Stat()}");
                // 4) 잠
                int wk0 = S != null ? S.week : -1;
                for (int tries = 0; tries < 3; tries++)
                {
                    if (Hub != null) { if (F(Hub, "_interior") as Object != null) { M(Hub, "ExitHouse"); yield return Run(1.5f); } M(Hub, "SleepBed"); }
                    yield return Run(4f);
                    float ws = 0f; int seenHub = 0;
                    while (ws < 420f) { yield return Run(1f); ws += 1f; if (Hub != null && !(bool)F(Hub, "_busy") && (Hud == null || !Hud.PopupOpen) && ws > 8f) { seenHub++; if (seenHub > 3) break; } else seenHub = 0; }
                    if (S == null || S.week != wk0) break;
                    Log($"  (아직 같은 주 — 다시 침대로) · {Stat()}");
                }
                Log($"{day}일째 끝 · {Stat()}");
            }
            Log("=== 끝 ===");
        }

        bool StoryAny() { if (Hub == null) return false; var args = new object[] { null, null, null }; var r = M(Hub, "StoryTarget", args); var k = r != null ? r.ToString() : "None"; return k == "Tut" || k == "Scene" || k == "Frag"; }
        IEnumerator FollowStory()
        {
            if (Hub == null) yield break;
            var args = new object[] { null, null, null }; var k = M(Hub, "StoryTarget", args)?.ToString(); var sc = args[0] as VillageStory.Scene;
            Log($"이야기 따라가기 [{k}] {(sc != null ? sc.id : "")} {args[2]}");
            M(Hub, "StoryGuide");
            float t = 0f; while (!(bool)F(Hub, "_storyPlaying") && t < 90f) { yield return Run(0.5f); t += 0.5f; }
            if (!(bool)F(Hub, "_storyPlaying")) { Log("  !! 90초 안에 못 감 — 순간이동"); M(Hub, "DevStoryGoTarget"); yield return Run(3f); }
            bool shot = false; t = 0f;
            while (Hub != null && ((bool)F(Hub, "_storyPlaying") || (bool)F(Hub, "_busy")) && t < 400f)
            {
                if (CinematicPlayer.IsPlaying && !shot && t > 6f) { shot = true; Shot("cine_" + CinematicPlayer.CurrentId); }
                yield return Run(0.5f); t += 0.5f;
            }
            yield return Run(2f);
            Log("  이야기 끝 · " + Stat());
        }
        IEnumerator Visit(string id)
        {
            var spots = F(Hub, "_spots") as IList; object sp = null; foreach (var s in spots) if ((string)F(s, "id") == id) { sp = s; break; }
            if (sp == null) { Log($"({id} 스팟 없음)"); yield break; }
            bool open = (bool)M(Hub, "AutoMoveOpen", sp);
            Log($"\n-- {id} {(open ? "" : "(자동이동 잠김 — 걸어서)")} · {Stat()}");
            if (open) M(Hub, "StartAutoMove", id);
            else { var p = (Vector3)F(sp, "pos"); Hub.Teleport(p + new Vector3(2f, 0f, 2f)); M(Hub, "WalkTo", p); }
            float t = 0f; while (F(Hub, "_autoCo") != null && t < 60f) { yield return Run(0.5f); t += 0.5f; }
            if (!open) { yield return Run(3f); var on = F(sp, "on") as System.Action; if (on != null && F(Hub, "_interior") as Object == null && !(bool)F(Hub, "_busy")) on(); }
            yield return Run(2.5f);
            // 실내(가게·알바·카페)면 카운터로
            if (F(Hub, "_interior") as Object != null)
            {
                foreach (var s in spots) { var sid = (string)F(s, "id"); if (sid == "counter" || sid.StartsWith("in_") || sid == "home_table") { var on = F(s, "on") as System.Action; if (on != null) { Log($"  실내 {sid}"); on(); yield return Run(14f); } break; } }
                M(Hub, "ExitHouse"); yield return Run(2f);
            }
            else yield return Run(14f);
            // 남은 화면 정리
            foreach (var cv in FindObjectsByType<Canvas>(FindObjectsSortMode.None)) if (cv != null && cv.isRootCanvas && cv.sortingOrder >= 140 && !cv.name.Contains("Hud") && !cv.name.Contains("Toast") && !cv.name.Contains("Cinematic")) { Log("  (열린 화면 닫음) " + cv.name); Destroy(cv.gameObject); }
            Hud?.ClosePopup(); var fb = Hub.GetType().GetField("_busy", BF); if (!(bool)F(Hub, "_storyPlaying")) fb?.SetValue(Hub, false);
            yield return Run(1f);
        }
    }
}
#endif
