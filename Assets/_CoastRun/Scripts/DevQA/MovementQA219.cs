#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace CoastRun.DevQA
{
    /// 219차(사용자: 「움직임 위주로 통합테스트」): 러닝 움직임 자동 점검(에디터 전용, 게임 빌드에 안 들어감).
    ///  A 레인 이동(폭·걸린 시간) · B 점프/2단 점프(높이·체공) · C 숙이기(몸 높이·지속)
    ///  D 장애물 판정 감사 — 60/30/20 fps 에서 장애물이 플레이어를 지나갈 때 「맞음 / 옆으로 피함 / 뛰어넘음 / 통과(버그)」 분류.
    /// 세이브(save_0.json·profile.json)는 시작 때 백업, 플레이 종료 후 자동 복원. 결과: Builds/qa/move_qa_219.txt
    public class MovementQA219 : MonoBehaviour
    {
        public const string RestoreKey = "MoveQA219_RestoreDir";
        public static void Begin(int mode, int chapter = 0)
        {
            if (!Application.isPlaying) { Debug.LogError("[MoveQA] 플레이 중에만"); return; }
            var go = new GameObject("MovementQA219"); DontDestroyOnLoad(go);
            var q = go.AddComponent<MovementQA219>(); q._mode = mode; q._chapter = chapter;
        }

        int _mode, _chapter;
        int _lastPass, _lastHit, _lastDodge;

        // 219차 추가: 스토리 러닝 여러 챕터를 한 번에 — 챕터마다 마을 씬을 다시 불러 새로 시작
        static readonly int[] SweepChapters = { 2, 3, 4, 6, 7, 8, 9, 11, 12, 13, 14, 16, 17, 18, 19 };
        IEnumerator StorySweep()
        {
            var summary = new List<string>(); int totalPass = 0, totalCross = 0;
            foreach (int ch in SweepChapters)
            {
                var gm = GameManager.I; if (gm == null || gm.Save == null) { Log($"CH{ch}: GameManager 없음 — 중단"); break; }
                gm.Save.chapter = ch; Log($"--- 스토리 챕터 {ch} ---");
                gm.StartStoryRun();
                float t0 = Time.realtimeSinceStartup; _pc = null;
                while (Time.realtimeSinceStartup - t0 < 60f)
                {
                    SkipCutscenes();
                    _pc = FindAnyObjectByType<PlayerController>(); _in = FindAnyObjectByType<MobileSwipeInput>();
                    if (_pc != null && _in != null && Speed > 0.5f) break;
                    yield return null;
                }
                if (_pc == null || Speed <= 0.5f) { Log($"CH{ch}: 런 시작 안 됨(60 s)"); summary.Add($"CH{ch}: 시작 실패"); }
                else
                {
                    yield return Wait(1.5f);
                    int p = 0, h = 0, d = 0;
                    yield return AuditObstacles(60, 30f); p += _lastPass; h += _lastHit; d += _lastDodge;
                    yield return AuditObstacles(20, 30f); p += _lastPass; h += _lastHit; d += _lastDodge;
                    Application.targetFrameRate = 60;
                    totalPass += p; totalCross += p + h + d;
                    summary.Add($"CH{ch}: 통과 {p} (맞음 {h}, 피함 {d})");
                }
                // 다음 챕터: 마을 씬으로 돌아가 새로 시작
                UnityEngine.SceneManagement.SceneManager.LoadScene("05_Raising");
                float t1 = Time.realtimeSinceStartup;
                yield return null;
                while (Time.realtimeSinceStartup - t1 < 40f && (GameManager.I == null || FindAnyObjectByType<PlayerController>() != null)) yield return null;
                yield return new WaitForSecondsRealtime(8f);
            }
            Log("=== 스토리 스윕 요약 ===");
            foreach (var l in summary) Log(l);
            Log($"합계: 지나간 장애물 {totalCross}개 중 통과(판정 누락) {totalPass}");
        }
        readonly StringBuilder _r = new StringBuilder();
        const BindingFlags BF = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        static T Get<T>(object o, string n) { var f = o.GetType().GetField(n, BF); return f != null ? (T)f.GetValue(o) : default; }
        void Log(string s) { _r.AppendLine(s); Debug.LogWarning("[MoveQA] " + s); }

        PlayerController _pc; MobileSwipeInput _in;
        float Hop => Get<float>(_pc, "_hop");
        float Ground => Get<float>(_pc, "_groundY") + Get<float>(_pc, "_bodyHeight") * 0.5f;
        float Speed => Get<float>(_pc, "_speed");
        float LaneOffset { get { var c = Get<object>(_pc, "config"); if (c == null) return 0f; var f = c.GetType().GetField("laneOffset", BF); return f != null ? (float)f.GetValue(c) : 0f; } }

        void KeepAlive() { HealthSystem.Instance?.SetFraction(1f); }

        IEnumerator Start()
        {
            // 세이브 백업
            string bk = Path.Combine(Application.persistentDataPath, "qa219_bak"); Directory.CreateDirectory(bk);
            foreach (var f in new[] { "save_0.json", "profile.json" })
            {
                string p = Path.Combine(Application.persistentDataPath, f);
                if (File.Exists(p)) File.Copy(p, Path.Combine(bk, f), true); else File.WriteAllText(Path.Combine(bk, f + ".absent"), "");
            }
            EditorPrefs.SetString(RestoreKey, bk);
            EditorPrefs.SetInt("MoveQA219_Coins", PlayerPrefs.GetInt(CoinWallet.PrefsKey, -1));   // 코인은 PlayerPrefs 라 세이브 파일 백업에 안 들어간다
            Log($"=== Movement QA 219 · {System.DateTime.Now:yyyy-MM-dd HH:mm} · mode={(_mode == 3 ? "story-sweep" : _mode == 2 ? "endless-20fps" : _mode == 1 ? "endless" : "story")} ===");

            var gm = GameManager.I;
            if (gm == null) { Log("FAIL GameManager 없음"); yield break; }
            if (_mode == 3) { yield return StorySweep(); yield return Finish(); yield break; }
            if (_mode >= 1) ArcadeRun.Start(ArcadeKind.Endless, gm);
            else { if (_chapter > 0 && gm.Save != null) { gm.Save.chapter = _chapter; Log($"스토리 챕터 {_chapter}"); } gm.StartStoryRun(); }

            float t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < 90f)
            {
                SkipCutscenes();
                _pc = FindAnyObjectByType<PlayerController>(); _in = FindAnyObjectByType<MobileSwipeInput>();
                if (_pc != null && _in != null && Speed > 0.5f) break;
                yield return null;
            }
            if (_pc == null || _in == null || Speed <= 0.5f) { Log("FAIL 런 시작 안 됨(90 s)"); yield return Finish(); yield break; }
            Log($"런 시작 {Time.realtimeSinceStartup - t0:0.0}s, 속도 {Speed:0.0} m/s, 레인 폭 {LaneOffset:0.00} m");
            yield return Wait(1.5f);

            yield return TestLanes();
            yield return TestJump();
            yield return TestCrouch();
            if (_mode == 1) { foreach (var fps in new[] { 60, 30, 20 }) yield return AuditObstacles(fps, 35f); }
            else if (_mode == 2) { yield return AuditObstacles(20, 80f); }
            else { yield return AuditObstacles(60, 30f); yield return AuditObstacles(20, 25f); }
            Application.targetFrameRate = 60;
            yield return Finish();
        }

        void SkipCutscenes()
        {
            foreach (var t in new[] { typeof(ChapterVN), typeof(CinematicPlayer) })
                foreach (var o in FindObjectsByType(t, FindObjectsSortMode.None))
                {
                    var f = t.GetField("_skip", BF); if (f != null) f.SetValue(o, true);
                }
        }

        IEnumerator Wait(float s) { float t = 0f; while (t < s) { KeepAlive(); t += Time.unscaledDeltaTime; yield return null; } }

        IEnumerator TestLanes()
        {
            float lo = LaneOffset;
            foreach (int dir in new[] { -1, 1, 1, -1 })
            {
                int lane0 = _pc.Lane; float l0 = _pc.LateralOffset;
                _in.Inject(dir, false, false);
                float t = 0f, reach = -1f; float target = Mathf.Clamp(lane0 + dir, -1, 1) * lo; float maxOver = 0f;
                while (t < 0.9f)
                {
                    KeepAlive(); t += Time.unscaledDeltaTime;
                    float l = _pc.LateralOffset;
                    if (reach < 0f && Mathf.Abs(l - target) < lo * 0.05f) reach = t;
                    maxOver = Mathf.Max(maxOver, (l - target) * Mathf.Sign(target - l0 == 0 ? 1 : target - l0));
                    yield return null;
                }
                bool ok = _pc.Lane == Mathf.Clamp(lane0 + dir, -1, 1) && Mathf.Abs(_pc.LateralOffset - target) < lo * 0.05f;
                Log($"[A 레인] {lane0}→{_pc.Lane} ({(dir < 0 ? "왼쪽" : "오른쪽")}) 이동 {l0:0.00}→{_pc.LateralOffset:0.00} m, 도착 {(reach < 0 ? "미도착" : reach.ToString("0.00") + "s")}, 넘침 {maxOver:0.00} m → {(ok ? "OK" : "확인 필요")}");
                yield return Wait(0.3f);
            }
        }

        IEnumerator TestJump()
        {
            for (int k = 0; k < 2; k++)
            {
                yield return Wait(0.6f);
                float g = Ground; _in.Inject(0, true, false);
                float t = 0f, max = 0f; bool left = false; float air = -1f; bool dj = k == 1;
                while (t < 3f)
                {
                    KeepAlive(); t += Time.deltaTime;
                    if (dj && t > 0.25f && t < 0.3f) _in.Inject(0, true, false);
                    max = Mathf.Max(max, Hop - g);
                    if (!_pc.IsGrounded) left = true; else if (left) { air = t; break; }
                    yield return null;
                }
                Log($"[B {(dj ? "2단 점프" : "점프")}] 최고 높이 {max:0.00} m, 체공 {(air < 0 ? "착지 안 함(3s)" : air.ToString("0.00") + "s")} → {(left && air > 0 && max > 0.3f ? "OK" : "확인 필요")}");
            }
        }

        IEnumerator TestCrouch()
        {
            yield return Wait(0.8f);
            var col = Get<CapsuleCollider>(_pc, "_bodyCollider");
            float h0 = col != null ? col.height : 0f;
            _in.Inject(0, false, true);
            float t = 0f, minH = h0, dur = 0f; bool seen = false;
            while (t < 2.5f)
            {
                KeepAlive(); t += Time.deltaTime;
                if (col != null) minH = Mathf.Min(minH, col.height);
                if (_pc.IsCrouching) { seen = true; dur += Time.deltaTime; }
                yield return null;
            }
            Log($"[C 숙이기] 몸 높이 {h0:0.00}→{minH:0.00} m, 숙인 시간 {dur:0.00}s, 복귀 후 {(col != null ? col.height : 0f):0.00} m → {(seen && minH < h0 * 0.7f && col.height > h0 * 0.95f ? "OK" : "확인 필요")}");
        }

        class Track { public Collider c; public Object key; public string name; public bool duck; public float prevZ = float.NaN; public float depth; public bool done; }

        IEnumerator AuditObstacles(int fps, float seconds)
        {
            QualitySettings.vSyncCount = 0; Application.targetFrameRate = fps;
            var tracks = new Dictionary<Object, Track>();
            int hit = 0, dodge = 0, jump = 0, duckOk = 0, pass = 0, prot = 0; float maxStep = 0f, minDepth = 99f, maxSpeed = 0f;
            var passLines = new List<string>();
            float t = 0f;
            while (t < seconds)
            {
                KeepAlive(); t += Time.unscaledDeltaTime;
                if (_pc == null) break;
                float step = Speed * Time.deltaTime; maxStep = Mathf.Max(maxStep, step); maxSpeed = Mathf.Max(maxSpeed, Speed);
                // 등록
                foreach (var h in ObstacleHazard.Active) if (h != null) Reg(tracks, h, h.GetComponent<Collider>(), false);
                foreach (var d in FindObjectsByType<DuckHazard>(FindObjectsSortMode.None)) Reg(tracks, d, d.GetComponent<Collider>(), true);
                var tr = _pc.transform; var feet = _pc.FeetPosition;
                foreach (var kv in tracks)
                {
                    var k = kv.Value; if (k.done) continue;
                    if (k.key == null || k.c == null || !k.c.enabled || (k.c.transform.root != null && k.c.GetComponentInParent<ObstaclePopAnim>() != null)) { k.done = true; hit++; continue; }
                    var rel = tr.InverseTransformPoint(k.c.bounds.center);
                    if (!float.IsNaN(k.prevZ) && k.prevZ > 0f && rel.z <= 0f)
                    {
                        k.done = true;
                        var e = k.c.bounds.extents; var rv = tr.right; float halfW = Mathf.Abs(rv.x) * e.x + Mathf.Abs(rv.y) * e.y + Mathf.Abs(rv.z) * e.z + 0.25f;   // 옆 방향 반폭 + 몸 반폭
                        bool overlap = Mathf.Abs(rel.x) < halfW;
                        bool protectedNow = _pc.Invincible || FeverMode.Active || GiantMode.Active;
                        if (!overlap) dodge++;
                        else if (k.duck && _pc.IsCrouching) duckOk++;
                        else if (BodyBottom() > k.c.bounds.max.y - 0.02f) jump++;   // 224차: 숙이는 장애물(빨래줄)도 몸 전체가 판정 위로 지나가면 「뛰어넘음」
                        else if (protectedNow) prot++;
                        else
                        {
                            pass++;
                            var bc = Get<CapsuleCollider>(_pc, "_bodyCollider"); var hz = k.key as Behaviour;
                            if (passLines.Count < 12) passLines.Add($"    통과: {k.name} 깊이 {k.depth:0.00} m, 옆거리 {rel.x:0.00}/{halfW:0.00}, 속도 {Speed:0.0} m/s, 프레임 이동 {step:0.00} m, 발높이 {feet.y - k.c.bounds.min.y:0.00} m, 숙임 {_pc.IsCrouching} | 상태 {Get<object>(_pc, "_state")}, 경직창 {_pc.InIFrames}, 몸판정 y {(bc != null ? bc.bounds.min.y - k.c.bounds.min.y : -9):0.00}~{(bc != null ? bc.bounds.max.y - k.c.bounds.min.y : -9):0.00} (켜짐 {(bc != null && bc.enabled)}), 장애물 y 0~{k.c.bounds.size.y:0.00}, 컴포넌트 켜짐 {(hz != null && hz.enabled)}, dt {Time.deltaTime:0.000}, timeScale {Time.timeScale:0.00}");
                        }
                        minDepth = Mathf.Min(minDepth, k.depth);
                    }
                    k.prevZ = rel.z;
                }
                yield return null;
            }
            _lastPass = pass; _lastHit = hit; _lastDodge = dodge;
            Log($"[D 장애물 {fps}fps · {seconds:0}s] 맞음 {hit} · 옆으로 피함 {dodge} · 뛰어넘음 {jump} · 숙여 통과 {duckOk} · 무적 중 {prot} · **통과(판정 누락) {pass}** | 최고 속도 {maxSpeed:0.0} m/s, 한 프레임 최대 이동 {maxStep:0.00} m, 지나간 장애물 최소 깊이 {(minDepth > 90 ? 0 : minDepth):0.00} m");
            foreach (var l in passLines) Log(l);
        }

        float BodyBottom() { var bc = Get<CapsuleCollider>(_pc, "_bodyCollider"); return bc != null ? bc.bounds.min.y : _pc.FeetPosition.y; }

        void Reg(Dictionary<Object, Track> tracks, Object key, Collider c, bool duck)
        {
            if (key == null || c == null) return;
            var id = key;
            if (tracks.ContainsKey(id)) return;
            var f = _pc.transform.forward; var s = c.bounds.size;
            float depth = Mathf.Abs(f.x) * s.x + Mathf.Abs(f.y) * s.y + Mathf.Abs(f.z) * s.z;
            Transform root = c.transform; while (root.parent != null && !root.name.StartsWith("Obstacle_")) root = root.parent;
            tracks[id] = new Track { c = c, key = key, duck = duck, depth = depth, name = root.name };
        }

        IEnumerator Finish()
        {
            Application.targetFrameRate = 60;
            Directory.CreateDirectory("Builds/qa");
            string fn = _mode == 3 ? "Builds/qa/move_qa_219_story_sweep.txt" : _mode == 2 ? "Builds/qa/move_qa_219_20fps.txt" : _mode == 1 ? "Builds/qa/move_qa_219.txt" : $"Builds/qa/move_qa_219_story_ch{_chapter}.txt"; File.WriteAllText(fn, _r.ToString());
            Log("결과 저장: " + fn + " — 플레이 종료 후 세이브 자동 복원");
            yield return null;
            EditorApplication.isPlaying = false;
        }
    }
}

#endif
