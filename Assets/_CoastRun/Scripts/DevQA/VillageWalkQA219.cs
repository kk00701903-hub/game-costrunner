#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using CoastRun.Village;

namespace CoastRun.DevQA
{
    /// 219차(사용자: 「스토리모드 추가 테스트 — 장애물 통과 위주」): 마을 걷기 충돌 감사(에디터 전용).
    /// 막힌 물체(바위·울타리·벽·나무·경계 등)마다 2 m 앞에 세우고 물체 한가운데를 지나 반대편으로 걸어가게 한 뒤,
    /// 몸(CharacterController)이 물체 안으로 얼마나 파고들었는지(최대 침투 깊이)와 반대편까지 뚫고 갔는지 본다.
    /// 옆으로 미끄러져 돌아간 것은 정상(침투 없음). 결과: Builds/qa/village_walk_qa_219.txt
    public class VillageWalkQA219 : MonoBehaviour
    {
        public static void Begin()
        {
            if (!Application.isPlaying) { Debug.LogError("[WalkQA] 플레이 중에만"); return; }
            var go = new GameObject("VillageWalkQA219"); DontDestroyOnLoad(go); go.AddComponent<VillageWalkQA219>();
        }

        readonly StringBuilder _r = new StringBuilder();
        void Log(string s) { _r.AppendLine(s); Debug.LogWarning("[WalkQA] " + s); }
        const BindingFlags BF = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        IEnumerator Start()
        {
            // 세이브·코인 백업(움직임 QA 와 같은 복원 경로 사용)
            string bk = Path.Combine(Application.persistentDataPath, "qa219_bak"); Directory.CreateDirectory(bk);
            foreach (var f in new[] { "save_0.json", "profile.json" })
            {
                string p = Path.Combine(Application.persistentDataPath, f);
                if (File.Exists(p)) File.Copy(p, Path.Combine(bk, f), true); else File.WriteAllText(Path.Combine(bk, f + ".absent"), "");
            }
            EditorPrefs.SetString(MovementQA219.RestoreKey, bk);
            EditorPrefs.SetInt("MoveQA219_Coins", PlayerPrefs.GetInt(CoinWallet.PrefsKey, -1));
            Log($"=== Village walk collision QA 219 · {System.DateTime.Now:yyyy-MM-dd HH:mm} ===");

            float t0 = Time.realtimeSinceStartup;
            while (VillageHub.I == null && Time.realtimeSinceStartup - t0 < 30f) yield return null;
            var hub = VillageHub.I; if (hub == null) { Log("FAIL 마을 없음"); yield return Finish(); yield break; }
            var cc = (CharacterController)hub.GetType().GetField("_cc", BF).GetValue(hub);
            var player = (Transform)hub.GetType().GetField("_player", BF).GetValue(hub);
            var hud = hub.GetType().GetField("_hud", BF)?.GetValue(hub); var busyF = hub.GetType().GetField("_busy", BF);
            var lockF = hud?.GetType().GetField("Locked", BF);
            System.Action unlock = () => { if (lockF != null) lockF.SetValue(hud, false); if (busyF != null) busyF.SetValue(hub, false); };
            Log($"시작 상태: 팝업 잠금 {(lockF != null ? lockF.GetValue(hud) : "?")}, busy {(busyF != null ? busyF.GetValue(hub) : "?")} — 시험 중엔 풀어 둔다");
            yield return new WaitForSeconds(2f);
            Vector3 home = player.position;

            // 후보 모으기
            var byCat = new Dictionary<string, List<Collider>>();
            foreach (var c in FindObjectsByType<Collider>(FindObjectsSortMode.None))
            {
                if (c == null || !c.enabled || c.isTrigger || c is TerrainCollider || c == cc) continue;
                if (c.transform.IsChildOf(player) || c.GetComponentInParent<VillageCreatures>() != null) continue;
                var b = c.bounds; if (b.size.x > 30f || b.size.z > 30f || b.size.y < 0.35f) continue;
                float gy = VillageWorld.Height(b.center.x, b.center.z);
                if (b.min.y > gy + 0.6f || b.max.y < gy + 0.35f) continue;   // 몸 높이에 안 닿는 것(머리 위·땅속)
                string cat = Cat(c);
                if (!byCat.TryGetValue(cat, out var l)) byCat[cat] = l = new List<Collider>();
                l.Add(c);
            }
            var picks = new List<Collider>();
            var rng = new System.Random(219);
            foreach (var kv in byCat)
            {
                var l = kv.Value; int n = Mathf.Min(l.Count, 3);
                for (int i = 0; i < n; i++) picks.Add(l[rng.Next(l.Count)]);
            }
            if (picks.Count > 60) picks = picks.GetRange(0, 60);
            Log($"막힌 물체 종류 {byCat.Count}개, 시험 {picks.Count}개");

            int ok = 0, slid = 0, pass = 0, deep = 0, skip = 0; var bad = new List<string>();
            foreach (var c in picks)
            {
                if (c == null) continue;
                var b = c.bounds; var ctr = b.center; ctr.y = VillageWorld.Height(ctr.x, ctr.z);
                float ext = Mathf.Max(b.extents.x, b.extents.z);
                // 비어 있는 출발점 찾기(4방향)
                Vector3 dir = Vector3.zero, start = Vector3.zero; bool found = false;
                for (int k = 0; k < 8 && !found; k++)
                {
                    float a = (k * 45f + rng.Next(20)) * Mathf.Deg2Rad; var d = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                    var s = ctr + d * (ext + 2.2f); s.y = VillageWorld.Height(s.x, s.z);
                    if (Mathf.Abs(s.y - ctr.y) > 1.2f) continue;
                    if (Physics.CheckCapsule(s + Vector3.up * 0.45f, s + Vector3.up * 1.2f, 0.32f, ~0, QueryTriggerInteraction.Ignore)) continue;
                    dir = d; start = s; found = true;
                }
                if (!found) { skip++; continue; }
                unlock(); hub.Teleport(start, Quaternion.LookRotation(-dir).eulerAngles.y);
                yield return new WaitForSeconds(0.25f);
                var target = ctr - dir * (ext + 2.5f);
                float maxDepth = 0f, tt = 0f;
                while (tt < 3.0f)
                {
                    unlock(); hub.WalkTo(target);
                    tt += Time.deltaTime;
                    if (Physics.ComputePenetration(cc, cc.transform.position, cc.transform.rotation, c, c.transform.position, c.transform.rotation, out _, out float depth))
                        maxDepth = Mathf.Max(maxDepth, depth);
                    yield return null;
                }
                var p = player.position; float moved = Vector3.Distance(new Vector3(p.x, 0f, p.z), new Vector3(start.x, 0f, start.z)); float along = Vector3.Dot(p - ctr, -dir); float side = Mathf.Abs(Vector3.Dot(p - ctr, Vector3.Cross(Vector3.up, dir)));
                bool farSide = along > ext * 0.6f;
                string res;
                if (moved < 0.3f && maxDepth <= 0.01f && Vector3.Distance(start, ctr) > ext + 1.0f) { skip++; Log($"{Cat(c)} → 안 움직임(시험 무효) 이동 {moved:0.00} m"); continue; }
                if (maxDepth > 0.25f && farSide) { pass++; res = "뚫고 지나감"; }
                else if (maxDepth > 0.25f) { deep++; res = "깊이 파고듦"; }
                else if (farSide) { slid++; res = "옆으로 돌아감(정상)"; }
                else { ok++; res = "막힘(정상)"; }
                string line = $"{Cat(c)} [{c.GetType().Name}] 크기 {b.size.x:0.0}×{b.size.z:0.0}×{b.size.y:0.0} 침투 {maxDepth:0.00} m, 반대편 {along:0.0}/{ext:0.0} 옆 {side:0.0} → {res}  @{ctr.x:0},{ctr.z:0}";
                if (res.Contains("뚫고") || res.Contains("파고")) bad.Add(line);
                Log(line);
            }
            hub.WalkTo(home); hub.Teleport(home);
            Log($"=== 요약: 막힘 {ok} · 옆으로 돌아감 {slid} · **뚫고 지나감 {pass}** · 깊이 파고듦 {deep} · 출발점 없음 {skip} ===");
            foreach (var l in bad) Log("  문제: " + l);
            yield return Finish();
        }

        static string Cat(Collider c)
        {
            string n = c.name;
            if (n == "Col" || n.StartsWith("Collider") || n == "Box" || n.Length <= 2) n = c.transform.parent != null ? c.transform.parent.name : n;
            n = System.Text.RegularExpressions.Regex.Replace(n, @"[\s_]*\(?\d+\)?$", "");
            return n;
        }

        IEnumerator Finish()
        {
            Directory.CreateDirectory("Builds/qa");
            File.WriteAllText("Builds/qa/village_walk_qa_219.txt", _r.ToString());
            Log("결과 저장: Builds/qa/village_walk_qa_219.txt — 플레이 종료 후 세이브·코인 자동 복원");
            yield return null;
            EditorApplication.isPlaying = false;
        }
    }
}
#endif
